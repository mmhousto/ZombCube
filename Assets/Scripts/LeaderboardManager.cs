#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using UnityEngine;
/*#if UNITY_ANDROID
using GooglePlayGames;
#endif*/

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
                foreach (WaveBoard board in WaveLeaderboards.All)
                {
                    int best = Player.Instance.BestWave(board.LevelId, board.Mode);
                    if (best > 0) SteamLeaderboardManager.Instance.UpdateScore(best, board.SteamName);
                }
                SteamLeaderboardManager.Instance.UpdateScore(Player.Instance.cubesEliminated, SteamLeaderboardManager.LeaderboardName.CubesDestroyed);
                SteamLeaderboardManager.Instance.UpdateScore((Player.Instance.totalProjectilesFired != 0) ? Player.Instance.cubesEliminated * 100 / Player.Instance.totalProjectilesFired : 0, SteamLeaderboardManager.LeaderboardName.BestAccuracy);
            }

        }
#endif

        public static void UpdateMostPointsLeaderboard()
        {
            if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportScore(Player.Instance.totalPointsEarned, "CgkI07-ynroOEAIQBQ", (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportScore(Player.Instance.totalPointsEarned, "most_points", (bool success) =>
                {
                    // handle success or failure
                });
            }
#if !DISABLESTEAMWORKS
            else if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Steam)
            {
                SteamLeaderboardManager.Instance.UpdateScore(Player.Instance.totalPointsEarned, SteamLeaderboardManager.LeaderboardName.MostPoints);
            }
#endif
        }

        /// <summary>
        /// Reports the player's best wave on a map in a mode (Solo, Co-op or Party) to the
        /// signed-in platform's leaderboard for it. Boards a platform doesn't have are skipped.
        /// </summary>
        public static void UpdateWaveLeaderboard(string levelId, WaveMode mode)
        {
            WaveBoard board = WaveLeaderboards.For(levelId, mode);
            if (board == null || Player.Instance == null || CloudSaveLogin.Instance == null) return;
            int best = Player.Instance.BestWave(levelId, mode);
            if (best <= 0) return;

            if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Google)
            {
                if (board.GoogleId != null)
                    Social.ReportScore(best, board.GoogleId, (bool success) => { });
            }
            else if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                if (board.AppleId != null)
                    Social.ReportScore(best, board.AppleId, (bool success) => { });
            }
#if !DISABLESTEAMWORKS
            else if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Steam)
            {
                if (board.SteamName != null && SteamLeaderboardManager.Instance != null)
                    SteamLeaderboardManager.Instance.UpdateScore(best, board.SteamName);
            }
