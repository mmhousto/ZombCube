using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Photon.Pun;
using Photon.Realtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Com.GCTC.ZombCube.EditorTools.Bots
{
    /// <summary>
    /// One bot run in play mode (Editor only; nothing is added to the game's scripts or scenes):
    /// loads the map in Solo, Co-op or Party, gives every local player a <see cref="VirtualGamepad"/> and a
    /// <see cref="PlayerBot"/>, starts at wave 49 with 10,000 points and a 15 s buy window, presses Continue
    /// after the boss, and records the result.
    ///
    /// Pass: the boss is defeated and wave 51 is cleared. Fail: game over, a stall or the time limit.
    /// The result is logged as "[ZCBOT] RESULT {json}" and written to a JSON file.
    /// Runs every frame from InputSystem.onBeforeUpdate (so the pads' state is read that frame) and gets
    /// Photon callbacks as a callback target, so it needs no MonoBehaviour.
    /// </summary>
    public class BotRun : IConnectionCallbacks, IMatchmakingCallbacks, IInRoomCallbacks
    {
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static BotRun Current { get; private set; }
        public static bool Finished { get; private set; }
        public static bool Passed { get; private set; }
        public static string ResultJson { get; private set; }

        private readonly float startedAt;
        private bool bossDefeated, loadingStarted, waveSet;
        private float sceneLoadedAt = -1f;
        private int highestWave, lastWave = -1, lastEnemyCount = -1, botsEliminated, padCount;
        private float lastProgressAt;
        private readonly Dictionary<GameObject, PlayerBot> bots = new Dictionary<GameObject, PlayerBot>();
        private readonly Dictionary<GameObject, float> pointsDueAt = new Dictionary<GameObject, float>();
        private readonly HashSet<GameObject> eliminated = new HashSet<GameObject>();
        private static readonly Dictionary<string, int> purchases = new Dictionary<string, int>();
        private InputSettings.BackgroundBehavior savedBackground;
        private InputSettings.EditorInputBehaviorInPlayMode savedEditorInput;

        // ------------------------------------------------------------------ start / stop

        /// <summary>Starts a run in play mode, e.g. "coop -zcbotMap theatre -zcbotPlayers 2". Used by the scenarios and tests.</summary>
        public static bool Start(string arguments)
        {
            if (!EditorApplication.isPlaying || !BotSettings.TryParse(arguments)) return false;
            Current?.Stop();
            Finished = false;
            Passed = false;
            ResultJson = null;
            purchases.Clear();
            Current = new BotRun();
            return true;
        }

        private BotRun()
        {
            startedAt = Time.realtimeSinceStartup;
            lastProgressAt = startedAt;
            Log($"Starting {BotSettings.Mode} on {BotSettings.MapId} ({(BotSettings.Mode == BotSettings.RunMode.Solo ? 1 : BotSettings.Players)} players) from wave {BotSettings.StartWave}");

            // Bots must never write the player's save; nothing reports scores without a sign-in.
            SaveSystem.WritesDisabled = true;
            // Virtual players and batch mode aren't focused: let the pads' input through anyway.
            savedBackground = InputSystem.settings.backgroundBehavior;
            savedEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;

            if (Player.Instance == null) new GameObject("BotPlayerData").AddComponent<Player>();
            Player.Instance.playerName = "Bot";
            LevelSelect.mapSelected = MapIndex(BotSettings.MapId);
            LevelSelect.levelSelected = 0;

            BossCube.bossDead += OnBossDead;
            NetworkBossCube.bossDead += OnBossDead;
            InputSystem.onBeforeUpdate += BeforeInputUpdate;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;

            switch (BotSettings.Mode)
            {
                case BotSettings.RunMode.Solo:
                    GameManager.mode = 0;
                    LoadSoloScene();
                    break;
                case BotSettings.RunMode.Coop:
                    GameManager.mode = 1;
                    if (!SetUpCoopPlayers()) { Finish(false, "co-op setup failed (prefabs not found)"); return; }
                    LoadSoloScene();
                    break;
                case BotSettings.RunMode.Party:
                    PhotonNetwork.AddCallbackTarget(this);
                    StartParty();
                    break;
            }
        }

        public void Stop()
        {
            BossCube.bossDead -= OnBossDead;
            NetworkBossCube.bossDead -= OnBossDead;
            InputSystem.onBeforeUpdate -= BeforeInputUpdate;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            PhotonNetwork.RemoveCallbackTarget(this);
            foreach (PlayerBot bot in bots.Values) { bot.Dispose(); bot.Pad.Remove(); }
            bots.Clear();
            InputSystem.settings.backgroundBehavior = savedBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = savedEditorInput;
            SaveSystem.WritesDisabled = false;
            BotSettings.Reset();
            if (Current == this) Current = null;
        }

        private void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode) Stop();
        }

        private static int MapIndex(string id)
        {
            LevelDatabase db = LevelDatabase.Instance;
            if (db == null) return 0;
            for (int i = 0; i < db.Count; i++)
                if (string.Equals(db.Get(i).id, id, StringComparison.OrdinalIgnoreCase)) return i;
            Log($"Unknown map '{id}', using {db.Get(0).id}");
            return 0;
        }

        private void LoadSoloScene()
        {
            loadingStarted = true;
            SceneManager.LoadScene(GameScenes.SelectedSoloScene());
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (GameScenes.IsSolo(scene.name) || GameScenes.IsParty(scene.name))
            {
                sceneLoadedAt = Time.realtimeSinceStartup;
                waveSet = false;
            }
        }

        // The co-op lobby normally creates these: a CoopManager with one PlayerInput object per player.
        private static bool SetUpCoopPlayers()
        {
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/CoopPlayer.prefab");
            var inputPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Managers/InputManager.prefab");
            if (playerPrefab == null || inputPrefab == null) return false;

            GameObject existing = GameObject.Find("CoopManager");
            if (existing != null) UnityEngine.Object.Destroy(existing);

            var go = new GameObject("CoopManager");
            go.SetActive(false);
            go.AddComponent<PlayerInputManager>().joinBehavior = PlayerJoinBehavior.JoinPlayersManually;
            var coop = go.AddComponent<CouchCoopManager>();
            coop.playerPrefab = playerPrefab;
            coop.minimaps = new RenderTexture[4];
            for (int i = 0; i < 4; i++) coop.minimaps[i] = new RenderTexture(256, 256, 16);
            for (int i = 0; i < BotSettings.Players; i++)
            {
                GameObject input = UnityEngine.Object.Instantiate(inputPrefab);
                input.name = $"Bot {i + 1}";
                UnityEngine.Object.DontDestroyOnLoad(input);
                coop.joinedPlayers.Add(input);
                coop.joinedPlayerIDs.Add(-1000 - i);
            }
            go.SetActive(true);
            return true;
        }

        // ------------------------------------------------------------------ Party (Photon)

        private void StartParty()
        {
            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.NickName = "Bot";
            PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { "UserName", "Bot" }, { "IsReady", true }, { "IsRestricted", false } });
            if (BotSettings.PartyOffline) PhotonNetwork.OfflineMode = true;   // calls OnConnectedToMaster
            else if (!PhotonNetwork.IsConnected) PhotonNetwork.ConnectUsingSettings();
            else OnConnectedToMaster();
        }

        public void OnConnectedToMaster()
        {
            if (Finished || loadingStarted) return;
            if (BotSettings.PartyOffline) PhotonNetwork.CreateRoom("zcbot-offline");
            else PhotonNetwork.JoinOrCreateRoom(BotSettings.Room, new RoomOptions { MaxPlayers = 4 }, TypedLobby.Default);
        }

        public void OnJoinedRoom()
        {
            Log($"Joined room {PhotonNetwork.CurrentRoom.Name} ({PhotonNetwork.CurrentRoom.PlayerCount} player(s))");
            TryStartPartyGame();
        }

        public void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            Log($"Player joined: {PhotonNetwork.CurrentRoom.PlayerCount} in room");
            TryStartPartyGame();
        }

        public void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient) => TryStartPartyGame();
        public void OnJoinRoomFailed(short returnCode, string message) => Finish(false, "couldn't join the Photon room: " + message);
        public void OnCreateRoomFailed(short returnCode, string message) => Finish(false, "couldn't create the Photon room: " + message);

        public void OnDisconnected(DisconnectCause cause)
        {
            if (!Finished && !BotSettings.PartyOffline) Finish(false, "disconnected from Photon: " + cause);
        }

        // The first instance in the room starts once all scenario players are in (or straight away offline).
        private void TryStartPartyGame()
        {
            if (loadingStarted || !PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null) return;
            if (PhotonNetwork.CurrentRoom.PlayerCount < (BotSettings.PartyOffline ? 1 : BotSettings.Players)) return;

            loadingStarted = true;
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
            {
                { RoomLevelKeys.Map, LevelSelect.mapSelected },
                { RoomLevelKeys.TexturePack, LevelSelect.levelSelected },
            });
            PhotonNetwork.LoadLevel(GameScenes.SelectedPartyScene());
        }

        public void OnConnected() { }
        public void OnRegionListReceived(RegionHandler regionHandler) { }
        public void OnCustomAuthenticationResponse(Dictionary<string, object> data) { }
        public void OnCustomAuthenticationFailed(string debugMessage) => Finish(false, "Photon authentication failed: " + debugMessage);
        public void OnFriendListUpdate(List<FriendInfo> friendList) { }
        public void OnCreatedRoom() { }
        public void OnJoinRandomFailed(short returnCode, string message) { }
        public void OnLeftRoom() { }
        public void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer) { }
        public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged) { }
        public void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, Hashtable changedProps) { }

        // ------------------------------------------------------------------ every frame

        private void BeforeInputUpdate()
        {
            // Once per game frame (the Input System also updates for the Editor UI).
            if (InputState.currentUpdateType != InputUpdateType.Dynamic && InputState.currentUpdateType != InputUpdateType.Fixed) return;
            if (InputState.currentUpdateType == InputUpdateType.Fixed && InputSystem.settings.updateMode != InputSettings.UpdateMode.ProcessEventsInFixedUpdate) return;
            if (Finished) return;
            try { Tick(); }
            catch (Exception e) { Debug.LogException(e); }
        }

        private void Tick()
        {
            float now = Time.realtimeSinceStartup;
            if (now - startedAt > BotSettings.TimeoutMinutes * 60f)
            {
                Finish(false, $"timed out after {BotSettings.TimeoutMinutes:0} minutes");
                return;
            }

            if (!(GameScenes.IsSoloActive || GameScenes.IsPartyActive))
            {
                if (BotSettings.Mode == BotSettings.RunMode.Party && !loadingStarted && now - startedAt > 300f)
                    Finish(false, $"Party: only {(PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.PlayerCount : 0)} of {BotSettings.Players} players joined in 5 minutes");
                return;
            }

            if (Time.timeScale > 0f && !Mathf.Approximately(Time.timeScale, BotSettings.Speed)) Time.timeScale = BotSettings.Speed;

            SetStartingWave(now);
            AttachBots(now);
            foreach (PlayerBot bot in bots.Values) bot.Tick();
            HandleContinueScreen();
            CheckProgress(now);
        }

        // Wave 49 with its normal enemy count and a 15 s countdown, set on the game's own objects after they start.
        private void SetStartingWave(float now)
        {
            if (waveSet || sceneLoadedAt < 0f || now - sceneLoadedAt < 0.3f) return;

            if (GameManager.Instance != null)
            {
                Spawner spawner = UnityEngine.Object.FindFirstObjectByType<Spawner>();
                if (spawner == null) return;
                int players = BotSettings.Mode == BotSettings.RunMode.Coop ? BotSettings.Players : 1;
                GameManager.Instance.CurrentRound = BotSettings.StartWave;
                if (GameManager.Instance.waveTxt != null) GameManager.Instance.waveTxt.text = "Wave: " + BotSettings.StartWave;
                SetField(spawner, "cubesToSpawn", BotSettings.SoloCubesAtWave(BotSettings.StartWave, players));
                SetField(spawner, "timeTilNextWave", BotSettings.BuyWindowSeconds);
                waveSet = true;
            }
            else if (NetworkGameManager.Instance != null && NetworkSpawner.Instance != null)
            {
                int players = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.PlayerCount : 1;
                NetworkGameManager.Instance.CurrentRound = BotSettings.StartWave;
                SetField(NetworkSpawner.Instance, "cubesToSpawn", BotSettings.PartyCubesAtWave(BotSettings.StartWave, players));
                SetField(NetworkSpawner.Instance, "timeTilNextWave", BotSettings.BuyWindowSeconds);
                waveSet = true;
            }
            if (waveSet) Log($"Starting on wave {BotSettings.StartWave} with {BotSettings.BuyWindowSeconds} s to buy");
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo f = target.GetType().GetField(name, Any);
            if (f != null) f.SetValue(target, value);
            else Log($"Field {target.GetType().Name}.{name} not found");
        }

        private void AttachBots(float now)
        {
            foreach (GameObject tagged in GameObject.FindGameObjectsWithTag("Player"))
            {
                GameObject root = PlayerRoot(tagged);
                if (root == null || bots.ContainsKey(root) || !IsLocal(root)) continue;

                PlayerInput input = root.GetComponentInParent<PlayerInput>(true);
                if (input == null) continue;
                var pad = new VirtualGamepad($"ZCBot Pad {++padCount}");
                pad.PairWith(input);
                bots[root] = new PlayerBot(root, pad);
                pointsDueAt[root] = now + 1f;   // after the player's own Start resets the points
                Log($"Bot playing {root.name} with {pad.Device.name}");
            }

            foreach (var kv in new List<KeyValuePair<GameObject, PlayerBot>>(bots))
            {
                if (kv.Key == null) { kv.Value.Dispose(); kv.Value.Pad.Remove(); bots.Remove(kv.Key); continue; }
                if (pointsDueAt.TryGetValue(kv.Key, out float due) && now >= due)
                {
                    pointsDueAt.Remove(kv.Key);
                    GivePoints(kv.Value);
                }
                if (!kv.Value.Alive && eliminated.Add(kv.Key)) botsEliminated++;
            }
        }

        // 10,000 per bot. Co-op players on one machine share one points pool, so co-op gets 10,000 x bots.
        private static void GivePoints(PlayerBot bot)
        {
            int start = BotSettings.Mode == BotSettings.RunMode.Coop ? BotSettings.StartPoints * BotSettings.Players : BotSettings.StartPoints;
            if (bot.IsNetwork)
            {
                FieldInfo f = typeof(NetworkPlayerManager).GetField("currentPoints", BindingFlags.Public | BindingFlags.Static);
                if (f != null && (int)f.GetValue(null) < start) f.SetValue(null, start);
            }
            else if (PlayerManager.currentPoints < start) PlayerManager.currentPoints = start;
        }

        private static GameObject PlayerRoot(GameObject tagged)
        {
            for (Transform t = tagged.transform; t != null; t = t.parent)
                if (t.GetComponent<PlayerManager>() != null || t.GetComponent<NetworkPlayerManager>() != null) return t.gameObject;
            return null;
        }

        private static bool IsLocal(GameObject root)
        {
            if (root.GetComponent<PlayerManager>() != null) return true;
            PhotonView view = root.GetComponent<PhotonView>();
            return view == null || view.IsMine;
        }

        private void HandleContinueScreen()
        {
            if (GameManager.Instance != null && GameManager.Instance.continueScreen != null && GameManager.Instance.continueScreen.activeInHierarchy)
            {
                Log("Continue after the boss");
                GameManager.Instance.Resume();
            }
            else if (NetworkGameManager.Instance != null && NetworkGameManager.Instance.continueScreen != null
                     && NetworkGameManager.Instance.continueScreen.activeInHierarchy && PhotonNetwork.IsMasterClient)
            {
                Log("Continue after the boss");
                NetworkGameManager.Instance.CallResumeAfterGame();
            }
        }

        private void CheckProgress(float now)
        {
            int wave = GameManager.Instance != null ? GameManager.Instance.CurrentRound
                     : NetworkGameManager.Instance != null ? NetworkGameManager.Instance.CurrentRound : 0;
            bool gameOver = GameManager.Instance != null ? GameManager.Instance.isGameOver
                          : NetworkGameManager.Instance != null && NetworkGameManager.Instance.isGameOver;

            if (wave != lastWave)
            {
                if (lastWave >= 0) Log($"Wave {wave}");
                lastWave = wave;
                lastProgressAt = now;
            }
            if (waveSet) highestWave = Mathf.Max(highestWave, wave);

            int enemies = GameObject.FindGameObjectsWithTag("Enemy").Length + (GameObject.FindWithTag("BossCube") != null ? 1 : 0);
            if (enemies != lastEnemyCount) { lastEnemyCount = enemies; lastProgressAt = now; }

            if (bossDefeated && wave > BotSettings.FinalWave) { Finish(true, $"boss defeated and wave {BotSettings.FinalWave} cleared"); return; }
            if (gameOver) { Finish(false, $"game over on wave {wave}{(bossDefeated ? " (boss was defeated)" : "")}"); return; }
            if (now - lastProgressAt > 240f / Mathf.Max(1f, BotSettings.Speed) + 60f)
                Finish(false, $"stalled on wave {wave}: no wave or enemy change for 4+ minutes ({enemies} enemies left)");
        }

        private void OnBossDead()
        {
            if (bossDefeated) return;
            bossDefeated = true;
            Log("Boss defeated");
        }

        public static void RecordPurchase(PlayerBot bot, string item)
        {
            purchases.TryGetValue(item, out int n);
            purchases[item] = n + 1;
            Log($"{bot.Name} bought {item}");
        }

        /// <summary>One line per bot, for the status overlay.</summary>
        public string Status()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"BOT RUN  {BotSettings.Mode} - {BotSettings.MapId}  x{BotSettings.Speed:0.#}");
            sb.AppendLine($"Wave {Mathf.Max(lastWave, 0)}   Enemies {Mathf.Max(lastEnemyCount, 0)}   Boss {(bossDefeated ? "defeated" : "alive")}");
            foreach (PlayerBot bot in bots.Values)
                sb.AppendLine($"{bot.Name}: {Mathf.RoundToInt(bot.Health)} HP, {bot.Points} pts, {bot.Activity}");
            return sb.ToString();
        }

        // ------------------------------------------------------------------ results

        private void Finish(bool passed, string reason)
        {
            if (Finished) return;
            Finished = true;
            Passed = passed;
            Time.timeScale = 1f;
            foreach (PlayerBot bot in bots.Values) bot.Pad.Release();

            var sb = new StringBuilder("{");
            sb.Append($"\"outcome\":\"{(passed ? "pass" : "fail")}\",\"reason\":\"{Escape(reason)}\",");
            sb.Append($"\"mode\":\"{BotSettings.Mode.ToString().ToLowerInvariant()}\",\"map\":\"{Escape(BotSettings.MapId)}\",");
            sb.Append($"\"players\":{(BotSettings.Mode == BotSettings.RunMode.Solo ? 1 : BotSettings.Players)},");
            sb.Append($"\"partyOffline\":{(BotSettings.PartyOffline ? "true" : "false")},");
            sb.Append($"\"startWave\":{BotSettings.StartWave},\"highestWave\":{highestWave},");
            sb.Append($"\"bossDefeated\":{(bossDefeated ? "true" : "false")},\"botsEliminated\":{botsEliminated},");
            sb.Append($"\"speed\":{BotSettings.Speed.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
            sb.Append($"\"realSeconds\":{Mathf.RoundToInt(Time.realtimeSinceStartup - startedAt)},\"purchases\":{{");
            bool first = true;
            foreach (var kv in purchases)
            {
                sb.Append(first ? "" : ",").Append($"\"{Escape(kv.Key)}\":{kv.Value}");
                first = false;
            }
            sb.Append("}}");
            ResultJson = sb.ToString();
            Log("RESULT " + ResultJson);

            try
            {
                string path = BotSettings.ResultPath;
                if (string.IsNullOrEmpty(path))
                {
                    string dir = Path.Combine(Application.dataPath, "..", "bot-results");
                    Directory.CreateDirectory(dir);
                    path = Path.Combine(dir, $"{BotSettings.Mode}-{BotSettings.MapId}-{DateTime.Now:yyyyMMdd-HHmmss}.json".ToLowerInvariant());
                }
                File.WriteAllText(path, ResultJson);
                Log("Result written to " + Path.GetFullPath(path));
            }
            catch (Exception e)
            {
                Log("Couldn't write the result file: " + e.Message);
            }
        }

        private static string Escape(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
        private static void Log(string message) => Debug.Log("[ZCBOT] " + message);
    }
}
