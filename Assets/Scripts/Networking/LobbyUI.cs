using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Com.MorganHouston.ZombCube
{
    public class LobbyUI : MonoBehaviourPunCallbacks
    {
        [Header("References")]
        [SerializeField] [Tooltip("Array of PlayerCards.")] private PlayerCard[] lobbyPlayerCards;
        [SerializeField] [Tooltip("UI button to start the game.")] private Button startGameButton;

        Hashtable hash = new Hashtable();


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

            var playerData = SaveSystem.LoadPlayer();

            hash.Add("PlayerId", PhotonNetwork.LocalPlayer.ActorNumber);
            hash.Add("PlayerName", playerData.playerName);
            hash.Add("IsReady", false);
            hash.Add("Blaster", playerData.currentBlaster);

            if (playerData.Equals(null)) { return; }

            PhotonNetwork.SetPlayerCustomProperties(hash);

            
        }


        #endregion


        #region Private Methods


        /// <summary>
        /// Determines if everone is ready.
        /// If there is less than 1 player then return false.
        /// If any player is not ready, then return false.
        /// else return true.
        /// </summary>
        /// <returns></returns>
        private bool IsEveryoneReady()
        {
            if (PhotonNetwork.PlayerList.Length < 1)
            {
                return false;
            }

            foreach (var player in PhotonNetwork.PlayerList)
            {
                if ((bool)player.CustomProperties["IsReady"] == false)
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

            if (!IsEveryoneReady()) { return; }

            PhotonNetwork.LoadLevel("NetworkGameScene");
        }

        /// <summary>
        /// Updates player cards when someone enters, leaves, or ready's up.
        /// Updates start game button when all players are ready.
        /// </summary>
        /// <param name="lobbyState"></param>
        [PunRPC]
        private void HandleLobbyPlayersStateChangedRpc()
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
                startGameButton.interactable = IsEveryoneReady();
            }
        }


        #endregion


        #region PunCallbacks

        /// <summary>
        /// Calls HandleLobbyplayersStateChangedRpc when a players leaves the room.
        /// </summary>
        /// <param name="otherPlayer"></param>
        public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
        {
            this.photonView.RPC("HandleLobbyPlayersStateChangedRpc", RpcTarget.AllBuffered);
        }

        /// <summary>
        /// Calls HandleLobbyplayersStateChangedRpc when a players properties are updated.
        /// </summary>
        /// <param name="targetPlayer"></param>
        /// <param name="changedProps"></param>
        public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, Hashtable changedProps)
        {
            this.photonView.RPC("HandleLobbyPlayersStateChangedRpc", RpcTarget.AllBuffered);

        }

        /// <summary>
        /// When a player disconnects, the main menu scene is loaded.
        /// </summary>
        /// <param name="cause"></param>
        public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
        {
            Debug.Log(cause);
            SceneLoader.ToMainMenu();
        }


        #endregion

    }
}
