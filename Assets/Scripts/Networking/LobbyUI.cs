using System;
using DapperDino.UMT.Lobby.Networking;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections.Generic;
using System.Collections;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace DapperDino.UMT.Lobby.UI
{
    public class LobbyUI : MonoBehaviourPunCallbacks
    {
        [Header("References")]
        [SerializeField] [Tooltip("Array of PlayerCards.")] private PlayerCard[] lobbyPlayerCards;
        [SerializeField] [Tooltip("UI button to start the game.")] private Button startGameButton;

        bool playerExists = false;

        private List<LobbyPlayerState> lobbyPlayers = new List<LobbyPlayerState>();

        Hashtable hash = new Hashtable();

        public override void OnJoinedRoom()
        {
            
        }

        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            
        }

        public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
        {
            /*for (int i = 0; i < lobbyPlayers.Count; i++)
            {
                if (lobbyPlayers[i].ActorId == otherPlayer.ActorNumber)
                {
                    lobbyPlayers.RemoveAt(i);
                    break;
                }
            }*/
            photonView.RPC("HandleLobbyPlayersStateChangedRpc", RpcTarget.AllBuffered);
        }

        /// <summary>
        /// Adds methods to callbacks on network start.
        /// </summary>
        void Start()
        {
            PhotonView photonView = PhotonView.Get(this);

            if (PhotonNetwork.IsMasterClient)
            {
                startGameButton.gameObject.SetActive(true);
            }

            var playerData = SaveSystem.LoadPlayer();

            Debug.Log(playerData.playerName);

            

            hash.Add("PlayerId", PhotonNetwork.LocalPlayer.ActorNumber);
            hash.Add("PlayerName", playerData.playerName);
            hash.Add("IsReady", false);
            hash.Add("Blaster", playerData.currentBlaster);



            if (playerData.Equals(null)) { return; }

            PhotonNetwork.SetPlayerCustomProperties(hash);

            
        }

        public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, Hashtable changedProps)
        {
            photonView.RPC("HandleLobbyPlayersStateChangedRpc", RpcTarget.AllBuffered);
            
        }


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

        /// <summary>
        /// Sets the players toggle over the network.
        /// </summary>
        [PunRPC]
        private void ToggleReadyRpc(int playerId)
        {
            for (int i = 0; i < lobbyPlayers.Count; i++)
            {
                if (lobbyPlayers[i].ActorId == playerId)
                {
                    hash["IsReady"] = !lobbyPlayers[i].IsReady;

                    PhotonNetwork.SetPlayerCustomProperties(hash);
                }
            }
        }

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
            //photonView.RPC("ToggleReadyRpc", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber);
            hash["IsReady"] = !(bool)hash["IsReady"];
            PhotonNetwork.SetPlayerCustomProperties(hash);
            
        }

        /// <summary>
        /// Asks the server to start the game.
        /// </summary>
        public void OnStartGameClicked()
        {
            photonView.RPC("StartGameServerRpc", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
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

        public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
        {
            Debug.Log(cause);
            SceneLoader.ToMainMenu();
        }


    }
}
