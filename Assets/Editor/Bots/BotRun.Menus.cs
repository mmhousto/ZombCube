using System;
using System.Collections.Generic;
using System.Reflection;
using Photon.Pun;
using Photon.Realtime;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Com.GCTC.ZombCube.EditorTools.Bots
{
    /// <summary>
    /// The menu part of a bot run: the bots go through the game's own screens the way a player does, calling
    /// the same methods the buttons call.
    ///   SignIn: anonymous sign-in on a throwaway Authentication profile (never the player's "default" account).
    ///   Main Menu: a random name, a random owned blaster and skin, the run's map.
    ///   Solo: Solo button. Co-op: open the co-op lobby, the virtual pads press A to join, set Hardcore/Chill, Start.
    ///   Party: Multiplayer, Connect (or Photon offline mode), join the room or host it, Ready, Hardcore/Normal
    ///   (party leader), Start once everyone is in.
    /// </summary>
    public partial class BotRun
    {
        private enum MenuStep { SignIn, SigningIn, MainMenu, CoopLobby, PartyConnect, PartyLobby, PartyRoom, Loading }

        private MenuStep step = MenuStep.SignIn;
        private float stepStartedAt, nextActionAt;
        private string botName;
        private int blasterChosen = -1, skinChosen = -1;
        private bool hardcoreStartBlocked, roomJoinPending, readyClicked, startClicked;
        private readonly List<VirtualGamepad> menuPads = new List<VirtualGamepad>();
        private readonly List<BaseInputModule> pausedModules = new List<BaseInputModule>();
        private string authProfile;
        private bool sawSignIn;

        /// <summary>True while the throwaway anonymous account is being deleted after a run.</summary>
        public static bool CleaningUp { get; private set; }

        private void StartMenus()
        {
            authProfile = "zcbot_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            botName = "Bot" + UnityEngine.Random.Range(1000, 10000);
            GameManager.chillCoop = false;
            CleanUpEarlierRun();
            Step(MenuStep.SignIn);
            SceneManager.LoadScene(0);   // SignIn
        }

        // An earlier run in the same play session (the tests run back to back) can leave a Photon connection and
        // co-op players kept across scenes; clear them so this run starts like a fresh launch. Saving is already
        // off, so nothing the game does on the way out reaches the player's save.
        private static void CleanUpEarlierRun()
        {
            if (PhotonNetwork.IsConnected || PhotonNetwork.OfflineMode) PhotonNetwork.Disconnect();
            PhotonNetwork.OfflineMode = false;

            foreach (PlayerInput input in UnityEngine.Object.FindObjectsByType<PlayerInput>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (input.gameObject.scene.name == "DontDestroyOnLoad") UnityEngine.Object.Destroy(input.gameObject);
            foreach (CouchCoopManager coop in UnityEngine.Object.FindObjectsByType<CouchCoopManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (coop.gameObject.scene.name == "DontDestroyOnLoad") UnityEngine.Object.Destroy(coop.gameObject);
        }

        private void Step(MenuStep next)
        {
            step = next;
            stepStartedAt = Time.realtimeSinceStartup;
            nextActionAt = stepStartedAt + 1f;   // let the scene's Start methods run first
        }

        private void TickMenus(float now)
        {
            if (step == MenuStep.Loading || now < nextActionAt) return;
            string scene = SceneManager.GetActiveScene().name;

            switch (step)
            {
                case MenuStep.SignIn:
                    if (scene != "SignIn")
                    {
                        // With no internet the SignIn screen goes straight to the main menu (offline guest).
                        if (sawSignIn && scene == "MainMenu") { Log("SignIn: offline, signed in as a guest"); Step(MenuStep.SigningIn); }
                        // Something else (e.g. leaving an earlier run's Photon room) loaded another scene over it.
                        else if (!sawSignIn && now - stepStartedAt > 3f) { Step(MenuStep.SignIn); SceneManager.LoadScene(0); }
                        break;
                    }
                    var signIn = UnityEngine.Object.FindFirstObjectByType<SignInManager>();
                    if (signIn == null || CloudSaveLogin.Instance == null || CloudSaveLogin.Instance.isSigningIn) break;
                    CloudSaveLogin.AnonymousProfile = authProfile;
                    Log($"SignIn: signing in anonymously (profile {authProfile})");
                    signIn.SignInAnon();
                    Step(MenuStep.SigningIn);
                    break;

                case MenuStep.SigningIn:
                    if (scene == "MainMenu")
                    {
                        bool online = UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn;
                        Log(online ? $"Signed in as anonymous player {AuthenticationService.Instance.PlayerId}" : "Signed in offline (no Unity Services)");
                        Step(MenuStep.MainMenu);
                        nextActionAt += 1f;
                    }
                    else if (now - stepStartedAt > 90f) Finish(false, "sign-in never reached the main menu");
                    break;

                case MenuStep.MainMenu:
                    if (scene != "MainMenu") break;
                    MainMenuChoices();
                    break;

                case MenuStep.CoopLobby:
                    CoopLobby(now);
                    break;

                case MenuStep.PartyConnect:
                    if (scene != "LoadingScene") { if (now - stepStartedAt > 60f) Finish(false, "Multiplayer didn't open the connect screen"); break; }
                    PartyConnect(now);
                    break;

                case MenuStep.PartyLobby:
                    if (scene != "LobbyScene") { if (now - stepStartedAt > 60f) Finish(false, "couldn't reach the Photon lobby"); break; }
                    PartyLobby(now);
                    break;

                case MenuStep.PartyRoom:
                    if (scene != "RoomScene") { if (now - stepStartedAt > 120f) Finish(false, "couldn't join or host the room " + RoomName); break; }
                    PartyRoom(now);
                    break;
            }
        }

        // ------------------------------------------------------------------ main menu

        private void MainMenuChoices()
        {
            var main = UnityEngine.Object.FindFirstObjectByType<MainManager>(FindObjectsInactive.Include);
            if (main == null || Player.Instance == null) { Finish(false, "main menu not found"); return; }

            // Name: typed into the name field (as a player would), which also calls SetPlayerName.
            if (main.playerNameText != null) main.playerNameText.text = botName;
            main.SetPlayerName(botName);

            // Cosmetics: a random owned blaster and skin, picked through the inventory.
            var inventory = UnityEngine.Object.FindFirstObjectByType<InventoryManager>(FindObjectsInactive.Include);
            if (inventory != null)
            {
                if (!inventory.gameObject.activeInHierarchy) ActivateWithParents(inventory.transform);
                Player.Instance.EnsureCosmeticArrays();
                blasterChosen = RandomOwned(Player.Instance.ownedBlasters);
                skinChosen = RandomOwned(Player.Instance.ownedSkins);
                inventory.SelectBlaster(blasterChosen);
                inventory.SelectSkin(skinChosen);
            }
            Log($"Main Menu: name {Player.Instance.playerName}, blaster {Player.Instance.currentBlaster}, skin {Player.Instance.currentSkin}");

            var levels = UnityEngine.Object.FindFirstObjectByType<LevelSelect>(FindObjectsInactive.Include);
            if (levels != null) levels.SelectLevel(LevelOption(BotSettings.MapId));
            else LevelSelect.mapSelected = MapIndex(BotSettings.MapId);
            Log($"Map {LevelDatabase.Instance?.Get(LevelSelect.mapSelected).id} (pack {LevelSelect.levelSelected})");

            switch (BotSettings.Mode)
            {
                case BotSettings.RunMode.Solo:
                    Log("Solo");
                    Step(MenuStep.Loading);
                    main.StartSoloGame();
                    break;
                case BotSettings.RunMode.Coop:
                    Step(MenuStep.CoopLobby);
                    break;
                case BotSettings.RunMode.Party:
                    Log("Multiplayer");
                    Step(MenuStep.PartyConnect);
                    main.StartMultiplayer();
                    break;
            }
        }

        private static int RandomOwned(int[] owned)
        {
            var choices = new List<int>();
            if (owned != null)
                for (int i = 0; i < owned.Length; i++)
                    if (owned[i] == 1) choices.Add(i);
            return choices.Count == 0 ? 0 : choices[UnityEngine.Random.Range(0, choices.Count)];
        }

        // Level cards list every map's texture packs in order; the run uses the map's first pack.
        private static int LevelOption(string mapId)
        {
            LevelDatabase db = LevelDatabase.Instance;
            int map = MapIndex(mapId), option = 0;
            if (db == null) return 0;
            for (int i = 0; i < map && i < db.Count; i++) option += Mathf.Max(1, db.entries[i].texturePackCount);
            return option;
        }

        private static void ActivateWithParents(Transform t)
        {
            var chain = new List<Transform>();
            for (; t != null; t = t.parent) chain.Add(t);
            for (int i = chain.Count - 1; i >= 0; i--)
                if (!chain[i].gameObject.activeSelf) chain[i].gameObject.SetActive(true);
        }

        // ------------------------------------------------------------------ co-op lobby

        private CouchCoopRoomManager coopLobby;
        private int coopPressFrames;

        private void CoopLobby(float now)
        {
            if (coopLobby == null)
            {
                coopLobby = UnityEngine.Object.FindFirstObjectByType<CouchCoopRoomManager>(FindObjectsInactive.Include);
                if (coopLobby == null) { Finish(false, "co-op lobby not found"); return; }

                // A player with no device would break the lobby's join list; give it a pad, like a gamepad-only player.
                foreach (PlayerInput existing in UnityEngine.Object.FindObjectsByType<PlayerInput>(FindObjectsSortMode.None))
                    if (existing.devices.Count == 0) NewMenuPad().PairWith(existing);

                // The lobby selects Leave until two players are in; keep the pads' A press from clicking it.
                if (EventSystem.current != null)
                    foreach (BaseInputModule module in EventSystem.current.GetComponents<BaseInputModule>())
                        if (module.enabled) { module.enabled = false; pausedModules.Add(module); }

                Log("Co-op lobby");
                ActivateWithParents(coopLobby.transform);
                nextActionAt = now + 0.5f;
                return;
            }

            CouchCoopManager manager = coopLobby.couchCoopManager;
            int joined = manager != null ? manager.joinedPlayers.Count : 0;

            if (coopPressFrames > 0)
            {
                // Hold A for a few frames, then let go.
                VirtualGamepad pad = menuPads[menuPads.Count - 1];
                pad.Jump = --coopPressFrames > 0;
                pad.Send();
                if (coopPressFrames == 0) nextActionAt = now + 0.3f;
                return;
            }

            if (joined < BotSettings.Players)
            {
                if (now - stepStartedAt > 30f) { Finish(false, $"only {joined} of {BotSettings.Players} players joined the co-op lobby"); return; }
                Log($"Co-op lobby: pad {menuPads.Count + 1} presses A to join ({joined} in)");
                NewMenuPad();
                coopPressFrames = 4;
                return;
            }

            foreach (BaseInputModule module in pausedModules) if (module != null) module.enabled = true;
            pausedModules.Clear();

            // Hardcore (default) or Chill, with the lobby's mode button.
            Button mode = FindChildButton(coopLobby.startButton != null ? coopLobby.startButton.transform.parent : null, "ModeButton");
            if (GameManager.chillCoop != BotSettings.Chill && mode != null)
            {
                mode.onClick.Invoke();
                Log($"Co-op lobby: mode {(GameManager.chillCoop ? "CHILL" : "HARD")}");
                nextActionAt = now + 0.3f;
                return;
            }
            if (GameManager.chillCoop != BotSettings.Chill) { Finish(false, "co-op mode button not found"); return; }

            if (coopLobby.startButton == null || !coopLobby.startButton.interactable) { nextActionAt = now + 0.2f; return; }
            Log($"Co-op lobby: {joined} players, {(GameManager.chillCoop ? "Chill" : "Hardcore")}: Start");
            Step(MenuStep.Loading);
            coopLobby.startButton.onClick.Invoke();
        }

        private VirtualGamepad NewMenuPad()
        {
            var pad = new VirtualGamepad($"ZCBot Menu Pad {menuPads.Count + 1}");
            menuPads.Add(pad);
            return pad;
        }

        private static Button FindChildButton(Transform parent, string name)
        {
            if (parent == null) return null;
            Transform t = parent.Find(name);
            return t != null ? t.GetComponent<Button>() : null;
        }

        // ------------------------------------------------------------------ Party

        private string RoomName => BotSettings.PartyOffline ? "zcbot-offline" : BotSettings.Room;

        private void PartyConnect(float now)
        {
            var connect = UnityEngine.Object.FindFirstObjectByType<ConnectToServer>();
            if (connect == null) return;

            if (BotSettings.PartyOffline)
            {
                // Photon offline mode stands in for Connect: it is "connected" straight away, but there's no lobby to
                // join, so go to the lobby screen the way OnJoinedLobby does.
                Log("Connect: Photon offline mode");
                PhotonNetwork.OfflineMode = true;
                PhotonNetwork.NickName = Player.Instance.playerName;
                SceneLoader.ToLobby();
            }
            else
            {
                Log("Connect");
                connect.Connect();
            }
            Step(MenuStep.PartyLobby);
        }

        private void PartyLobby(float now)
        {
            var rooms = UnityEngine.Object.FindFirstObjectByType<CreateJoinRooms>();
            if (rooms == null || roomJoinPending) return;

            rooms.ChangeRoomName(RoomName);
            roomJoinPending = true;
            if (BotSettings.PartyOffline)
            {
                Log($"Lobby: host room {RoomName}");
                rooms.CreateRoom();
            }
            else
            {
                // Join the room if a teammate already hosts it, otherwise host it (see OnJoinRoomFailed).
                Log($"Lobby: join room {RoomName}");
                rooms.JoinRoom();
            }
            Step(MenuStep.PartyRoom);
        }

        // Join failed because nobody hosts the room yet: host it. Host failed because a teammate just did: join it.
        private void RetryRoom(bool host)
        {
            if (step != MenuStep.PartyRoom || Finished) return;
            var rooms = UnityEngine.Object.FindFirstObjectByType<CreateJoinRooms>();
            if (rooms == null) return;
            rooms.ChangeRoomName(RoomName);
            Log(host ? $"Lobby: no room {RoomName} yet, hosting it" : $"Lobby: room {RoomName} exists, joining it");
            if (host) rooms.CreateRoom();
            else rooms.JoinRoom();
        }

        private void PartyRoom(float now)
        {
            var lobby = UnityEngine.Object.FindFirstObjectByType<LobbyUI>();
            if (lobby == null || PhotonNetwork.CurrentRoom == null) return;
            nextActionAt = now + 0.5f;

            if (!readyClicked)
            {
                readyClicked = true;
                Log($"Room {PhotonNetwork.CurrentRoom.Name}: Ready");
                lobby.OnReadyClicked();
                return;
            }
            if (!PhotonNetwork.IsMasterClient || startClicked) return;

            // The party leader sets the mode.
            if (RoomLevelKeys.IsHardcoreRoom != BotSettings.Hardcore)
            {
                Button mode = FindChildButton(lobby.readyButton != null ? lobby.readyButton.transform.parent : null, "ModeButton");
                if (mode == null) { Finish(false, "party mode button not found"); return; }
                mode.onClick.Invoke();
                Log($"Room: mode {(BotSettings.Hardcore ? "HARDCORE" : "NORMAL")}");
                return;
            }

            int needed = BotSettings.PartyOffline ? 1 : BotSettings.Players;
            if (PhotonNetwork.CurrentRoom.PlayerCount < needed)
            {
                if (now - stepStartedAt > 300f) Finish(false, $"Party: only {PhotonNetwork.CurrentRoom.PlayerCount} of {needed} players joined in 5 minutes");
                return;
            }

            var start = typeof(LobbyUI).GetField("startGameButton", Any)?.GetValue(lobby) as Button;
            bool canStart = start != null && start.gameObject.activeInHierarchy && start.interactable;
            if (canStart && BotSettings.Hardcore && PhotonNetwork.CurrentRoom.PlayerCount < 2)
            {
                Finish(false, "Start allowed a Hardcore Party game with only 1 player");
                return;
            }

            if (!canStart && BotSettings.PartyOffline && BotSettings.Hardcore && PhotonNetwork.CurrentRoom.PlayerCount < 2 && now - stepStartedAt > 3f)
            {
                // One Editor is one Photon client, so the offline test can't have the two players Hardcore needs.
                // Check Start refuses, then start the game the way the leader's Start does, without the player check.
                hardcoreStartBlocked = true;
                Log("Room: Start is blocked for Hardcore with 1 player (expected); starting the offline test game anyway");
                startClicked = true;
                Step(MenuStep.Loading);
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
                {
                    { RoomLevelKeys.Map, LevelSelect.mapSelected },
                    { RoomLevelKeys.TexturePack, LevelSelect.levelSelected },
                });
                PhotonNetwork.LoadLevel(GameScenes.SelectedPartyScene());
                return;
            }
            if (!canStart)
            {
                if (now - stepStartedAt > 120f) Finish(false, "Start never became available in the room (players not ready?)");
                return;
            }

            Log($"Room: {PhotonNetwork.CurrentRoom.PlayerCount} players ready, {(RoomLevelKeys.IsHardcoreRoom ? "Hardcore" : "Normal")}: Start");
            startClicked = true;
            Step(MenuStep.Loading);
            lobby.OnStartGameClicked();
        }

        // ------------------------------------------------------------------ clean up

        private void RemoveMenuPads()
        {
            foreach (BaseInputModule module in pausedModules) if (module != null) module.enabled = true;
            pausedModules.Clear();
            foreach (VirtualGamepad pad in menuPads) pad.Remove();
            menuPads.Clear();
        }

        /// <summary>Deletes the run's throwaway anonymous account and goes back to the usual profile.</summary>
        private async void DeleteBotAccount()
        {
            string profile = authProfile;
            CloudSaveLogin.AnonymousProfile = "default";
            if (string.IsNullOrEmpty(profile) || UnityServices.State != ServicesInitializationState.Initialized) return;
            IAuthenticationService auth = AuthenticationService.Instance;
            if (!auth.IsSignedIn || auth.Profile != profile) return;

            CleaningUp = true;
            try
            {
                await auth.DeleteAccountAsync();
                Log($"Deleted the bot's anonymous account (profile {profile})");
            }
            catch (Exception e)
            {
                Log("Couldn't delete the bot's anonymous account: " + e.Message);
            }
            finally
            {
                CleaningUp = false;
            }
        }
    }
}
