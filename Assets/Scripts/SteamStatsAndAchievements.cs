#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using UnityEngine;
using System.Collections;
using System.ComponentModel;
using Unity.Services.Core;

#if !DISABLESTEAMWORKS
using Steamworks;
#endif
using Unity.Services.Authentication;

namespace Com.GCTC.ZombCube
{
	// This is a port of StatsAndAchievements.cpp from SpaceWar, the official Steamworks Example.
	class SteamStatsAndAchievements : MonoBehaviour
	{
#if !DISABLESTEAMWORKS
		private static SteamStatsAndAchievements instance;

		public static SteamStatsAndAchievements Instance { get { return instance; } }

		private enum Achievement : int
		{
            stayin_alive_solo,
            stayin_alive_party,
            cube_destroyer_i,
            cube_destroyer_ii,
            cube_destroyer_iii,
            ricochet_king,
            trigger_happy_i,
            trigger_happy_ii,
            trigger_happy_iii,
            n_gamer_1
		};

		private Achievement_t[] m_Achievements = new Achievement_t[] {
		new Achievement_t(Achievement.stayin_alive_solo, "Stayin' Alive Alone", "Reach level 50 on the starting level in Solo mode."),
		new Achievement_t(Achievement.stayin_alive_party, "Stayin' Alive Together", "Reach level 50 on the starting level in Party mode."),
		new Achievement_t(Achievement.cube_destroyer_i, "Cube Destroyer I", "Eliminate 10,000 ZombCube's."),
		new Achievement_t(Achievement.cube_destroyer_ii, "Cube Destroyer II", "Eliminate 100,000 ZombCube's."),
		new Achievement_t(Achievement.cube_destroyer_iii, "Cube Destroyer III", "Eliminate 1,000,000 ZombCube's."),
		new Achievement_t(Achievement.ricochet_king, "Ricochet King", "Eliminate 5 ZombCube's with a single projectile."),
		new Achievement_t(Achievement.trigger_happy_i, "Trigger Happy I", "Launch 100,000 Projectiles."),
		new Achievement_t(Achievement.trigger_happy_ii, "Trigger Happy II", "Launch 1,000,000 Projectiles."),
		new Achievement_t(Achievement.trigger_happy_iii, "Trigger Happy III", "Launch 10,000,000 Projectiles."),
		new Achievement_t(Achievement.n_gamer_1, "NGamer1", "Change your player name to NGamer1.")

    };

		// Our GameID
		private CGameID m_GameID;

		// Did we get the stats from Steam?
		private bool m_bRequestedStats;
		private bool m_bStatsValid;

		// Should we store stats this frame?
		private bool m_bStoreStats;

		// Current Stat details
		private float m_flGameFeetTraveled;
		private float m_ulTickCountGameStart;
		private double m_flGameDurationSeconds;

		// Persisted Stat details
		private int m_nUserPoints;
		private int m_nUserCoins;
		private int m_nUserBlaster;
		private int m_nUserSkin;
		private int m_nUserTotalPoints;
		private int m_nHighestWave;
		private int m_nHighestPartyWave;
		private int m_nCubesEliminated;
		private int m_nTotalProjectiles;

		private float m_flAccuracy;

		private Player player;

		protected Callback<UserStatsReceived_t> m_UserStatsReceived;
		protected Callback<UserStatsStored_t> m_UserStatsStored;
		protected Callback<UserAchievementStored_t> m_UserAchievementStored;

        private void Awake()
        {
			if (instance != null && instance != this)
			{
				Destroy(this.gameObject);
				return;
			}
			else
			{
				instance = this;
				DontDestroyOnLoad(Instance.gameObject);
			}
        }

        void Start()
		{
            if (!SteamManager.Initialized || (AuthenticationService.Instance != null && !AuthenticationService.Instance.IsSignedIn) || (CloudSaveLogin.Instance != null && CloudSaveLogin.Instance.currentSSO != CloudSaveLogin.ssoOption.Steam))
				return;

			player = Player.Instance;
			// Cache the GameID for use in the Callbacks
			m_GameID = new CGameID(SteamUtils.GetAppID());

			m_UserStatsReceived = Callback<UserStatsReceived_t>.Create(OnUserStatsReceived);
			m_UserStatsStored = Callback<UserStatsStored_t>.Create(OnUserStatsStored);
			m_UserAchievementStored = Callback<UserAchievementStored_t>.Create(OnAchievementStored);

			// These need to be reset to get the stats upon an Assembly reload in the Editor.
			m_bRequestedStats = false;
			m_bStatsValid = false;
		}

