namespace Com.MorganHouston.ZombCube
{
    [System.Serializable]
    public class SaveData
    {
        public string playerName;
        public int coins;
        public int points;
        public int highestWave;
        public int currentBlaster;
        public int currentSkin;
        public int[] ownedBlasters;
        public int[] ownedSkins;

        public SaveData()
        {
            playerName = "PlayerName";
            coins = 0;
            points = 0;
            highestWave = 0;
            currentBlaster = 0;
            ownedBlasters = new int[] { 1, 0, 0, 0, 0, 0, 0, 0 };
            currentSkin = 0;
            ownedSkins = new int[] { 1, 0, 0, 0, 0, 0, 0, 0 };

        }

        public SaveData(Player player)
        {
            playerName = player.playerName;
            coins = player.coins;
            points = player.points;
            highestWave = player.highestWave;
            currentBlaster = player.currentBlaster;
            ownedBlasters = player.ownedBlasters;
            currentSkin = player.currentSkin;
            ownedSkins = player.ownedSkins;

        }
    }

}
