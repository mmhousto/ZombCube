#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using PSNSample;
using UnityEngine;
#if UNITY_ANDROID
using GooglePlayGames;
#endif

namespace Com.GCTC.ZombCube
{
    public static class LeaderboardManager
    {
#if !DISABLESTEAMWORKS
        public static void UpdateSteamLeaderboards()
        {
            if (Player.Instance != null)
            {
                SteamLeaderboardManager.Instance.UpdateScore(Player.Instance.totalPointsEarned, SteamLeaderboardManager.LeaderboardName.MostPoints);
                SteamLeaderboardManager.Instance.UpdateScore(Player.Instance.highestWave, SteamLeaderboardManager.LeaderboardName.HighestSoloWave);
                SteamLeaderboardManager.Instance.UpdateScore(Player.Instance.highestWaveParty, SteamLeaderboardManager.LeaderboardName.HighestPartyWave);
                SteamLeaderboardManager.Instance.UpdateScore(Player.Instance.cubesEliminated, SteamLeaderboardManager.LeaderboardName.CubesDestroyed);
                SteamLeaderboardManager.Instance.UpdateScore((Player.Instance.totalProjectilesFired != 0) ? Player.Instance.cubesEliminated * 100 / Player.Instance.totalProjectilesFired : 0, SteamLeaderboardManager.LeaderboardName.BestAccuracy);
            }

        }
#endif

        public static void UpdatePSNStats(Player player)
        {
            if (player != null)
            {
                PSTrophies.IncreaseProgressStat(player.totalPointsEarned, "UpdateTotalPoints", "custom");
                PSTrophies.IncreaseProgressStat(player.coins, "UpdateCoins", "custom");
                PSTrophies.IncreaseProgressStat(player.highestWave, "UpdateWaveSolo", "custom");
                PSTrophies.IncreaseProgressStat(player.highestWaveParty, "UpdateWaveParty", "custom");
                PSTrophies.IncreaseProgressStat(player.cubesEliminated, "UpdateCubesDestroyed", "custom");
                PSTrophies.IncreaseProgressStat(player.totalProjectilesFired, "UpdateProjectilesFired", "custom");
            }
            
        }

        public static void UpdateMostPointsLeaderboard()
        {
            if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportScore(Player.Instance.totalPointsEarned, "CgkI07-ynroOEAIQBQ", (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportScore(Player.Instance.totalPointsEarned, "most_points", (bool success) =>
                {
                    // handle success or failure
                });
            }
#if !DISABLESTEAMWORKS
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Steam)
            {
                SteamLeaderboardManager.Instance.UpdateScore(Player.Instance.totalPointsEarned, SteamLeaderboardManager.LeaderboardName.MostPoints);
            }
#endif
        }

        public static void UpdateSoloHighestWaveLeaderboard()
        {
            if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportScore(Player.Instance.highestWave, "CgkI07-ynroOEAIQAQ", (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportScore(Player.Instance.highestWave, "highest_wave_on_starting_level", (bool success) =>
                {
                    // handle success or failure
                });
            }
#if !DISABLESTEAMWORKS
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Steam)
            {
                SteamLeaderboardManager.Instance.UpdateScore(Player.Instance.highestWave, SteamLeaderboardManager.LeaderboardName.HighestSoloWave);
            }
#endif
        }

        public static void UpdatePartyHighestWaveLeaderboard()
        {
            if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportScore(Player.Instance.highestWaveParty, "CgkI07-ynroOEAIQAg", (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportScore(Player.Instance.highestWaveParty, "highest_party_wave_on_starting_level", (bool success) =>
                {
                    // handle success or failure
                });
            }
#if !DISABLESTEAMWORKS
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Steam)
            {
                SteamLeaderboardManager.Instance.UpdateScore(Player.Instance.highestWaveParty, SteamLeaderboardManager.LeaderboardName.HighestPartyWave);
            }
#endif
        }

        public static void UpdateCubesDestroyedLeaderboard()
        {
            if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportScore(Player.Instance.cubesEliminated, "CgkI07-ynroOEAIQBA", (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportScore(Player.Instance.cubesEliminated, "cubes_destroyed", (bool success) =>
                {
                    // handle success or failure
                });
            }
#if !DISABLESTEAMWORKS
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Steam)
            {
                SteamLeaderboardManager.Instance.UpdateScore(Player.Instance.cubesEliminated, SteamLeaderboardManager.LeaderboardName.CubesDestroyed);
            }
#endif
        }

        public static void UpdateAccuracyLeaderboard()
        {
            if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportScore(Player.Instance.cubesEliminated * 100 / Player.Instance.totalProjectilesFired, "CgkI07-ynroOEAIQAw", (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportScore(Player.Instance.cubesEliminated * 100 / Player.Instance.totalProjectilesFired, "best_accuracy", (bool success) =>
                {
                    // handle success or failure
                });
            }
#if !DISABLESTEAMWORKS
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Steam)
            {
                SteamLeaderboardManager.Instance.UpdateScore((Player.Instance.totalProjectilesFired != 0) ? Player.Instance.cubesEliminated * 100 / Player.Instance.totalProjectilesFired : 0, SteamLeaderboardManager.LeaderboardName.BestAccuracy);
            }
#endif
        }

