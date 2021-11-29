using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Com.MorganHouston.ZombCube;

public class GameSetup : MonoBehaviourPun
{
    public static GameSetup GS;

    public Transform[] spawnLocations;


    private void OnEnable()
    {
        if(GameSetup.GS == null)
        {
            GameSetup.GS = this;
        }
    }


}
