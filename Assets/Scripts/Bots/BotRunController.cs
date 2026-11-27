using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Com.GCTC.ZombCube
{
    /// <summary>
    /// Runs an automated bot test (see <see cref="BotSettings"/>): loads the map in Solo, Co-op or Party,
    /// puts a <see cref="PlayerBot"/> on every local player, continues after the boss, and records the result.
    ///
    /// Pass: the boss is defeated and wave <see cref="BotSettings.FinalWave"/> is cleared.
    /// Fail: game over (every bot eliminated), the game stalls, or the time limit runs out.
    /// The result is logged as "[ZCBOT] RESULT {json}" and written to a JSON file.
    /// Start it with the -zcbot command line (builds), the ZombCube > Bots menu (Editor) or <see cref="StartRun"/>.
    /// </summary>
    public class BotRunController : MonoBehaviourPunCallbacks
    {
        public static BotRunController Instance { get; private set; }
        public static bool Finished { get; private set; }
        public static bool Passed { get; private set; }
        public static string ResultJson { get; private set; }

        private float startedAt;
        private bool bossDefeated;
        private int highestWave;
        private int lastWave = -1;
        private float lastProgressAt;
        private int lastEnemyCount = -1;
        private string failReason;
        private bool loadingStarted;
        private readonly Dictionary<GameObject, float> pointsDueAt = new Dictionary<GameObject, float>();
        private readonly HashSet<GameObject> pointsGiven = new HashSet<GameObject>();
        private static readonly Dictionary<string, int> purchases = new Dictionary<string, int>();
        private int botsEliminated;
        private readonly HashSet<GameObject> countedEliminated = new HashSet<GameObject>();

        // ------------------------------------------------------------------ starting

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartFromCommandLineOrEditor()
        {
            if (BotSettings.TryParse(Environment.GetCommandLineArgs()))
            {
                Begin();
                return;
            }
#if UNITY_EDITOR
            string pending = UnityEditor.SessionState.GetString(EditorPendingKey, "");
            if (!string.IsNullOrEmpty(pending))
            {
                UnityEditor.SessionState.EraseString(EditorPendingKey);
                StartRun(pending);
            }
#endif
        }

        /// <summary>Key the ZombCube > Bots menu uses to hand a run to play mode.</summary>
        public const string EditorPendingKey = "ZombCube.PendingBotRun";

        /// <summary>Starts a run from arguments like "coop -zcbotMap theatre -zcbotPlayers 2". Used by the bot tests.</summary>
        public static bool StartRun(string arguments)
        {
            if (!BotSettings.TryParse(arguments)) return false;
            Begin();
            return true;
        }

        private static void Begin()
        {
            if (Instance != null) Destroy(Instance.gameObject);
            Finished = false;
            Passed = false;
            ResultJson = null;
            purchases.Clear();
            var go = new GameObject("BotRunController");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<BotRunController>();
        }

        private void Start()
        {
            startedAt = Time.realtimeSinceStartup;
            lastProgressAt = startedAt;
            Log($"Starting {BotSettings.Mode} on {BotSettings.MapId} ({(BotSettings.Mode == BotSettings.RunMode.Solo ? 1 : BotSettings.Players)} players) from wave {BotSettings.StartWave}");

            if (global::Com.GCTC.ZombCube.Player.Instance == null)
                new GameObject("BotPlayerData").AddComponent<Player>();
            global::Com.GCTC.ZombCube.Player.Instance.playerName = "Bot";

            int map = MapIndex(BotSettings.MapId);
            LevelSelect.mapSelected = map;
            LevelSelect.levelSelected = 0;

            BossCube.bossDead += OnBossDead;
            NetworkBossCube.bossDead += OnBossDead;

            switch (BotSettings.Mode)
            {
                case BotSettings.RunMode.Solo:
                    GameManager.mode = 0;
                    LoadSoloScene();
                    break;
                case BotSettings.RunMode.Coop:
                    GameManager.mode = 1;
                    if (!SetUpCoopPlayers()) { Finish(false, "co-op setup failed (BotRunConfig missing)"); return; }
                    LoadSoloScene();
                    break;
                case BotSettings.RunMode.Party:
                    StartParty();
                    break;
            }
        }

        private void OnDestroy()
        {
            BossCube.bossDead -= OnBossDead;
            NetworkBossCube.bossDead -= OnBossDead;
            if (Instance == this) Instance = null;
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

        // The co-op lobby normally creates these: a CoopManager with one PlayerInput object per player.
        private static bool SetUpCoopPlayers()
        {
            BotRunConfig config = Resources.Load<BotRunConfig>(BotRunConfig.ResourcePath);
            if (config == null || config.coopPlayerPrefab == null || config.playerInputPrefab == null) return false;

            GameObject existing = GameObject.Find("CoopManager");
            if (existing != null) Destroy(existing);

            var go = new GameObject("CoopManager");
            go.SetActive(false);
            var inputManager = go.AddComponent<PlayerInputManager>();
            inputManager.joinBehavior = PlayerJoinBehavior.JoinPlayersManually;
            var coop = go.AddComponent<CouchCoopManager>();
            coop.playerPrefab = config.coopPlayerPrefab;
            coop.minimaps = new RenderTexture[4];
            for (int i = 0; i < 4; i++) coop.minimaps[i] = new RenderTexture(256, 256, 16);
            for (int i = 0; i < BotSettings.Players; i++)
            {
                GameObject input = Instantiate(config.playerInputPrefab);
                input.name = $"BotInput{i + 1}";
                DontDestroyOnLoad(input);
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
            if (BotSettings.PartyOffline)
            {
                PhotonNetwork.OfflineMode = true;   // calls OnConnectedToMaster
            }
            else if (!PhotonNetwork.IsConnected)
            {
                PhotonNetwork.ConnectUsingSettings();
            }
            else OnConnectedToMaster();
        }

        public override void OnConnectedToMaster()
        {
            if (BotSettings.Mode != BotSettings.RunMode.Party || Finished) return;
            if (BotSettings.PartyOffline) PhotonNetwork.CreateRoom("zcbot-offline");
            else PhotonNetwork.JoinOrCreateRoom(BotSettings.Room, new RoomOptions { MaxPlayers = 4 }, TypedLobby.Default);
        }

        public override void OnJoinedRoom()
        {
            Log($"Joined room {PhotonNetwork.CurrentRoom.Name} ({PhotonNetwork.CurrentRoom.PlayerCount} player(s))");
            TryStartPartyGame();
        }

        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            Log($"Player joined: {PhotonNetwork.CurrentRoom.PlayerCount} in room");
            TryStartPartyGame();
        }

        public override void OnJoinRoomFailed(short returnCode, string message) => Finish(false, "couldn't join Photon room: " + message);
        public override void OnCreateRoomFailed(short returnCode, string message) => Finish(false, "couldn't create Photon room: " + message);

        public override void OnDisconnected(DisconnectCause cause)
        {
            if (!Finished && BotSettings.Mode == BotSettings.RunMode.Party && !BotSettings.PartyOffline)
                Finish(false, "disconnected from Photon: " + cause);
        }

        // The first instance in the room starts once everyone is there (or straight away offline).
        private void TryStartPartyGame()
        {
            if (loadingStarted || !PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null) return;
            int needed = BotSettings.PartyOffline ? 1 : BotSettings.Players;
            if (PhotonNetwork.CurrentRoom.PlayerCount < needed) return;

            loadingStarted = true;
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
            {
                { RoomLevelKeys.Map, LevelSelect.mapSelected },
                { RoomLevelKeys.TexturePack, LevelSelect.levelSelected },
            });
            PhotonNetwork.LoadLevel(GameScenes.SelectedPartyScene());
        }

        // ------------------------------------------------------------------ running

        private void Update()
        {
            if (Finished) return;
            float now = Time.realtimeSinceStartup;

            if (now - startedAt > BotSettings.TimeoutMinutes * 60f)
            {
                Finish(false, $"timed out after {BotSettings.TimeoutMinutes:0} minutes");
                return;
            }

            bool inGame = GameScenes.IsSoloActive || GameScenes.IsPartyActive;
            if (!inGame)
            {
                if (BotSettings.Mode == BotSettings.RunMode.Party && !loadingStarted && now - startedAt > 300f)
                    Finish(false, $"Party: only {(PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.PlayerCount : 0)} of {BotSettings.Players} instances joined in 5 minutes");
                return;
            }

            // Play faster if asked (the boss "continue" screen pauses with timeScale 0).
            if (Time.timeScale > 0f && !Mathf.Approximately(Time.timeScale, BotSettings.Speed)) Time.timeScale = BotSettings.Speed;

            AttachBots(now);
            HandleContinueScreen();
            CheckProgress(now);
        }

        private void AttachBots(float now)
        {
            foreach (GameObject p in GameObject.FindGameObjectsWithTag("Player"))
            {
                GameObject root = PlayerRoot(p);
                if (root == null || !IsLocal(root)) continue;

                if (root.GetComponent<PlayerBot>() == null && PlayerBot.Attach(root) != null)
                {
                    Log($"Bot attached to {root.name}");
                    pointsDueAt[root] = now + 1f;   // after the player's own Start resets its points
                }

                if (!pointsGiven.Contains(root) && pointsDueAt.TryGetValue(root, out float due) && now >= due)
                {
                    pointsGiven.Add(root);
                    PlayerBot bot = root.GetComponent<PlayerBot>();
                    // Points are shared by all co-op players on one machine, so co-op gets 10,000 per bot.
                    int start = BotSettings.Mode == BotSettings.RunMode.Coop ? BotSettings.StartPoints * BotSettings.Players : BotSettings.StartPoints;
                    if (bot != null && bot.Player.Points < start) bot.Player.Points = start;
                }
            }

            foreach (PlayerBot bot in PlayerBot.All)
                if (bot.Player != null && !bot.Player.Alive && countedEliminated.Add(bot.gameObject)) botsEliminated++;
        }

        private static GameObject PlayerRoot(GameObject tagged)
        {
            Transform t = tagged.transform;
            while (t != null)
            {
                if (t.GetComponent<PlayerManager>() != null || t.GetComponent("NetworkPlayerManager") != null) return t.gameObject;
                t = t.parent;
            }
            return null;
        }

        private static bool IsLocal(GameObject root)
        {
            PhotonView view = root.GetComponent<PhotonView>();
            return view == null || root.GetComponent<PlayerManager>() != null || view.IsMine;
        }

        private void HandleContinueScreen()
        {
            if (GameManager.Instance != null && GameManager.Instance.continueScreen != null && GameManager.Instance.continueScreen.activeInHierarchy)
            {
                Log("Boss defeated: continuing");
                GameManager.Instance.Resume();
            }
            else if (NetworkGameManager.Instance != null && NetworkGameManager.Instance.continueScreen != null
                     && NetworkGameManager.Instance.continueScreen.activeInHierarchy && PhotonNetwork.IsMasterClient)
            {
                Log("Boss defeated: continuing");
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

            int enemies = GameObject.FindGameObjectsWithTag("Enemy").Length + (GameObject.FindWithTag("BossCube") != null ? 1 : 0);
            if (enemies != lastEnemyCount) { lastEnemyCount = enemies; lastProgressAt = now; }

            // Cleared wave 51 after the boss: the game has moved on to wave 52.
            if (bossDefeated && wave > BotSettings.FinalWave)
            {
                Finish(true, $"boss defeated and wave {BotSettings.FinalWave} cleared");
                return;
            }
            if (gameOver)
            {
                Finish(false, $"game over on wave {wave}{(bossDefeated ? " (boss was defeated)" : "")}");
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
            Log($"{bot.name} bought {item}");
        }

        // ------------------------------------------------------------------ results

        private void Finish(bool passed, string reason)
        {
            if (Finished) return;
            Finished = true;
            Passed = passed;
            failReason = reason;
            Time.timeScale = 1f;

            var sb = new StringBuilder();
            sb.Append('{');
            sb.Append($"\"outcome\":\"{(passed ? "pass" : "fail")}\",");
            sb.Append($"\"reason\":\"{Escape(reason)}\",");
            sb.Append($"\"mode\":\"{BotSettings.Mode.ToString().ToLowerInvariant()}\",");
            sb.Append($"\"map\":\"{Escape(BotSettings.MapId)}\",");
            sb.Append($"\"players\":{(BotSettings.Mode == BotSettings.RunMode.Solo ? 1 : BotSettings.Players)},");
            sb.Append($"\"partyOffline\":{(BotSettings.PartyOffline ? "true" : "false")},");
            sb.Append($"\"startWave\":{BotSettings.StartWave},");
            sb.Append($"\"highestWave\":{highestWave},");
            sb.Append($"\"bossDefeated\":{(bossDefeated ? "true" : "false")},");
            sb.Append($"\"botsEliminated\":{botsEliminated},");
            sb.Append($"\"speed\":{BotSettings.Speed.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
            sb.Append($"\"realSeconds\":{Mathf.RoundToInt(Time.realtimeSinceStartup - startedAt)},");
            sb.Append("\"purchases\":{");
            bool first = true;
            foreach (var kv in purchases)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append($"\"{Escape(kv.Key)}\":{kv.Value}");
            }
            sb.Append("}}");
            ResultJson = sb.ToString();
            Log("RESULT " + ResultJson);

            try
            {
                string path = BotSettings.ResultPath;
                if (string.IsNullOrEmpty(path))
                {
                    string dir = Path.Combine(Application.persistentDataPath, "zcbot-results");
                    Directory.CreateDirectory(dir);
                    path = Path.Combine(dir, $"{BotSettings.Mode}-{BotSettings.MapId}-{DateTime.Now:yyyyMMdd-HHmmss}.json".ToLowerInvariant());
                }
                File.WriteAllText(path, ResultJson);
                Log("Result written to " + path);
            }
            catch (Exception e)
            {
                Log("Couldn't write the result file: " + e.Message);
            }

            foreach (PlayerBot bot in PlayerBot.All.ToArray()) if (bot != null) bot.enabled = false;

            if (BotSettings.QuitWhenDone)
            {
#if UNITY_EDITOR
                if (Application.isBatchMode) UnityEditor.EditorApplication.Exit(passed ? 0 : 1);
                else UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit(passed ? 0 : 1);
#endif
            }
        }

        private static string Escape(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");

        private static void Log(string message) => Debug.Log("[ZCBOT] " + message);

        // Small status box so a person watching knows what the bots are doing.
        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 14 };
            var sb = new StringBuilder();
            sb.AppendLine($"BOT RUN  {BotSettings.Mode} - {BotSettings.MapId}  x{BotSettings.Speed:0.#}");
            if (Finished) sb.AppendLine($"{(Passed ? "PASSED" : "FAILED")}: {failReason}");
            else sb.AppendLine($"Wave {Mathf.Max(lastWave, 0)}   Enemies {Mathf.Max(lastEnemyCount, 0)}   Boss {(bossDefeated ? "defeated" : "alive")}");
            foreach (PlayerBot bot in PlayerBot.All)
                if (bot.Player != null)
                    sb.AppendLine($"{bot.name}: {Mathf.RoundToInt(bot.Player.Health)} HP, {bot.Player.Points} pts, {bot.Activity}");
            GUI.Box(new Rect(10, 10, 430, 26 + 20 * (2 + PlayerBot.All.Count)), sb.ToString(), style);
        }
    }
}
