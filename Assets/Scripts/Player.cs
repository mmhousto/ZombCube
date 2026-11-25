using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.GCTC.ZombCube
{

    public class Player : MonoBehaviour
    {
        private static Player _instance;

        public static Player Instance { get { return _instance; } }

        public string userID;
        public string userName;
        public int points = 0;
        public int coins = 0;
        public int currentBlaster = 0;
        public int currentSkin = 0;
        public int highestWave = 0;
        public int highestWaveParty = 0;
        public string playerName = "PlayerName";
        public string RewardOvertime { get; private set; }
        public int[] ownedBlasters = SaveMigration.NewOwnership();
        public int[] ownedSkins = SaveMigration.NewOwnership();
        public int cubesEliminated = 0;
        public int totalPointsEarned = 0;
        public int totalProjectilesFired = 0;
        public WaveRecord[] waveRecords = new WaveRecord[0];
        public string[] achievements = new string[0];

        public Player(Player player)
        {
            userID = player.userID;
            userName = player.userName;
            points = player.points;
            coins = player.coins;
            highestWave = player.highestWave;
            highestWaveParty = player.highestWaveParty;
            playerName = player.playerName;

            if(RewardOvertime == null || RewardOvertime == "")
                RewardOvertime = DateTime.Now.AddHours(-2).ToString();
            else
                RewardOvertime = player.RewardOvertime;

            currentBlaster = player.currentBlaster;
            ownedBlasters = SaveMigration.ResizeFlags(player.ownedBlasters);

            currentSkin = player.currentSkin;
            ownedSkins = SaveMigration.ResizeFlags(player.ownedSkins);

            cubesEliminated = player.cubesEliminated;
            totalPointsEarned = player.totalPointsEarned;
            totalProjectilesFired = player.totalProjectilesFired;
            waveRecords = player.waveRecords ?? new WaveRecord[0];
            achievements = player.achievements ?? new string[0];
        }

        /// <summary>Saves an event achievement locally (works for every player, signed in or not).</summary>
        public void UnlockAchievement(string id)
        {
            achievements = AchievementCatalog.AddUnlock(achievements, id, out _);
        }

        /// <summary>The saved number a stat achievement counts towards.</summary>
        public long StatFor(AchievementStat stat)
        {
            switch (stat)
            {
                case AchievementStat.CubesEliminated: return cubesEliminated;
                case AchievementStat.ProjectilesFired: return totalProjectilesFired;
                case AchievementStat.TotalPoints: return totalPointsEarned;
                case AchievementStat.ArenaSoloWave: return BestWave(WaveLeaderboards.Arena, WaveMode.Solo);
                case AchievementStat.ArenaPartyWave: return BestWave(WaveLeaderboards.Arena, WaveMode.Party);
                default: return 0;
            }
        }

        /// <summary>Whether an achievement is earned, from the saved stats or the saved event unlocks.</summary>
        public bool HasAchievement(AchievementDef a)
        {
            if (a == null) return false;
            if (a.Id == "n_gamer_1" && playerName == "NGamer1") return true;
            if (achievements != null && Array.IndexOf(achievements, a.Id) >= 0) return true;
            return a.HasProgress && StatFor(a.Stat) >= a.Target;
        }

        /// <summary>Best wave reached on a map in a mode.</summary>
        public int BestWave(string levelId, WaveMode mode)
        {
            int best = WaveLeaderboards.Best(waveRecords, levelId, mode);
            if (levelId == WaveLeaderboards.Arena && mode == WaveMode.Solo) best = Math.Max(best, highestWave);
            if (levelId == WaveLeaderboards.Arena && mode == WaveMode.Party) best = Math.Max(best, highestWaveParty);
            return best;
        }

        /// <summary>Saves a wave reached on a map in a mode if it beats the best. Arena Solo/Party also update the legacy fields.</summary>
        public bool RecordWave(string levelId, WaveMode mode, int wave)
        {
            waveRecords = WaveLeaderboards.Record(waveRecords, levelId, mode, wave, out bool improved);
            if (levelId == WaveLeaderboards.Arena && mode == WaveMode.Solo && wave > highestWave) highestWave = wave;
            if (levelId == WaveLeaderboards.Arena && mode == WaveMode.Party && wave > highestWaveParty) highestWaveParty = wave;
            return improved;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                _instance = this;
            }
            DontDestroyOnLoad(this.gameObject);
            EnsureCosmeticArrays();
        }

        /// <summary>
        /// Makes sure the owned-cosmetic arrays match the catalog size. The values
        /// serialized on the prefab are only nine long.
        /// </summary>
        public void EnsureCosmeticArrays()
        {
            if (ownedBlasters == null || ownedBlasters.Length != CosmeticCatalog.OwnershipSize)
                ownedBlasters = SaveMigration.ResizeFlags(ownedBlasters);
            if (ownedSkins == null || ownedSkins.Length != CosmeticCatalog.OwnershipSize)
                ownedSkins = SaveMigration.ResizeFlags(ownedSkins);
        }

        public void GainPoints()
        {
            points += 1000;
            totalPointsEarned += 1000;
        }

        public void SetRewardOvertime(string time)
        {
            RewardOvertime = time;
        }

    }

}