		private void Update()
		{
			if (!SteamManager.Initialized || !AuthenticationService.Instance.IsSignedIn || CloudSaveLogin.Instance.currentSSO != CloudSaveLogin.ssoOption.Steam)
				return;

			if(m_GameID == null)
			{
                player = Player.Instance;
                // Cache the GameID for use in the Callbacks
                m_GameID = new CGameID(SteamUtils.GetAppID());

                m_UserStatsReceived = Callback<UserStatsReceived_t>.Create(OnUserStatsReceived);
                m_UserStatsStored = Callback<UserStatsStored_t>.Create(OnUserStatsStored);
                m_UserAchievementStored = Callback<UserAchievementStored_t>.Create(OnAchievementStored);

                // These need to be reset to get the stats upon an Assembly reload in the Editor.
                m_bRequestedStats = false;
                m_bStatsValid = false;
            }

			if(player == null)
				player = Player.Instance;

			if (!m_bRequestedStats)
			{
				// Is Steam Loaded? if no, can't get stats, done
				if (!SteamManager.Initialized)
				{
					m_bRequestedStats = true;
					return;
				}

				// If yes, request our stats
				bool bSuccess = SteamUserStats.RequestCurrentStats();

				// This function should only return false if we weren't logged in, and we already checked that.
				// But handle it being false again anyway, just ask again later.
				m_bRequestedStats = bSuccess;
			}

			if (!m_bStatsValid)
				return;

			// Get info from sources
			m_nUserPoints = player.points;
			m_nUserCoins = player.coins;
			m_nUserBlaster = player.currentBlaster;
			m_nUserSkin = player.currentSkin;
			m_nUserTotalPoints = player.totalPointsEarned;
			m_nHighestWave = player.highestWave;
			m_nHighestPartyWave = player.highestWaveParty;
			m_nCubesEliminated = player.cubesEliminated;
			m_nTotalProjectiles = player.totalProjectilesFired;

			// Evaluate achievements
			foreach (Achievement_t achievement in m_Achievements)
			{
				if (achievement.m_bAchieved)
					continue;

				switch (achievement.m_eAchievementID)
				{
					// APPLES
					case Achievement.stayin_alive_solo:
						if (m_nHighestWave >= 50)
						{
							UnlockAchievement(achievement);
						}
						break;
					case Achievement.stayin_alive_party:
						if (m_nHighestPartyWave >= 50)
						{
							UnlockAchievement(achievement);
						}
						break;
					case Achievement.cube_destroyer_i:
						if (m_nCubesEliminated >= 10000)
						{
							UnlockAchievement(achievement);
						}
						break;
					case Achievement.cube_destroyer_ii:
						if (m_nCubesEliminated >= 100000)
						{
							UnlockAchievement(achievement);
						}
						break;
					case Achievement.cube_destroyer_iii:
						if (m_nCubesEliminated >= 1000000)
						{
							UnlockAchievement(achievement);
						}
						break;
					case Achievement.trigger_happy_i:
						if (m_nTotalProjectiles >= 100000)
						{
							UnlockAchievement(achievement);
						}
						break;
					case Achievement.trigger_happy_ii:
						if (m_nTotalProjectiles >= 1000000)
						{
							UnlockAchievement(achievement);
						}
						break;
					case Achievement.trigger_happy_iii:
						if (m_nTotalProjectiles >= 1000000)
						{
							UnlockAchievement(achievement);
						}
						break;
				}
			}

			//Store stats in the Steam database if necessary
			if (m_bStoreStats)
			{
				// already set any achievements in UnlockAchievement

				// set stats
				SteamUserStats.SetStat("stat_points", m_nUserTotalPoints);
				SteamUserStats.SetStat("stat_coins", m_nUserCoins);
				SteamUserStats.SetStat("stat_highest_wave", m_nHighestWave);
				SteamUserStats.SetStat("stat_highest_party_wave", m_nHighestPartyWave);
				SteamUserStats.SetStat("stat_cubes_eliminated", m_nCubesEliminated);
				SteamUserStats.SetStat("stat_projectiles_fired", m_nTotalProjectiles);
				SteamUserStats.SetStat("stat_accuracy", m_flAccuracy);
				
				
				/*// Update average feet / second stat
				SteamUserStats.UpdateAvgRateStat("AverageSpeed", m_flGameFeetTraveled, m_flGameDurationSeconds);
				// The averaged result is calculated for us
				SteamUserStats.GetStat("AverageSpeed", out m_flAverageSpeed);*/

				bool bSuccess = SteamUserStats.StoreStats();
				// If this failed, we never sent anything to the server, try
				// again later.
				m_bStoreStats = !bSuccess;
			}
		}

