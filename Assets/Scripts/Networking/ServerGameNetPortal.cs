using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MLAPI;
using MLAPI.SceneManagement;
using MLAPI.Spawning;
using MLAPI.Transports.UNET;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DapperDino.UMT.Lobby.Networking
{
    public class ServerGameNetPortal : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] [Tooltip("Max players allowed in a game.")] private int maxPlayers = 4;

        public static ServerGameNetPortal Instance => instance;
        private static ServerGameNetPortal instance;

        private Dictionary<string, PlayerData> clientData; // stores players in room.
        private Dictionary<ulong, string> clientIdToGuid; // stores players client id
        private Dictionary<ulong, int> clientSceneMap; // stores scene
        private bool gameInProgress; // Is the game in progress?

        private const int MaxConnectionPayload = 1024;

        private GameNetPortal gameNetPortal;

        private UNetTransport transport;

        /// <summary>
        /// Signleton Pattern
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

        /// <summary>
        /// Adds methods to callbacks.
        /// Creates server data dictionary for scene, player data, and client id.
        /// </summary>
        private void Start()
        {
            gameNetPortal = GetComponent<GameNetPortal>();
            gameNetPortal.OnNetworkReadied += HandleNetworkReadied;

            NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
            NetworkManager.Singleton.OnServerStarted += HandleServerStarted;

            clientData = new Dictionary<string, PlayerData>();
            clientIdToGuid = new Dictionary<ulong, string>();
            clientSceneMap = new Dictionary<ulong, int>();
        }

        /// <summary>
        /// Removes methods from callbacks.
        /// </summary>
        private void OnDestroy()
        {
            if (gameNetPortal == null) { return; }

            gameNetPortal.OnNetworkReadied -= HandleNetworkReadied;

            if (NetworkManager.Singleton == null) { return; }

            NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
            NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
        }

        /// <summary>
        /// Tries to get the players data based on clientId.
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public PlayerData? GetPlayerData(ulong clientId)
        {
            if (clientIdToGuid.TryGetValue(clientId, out string clientGuid))
            {
                if (clientData.TryGetValue(clientGuid, out PlayerData playerData))
                {
                    return playerData;
                }
                else
                {
                    Debug.LogWarning($"No player data found for client id: {clientId}");
                }
            }
            else
            {
                Debug.LogWarning($"No client guid found for client id: {clientId}");
            }

            return null;
        }

        /// <summary>
        /// Starts the game.
        /// </summary>
        public void StartGame()
        {
            gameInProgress = true;

            NetworkSceneManager.SwitchScene("GameScene");
        }

        /// <summary>
        /// Ends round and returns players to the lobby.
        /// </summary>
        public void EndRound()
        {
            gameInProgress = false;

            NetworkSceneManager.SwitchScene("LobbyScene");
        }

        /// <summary>
        /// When connected to the network and room, switches you to the lobby scene and adds methods to callbacks.
        /// Sets client scene data for host.
        /// </summary>
        private void HandleNetworkReadied()
        {
            if (!NetworkManager.Singleton.IsServer) { return; }

            gameNetPortal.OnUserDisconnectRequested += HandleUserDisconnectRequested;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
            gameNetPortal.OnClientSceneChanged += HandleClientSceneChanged;

            NetworkSceneManager.SwitchScene("LobbyScene");

            if (NetworkManager.Singleton.IsHost)
            {
                clientSceneMap[NetworkManager.Singleton.LocalClientId] = SceneManager.GetActiveScene().buildIndex;
            }
        }

        /// <summary>
        /// Removes client from server data and removes methods from callbacks.
        /// </summary>
        /// <param name="clientId"></param>
        private void HandleClientDisconnect(ulong clientId)
        {
            clientSceneMap.Remove(clientId);

            if (clientIdToGuid.TryGetValue(clientId, out string guid))
            {
                clientIdToGuid.Remove(clientId);

                if (clientData[guid].ClientId == clientId)
                {
                    clientData.Remove(guid);
                }
            }

            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                gameNetPortal.OnUserDisconnectRequested -= HandleUserDisconnectRequested;
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
                gameNetPortal.OnClientSceneChanged -= HandleClientSceneChanged;
            }
        }

        /// <summary>
        /// Changes scene data for that client.
        /// </summary>
        /// <param name="clientId">The users id.</param>
        /// <param name="sceneIndex">The scene number they are in.</param>
        private void HandleClientSceneChanged(ulong clientId, int sceneIndex)
        {
            clientSceneMap[clientId] = sceneIndex;
        }

        /// <summary>
        /// Disconnects host and clears server data and return to MainMenu scene.
        /// </summary>
        private void HandleUserDisconnectRequested()
        {
            HandleClientDisconnect(NetworkManager.Singleton.LocalClientId);

            NetworkManager.Singleton.StopHost();

            ClearData();

            SceneManager.LoadScene("MainMenu");
        }

        /// <summary>
        /// Creates the server and adds the host to client data.
        /// </summary>
        private void HandleServerStarted()
        {
            if (!NetworkManager.Singleton.IsHost) { return; }

            string clientGuid = Guid.NewGuid().ToString();
            string playerName = Player.Instance.playerName;
            int currentBlaster = Player.Instance.currentBlaster;

            clientData.Add(clientGuid, new PlayerData(playerName, NetworkManager.Singleton.LocalClientId, currentBlaster));
            clientIdToGuid.Add(NetworkManager.Singleton.LocalClientId, clientGuid);
        }

        /// <summary>
        /// Clears server data.
        /// </summary>
        private void ClearData()
        {
            clientData.Clear();
            clientIdToGuid.Clear();
            clientSceneMap.Clear();

            gameInProgress = false;
        }

        /// <summary>
        /// Returns if connectionData length is greater than MaxConnectionPayLoad.
        /// Gets connection data and stores it in a variable.
        /// Gets password from user and compares it with server password.
        /// If passwords match success, else wrong password and disconnects.
        /// If server is full, disconnects.
        /// If game is in progress, disconnects.
        /// If success, tells server connection status and adds player data to client data array. 
        /// </summary>
        /// <param name="connectionData"></param>
        /// <param name="clientId"></param>
        /// <param name="callback"></param>
        private void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback)
        {
            if (connectionData.Length > MaxConnectionPayload)
            {
                callback(false, 0, false, null, null);
                return;
            }

            string payload = Encoding.UTF8.GetString(connectionData);
            var connectionPayload = JsonUtility.FromJson<ConnectionPayload>(payload);

            string password = Player.Instance.ipAddress;

            bool approveConnection = password == connectionPayload.password;

            ConnectStatus gameReturnStatus;

            if (approveConnection)
                gameReturnStatus = ConnectStatus.Success;
            else
                gameReturnStatus = ConnectStatus.WrongPassword;

            // This stops us from running multiple standalone builds since 
            // they disconnect eachother when trying to join
            //
            // if (clientData.ContainsKey(connectionPayload.clientGUID))
            // {
            //     ulong oldClientId = clientData[connectionPayload.clientGUID].ClientId;
            //     StartCoroutine(WaitToDisconnectClient(oldClientId, ConnectStatus.LoggedInAgain));
            // }

            if (gameInProgress)
            {
                gameReturnStatus = ConnectStatus.GameInProgress;
            }
            else if (clientData.Count >= maxPlayers)
            {
                gameReturnStatus = ConnectStatus.ServerFull;
            }

            if (gameReturnStatus == ConnectStatus.Success)
            {
                clientSceneMap[clientId] = connectionPayload.clientScene;
                clientIdToGuid[clientId] = connectionPayload.clientGUID;
                clientData[connectionPayload.clientGUID] = new PlayerData(connectionPayload.playerName, clientId, connectionPayload.currentBlaster);
            }

            callback(false, 0, approveConnection, null, null);

            gameNetPortal.ServerToClientConnectResult(clientId, gameReturnStatus);

            if (gameReturnStatus != ConnectStatus.Success)
            {
                StartCoroutine(WaitToDisconnectClient(clientId, gameReturnStatus));
            }
        }

        /// <summary>
        /// Tells client reason of disconnect, then kicks client.
        /// </summary>
        /// <param name="clientId">The id of the client.</param>
        /// <param name="reason">ConnectStatus reason for disconnect.</param>
        /// <returns></returns>
        private IEnumerator WaitToDisconnectClient(ulong clientId, ConnectStatus reason)
        {
            gameNetPortal.ServerToClientSetDisconnectReason(clientId, reason);

            yield return new WaitForSeconds(0);

            KickClient(clientId);
        }

        /// <summary>
        /// Kicks client from game based on clientId.
        /// </summary>
        /// <param name="clientId">The id of the client.</param>
        private void KickClient(ulong clientId)
        {
            NetworkObject networkObject = NetworkSpawnManager.GetPlayerNetworkObject(clientId);
            if (networkObject != null)
            {
                networkObject.Despawn(true);
            }

            NetworkManager.Singleton.DisconnectClient(clientId);
        }
    }
}
