using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private static Player _instance;

    public static Player Instance { get { return _instance; } }

    public int points = 0;
    public int coins = 0;
    public int currentBlaster = 0;
    public int highestWave = 0;
    public string playerName = "PlayerName";
    public string password = "";
    public int[] ownedBlasters = { 1, 0, 0, 0, 0, 0, 0};
    public Material[] materials;

    /// <summary>
    /// Singleton Pattern.
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
    }

    /// <summary>
    /// Sets playerName to name and saves the player data.
    /// </summary>
    /// <param name="name">The Name Input Field</param>
    public void SetPlayerName(string name)
    {
        playerName = name;
        SaveSystem.SavePlayer(_instance);
    }

    /// <summary>
    /// Sets password to word.
    /// </summary>
    /// <param name="word">The Password Input Field</param>
    public void SetPassword(string word)
    {
        password = word;
    }

}
