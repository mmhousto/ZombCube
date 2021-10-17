using System;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.Serialization.Pooled;
using MLAPI.Transports;
using UnityEngine;

namespace DapperDino.UMT.Lobby.Networking
{
    public class GameNetPortal : MonoBehaviour
    {
        // Singleton
        public static GameNetPortal Instance => instance;
        private static GameNetPortal instance;

        public event Action OnNetworkReadied;

        public event Action<ConnectStatus> OnConnectionFinished;
        public event Action<ConnectStatus> OnDisconnectReasonReceived;

        public event Action<ulong, int> OnClientSceneChanged;

        public event Action OnUserDisconnectRequested;
        
        /// <summary>
        /// Singleton Implementation.
        /// Do Not Destroy on load.
        /// </summary>
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            NetworkManager.Singleton.OnServerStarted += HandleNetworkReady; // Calls HandleNetworkReady method on server start
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected; // When Client is connected calls HandleClientConnected method.

            RegisterClientMessageHandlers();
            RegisterServerMessageHandlers();
        }

        private void OnDestroy()
        {
            // If the instance is not null, removes the methods
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnServerStarted -= HandleNetworkReady;
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            }

            // Unregisters messages
            UnregisterClientMessageHandlers();
            UnregisterServerMessageHandlers();
        }

        /// <summary>
        /// Starts a room.
        /// </summary>
        public void StartHost()
        {
            NetworkManager.Singleton.StartHost();
        }

        /// <summary>
        /// Requests to disconnect
        /// </summary>
        public void RequestDisconnect()
        {
            OnUserDisconnectRequested?.Invoke();
        }

        /// <summary>
        /// If clientId is not the local user then return else, call HandleNetworkReady.
        /// </summary>
        /// <param name="clientId">The players client id.</param>
        private void HandleClientConnected(ulong clientId)
        {
            if (clientId != NetworkManager.Singleton.LocalClientId) { return; }

            HandleNetworkReady();
        }

        /// <summary>
        /// If host, sets connection status to success. Invokes the OnNetworkReadied Action.
        /// </summary>
        private void HandleNetworkReady()
        {
            if (NetworkManager.Singleton.IsHost)
            {
                OnConnectionFinished?.Invoke(ConnectStatus.Success);
            }

            OnNetworkReadied?.Invoke();
        }

        #region Message Handlers

        /// <summary>
        /// Registers client connection status messages over the network.
        /// </summary>
        private void RegisterClientMessageHandlers()
        {
            CustomMessagingManager.RegisterNamedMessageHandler("ServerToClientConnectResult", (senderClientId, stream) =>
            {
                using (var reader = PooledNetworkReader.Get(stream))
                {
                    ConnectStatus status = (ConnectStatus)reader.ReadInt32();

                    OnConnectionFinished?.Invoke(status);
                }
            });

            CustomMessagingManager.RegisterNamedMessageHandler("ServerToClientSetDisconnectReason", (senderClientId, stream) =>
            {
                using (var reader = PooledNetworkReader.Get(stream))
                {
                    ConnectStatus status = (ConnectStatus)reader.ReadInt32();

                    OnDisconnectReasonReceived?.Invoke(status);
                }
            });
        }

        /// <summary>
        /// Registers server scene messages over the network.
        /// </summary>
        private void RegisterServerMessageHandlers()
        {
            CustomMessagingManager.RegisterNamedMessageHandler("ClientToServerSceneChanged", (senderClientId, stream) =>
            {
                using (var reader = PooledNetworkReader.Get(stream))
                {
                    int sceneIndex = reader.ReadInt32();

                    OnClientSceneChanged?.Invoke(senderClientId, sceneIndex);
                }
            });
        }

        /// <summary>
        /// Unregisters client messages.
        /// </summary>
        private void UnregisterClientMessageHandlers()
        {
            CustomMessagingManager.UnregisterNamedMessageHandler("ServerToClientConnectResult");
            CustomMessagingManager.UnregisterNamedMessageHandler("ServerToClientSetDisconnectReason");
        }

        /// <summary>
        /// Unregisters server messages.
        /// </summary>
        private void UnregisterServerMessageHandlers()
        {
            CustomMessagingManager.UnregisterNamedMessageHandler("ClientToServerSceneChanged");
        }

        #endregion

        #region Message Senders

        /// <summary>
        /// Sends connection status over the network to client.
        /// </summary>
        /// <param name="netId"></param>
        /// <param name="status"></param>
        public void ServerToClientConnectResult(ulong netId, ConnectStatus status)
        {
            using (var buffer = PooledNetworkBuffer.Get())
            {
                using (var writer = PooledNetworkWriter.Get(buffer))
                {
                    writer.WriteInt32((int)status);
                    CustomMessagingManager.SendNamedMessage("ServerToClientConnectResult", netId, buffer, NetworkChannel.Internal);
                }
            }
        }

        /// <summary>
        /// Sets disconnect reason over the network to client.
        /// </summary>
        /// <param name="netId"></param>
        /// <param name="status"></param>
        public void ServerToClientSetDisconnectReason(ulong netId, ConnectStatus status)
        {
            using (var buffer = PooledNetworkBuffer.Get())
            {
                using (var writer = PooledNetworkWriter.Get(buffer))
                {
                    writer.WriteInt32((int)status);
                    CustomMessagingManager.SendNamedMessage("ServerToClientSetDisconnectReason", netId, buffer, NetworkChannel.Internal);
                }
            }
        }

        /// <summary>
        /// Client sends new scene to server to change to.
        /// </summary>
        /// <param name="newScene"></param>
        public void ClientToServerSceneChanged(int newScene)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                OnClientSceneChanged?.Invoke(NetworkManager.Singleton.ServerClientId, newScene);
            }
            else if (NetworkManager.Singleton.IsConnectedClient)
            {
                using (var buffer = PooledNetworkBuffer.Get())
                {
                    using (var writer = PooledNetworkWriter.Get(buffer))
                    {
                        writer.WriteInt32(newScene);
                        CustomMessagingManager.SendNamedMessage("ClientToServerSceneChanged", NetworkManager.Singleton.ServerClientId, buffer, NetworkChannel.Internal);
                    }
                }
            }
        }

        #endregion
    }
}
