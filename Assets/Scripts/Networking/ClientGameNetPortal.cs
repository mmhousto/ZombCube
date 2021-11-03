using System;
using System.Text;
using MLAPI;
using MLAPI.Transports.UNET;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DapperDino.UMT.Lobby.Networking
{
    [RequireComponent(typeof(GameNetPortal))]
    public class ClientGameNetPortal : MonoBehaviour
    {
        public static ClientGameNetPortal Instance => instance;
        private static ClientGameNetPortal instance;

        public DisconnectReason DisconnectReason { get; private set; } = new DisconnectReason();

        public event Action<ConnectStatus> OnConnectionFinished;

        public event Action OnNetworkTimedOut;

        private GameNetPortal gameNetPortal;

        private UNetTransport transport;

        /// <summary>
        /// Singleton Pattern
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
            gameNetPortal = GetComponent<GameNetPortal>();

            // Adds methods to callbacks
            gameNetPortal.OnNetworkReadied += HandleNetworkReadied;
            gameNetPortal.OnConnectionFinished += HandleConnectionFinished;
            gameNetPortal.OnDisconnectReasonReceived += HandleDisconnectReasonReceived;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
        }

        private void OnDestroy()
        {
            if (gameNetPortal == null) { return; }

            // Removes methods from callbacks.
            gameNetPortal.OnNetworkReadied -= HandleNetworkReadied;
            gameNetPortal.OnConnectionFinished -= HandleConnectionFinished;
            gameNetPortal.OnDisconnectReasonReceived -= HandleDisconnectReasonReceived;

            if (NetworkManager.Singleton == null) { return; }

            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
        }

        /// <summary>
        /// Passes through information through bytes and the ConnectionData.
        /// Calls StartClient to authenticate password.
        /// </summary>
        public void StartClient()
        {
            transport = NetworkManager.Singleton.GetComponent<UNetTransport>();
            transport.ConnectAddress = Player.Instance.ipAddress;

            var payload = JsonUtility.ToJson(new ConnectionPayload()
            {
                clientGUID = Guid.NewGuid().ToString(),
                clientScene = SceneManager.GetActiveScene().buildIndex,
                playerName = Player.Instance.playerName,
                currentBlaster = Player.Instance.currentBlaster,
                password = Player.Instance.ipAddress
            });

            byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);


            NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;

            NetworkManager.Singleton.StartClient();
        }

        /// <summary>
        /// If you are not the client returns.
        /// If you are not the host, adds HandleUserDisconnectRequested method to OnUserDisconnectRequested callback.
        /// Adds HandleSceneLoaded method to sceneLoaded callback.
        /// </summary>
        private void HandleNetworkReadied()
        {
            if (!NetworkManager.Singleton.IsClient) { return; }

            if (!NetworkManager.Singleton.IsHost)
            {
                gameNetPortal.OnUserDisconnectRequested += HandleUserDisconnectRequested;
            }

            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        /// <summary>
        /// Handles scene to load with build index.
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="mode"></param>
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            gameNetPortal.ClientToServerSceneChanged(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>
        /// Disconnects user and stops the client and returns player to Main Menu.
        /// </summary>
        private void HandleUserDisconnectRequested()
        {
            DisconnectReason.SetDisconnectReason(ConnectStatus.UserRequestedDisconnect);
            NetworkManager.Singleton.StopClient();

            HandleClientDisconnect(NetworkManager.Singleton.LocalClientId);

            SceneManager.LoadScene("MainMenu");
        }

        /// <summary>
        /// If connection is not successfull, calls and sets disconnect reason.
        /// Invokes OnConnectionFinished action.
        /// </summary>
        /// <param name="status">The connection status.</param>
        private void HandleConnectionFinished(ConnectStatus status)
        {
            if (status != ConnectStatus.Success)
            {
                DisconnectReason.SetDisconnectReason(status);
            }

            OnConnectionFinished?.Invoke(status);
        }

        /// <summary>
        /// Calls and sets the disconnect reason.
        /// </summary>
        /// <param name="status">The connection status.</param>
        private void HandleDisconnectReasonReceived(ConnectStatus status)
        {
            DisconnectReason.SetDisconnectReason(status);
        }

        /// <summary>
        /// Diconnects client and sends them to Main Menu scene if they are not there.
        /// </summary>
        /// <param name="clientId"></param>
        private void HandleClientDisconnect(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsConnectedClient && !NetworkManager.Singleton.IsHost)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                gameNetPortal.OnUserDisconnectRequested -= HandleUserDisconnectRequested;

                if (SceneManager.GetActiveScene().name != "MainMenu")
                {
                    if (!DisconnectReason.HasTransitionReason)
                    {
                        DisconnectReason.SetDisconnectReason(ConnectStatus.GenericDisconnect);
                    }

                    SceneManager.LoadScene("MainMenu");
                }
                else
                {
                    OnNetworkTimedOut?.Invoke();
                }
            }
        }
    }
}
