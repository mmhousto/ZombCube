[System.Serializable]
public class SaveData
{
    public string playerName;
    public int coins;
    public int points;
    public int highestWave;
    public int currentBlaster;
    public int[] ownedBlasters;

    public SaveData(Player player)
    {
        playerName = player.playerName;
        coins = player.coins;
        points = player.points;
        highestWave = player.highestWave;
        currentBlaster = player.currentBlaster;
        ownedBlasters = player.ownedBlasters;
        
    }
}
