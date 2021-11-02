using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.Connection;
using DapperDino.UMT.Lobby.Networking;

public class PlayerSpawnSystem : NetworkBehaviour
{

    public GameObject player;

    [SerializeField] public GameObject networkPlayer;

    [SerializeField] private int currentPlayers = 0;

    private List<ulong> loadingClients = new List<ulong>();


    public override void NetworkStart()
    {
        if (IsServer)
        {
            foreach(NetworkClient networkClient in NetworkManager.Singleton.ConnectedClientsList)
            {
                loadingClients.Add(networkClient.ClientId);
            }
        }

        if (IsClient)
        {
            ClientIsReadyServerRpc();
            IncreasePlayerCountServerRpc();
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        if (SceneLoader.GetCurrentScene() == "GameScene")
        {
            SpawnPlayer();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void SpawnPlayer()
    {
        Instantiate(player, new Vector3(30f, 3f, -47f), Quaternion.Euler(new Vector3(0, -160f, 0)));

    }

    [ServerRpc(RequireOwnership = false)]
    private void ClientIsReadyServerRpc(ServerRpcParams serverRpcParams = default)
    {
        if(!loadingClients.Contains(serverRpcParams.Receive.SenderClientId)) { return; }

        SpawnPlayer(serverRpcParams.Receive.SenderClientId);

        loadingClients.Remove(serverRpcParams.Receive.SenderClientId);

        if(loadingClients.Count != 0) { return; }

        Debug.Log("Everyone Is Ready");

    }

    private void SpawnPlayer(ulong clientID)
    {
        GameObject client = null;

        Debug.Log("RPC CALLED");
        switch (currentPlayers)
        {
            case 0:
                client = Instantiate(networkPlayer, new Vector3(30f, 3f, -47f), Quaternion.Euler(new Vector3(0, -160f, 0)));
                client.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientID, null, true);
                Debug.Log("Player 1 spawned");
                break;
            case 1:
                client = Instantiate(networkPlayer, new Vector3(36f, 3f, -47f), Quaternion.Euler(new Vector3(0, -230f, 0)));
                client.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientID, null, true);
                Debug.Log("Player 2 spawned");
                break;
            case 2:
                client = Instantiate(networkPlayer, new Vector3(36f, 3f, -38f), Quaternion.Euler(new Vector3(0, 50f, 0)));
                client.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientID, null, true);
                Debug.Log("Player 3 spawned");
                break;
            case 3:
                client = Instantiate(networkPlayer, new Vector3(26f, 3f, -39f), Quaternion.Euler(new Vector3(0, -36f, 0)));
                client.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientID, null, true);
                Debug.Log("Player 4 spawned");
                break;
        }


    }

    [ServerRpc(RequireOwnership = false)]
    private void IncreasePlayerCountServerRpc()
    {
        currentPlayers++;
    }
}
