using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.MorganHouston.ZombCube
{

    public class MainManager : MonoBehaviour
    {
        [Tooltip("The players data object.")]
        public Player player;

        /// <summary>
        /// Tries to load the players data.
        /// </summary>
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


        /// <summary>
        /// Starts solo version of game.
        /// </summary>
        public void StartSoloGame()
        {
            SceneLoader.PlayGame();
        }

        /// <summary>
        /// Starts Pun multiplayer if player name field is not empty.
        /// Loads the loading screne.
        /// </summary>
        public void StartMultiplayer()
        {
            if (string.IsNullOrEmpty(player.playerName))
            {
                Debug.LogError("Player Name is null or empty!");
                return;
            }
            SceneLoader.ToLoading();
        }

        /// <summary>
        /// Saves the players data to file.
        /// </summary>
        public void SavePlayerData()
        {
            SaveSystem.SavePlayer(player);
        }

        /// <summary>
        /// Loads the players data and sets it to the player.
        /// </summary>
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

}
