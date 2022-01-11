using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.CloudSave;
using TMPro;

namespace Com.MorganHouston.ZombCube
{

    public class MainManager : MonoBehaviour
    {

        [Tooltip("The players data object.")]
        private Player player;

        private Dictionary<string, object> analyticParams;

        public TMP_InputField playerNameText;

        /// <summary>
        /// Tries to load the players data.
        /// </summary>
        void Awake()
        {
            player = GameObject.FindWithTag("PlayerData").GetComponent<Player>();

            CloudSaveSample.CloudSaveSample.Instance.SignIn();

            

        }

        private void Start()
        {
            analyticParams = new Dictionary<string, object>();
            analyticParams.Add("PlayerName", player.playerName);

            CustomAnalytics.SendPlayerName(analyticParams);
        }


        // Update is called once per frame
        void Update()
        {
            playerNameText.text = player.playerName;
        }


        /// <summary>
        /// Starts solo version of game.
        /// </summary>
        public async void StartSoloGame()
        {
            await CloudSaveSample.CloudSaveSample.Instance.SavePlayerData(SaveSystem.LoadPlayer());
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

        public void CallStoreVisit()
        {
            CustomAnalytics.StoreVisit();
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
