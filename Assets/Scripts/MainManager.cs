using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.CloudSave;
using TMPro;
using UnityEngine.UI;

namespace Com.GCTC.ZombCube
{

    public class MainManager : MonoBehaviour
    {

        [Tooltip("The players data object.")]
        private Player player;

        private Dictionary<string, object> analyticParams;

        public TMP_InputField playerNameText;
        
        public GameObject iapButton;

        public Slider horizontalSens, verticalSens;

        private bool reportedNGamer;

        /// <summary>
        /// Tries to load the players data.
        /// </summary>
        void Awake()
        {
#if (UNITY_XBOXONE || UNITY_PS4 || UNITY_WSA || UNITY_WAS_10_0 || UNITY_WEBGL || UNITY_STANDALONE_WIN || UNITY_STADALONE_OSX)
            iapButton.SetActive(false);
#endif
        }

        private void Start()
        {
            player = Player.Instance;

            analyticParams = new Dictionary<string, object>();
            analyticParams.Add("PlayerName", player.playerName);

            CustomAnalytics.SendPlayerName(analyticParams);

            if(horizontalSens)
                horizontalSens.value = PreferencesManager.GetHorizontalSens();
            if(verticalSens)
                verticalSens.value = PreferencesManager.GetVerticalSens();

            playerNameText.text = player.playerName;

            if (player.playerName == "NGamer1" && Social.localUser.authenticated && reportedNGamer == false)
            {
                LeaderboardManager.UnlockNGamer1();
                reportedNGamer = true;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if(playerNameText.text != player.playerName)
                playerNameText.text = player.playerName;

            CheckNGamer1();
        }

        public void ChangeHorizontalSens(float sensitivty)
        {
            PreferencesManager.SetHorizontalSens(sensitivty);
        }

        public void ChangeVerticalSens(float sensitivity)
        {
            PreferencesManager.SetVerticalSens(sensitivity);
        }

        /// <summary>
        /// Sets playerName to name and saves the player data.
        /// </summary>
        /// <param name="name">The Name Input Field</param>
        public void SetPlayerName(string name)
        {
            if (player)
            {
                player.playerName = name;
            }

        }

        /// <summary>
        /// Starts solo version of game.
        /// </summary>
        public void StartSoloGame()
        {
            CloudSaveLogin.Instance.SaveCloudData();
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
            CloudSaveLogin.Instance.SaveCloudData();
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
            try
            {
                SaveSystem.SavePlayer(player);
            }
            catch
            {
                Debug.Log("Failed to save data.");
            }
        }

        private void CheckNGamer1()
        {
            if (player.playerName == "NGamer1" && Social.localUser.authenticated && reportedNGamer == false)
            {
                LeaderboardManager.UnlockNGamer1();
                reportedNGamer = true;
            }
        }

    }

}
