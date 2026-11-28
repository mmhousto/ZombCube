using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.EventSystems;
using System;

namespace Com.GCTC.ZombCube
{
    public class LobbyUI : MonoBehaviourPunCallbacks
    {
        [Header("References")]
        [SerializeField] [Tooltip("Array of PlayerCards.")] private PlayerCard[] lobbyPlayerCards;
        [SerializeField] [Tooltip("UI button to start the game.")] private Button startGameButton;
        [Tooltip("UI button to ready up for player.")] public GameObject readyButton;

        Hashtable hash = new Hashtable();

        Player player;

        GameObject[] playerData;


        #region MonoBehaviour Methods


        /// <summary>
        /// Loads the player and saves properties in custom player properties with a hash table.
        /// If is host, then activates the start game button.
        /// </summary>
        void Start()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                startGameButton.gameObject.SetActive(true);
            }

            playerData = GameObject.FindGameObjectsWithTag("PlayerData");

            foreach(GameObject playerDataObject in playerData)
            {
                player = playerDataObject.GetComponent<Player>();
                if (player.playerName != PhotonNetwork.LocalPlayer.NickName)
                {
                    Destroy(playerDataObject);
                }
            }

            player = GameObject.FindWithTag("PlayerData").GetComponent<Player>();
            
            if (player.Equals(null)) { return; }

            hash.Add("PlayerId", PhotonNetwork.LocalPlayer.ActorNumber);
            hash.Add("PlayerName", player.playerName);
            hash.Add("UserName", player.userName);
            hash.Add("IsReady", false);
            hash.Add("Blaster", player.currentBlaster);
            hash.Add("Skin", player.currentSkin);
            hash.Add("AccountID", player.userID);
            hash.Add("IsRestricted", CloudSaveLogin.Instance.restricted);

            PhotonNetwork.SetPlayerCustomProperties(hash);

            CreateModeButton();
        }

        private void Update()
        {
            RefreshModeButton();

            if (PhotonNetwork.IsMasterClient && (EventSystem.current.currentSelectedGameObject == startGameButton.gameObject || EventSystem.current.currentSelectedGameObject == null) && CanStartGame() == false)
            {
                EventSystem.current.SetSelectedGameObject(readyButton);
            }
        }


