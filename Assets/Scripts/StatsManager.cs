using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using GooglePlayGames;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;

namespace Com.GCTC.ZombCube
{

    public class StatsManager : MonoBehaviour
    {

        public TextMeshProUGUI userIdLabel, userNameLabel, playerNameLabel, cubesDestroyedLabel,
            currentBlasterLabel, currentSkinLabel, soloWaveLabel, partyWaveLabel, projectilesLabel, totalPointsLabel;

        public GameObject gameCenterButton, leaderboardsButton, achievementsButton, logoutButton;

        private void Start()
        {
            SetButtons();
        }

        private void OnEnable()
        {
            SetLabels();
        }

        public void ShowAchievements()
        {
            if (Social.localUser.authenticated)
            {
                Social.ShowAchievementsUI();
            }
        }

        public void LogoutUser()
        {
            CloudSaveLogin.Instance.Logout();
        }

        public void ShowLeaderboard()
        {
            if(Social.localUser.authenticated)
            {
                Social.ShowLeaderboardUI();
            }
        }

        public void DeleteAccount()
        {
            CloudSaveLogin.Instance.DeleteAccount();
        }

        private void SetButtons()
        {
#if (UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_TVOS || UNITY_ANDROID)
            if (Social.localUser.authenticated)
            {
                gameCenterButton.SetActive(false);
                leaderboardsButton.SetActive(true);
                achievementsButton.SetActive(true);
            }
            else
            {
                gameCenterButton.SetActive(true);
                leaderboardsButton.SetActive(false);
                achievementsButton.SetActive(false);
            }
#else
            gameCenterButton.SetActive(false);
            leaderboardsButton.SetActive(false);
            achievementsButton.SetActive(false);
#endif

            if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                logoutButton.SetActive(false);
            }
            else
            {
                logoutButton.SetActive(true);
            }

        }

        public void SignInGameCenter()
        {
            CloudSaveLogin.Instance.SignInGameCenter();

            if (Social.localUser.authenticated)
            {
                gameCenterButton.SetActive(false);
                leaderboardsButton.SetActive(true);
                achievementsButton.SetActive(true);
            }
        }

        private void SetLabels()
        {
            userIdLabel.text = Player.Instance.userID.Substring(0, 25 > Player.Instance.userID.Length ? Player.Instance.userID.Length : 25);
            userNameLabel.text = Player.Instance.userName.Substring(0, 25 > Player.Instance.userName.Length ? Player.Instance.userName.Length : 25);
            playerNameLabel.text = Player.Instance.playerName.Substring(0, 25>Player.Instance.playerName.Length?Player.Instance.playerName.Length:25);
            cubesDestroyedLabel.text = $"{Player.Instance.cubesEliminated}";

            currentBlasterLabel.text = $"{GetColor(Player.Instance.currentBlaster)}";
            currentSkinLabel.text = $"{GetColor(Player.Instance.currentSkin)}";

            soloWaveLabel.text = $"{Player.Instance.highestWave}";
            partyWaveLabel.text = $"{Player.Instance.highestWaveParty}";
            projectilesLabel.text = $"{Player.Instance.totalProjectilesFired}";
            totalPointsLabel.text = $"{Player.Instance.totalPointsEarned}";
        }

        private string GetColor(int index)
        {
            string result = "";
            switch (index)
            {
                case 0:
                    result = "White";
                    break;
                case 1:
                    result = "Green";
                    break;
                case 2:
                    result = "Blue";
                    break;
                case 3:
                    result = "Yellow";
                    break;
                case 4:
                    result = "Pink";
                    break;
                case 5:
                    result = "Legend";
                    break;
                case 6:
                    result = "OSU";
                    break;
                case 7:
                    result = "OU";
                    break;
                case 8:
                    result = "Gold";
                    break;
                default:
                    result = "White";
                    break;
            }
            return result;
        }
    }

}
