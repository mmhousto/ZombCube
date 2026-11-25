using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
/*#if UNITY_ANDROID
using GooglePlayGames;
#endif*/
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;

namespace Com.GCTC.ZombCube
{

    public class StatsManager : MonoBehaviour
    {

        public TextMeshProUGUI userIdLabel, userNameLabel, playerNameLabel, cubesDestroyedLabel,
            currentBlasterLabel, currentSkinLabel, soloWaveLabel, partyWaveLabel, projectilesLabel, totalPointsLabel;

        public GameObject gameCenterButton, leaderboardsButton, achievementsButton, logoutButton, deleteButton;

        private StatsScreens screens;

        private void Start()
        {
            screens = StatsScreens.Create(this);
            SetButtons();
        }

        private void OnEnable()
        {
            SetLabels();
            if (screens != null)
            {
                screens.ResetTabs();
                SetButtons();
            }
        }

        // In-game page for everyone (it has a GAME CENTER button for Apple players); Apple UI as a fallback.
        public void ShowAchievements()
        {
            if (screens != null)
                screens.OpenAchievements();
            else if (LeaderboardManager.HasNativeUI)
                Social.ShowAchievementsUI();
        }

        public void LogoutUser()
        {
            CloudSaveLogin.Instance.Logout();
        }

        public void ShowLeaderboard()
        {
            if (screens != null)
                screens.OpenLeaderboards();
            else if (LeaderboardManager.HasNativeUI)
                Social.ShowLeaderboardUI();
        }

        public void DeleteAccount()
        {
            CloudSaveLogin.Instance.DeleteAccount();
        }

        public void SetButtons()
        {
            // Achievements are always available (Game Center UI, or the in-game page for everyone else).
            // Leaderboards need Game Center or Steam.
            bool steamBoards = LeaderboardManager.HasSteamLeaderboards && screens != null;
#if (UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_TVOS)
            bool gameCenter = Social.localUser.authenticated;
            gameCenterButton.SetActive(!gameCenter && !steamBoards);
            leaderboardsButton.SetActive(gameCenter || steamBoards);
#else
            gameCenterButton.SetActive(false);
            leaderboardsButton.SetActive(steamBoards);
#endif
            achievementsButton.SetActive(LeaderboardManager.HasNativeUI || screens != null);

            if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Apple)
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

            SetButtons();
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
            CosmeticEntry entry = CosmeticDatabase.Instance != null ? CosmeticDatabase.Instance.Get(index) : null;
            return entry != null && !string.IsNullOrEmpty(entry.displayName) ? entry.displayName : "White";
        }
    }

}