#endregion


        #region Private Methods

        // Party mode: Normal (the game ends when everyone is out) or Hardcore (one player out ends it).
        // A copy of the Ready button, placed above it; only the party leader can change it.
        private Button modeButton;
        private TMPro.TMP_Text modeLabelTmp;
        private Text modeLabel;

        private void CreateModeButton()
        {
            if (readyButton == null || modeButton != null) return;
            GameObject copy = Instantiate(readyButton, readyButton.transform.parent);
            copy.name = "ModeButton";
            modeButton = copy.GetComponent<Button>();
            if (modeButton == null) { Destroy(copy); return; }
            modeButton.onClick = new Button.ButtonClickedEvent();   // drop the Ready button's listeners
            modeButton.onClick.AddListener(OnModeClicked);
            modeLabelTmp = copy.GetComponentInChildren<TMPro.TMP_Text>(true);
            modeLabel = modeLabelTmp == null ? copy.GetComponentInChildren<Text>(true) : null;

            var source = (RectTransform)readyButton.transform;
            var rect = (RectTransform)copy.transform;
            rect.anchoredPosition = source.anchoredPosition + new Vector2(0f, source.rect.height + 20f);
            copy.SetActive(true);
            RefreshModeButton();
        }

        private void OnModeClicked()
        {
            if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null) return;
            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { RoomLevelKeys.Hardcore, !RoomLevelKeys.IsHardcoreRoom } });
        }

        private void RefreshModeButton()
        {
            if (modeButton == null) return;
            string text = RoomLevelKeys.IsHardcoreRoom ? "HARD" : "CHILL";
            if (modeLabelTmp != null && modeLabelTmp.text != text) modeLabelTmp.text = text;
            if (modeLabel != null && modeLabel.text != text) modeLabel.text = text;
            bool leader = PhotonNetwork.IsMasterClient;
            if (modeButton.interactable != leader) modeButton.interactable = leader;
            if (leader && startGameButton != null && startGameButton.interactable != CanStartGame())
                startGameButton.interactable = CanStartGame();
            if (RoomLevelKeys.IsHardcoreRoom && PhotonNetwork.PlayerList.Length < 2)
            {
                string needsTwo = "MODE: HARDCORE (2+ PLAYERS)";
                if (modeLabelTmp != null && modeLabelTmp.text != needsTwo) modeLabelTmp.text = needsTwo;
                if (modeLabel != null && modeLabel.text != needsTwo) modeLabel.text = needsTwo;
            }
        }


        /// <summary>
        /// Determines if everone is ready.
        /// If there is less than 1 player then return false.
        /// If any player is not ready, then return false.
        /// else return true.
        /// </summary>
        /// <returns></returns>
        /// <summary>Everyone is ready, and Hardcore needs at least two players (one out ends the game).</summary>
        private bool CanStartGame()
        {
            if (!IsEveryoneReady()) return false;
            return !RoomLevelKeys.IsHardcoreRoom || PhotonNetwork.PlayerList.Length >= 2;
        }

        private bool IsEveryoneReady()
        {
            if (PhotonNetwork.PlayerList.Length < 1)
            {
                return false;
            }

            foreach (var player in PhotonNetwork.PlayerList)
            {
                // A player who just joined may not have set IsReady yet; treat that as not ready.
                if (!(player.CustomProperties.TryGetValue("IsReady", out object ready) && ready is bool isReady && isReady))
                {
                    return false;
                }
            }

            return true;
        }


        #endregion


        #region Public Methods


        /// <summary>
        /// Requests to disconnect from lobby.
        /// </summary>
        public void OnLeaveClicked()
        {
            PhotonNetwork.Disconnect();
        }

        /// <summary>
        /// Calls method to set your ready toggle over the network.
        /// </summary>
        public void OnReadyClicked()
        {
            hash["IsReady"] = !(bool)hash["IsReady"];
            PhotonNetwork.SetPlayerCustomProperties(hash);
            
        }

        /// <summary>
        /// Asks the server to start the game.
        /// </summary>
        public void OnStartGameClicked()
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            this.photonView.RPC("StartGameServerRpc", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
        }


        #endregion


        #region PunRpc's


        /// <summary>
        /// If host and everone is ready, asks the server to start the game.
        /// </summary>
        [PunRPC]
        private void StartGameServerRpc(int playerId)
        {
            if (playerId != PhotonNetwork.LocalPlayer.ActorNumber) { return; }

            if (!CanStartGame()) { return; }

            // Everyone plays the host's map and texture pack.
            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
            {
                { RoomLevelKeys.Map, LevelSelect.mapSelected },
                { RoomLevelKeys.TexturePack, LevelSelect.levelSelected },
            });
            PhotonNetwork.LoadLevel(GameScenes.SelectedPartyScene());
        }

        /// <summary>
        /// Updates player cards when someone enters, leaves, or ready's up.
        /// Updates start game button when all players are ready.
        /// </summary>
        /// <param name="lobbyState"></param>
        private void HandleLobbyPlayersStateChanged()
        {
            for (int i = 0; i < lobbyPlayerCards.Length; i++)
            {
                if (PhotonNetwork.PlayerList.Length > i)
                {
                    lobbyPlayerCards[i].UpdateDisplay(PhotonNetwork.PlayerList[i]);
                }
                else
                {
                    lobbyPlayerCards[i].DisableDisplay();
                }
            }

            if(PhotonNetwork.IsMasterClient)
            {
                startGameButton.interactable = CanStartGame();
            }

            playerData = GameObject.FindGameObjectsWithTag("PlayerData");

            foreach (GameObject playerDataObject in playerData)
            {
                player = playerDataObject.GetComponent<Player>();
                if (player.playerName != PhotonNetwork.LocalPlayer.NickName)
                {
                    Destroy(playerDataObject);
                }
            }

            player = GameObject.FindWithTag("PlayerData").GetComponent<Player>();
        }


        #endregion


        #region PunCallbacks

        /// <summary>
        /// Calls HandleLobbyplayersStateChangedRpc when a players leaves the room.
        /// </summary>
        /// <param name="otherPlayer"></param>
        public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
        {
            HandleLobbyPlayersStateChanged();
        }

        /// <summary>
        /// Calls HandleLobbyplayersStateChangedRpc when a players properties are updated.
        /// </summary>
        /// <param name="targetPlayer"></param>
        /// <param name="changedProps"></param>
        public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, Hashtable changedProps)
        {
            HandleLobbyPlayersStateChanged();

        }

        /// <summary>
        /// When a player disconnects, the main menu scene is loaded.
        /// </summary>
        /// <param name="cause"></param>
        public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
        {
            if(cause != Photon.Realtime.DisconnectCause.DisconnectByClientLogic)
                ErrorManager.Instance.StartErrorMessage("Network Error: Player disconnected from the internet.");
            SceneLoader.ToMainMenu();
        }


        #endregion

    }
}
