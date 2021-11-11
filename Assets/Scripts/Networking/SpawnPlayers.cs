using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SpawnPlayers : MonoBehaviour
{
    public GameObject playerPrefab;
    public Transform[] spawnLocations;
    private static int numOfPlayers = 0;


    // Start is called before the first frame update
    void Start()
    {
        switch (numOfPlayers)
        {
            case 0:
                PhotonNetwork.Instantiate(playerPrefab.name, spawnLocations[0].position, spawnLocations[0].rotation);
                numOfPlayers++;
                break;
            case 1:
                PhotonNetwork.Instantiate(playerPrefab.name, spawnLocations[1].position, spawnLocations[1].rotation);
                numOfPlayers++;
                break;
            case 2:
                PhotonNetwork.Instantiate(playerPrefab.name, spawnLocations[2].position, spawnLocations[2].rotation);
                numOfPlayers++;
                break;
            case 3:
                PhotonNetwork.Instantiate(playerPrefab.name, spawnLocations[3].position, spawnLocations[3].rotation);
                numOfPlayers++;
                break;

        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
