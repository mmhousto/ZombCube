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
        public int[] ownedBlasters;
        public int[] ownedSkins;
        public int cubesEliminated = 0;
        public int totalPointsEarned = 0;
        public int totalProjectilesFired = 0;

        public SaveData()
        {
            userID = "";
            userName = "";
            playerName = "PlayerName";
            coins = 0;
            points = 0;
            highestWave = 0;
            highestWaveParty = 0;
            currentBlaster = 0;
            ownedBlasters = new int[] { 1, 0, 0, 0, 0, 0, 0, 0, 0 };
            currentSkin = 0;
            ownedSkins = new int[] { 1, 0, 0, 0, 0, 0, 0, 0, 0 };
            cubesEliminated = 0;
            totalPointsEarned = 0;
            totalProjectilesFired = 0;
        }

        public SaveData(Player player)
        {
            userID = player.userID;
            userName = player.userName;
            playerName = player.playerName;
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
        }
    }

}
