using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DataManager : MonoBehaviour
{
    private static DataManager _instance;

    public static DataManager Instance { get { return _instance; } }

    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI pointsText;

    public int[] ownedBlasters;
    private int coins = 0;
    private int points = 0;
    private int bestRound = 0;
    private int currentBlaster = 0;
    private float musicVolume;
    private float masterVolume;

    /// <summary>
    /// Singleton implementation
    /// </summary>
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

        ownedBlasters = new int[] {1,
                                PlayerPrefs.GetInt("Blaster1", 0),
                                PlayerPrefs.GetInt("Blaster2", 0),
                                PlayerPrefs.GetInt("Blaster3", 0),
                                PlayerPrefs.GetInt("Blaster4", 0),
                                PlayerPrefs.GetInt("Blaster5", 0),
                                PlayerPrefs.GetInt("Blaster6", 0) };
}

    // Start is called before the first frame update
    void Start()
    {
        coins = GetCoins();
        points = GetPoints();
        bestRound = GetBestRound();
        currentBlaster = GetBlaster();
        musicVolume = GetMusicVolume();
        masterVolume = GetMasterVolume();

        if (coinsText)
        {
            coinsText.text = "Coins: " + coins;
        }

        if (pointsText)
        {
            pointsText.text = "Points: " + points;
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        coins = GetCoins();
        points = GetPoints();
        bestRound = GetBestRound();
        currentBlaster = GetBlaster();
        musicVolume = GetMusicVolume();
        masterVolume = GetMasterVolume();

        if (coinsText)
        {
            coinsText.text = "Coins: " + coins;
        }

        if (pointsText)
        {
            pointsText.text = "Points: " + points;
        }
    }

    public int GetCoins()
    {
        return PlayerPrefs.GetInt("Coins", 0);
    }

    public int GetPoints()
    {
        return PlayerPrefs.GetInt("Points", 0);
    }

    public int GetBestRound()
    {
        return PlayerPrefs.GetInt("BestRound", 0);
    }

    public int GetBlaster()
    {
        return PlayerPrefs.GetInt("Blaster", 0);
    }

    public float GetMusicVolume()
    {
        return PlayerPrefs.GetFloat("MusicVolume", 1);
    }

    public float GetMasterVolume()
    {
        return PlayerPrefs.GetFloat("MasterVolume", 1);
    }


}