		//-----------------------------------------------------------------------------
		// Purpose: Accumulate distance traveled
		//-----------------------------------------------------------------------------
		public void AddDistanceTraveled(float flDistance)
		{
			m_flGameFeetTraveled += flDistance;
		}



		//-----------------------------------------------------------------------------
		// Purpose: Unlock this achievement
		//-----------------------------------------------------------------------------
		private void UnlockAchievement(Achievement_t achievement)
		{
			achievement.m_bAchieved = true;

			// the icon may change once it's unlocked
			//achievement.m_iIconImage = 0;

			// mark it down
			SteamUserStats.SetAchievement(achievement.m_eAchievementID.ToString());

			// Store stats end of frame
			m_bStoreStats = true;
		}

		public void UnlockRicochetKing()
		{
			foreach (Achievement_t achievement in m_Achievements)
			{
				if (achievement.m_bAchieved)
					continue;

				switch (achievement.m_eAchievementID)
				{
					// APPLES
					case Achievement.ricochet_king:
						UnlockAchievement(achievement);
						break;
				}
			}

		}

        public void UnlockNGamer1()
        {
            foreach (Achievement_t achievement in m_Achievements)
            {
                if (achievement.m_bAchieved)
                    continue;

                switch (achievement.m_eAchievementID)
                {
                    // APPLES
                    case Achievement.n_gamer_1:
                        UnlockAchievement(achievement);
                        break;
                }
            }

        }

        //-----------------------------------------------------------------------------
        // Purpose: We have stats data from Steam. It is authoritative, so update
        //			our data with those results now.
        //-----------------------------------------------------------------------------
        private void OnUserStatsReceived(UserStatsReceived_t pCallback)
		{
			if (!SteamManager.Initialized)
				return;

			// we may get callbacks for other games' stats arriving, ignore them
			if ((ulong)m_GameID == pCallback.m_nGameID)
			{
				if (EResult.k_EResultOK == pCallback.m_eResult)
				{
					Debug.Log("Received stats and achievements from Steam\n");

					m_bStatsValid = true;

					// load achievements
					foreach (Achievement_t ach in m_Achievements)
					{
						bool ret = SteamUserStats.GetAchievement(ach.m_eAchievementID.ToString(), out ach.m_bAchieved);
						if (ret)
						{
							ach.m_strName = SteamUserStats.GetAchievementDisplayAttribute(ach.m_eAchievementID.ToString(), "name");
							ach.m_strDescription = SteamUserStats.GetAchievementDisplayAttribute(ach.m_eAchievementID.ToString(), "desc");
						}
						else
						{
							Debug.LogWarning("SteamUserStats.GetAchievement failed for Achievement " + ach.m_eAchievementID + "\nIs it registered in the Steam Partner site?");
						}
					}

					// load stats
                    SteamUserStats.GetStat("stat_points", out m_nUserTotalPoints);
                    SteamUserStats.GetStat("stat_coins", out m_nUserCoins);
                    SteamUserStats.GetStat("stat_highest_wave", out m_nHighestWave);
                    SteamUserStats.GetStat("stat_highest_party_wave", out m_nHighestPartyWave);
                    SteamUserStats.GetStat("stat_cubes_eliminated", out m_nCubesEliminated);
                    SteamUserStats.GetStat("stat_projectiles_fired", out m_nTotalProjectiles);
                    SteamUserStats.GetStat("stat_accuracy", out m_flAccuracy);
                }
				else
				{
					Debug.Log("RequestStats - failed, " + pCallback.m_eResult);
				}
			}
		}

