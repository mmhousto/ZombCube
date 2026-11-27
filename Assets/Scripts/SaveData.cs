using System;

namespace Com.GCTC.ZombCube
{
    [System.Serializable]
    public class SaveData
    {
        public string userID;
        public string userName;
        public int points = 0;
        public int coins = 0;
        public int currentBlaster = 0;
        public int currentSkin = 0;
        public int highestWave = 0;
        public int highestWaveParty = 0;
        public string playerName = "PlayerName";
        public string rewardOverTime;
        public int[] ownedBlasters;
        public int[] ownedSkins;
        public int cubesEliminated = 0;
        public int totalPointsEarned = 0;
        public int totalProjectilesFired = 0;

        // Best wave per map and mode (Arena Solo/Party also stay in highestWave/highestWaveParty).
        public WaveRecord[] waveRecords = new WaveRecord[0];

        // Event achievements earned (AchievementCatalog IDs); stat achievements are worked out from the stats.
        public string[] achievements = new string[0];

        // Stable cosmetic IDs (see CosmeticCatalog). These are the saved source of truth.
        // The index fields above are kept in sync for the existing runtime code.
        public int schemaVersion = 0;
        public string[] ownedBlasterIds;
        public string[] ownedSkinIds;
        public string equippedBlasterId;
        public string equippedSkinId;

        public const int CurrentSchemaVersion = 2;

        public SaveData()
        {
            userID = "";
            userName = "";
            playerName = "PlayerName";
            rewardOverTime = DateTime.Now.AddHours(-2).ToString();
            coins = 0;
            points = 0;
            highestWave = 0;
            highestWaveParty = 0;
            currentBlaster = 0;
            ownedBlasters = SaveMigration.NewOwnership();
            currentSkin = 0;
            ownedSkins = SaveMigration.NewOwnership();
            cubesEliminated = 0;
            totalPointsEarned = 0;
            totalProjectilesFired = 0;
        }

        public SaveData(Player player)
        {
            userID = player.userID;
            userName = player.userName;
            playerName = player.playerName;
            rewardOverTime = player.RewardOvertime;
            coins = player.coins;
            points = player.points;
            highestWave = player.highestWave;
            highestWaveParty = player.highestWaveParty;
            currentBlaster = player.currentBlaster;
            ownedBlasters = player.ownedBlasters;
            currentSkin = player.currentSkin;
            ownedSkins = player.ownedSkins;
            cubesEliminated = player.cubesEliminated;
            totalPointsEarned = player.totalPointsEarned;
            totalProjectilesFired = player.totalProjectilesFired;
            waveRecords = player.waveRecords ?? new WaveRecord[0];
            achievements = player.achievements ?? new string[0];

            ownedBlasterIds = SaveMigration.IdsFromFlags(player.ownedBlasters);
            ownedSkinIds = SaveMigration.IdsFromFlags(player.ownedSkins);
            equippedBlasterId = SaveMigration.EquippedIdFromIndex(player.currentBlaster);
            equippedSkinId = SaveMigration.EquippedIdFromIndex(player.currentSkin);
            schemaVersion = CurrentSchemaVersion;
        }

        /// <summary>
        /// Brings any save (old index-only, or ID-based) to the current format and
        /// keeps the index fields in sync with the IDs. Safe to call more than once.
        /// </summary>
        public void Normalize()
        {
            if (waveRecords == null) waveRecords = new WaveRecord[0];
            if (achievements == null) achievements = new string[0];

            if (ownedBlasterIds == null)
                ownedBlasterIds = SaveMigration.IdsFromFlags(ownedBlasters);
            ownedBlasters = SaveMigration.FlagsFromIds(ownedBlasterIds);

            if (ownedSkinIds == null)
                ownedSkinIds = SaveMigration.IdsFromFlags(ownedSkins);
            ownedSkins = SaveMigration.FlagsFromIds(ownedSkinIds);

            if (equippedBlasterId == null)
                equippedBlasterId = SaveMigration.EquippedIdFromIndex(currentBlaster);
            currentBlaster = SaveMigration.EquippedIndexFromId(equippedBlasterId);
            if (ownedBlasters[currentBlaster] != 1)
            {
                currentBlaster = 0;
                equippedBlasterId = CosmeticCatalog.ColorIds[0];
            }

            if (equippedSkinId == null)
                equippedSkinId = SaveMigration.EquippedIdFromIndex(currentSkin);
            currentSkin = SaveMigration.EquippedIndexFromId(equippedSkinId);
            if (ownedSkins[currentSkin] != 1)
            {
                currentSkin = 0;
                equippedSkinId = CosmeticCatalog.ColorIds[0];
            }

            schemaVersion = CurrentSchemaVersion;
        }
    }

}
