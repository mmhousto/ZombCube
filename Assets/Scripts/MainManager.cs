using System.Collections;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.SaveData.PS5.Core;

namespace Com.GCTC.ZombCube
{

    public class MainManager : MonoBehaviour
    {

        [Tooltip("The players data object.")]
        private Player player;

        private Dictionary<string, object> analyticParams;

        public TMP_InputField playerNameText;

        public GameObject iapButton, iapButton2;

        public Slider horizontalSens, verticalSens;

        private bool reportedNGamer;

        /// <summary>
        /// Tries to load the players data.
        /// </summary>
        void Awake()
        {
#if (UNITY_XBOXONE || UNITY_PS4 || UNITY_PS5 || UNITY_WSA || UNITY_WAS_10_0 || UNITY_WEBGL || UNITY_STANDALONE_WIN || UNITY_STADALONE_OSX)
            iapButton.SetActive(false);
            iapButton2.SetActive(false);
#endif
        }

        private void Start()
        {
            player = Player.Instance;

            analyticParams = new Dictionary<string, object>();
            analyticParams.Add("PlayerName", player.playerName);

#if !UNITY_PLAYSTATION
            CustomAnalytics.SendPlayerName(analyticParams);
#endif

            if (horizontalSens)
                horizontalSens.value = PreferencesManager.GetHorizontalSens();
            if (verticalSens)
                verticalSens.value = PreferencesManager.GetVerticalSens();

            playerNameText.text = player.playerName;

            if (player.playerName == "NGamer1" && (Social.localUser.authenticated || CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Steam || CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.PS) && reportedNGamer == false)
            {
                LeaderboardManager.UnlockNGamer1();
                reportedNGamer = true;
            }

            if ((Social.localUser.authenticated || CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Steam || CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.PS) && player != null)
            {
                if (player.cubesEliminated >= 10000)
                {
                    LeaderboardManager.UnlockCubeDestroyerI();
                }

                if (player.cubesEliminated >= 100000)
                {
                    LeaderboardManager.UnlockCubeDestroyerII();
                }

                if (player.cubesEliminated >= 1000000)
                {
                    LeaderboardManager.UnlockCubeDestroyerIII();
                }

                if (player.totalProjectilesFired >= 100000)
                {
                    LeaderboardManager.UnlockTriggerHappyI();
                }

                if (player.totalProjectilesFired >= 1000000)
                {
                    LeaderboardManager.UnlockTriggerHappyII();
                }

                if (player.totalProjectilesFired >= 10000000)
                {
                    LeaderboardManager.UnlockTriggerHappyIII();
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (playerNameText.text != player.playerName)
                playerNameText.text = player.playerName;
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
#if UNITY_PS5 && !UNITY_EDITOR
            PSUDS.PostUDSStartEvent("activitySolo");
            PSUDS.PostUDSStartEvent("activityBossCube");
#endif
            CheckNGamer1();

            GameManager.mode = 0;
            CloudSaveLogin.Instance.SaveCloudData();
            SceneLoader.PlayGame();
        }

        /// <summary>
        /// Starts solo version of game.
        /// </summary>
        public void StartCoopGame()
        {
            CheckNGamer1();
            GameManager.mode = 1;
            CloudSaveLogin.Instance.SaveCloudData();
            SceneLoader.PlayGame();
        }

        /// <summary>
        /// Starts Pun multiplayer if player name field is not empty.
        /// Loads the loading screne.
        /// </summary>
        public void StartMultiplayer()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                ErrorManager.Instance.StartErrorMessage("Network Error: Not connected to the internet.");
                return;
            }

#if UNITY_PS5 && !UNITY_EDITOR
            if (PSFeatureGating.hasPremium == false)
            {
                //NO PREMIUM - Notify Player
                PSCommerce.OpenJoinPremium();
                return;
            }
#endif
            CheckNGamer1();

            if (string.IsNullOrEmpty(player.playerName))
            {
                ErrorManager.Instance.StartErrorMessage("Error: Player Name is null or empty!");
                return;
            }
            CloudSaveLogin.Instance.SaveCloudData();
            SceneLoader.ToLoading();
        }

        public void CallStoreVisit()
        {
#if !UNITY_PLAYSTATION
            CustomAnalytics.StoreVisit();
#endif
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
                ErrorManager.Instance.StartErrorMessage("Error: Failed to save data.");
            }
        }

        private void CheckNGamer1()
        {
            if (player.playerName == "NGamer1" && (Social.localUser.authenticated || CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Steam || CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.PS) && reportedNGamer == false)
            {
                LeaderboardManager.UnlockNGamer1();
                reportedNGamer = true;
            }
        }

        private void NoInternet(bool isConnected)
        {
            if (!isConnected)
            {
                ErrorManager.Instance.StartErrorMessage("Network Error: Not connected to the internet.");
            }
        }

        IEnumerator checkInternetConnection(Action<bool> action)
        {
            WWW www = new WWW("http://google.com");
            yield return www;
            if (www.error != null)
            {
                action(false);
            }
            else
            {
                action(true);
            }
        }

    }

}
