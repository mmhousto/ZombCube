using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Com.MorganHouston.ZombCube;

public class SpawnPlayers : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback
{
    public GameObject playerPrefab;
    public Transform[] spawnLocations;
    private int numOfPlayers;


    // Start is called before the first frame update
    void Start()
    {
        if (photonView.IsMine == false && PhotonNetwork.IsConnected == true)
        {
            return;
        }

        numOfPlayers = NetworkGameManager.playersSpawned;
        Debug.Log("Number of Players: " + numOfPlayers);
        switch (numOfPlayers)
        {
            case 0:
                PhotonNetwork.Instantiate(this.playerPrefab.name, spawnLocations[0].position, spawnLocations[0].rotation);
                NetworkGameManager.PlayerSpawned();
                break;
            case 1:
                PhotonNetwork.Instantiate(this.playerPrefab.name, spawnLocations[1].position, spawnLocations[1].rotation);
                NetworkGameManager.PlayerSpawned();
                break;
            case 2:
                PhotonNetwork.Instantiate(this.playerPrefab.name, spawnLocations[2].position, spawnLocations[2].rotation);
                NetworkGameManager.PlayerSpawned();
                break;
            case 3:
                PhotonNetwork.Instantiate(this.playerPrefab.name, spawnLocations[3].position, spawnLocations[3].rotation);
                NetworkGameManager.PlayerSpawned();
                break;

        }
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        // e.g. store this gameobject as this player's charater in Player.TagObject
        info.Sender.TagObject = this.gameObject;
    }

}