#endif
        }

        public static void UpdateCubesDestroyedLeaderboard()
        {
            if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportScore(Player.Instance.cubesEliminated, "CgkI07-ynroOEAIQBA", (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportScore(Player.Instance.cubesEliminated, "cubes_destroyed", (bool success) =>
                {
                    // handle success or failure
                });
            }
#if !DISABLESTEAMWORKS
            else if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Steam)
            {
                SteamLeaderboardManager.Instance.UpdateScore(Player.Instance.cubesEliminated, SteamLeaderboardManager.LeaderboardName.CubesDestroyed);
            }
#endif
        }

        public static void UpdateAccuracyLeaderboard()
        {
            if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportScore(Player.Instance.cubesEliminated * 100 / Player.Instance.totalProjectilesFired, "CgkI07-ynroOEAIQAw", (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportScore(Player.Instance.cubesEliminated * 100 / Player.Instance.totalProjectilesFired, "best_accuracy", (bool success) =>
                {
                    // handle success or failure
                });
            }
#if !DISABLESTEAMWORKS
            else if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Steam)
            {
                SteamLeaderboardManager.Instance.UpdateScore((Player.Instance.totalProjectilesFired != 0) ? Player.Instance.cubesEliminated * 100 / Player.Instance.totalProjectilesFired : 0, SteamLeaderboardManager.LeaderboardName.BestAccuracy);
            }
#endif
        }

        // Every unlock is also saved locally, so the in-game Achievements page works for all players.
        private static void RecordLocal(string id)
        {
            if (Player.Instance != null) Player.Instance.UnlockAchievement(id);
        }

        /// <summary>True when the platform's own achievements/leaderboards UI (Game Center) is available.</summary>
        public static bool HasNativeUI
        {
            get
            {
#if (UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_TVOS)
                return Social.localUser.authenticated;
#else
                return false;
#endif
            }
        }

        /// <summary>True when the in-game leaderboards can show online scores (signed in with Steam).</summary>
        public static bool HasSteamLeaderboards
        {
            get
            {
#if !DISABLESTEAMWORKS
                return SteamManager.IsReady && CloudSaveLogin.Instance != null
                    && CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Steam
                    && SteamLeaderboardManager.Instance != null;
#else
                return false;
#endif
            }
        }

        public static void UnlockStayinAlive()
        {
            RecordLocal("stayin_alive_solo");
            if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQBg", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("stayin_alive_solo", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
        }

        public static void UnlockStayinAliveTogether()
        {
            RecordLocal("stayin_alive_party");
            if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQDw", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("stayin_alive_party", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
        }

        public static void UnlockCubeDestroyerI()
        {
            RecordLocal("cube_destroyer_i");
            if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQBw", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("cubes_destroyed_i", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
        }

        public static void UnlockCubeDestroyerII()
        {
            RecordLocal("cube_destroyer_ii");
            if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQCA", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("cubes_destroyed_ii", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
        }

        public static void UnlockCubeDestroyerIII()
        {
            RecordLocal("cube_destroyer_iii");
            if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQCQ", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("cubes_destroyed_iii", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
        }

        public static void UnlockRicochetKing()
        {
            RecordLocal("ricochet_king");
            if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQCg", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("ricochet_king", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
#if !DISABLESTEAMWORKS
            else if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Steam)
            {
                SteamStatsAndAchievements.Instance.UnlockRicochetKing();
            }
#endif
        }

        public static void UnlockTriggerHappyI()
        {
            RecordLocal("trigger_happy_i");
            if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQCw", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("trigger_happy_i", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
        }

        public static void UnlockTriggerHappyII()
        {
            RecordLocal("trigger_happy_ii");
            if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQDA", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("trigger_happy_ii", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
        }

        public static void UnlockTriggerHappyIII()
        {
            RecordLocal("trigger_happy_iii");
            if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQDQ", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("trigger_happy_iii", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
        }

        public static void UnlockNGamer1()
        {
            RecordLocal("n_gamer_1");
            if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQDg", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("n_gamer_1", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
#if !DISABLESTEAMWORKS
            else if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Steam)
            {
                SteamStatsAndAchievements.Instance.UnlockNGamer1();
            }
#endif
        }

        public static void UnlockPointRackerI()
        {
            RecordLocal("point_racker_i");
            if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQDQ", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("point_racker_i", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
        }

        public static void UnlockPointRackerII()
        {
            RecordLocal("point_racker_ii");
            if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQDQ", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("point_racker_ii", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
        }

        public static void UnlockPointRackerIII()
        {
            RecordLocal("point_racker_iii");
            if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQDQ", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("point_racker_iii", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
        }

        public static void UnlockLetsPlay()
        {
            RecordLocal("lets_play");
            if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQDg", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("lets_play", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
#if !DISABLESTEAMWORKS
            else if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Steam)
            {
                SteamStatsAndAchievements.Instance.UnlockLetsPlay();
            }
#endif
        }

        public static void UnlockLetsPlayTogether()
        {
            RecordLocal("lets_play_together");
            if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQDg", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("lets_play_together", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
#if !DISABLESTEAMWORKS
            else if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Steam)
            {
                SteamStatsAndAchievements.Instance.UnlockLetsPlayTogether();
            }
#endif
        }

        public static void UnlockLetsParty()
        {
            RecordLocal("lets_party");
            if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Google)
            {
                Social.ReportProgress("CgkI07-ynroOEAIQDg", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
            else if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Apple)
            {
                Social.ReportProgress("lets_party", 100.0f, (bool success) =>
                {
                    // handle success or failure
                });
            }
#if !DISABLESTEAMWORKS
            else if (CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Steam)
            {
                SteamStatsAndAchievements.Instance.UnlockLetsParty();
            }
#endif
        }

    }
}
