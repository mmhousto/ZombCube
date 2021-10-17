using System;
using DapperDino.UMT.Lobby.Networking;
using MLAPI;
using MLAPI.Connection;
using MLAPI.Messaging;
using MLAPI.NetworkVariable.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DapperDino.UMT.Lobby.UI
{
    public class LobbyUI : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] [Tooltip("Array of PlayerCards.")] private PlayerCard[] lobbyPlayerCards;
        [SerializeField] [Tooltip("UI button to start the game.")] private Button startGameButton;

        private NetworkList<LobbyPlayerState> lobbyPlayers = new NetworkList<LobbyPlayerState>();

        /// <summary>
        /// Adds methods to callbacks on network start.
        /// </summary>
        public override void NetworkStart()
        {
            if (IsClient)
            {
                lobbyPlayers.OnListChanged += HandleLobbyPlayersStateChanged;
            }

            if (IsServer)
            {
                startGameButton.gameObject.SetActive(true);

                NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;

                foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    HandleClientConnected(client.ClientId);
                }
            }
        }

        /// <summary>
        /// Removes methods from callbacks.
        /// </summary>
        private void OnDestroy()
        {
            lobbyPlayers.OnListChanged -= HandleLobbyPlayersStateChanged;

            if (NetworkManager.Singleton)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
            }
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
            if (lobbyPlayers.Count < 1)
            {
                return false;
            }

            foreach (var player in lobbyPlayers)
            {
                if (!player.IsReady)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// When client is connected, adds their player card based on their player data.
        /// </summary>
        /// <param name="clientId"></param>
        private void HandleClientConnected(ulong clientId)
        {
            var playerData = ServerGameNetPortal.Instance.GetPlayerData(clientId);

            if (!playerData.HasValue) { return; }

            lobbyPlayers.Add(new LobbyPlayerState(
                clientId,
                playerData.Value.PlayerName,
                false,
                playerData.Value.CurrentBlaster
            ));
        }

        /// <summary>
        /// Disconnects a player based on clientId and removes their PlayerCard.
        /// </summary>
        /// <param name="clientId"></param>
        private void HandleClientDisconnect(ulong clientId)
        {
            for (int i = 0; i < lobbyPlayers.Count; i++)
            {
                if (lobbyPlayers[i].ClientId == clientId)
                {
                    lobbyPlayers.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>
        /// Sets the players toggle over the network.
        /// </summary>
        /// <param name="serverRpcParams"></param>
        [ServerRpc(RequireOwnership = false)]
        private void ToggleReadyServerRpc(ServerRpcParams serverRpcParams = default)
        {
            for (int i = 0; i < lobbyPlayers.Count; i++)
            {
                if (lobbyPlayers[i].ClientId == serverRpcParams.Receive.SenderClientId)
                {
                    lobbyPlayers[i] = new LobbyPlayerState(
                        lobbyPlayers[i].ClientId,
                        lobbyPlayers[i].PlayerName,
                        !lobbyPlayers[i].IsReady,
                        lobbyPlayers[i].CurrentBlaster
                    );
                }
            }
        }

        /// <summary>
        /// If host and everone is ready, asks the server to start the game.
        /// </summary>
        /// <param name="serverRpcParams"></param>
        [ServerRpc(RequireOwnership = false)]
        private void StartGameServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (serverRpcParams.Receive.SenderClientId != NetworkManager.Singleton.LocalClientId) { return; }

            if (!IsEveryoneReady()) { return; }

            ServerGameNetPortal.Instance.StartGame();
        }

        /// <summary>
        /// Requests to disconnect from lobby.
        /// </summary>
        public void OnLeaveClicked()
        {
            GameNetPortal.Instance.RequestDisconnect();
        }

        /// <summary>
        /// Calls method to set your ready toggle over the network.
        /// </summary>
        public void OnReadyClicked()
        {
            ToggleReadyServerRpc();
        }

        /// <summary>
        /// Asks the server to start the game.
        /// </summary>
        public void OnStartGameClicked()
        {
            StartGameServerRpc();
        }

        /// <summary>
        /// Updates player cards when someone enters, leaves, or ready's up.
        /// Updates start game button when all players are ready.
        /// </summary>
        /// <param name="lobbyState"></param>
        private void HandleLobbyPlayersStateChanged(NetworkListEvent<LobbyPlayerState> lobbyState)
        {
            for (int i = 0; i < lobbyPlayerCards.Length; i++)
            {
                if (lobbyPlayers.Count > i)
                {
                    lobbyPlayerCards[i].UpdateDisplay(lobbyPlayers[i]);
                }
                else
                {
                    lobbyPlayerCards[i].DisableDisplay();
                }
            }

            if(IsHost)
            {
                startGameButton.interactable = IsEveryoneReady();
            }
        }
    }
}
