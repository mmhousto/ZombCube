using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Com.MorganHouston.ZombCube;

public class SpawnPlayers : MonoBehaviourPun
{
    public GameObject playerPrefab;
    public Transform[] spawnLocations;

    Photon.Realtime.Player[] allPlayers;
    int index;
    GameObject myPlayer;
    PhotonView pv;


    // Start is called before the first frame update
    void Start()
    {
        pv = GetComponent<PhotonView>();

        allPlayers = PhotonNetwork.PlayerList;
        foreach(Photon.Realtime.Player player in allPlayers)
        {
            if(player != PhotonNetwork.LocalPlayer)
            {
                index++;
            }
        }
        Debug.Log(index);
        if (pv.IsMine)
        {
            myPlayer = PhotonNetwork.Instantiate(this.playerPrefab.name, spawnLocations[index].position, spawnLocations[index].rotation, 0);
        }
        
         
    }


}
