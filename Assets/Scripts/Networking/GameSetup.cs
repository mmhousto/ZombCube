using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Com.GCTC.ZombCube;

public class GameSetup : MonoBehaviour
{
    public static GameSetup GS;

    public Transform[] spawnLocations;


    private void OnEnable()
    {
        if(GS == null)
        {
            GS = this;
        }
    }


}
