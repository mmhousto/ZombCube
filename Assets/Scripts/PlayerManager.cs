using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour, IDamageable<float>
{
    private static PlayerManager _instance;

    public static PlayerManager Instance { get { return _instance; } }

    public Material[] blasterMaterial;

    public int currentPoints = 0;
    public TextMeshProUGUI scoreText;
    public Slider healthBar;

    private float healthPoints = 100f;
    private bool isGameOver;

    private void Awake()
    {
        if(_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }

    }

    // Start is called before the first frame update
    void Start()
    {
        LoadPlayerData();
        currentPoints = 0;
        healthBar.value = healthPoints;
        scoreText.text = "Score: " + currentPoints.ToString();

        GameObject[] blaster = GameObject.FindGameObjectsWithTag("Blaster");

        foreach(GameObject item in blaster)
        {
            item.GetComponent<MeshRenderer>().material = blasterMaterial[Player.Instance.currentBlaster];
        }
    }

    // Update is called once per frame
    void Update()
    {
        healthBar.value = healthPoints;
        scoreText.text = "Score: " + currentPoints.ToString();

        if(healthPoints <= 0 && !isGameOver)
        {
            healthPoints = 0;
            UpdateTotalPoints();
            SavePlayerData();
            GameManager.Instance.GameOver();
            isGameOver = true;
        }
    }

    public void AddPoints(int pointsToAdd)
    {
        currentPoints += pointsToAdd;
    }

    public void Damage(float damageTaken)
    {
        healthPoints -= damageTaken;
    }

    private void UpdateTotalPoints()
    {
        Player.Instance.points += currentPoints;
    }

    public void SavePlayerData()
    {
        SaveSystem.SavePlayer(Player.Instance);
    }

    public void LoadPlayerData()
    {
        SaveData data = SaveSystem.LoadPlayer();

        Player.Instance.playerName = data.playerName;
        Player.Instance.coins = data.coins;
        Player.Instance.points = data.points;
        Player.Instance.highestWave = data.highestWave;
        Player.Instance.currentBlaster = data.currentBlaster;
        Player.Instance.ownedBlasters = data.ownedBlasters;
    }
}