        public static void UnlockStayinAlive()
        {
            if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQBg", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("stayin_alive_solo", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
#if UNITY_PS5
            else if(CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.PS)
            {
                PSTrophies.UnlockTrophy((int)PSTrophies.Trophies.StayinAliveSolo);
            }
#endif
        }

        public static void UnlockStayinAliveTogether()
        {
            if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQDw", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("stayin_alive_party", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
#if UNITY_PS5
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.PS)
            {
                PSTrophies.UnlockTrophy((int)PSTrophies.Trophies.StayinAliveParty);
            }
#endif
        }

        public static void UnlockCubeDestroyerI()
        {
            if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQBw", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("cubes_destroyed_i", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
#if UNITY_PS5
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.PS)
            {
                PSTrophies.UnlockTrophy((int)PSTrophies.Trophies.CubeDestroyerI);
            }
#endif
        }

        public static void UnlockCubeDestroyerII()
        {
            if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQCA", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("cubes_destroyed_ii", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
#if UNITY_PS5
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.PS)
            {
                PSTrophies.UnlockTrophy((int)PSTrophies.Trophies.CubeDestroyerII);
            }
#endif
        }

        public static void UnlockCubeDestroyerIII()
        {
            if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQCQ", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("cubes_destroyed_iii", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
#if UNITY_PS5
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.PS)
            {
                PSTrophies.UnlockTrophy((int)PSTrophies.Trophies.CubeDestroyerIII);
            }
#endif
        }

        public static void UnlockRicochetKing()
        {
            if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQCg", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("ricochet_king", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
#if UNITY_PS5
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.PS)
            {
                PSTrophies.UnlockTrophy((int)PSTrophies.Trophies.RicochetKing);
            }
#endif
#if !DISABLESTEAMWORKS
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Steam)
            {
                SteamStatsAndAchievements.Instance.UnlockRicochetKing();
            }
#endif
        }

        public static void UnlockTriggerHappyI()
        {
            if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQCw", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("trigger_happy_i", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
#if UNITY_PS5
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.PS)
            {
                PSTrophies.UnlockTrophy((int)PSTrophies.Trophies.TriggerHappyI);
            }
#endif
        }

        public static void UnlockTriggerHappyII()
        {
            if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQDA", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("trigger_happy_ii", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
#if UNITY_PS5
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.PS)
            {
                PSTrophies.UnlockTrophy((int)PSTrophies.Trophies.TriggerHappyII);
            }
#endif
        }

        public static void UnlockTriggerHappyIII()
        {
            if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQDQ", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("trigger_happy_iii", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
#if UNITY_PS5
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.PS)
            {
                PSTrophies.UnlockTrophy((int)PSTrophies.Trophies.TriggerHappyIII);
            }
#endif
        }

        public static void UnlockNGamer1()
        {
            if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQDg", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("n_gamer_1", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
#if UNITY_PS5
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.PS)
            {
                PSTrophies.UnlockTrophy((int)PSTrophies.Trophies.Hidden);
            }
#endif
#if !DISABLESTEAMWORKS
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Steam)
            {
                SteamStatsAndAchievements.Instance.UnlockNGamer1();
            }
#endif
        }

        public static void UnlockPointRackerI()
        {
            if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQDQ", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("point_racker_i", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
#if UNITY_PS5
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.PS)
            {
                PSTrophies.UnlockTrophy((int)PSTrophies.Trophies.PointRackerI);
            }
#endif
        }

        public static void UnlockPointRackerII()
        {
            if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQDQ", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("point_racker_ii", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
#if UNITY_PS5
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.PS)
            {
                PSTrophies.UnlockTrophy((int)PSTrophies.Trophies.PointRackerII);
            }
#endif
        }

        public static void UnlockPointRackerIII()
        {
            if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQDQ", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("point_racker_iii", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
#if UNITY_PS5
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.PS)
            {
                PSTrophies.UnlockTrophy((int)PSTrophies.Trophies.PointRackerIII);
            }
#endif
        }

        public static void UnlockLetsPlay()
        {
            if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQDg", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("lets_play", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
#if UNITY_PS5
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.PS)
            {
                PSTrophies.UnlockTrophy((int)PSTrophies.Trophies.LetsPlay);
            }
#endif
#if !DISABLESTEAMWORKS
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Steam)
            {
                SteamStatsAndAchievements.Instance.UnlockLetsPlay();
            }
#endif
        }

        public static void UnlockLetsPlayTogether()
        {
            if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQDg", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("lets_play_together", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
#if UNITY_PS5
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.PS)
            {
                PSTrophies.UnlockTrophy((int)PSTrophies.Trophies.LetsPlayTogether);
            }
#endif
#if !DISABLESTEAMWORKS
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Steam)
            {
                SteamStatsAndAchievements.Instance.UnlockLetsPlayTogether();
            }
#endif
        }

        public static void UnlockLetsParty()
        {
            if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQDg", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("lets_party", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
#if UNITY_PS5
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.PS)
            {
                PSTrophies.UnlockTrophy((int)PSTrophies.Trophies.LetsParty);
            }
#endif
#if !DISABLESTEAMWORKS
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Steam)
            {
                SteamStatsAndAchievements.Instance.UnlockLetsParty();
            }
#endif
        }

    }
}
