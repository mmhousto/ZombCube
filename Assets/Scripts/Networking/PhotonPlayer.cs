using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PhotonPlayer : MonoBehaviour
{

    private PhotonView PV;

    int numOfPlayers;
    GameObject myPlayer;

    // Start is called before the first frame update
    void Start()
    {
        numOfPlayers = Com.MorganHouston.ZombCube.NetworkGameManager.Instance.playersSpawned;
        PV = GetComponent<PhotonView>();

        if (PV.IsMine)
        {
            switch (numOfPlayers)
            {
                case 0:
                    myPlayer = PhotonNetwork.Instantiate("NetworkPlayer",
                        GameSetup.GS.spawnLocations[numOfPlayers].position,
                        GameSetup.GS.spawnLocations[numOfPlayers].rotation, 0);
                    Com.MorganHouston.ZombCube.NetworkGameManager.Instance.CallPlayerSpawned();
                    break;
                case 1:
                    myPlayer = PhotonNetwork.Instantiate("NetworkPlayer",
                        GameSetup.GS.spawnLocations[numOfPlayers].position,
                        GameSetup.GS.spawnLocations[numOfPlayers].rotation, 0);
                    Com.MorganHouston.ZombCube.NetworkGameManager.Instance.CallPlayerSpawned();
                    break;
                case 2:
                    myPlayer = PhotonNetwork.Instantiate("NetworkPlayer",
                        GameSetup.GS.spawnLocations[numOfPlayers].position,
                        GameSetup.GS.spawnLocations[numOfPlayers].rotation, 0);
                    Com.MorganHouston.ZombCube.NetworkGameManager.Instance.CallPlayerSpawned();
                    break;
                case 3:
                    myPlayer = PhotonNetwork.Instantiate("NetworkPlayer",
                        GameSetup.GS.spawnLocations[numOfPlayers].position,
                        GameSetup.GS.spawnLocations[numOfPlayers].rotation, 0);
                    Com.MorganHouston.ZombCube.NetworkGameManager.Instance.CallPlayerSpawned();
                    break;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