		//-----------------------------------------------------------------------------
		// Purpose: Our stats data was stored!
		//-----------------------------------------------------------------------------
		private void OnUserStatsStored(UserStatsStored_t pCallback)
		{
			// we may get callbacks for other games' stats arriving, ignore them
			if ((ulong)m_GameID == pCallback.m_nGameID)
			{
				if (EResult.k_EResultOK == pCallback.m_eResult)
				{
					Debug.Log("StoreStats - success");
				}
				else if (EResult.k_EResultInvalidParam == pCallback.m_eResult)
				{
					// One or more stats we set broke a constraint. They've been reverted,
					// and we should re-iterate the values now to keep in sync.
					Debug.Log("StoreStats - some failed to validate");
					// Fake up a callback here so that we re-load the values.
					UserStatsReceived_t callback = new UserStatsReceived_t();
					callback.m_eResult = EResult.k_EResultOK;
					callback.m_nGameID = (ulong)m_GameID;
					OnUserStatsReceived(callback);
				}
				else
				{
					Debug.Log("StoreStats - failed, " + pCallback.m_eResult);
				}
			}
		}

		//-----------------------------------------------------------------------------
		// Purpose: An achievement was stored
		//-----------------------------------------------------------------------------
		private void OnAchievementStored(UserAchievementStored_t pCallback)
		{
			// We may get callbacks for other games' stats arriving, ignore them
			if ((ulong)m_GameID == pCallback.m_nGameID)
			{
				if (0 == pCallback.m_nMaxProgress)
				{
					Debug.Log("Achievement '" + pCallback.m_rgchAchievementName + "' unlocked!");
				}
				else
				{
					Debug.Log("Achievement '" + pCallback.m_rgchAchievementName + "' progress callback, (" + pCallback.m_nCurProgress + "," + pCallback.m_nMaxProgress + ")");
				}
			}
		}

		//-----------------------------------------------------------------------------
		// Purpose: Display the user's stats and achievements
		//-----------------------------------------------------------------------------
		public void Render()
		{
			if (!SteamManager.Initialized)
			{
				GUILayout.Label("Steamworks not Initialized");
				return;
			}

			GUILayout.Label("m_ulTickCountGameStart: " + m_ulTickCountGameStart);
			GUILayout.Label("m_flGameDurationSeconds: " + m_flGameDurationSeconds);
			GUILayout.Label("m_flGameFeetTraveled: " + m_flGameFeetTraveled);
			GUILayout.Space(10);
			//GUILayout.Label("ApplesShot: " + m_nApplesShot);

			GUILayout.BeginArea(new Rect(Screen.width - 300, 0, 300, 800));
			foreach (Achievement_t ach in m_Achievements)
			{
				GUILayout.Label(ach.m_eAchievementID.ToString());
				GUILayout.Label(ach.m_strName + " - " + ach.m_strDescription);
				GUILayout.Label("Achieved: " + ach.m_bAchieved);
				GUILayout.Space(20);
			}

			// FOR TESTING PURPOSES ONLY!
			if (GUILayout.Button("RESET STATS AND ACHIEVEMENTS"))
			{
				SteamUserStats.ResetAllStats(true);
				SteamUserStats.RequestCurrentStats();
			}
			GUILayout.EndArea();
		}

		private class Achievement_t
		{
			public Achievement m_eAchievementID;
			public string m_strName;
			public string m_strDescription;
			public bool m_bAchieved;

			/// <summary>
			/// Creates an Achievement. You must also mirror the data provided here in https://partner.steamgames.com/apps/achievements/yourappid
			/// </summary>
			/// <param name="achievement">The "API Name Progress Stat" used to uniquely identify the achievement.</param>
			/// <param name="name">The "Display Name" that will be shown to players in game and on the Steam Community.</param>
			/// <param name="desc">The "Description" that will be shown to players in game and on the Steam Community.</param>
			public Achievement_t(Achievement achievementID, string name, string desc)
			{
				m_eAchievementID = achievementID;
				m_strName = name;
				m_strDescription = desc;
				m_bAchieved = false;
			}
		}
#endif
	}
}
