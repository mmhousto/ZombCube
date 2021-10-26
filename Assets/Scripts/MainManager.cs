using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainManager : MonoBehaviour
{

    public Player player;

    // Start is called before the first frame update
    void Awake()
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
        SaveSystem.SavePlayer(player);
    }

    public void LoadPlayerData()
    {
        SaveData data = SaveSystem.LoadPlayer();

        player.playerName = data.playerName;
        player.coins = data.coins;
        player.points = data.points;
        player.highestWave = data.highestWave;
        player.currentBlaster = data.currentBlaster;
        player.ownedBlasters = data.ownedBlasters;
    }
}
