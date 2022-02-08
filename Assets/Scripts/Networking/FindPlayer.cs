using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public static class FindPlayer
{
    private static GameObject[] players;
    public static GameObject currentPlayer;
    private static int myID = PhotonNetwork.LocalPlayer.ActorNumber;

    public static GameObject GetPlayer()
    {
        players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            int playerID = player.GetComponent<PhotonView>().Owner.ActorNumber;
            if (playerID == myID)
            {
                currentPlayer = player;
            }
        }
        return currentPlayer;
    }
}
