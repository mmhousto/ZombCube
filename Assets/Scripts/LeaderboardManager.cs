#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_ANDROID
using GooglePlayGames;
#endif
using UnityEngine.SocialPlatforms;

namespace Com.GCTC.ZombCube
{
    public static class LeaderboardManager
    {
        public static void UpdateMostPointsLeaderboard()
        {
            if(CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportScore(Player.Instance.totalPointsEarned, "CgkI07-ynroOEAIQBQ", (bool success) => {
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
                SteamLeaderboardManager.Instance.UpdateScore(Player.Instance.cubesEliminated * 100 / Player.Instance.totalProjectilesFired, SteamLeaderboardManager.LeaderboardName.BestAccuracy);
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
            else if(CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("stayin_alive_solo", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
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
#if !DISABLESTEAMWORKS
            else if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Steam)
            {
                SteamStatsAndAchievements.Instance.UnlockNGamer1();
            }
#endif
        }

    }
}
