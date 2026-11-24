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
        }

        /// <summary>Best wave reached on a map in a mode.</summary>
        public int BestWave(string levelId, PlayMode mode)
        {
            int best = WaveLeaderboards.Best(waveRecords, levelId, mode);
            if (levelId == WaveLeaderboards.Arena && mode == PlayMode.Solo) best = Math.Max(best, highestWave);
            if (levelId == WaveLeaderboards.Arena && mode == PlayMode.Party) best = Math.Max(best, highestWaveParty);
            return best;
        }

        /// <summary>Saves a wave reached on a map in a mode if it beats the best. Arena Solo/Party also update the legacy fields.</summary>
        public bool RecordWave(string levelId, PlayMode mode, int wave)
        {
            waveRecords = WaveLeaderboards.Record(waveRecords, levelId, mode, wave, out bool improved);
            if (levelId == WaveLeaderboards.Arena && mode == PlayMode.Solo && wave > highestWave) highestWave = wave;
            if (levelId == WaveLeaderboards.Arena && mode == PlayMode.Party && wave > highestWaveParty) highestWaveParty = wave;
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
