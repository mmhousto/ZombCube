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
    /// starts at the SignIn screen and goes through the menus like a player (see BotRun.Menus.cs), then gives every local player a <see cref="VirtualGamepad"/> and a
    /// <see cref="PlayerBot"/>, plays a normal game from wave 1, spawns the boss when wave 5 starts, presses
    /// Continue after the boss, and records the result.
    ///
    /// Pass: the bots reach wave 5 and the boss spawns (defeating it isn't required). The run ends when the
    /// bots are eliminated, or the boss is defeated and wave 6 is cleared. Fail: eliminated before wave 5,
    /// a stall or the time limit.
    /// The result is logged as "[ZCBOT] RESULT {json}" and written to a JSON file.
    /// Runs every frame from InputSystem.onBeforeUpdate (so the pads' state is read that frame) and gets
    /// Photon callbacks as a callback target, so it needs no MonoBehaviour.
    /// </summary>
    public partial class BotRun : IConnectionCallbacks, IMatchmakingCallbacks, IInRoomCallbacks
    {
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static BotRun Current { get; private set; }
        public static bool Finished { get; private set; }
        public static bool Passed { get; private set; }
        public static string ResultJson { get; private set; }

        private readonly float startedAt;
        private bool bossDefeated;
        private float sceneLoadedAt = -1f;
        private int highestWave, lastWave = -1, lastEnemyCount = -1, botsEliminated, padCount;
        private float lastProgressAt, lastDiagnosticsAt;
        private readonly Dictionary<GameObject, PlayerBot> bots = new Dictionary<GameObject, PlayerBot>();
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

            BossCube.bossDead += OnBossDead;
            NetworkBossCube.bossDead += OnBossDead;
            InputSystem.onBeforeUpdate += BeforeInputUpdate;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;

            if (BotSettings.Mode == BotSettings.RunMode.Party) PhotonNetwork.AddCallbackTarget(this);
            StartMenus();
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
            RemoveMenuPads();
            CloudSaveLogin.AnonymousProfile = "default";
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

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "SignIn") sawSignIn = true;
            if (GameScenes.IsSolo(scene.name) || GameScenes.IsParty(scene.name))
            {
                sceneLoadedAt = Time.realtimeSinceStartup;
            }
        }

        // ------------------------------------------------------------------ Party (Photon callbacks)

        public void OnConnectedToMaster() { }

        public void OnJoinedRoom()
        {
            Log($"Joined room {PhotonNetwork.CurrentRoom.Name} ({PhotonNetwork.CurrentRoom.PlayerCount} player(s))");
        }

        public void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            Log($"Player joined: {PhotonNetwork.CurrentRoom.PlayerCount} in room");
        }

        public void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient) { }

        public void OnJoinRoomFailed(short returnCode, string message)
        {
            if (roomFailures++ > 20) { Finish(false, "couldn't join or host the Photon room: " + message); return; }
            RetryRoom(host: returnCode == ErrorCode.GameDoesNotExist);
        }

        public void OnCreateRoomFailed(short returnCode, string message)
        {
            if (roomFailures++ > 20) { Finish(false, "couldn't host the Photon room: " + message); return; }
            RetryRoom(host: false);
        }

        private int roomFailures;

        public void OnDisconnected(DisconnectCause cause)
        {
            if (!Finished && !BotSettings.PartyOffline) Finish(false, "disconnected from Photon: " + cause);
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
                TickMenus(now);
                return;
            }

            if (Time.timeScale > 0f && !Mathf.Approximately(Time.timeScale, BotSettings.Speed)) Time.timeScale = BotSettings.Speed;

            AttachBots(now);
            foreach (PlayerBot bot in bots.Values) bot.Tick();
            if (now - lastDiagnosticsAt > 5f)
            {
                lastDiagnosticsAt = now;
                foreach (PlayerBot bot in bots.Values) Log(bot.Describe());
            }
            HandleContinueScreen();
            CheckProgress(now);
        }

        // The game's own boss arrives on wave 50; for bot runs it spawns when wave 5 starts, the same way the
        // spawners do it (boss spawn point away from the players).
        private bool bossSpawned;

        private void SpawnBossAtBossWave(int wave)
        {
            if (bossSpawned || wave < BotSettings.BossWave) return;

            if (GameManager.Instance != null)
            {
                Spawner spawner = UnityEngine.Object.FindFirstObjectByType<Spawner>();
                if (spawner == null || spawner.enemies == null || spawner.enemies.Length < 5) return;
                Transform at = SpawnPointPicker.PickBoss(spawner.spawnPoints);
                if (at == null) return;
                UnityEngine.Object.Instantiate(spawner.enemies[4], at.position, at.rotation);
            }
            else if (NetworkGameManager.Instance != null && NetworkSpawner.Instance != null)
            {
                if (!PhotonNetwork.IsMasterClient) { bossSpawned = true; return; }   // the master spawns it for everyone
                Transform at = SpawnPointPicker.PickBoss(NetworkSpawner.Instance.spawnPoints);
                if (at == null) return;
                PhotonNetwork.InstantiateRoomObject("NetworkBossCube", at.position, at.rotation);
            }
            else return;

            bossSpawned = true;
            Log($"Boss spawned on wave {wave}");
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
                Log($"PlayerInput on {input.name}: scheme {input.currentControlScheme}, devices [{string.Join(", ", System.Linq.Enumerable.Select(input.devices, d => d.name))}], actions enabled {input.actions != null && input.actions.enabled}, behaviour {input.notificationBehavior}");
                Log($"Bot playing {root.name} with {pad.Device.name}");
            }

            foreach (var kv in new List<KeyValuePair<GameObject, PlayerBot>>(bots))
            {
                if (kv.Key == null) { kv.Value.Dispose(); kv.Value.Pad.Remove(); bots.Remove(kv.Key); continue; }
                if (!kv.Value.Alive && eliminated.Add(kv.Key)) botsEliminated++;
            }
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
            highestWave = Mathf.Max(highestWave, wave);
            SpawnBossAtBossWave(wave);

            int enemies = GameObject.FindGameObjectsWithTag("Enemy").Length + (GameObject.FindWithTag("BossCube") != null ? 1 : 0);
            if (enemies != lastEnemyCount) { lastEnemyCount = enemies; lastProgressAt = now; }

            if (bossDefeated && wave > BotSettings.FinalWave) { Finish(true, $"boss defeated and wave {BotSettings.FinalWave} cleared"); return; }
            if (gameOver)
            {
                // Reaching the boss is a pass; beating it isn't required.
                if (bossSpawned) Finish(true, $"reached the boss on wave {BotSettings.BossWave}; eliminated on wave {wave}{(bossDefeated ? " after defeating it" : "")}");
                else Finish(false, $"eliminated on wave {wave}, before the boss (wave {BotSettings.BossWave})");
                return;
            }
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
            DeleteBotAccount();

            var sb = new StringBuilder("{");
            sb.Append($"\"outcome\":\"{(passed ? "pass" : "fail")}\",\"reason\":\"{Escape(reason)}\",");
            sb.Append($"\"mode\":\"{BotSettings.Mode.ToString().ToLowerInvariant()}\",\"map\":\"{Escape(BotSettings.MapId)}\",");
            sb.Append($"\"players\":{(BotSettings.Mode == BotSettings.RunMode.Solo ? 1 : BotSettings.Players)},");
            sb.Append($"\"partyOffline\":{(BotSettings.PartyOffline ? "true" : "false")},");
            sb.Append($"\"chill\":{(BotSettings.Chill ? "true" : "false")},\"hardcore\":{(BotSettings.Hardcore ? "true" : "false")},");
            if (BotSettings.Hardcore && BotSettings.PartyOffline) sb.Append($"\"hardcoreStartBlockedSolo\":{(hardcoreStartBlocked ? "true" : "false")},");
            sb.Append($"\"name\":\"{Escape(botName)}\",\"blaster\":{blasterChosen},\"skin\":{skinChosen},\"menuStep\":\"{step}\",");
            sb.Append($"\"startWave\":{BotSettings.StartWave},\"highestWave\":{highestWave},");
            sb.Append($"\"bossSpawned\":{(bossSpawned ? "true" : "false")},\"bossDefeated\":{(bossDefeated ? "true" : "false")},\"botsEliminated\":{botsEliminated},");
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
        private static void Log(string message)
        {
            Debug.Log("[ZCBOT] " + message);
            // Also kept in bot-results/zcbot.log, so CI can show how far a run got even if the editor is stopped.
            try
            {
                string dir = Path.Combine(Application.dataPath, "..", "bot-results");
                Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "zcbot.log"), $"{DateTime.UtcNow:HH:mm:ss} {message}\n");
            }
            catch (Exception) { }
        }
    }
}
