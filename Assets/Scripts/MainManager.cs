using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainManager : MonoBehaviour
{

    // Start is called before the first frame update
    void Start()
    {
        try
        {
            LoadPlayerData();
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
        
    }



    // Update is called once per frame
    void Update()
    {
        
    }


    public void StartSoloGame()
    {
        SceneLoader.PlayGame();
    }

    public void SavePlayerData()
    {
        SaveSystem.SavePlayer(Player.Instance);
    }

    public void LoadPlayerData()
    {
        SaveData data = SaveSystem.LoadPlayer();

        Player.Instance.playerName = data.playerName;
        Player.Instance.coins = data.coins;
        Player.Instance.points = data.points;
        Player.Instance.highestWave = data.highestWave;
        Player.Instance.currentBlaster = data.currentBlaster;
        Player.Instance.ownedBlasters = data.ownedBlasters;
    }
}
