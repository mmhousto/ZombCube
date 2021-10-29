using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using MLAPI;
using MLAPI.Messaging;
using DapperDino.UMT.Lobby.Networking;

public class PlayerManager : NetworkBehaviour
{

    public Material[] blasterMaterial;

    public TextMeshProUGUI playerNameText;

    public int currentPoints = 0;
    private Slider healthBar;

    public static float healthPoints = 100f;
    private bool isGameOver;

    private Player player;


    // Start is called before the first frame update
    void Start()
    {
        player = GetComponent<Player>();
        currentPoints = 0;
        isGameOver = false;
        healthPoints = 100f;

        if (IsServer)
        {
            healthBar = GameObject.FindWithTag("Health").GetComponent<Slider>();
            
            currentPoints = 0;
            healthBar.value = healthPoints;
            
        }
        
        if (IsClient)
        {
            LoadPlayerData();
            playerNameText.text = player.GetPlayerName();
            
            

            GameObject[] blaster = GameObject.FindGameObjectsWithTag("Blaster");

            foreach (GameObject item in blaster)
            {
                item.GetComponent<MeshRenderer>().material = blasterMaterial[player.currentBlaster];
            }
        }
        else if (SceneLoader.GetCurrentScene() == "GameScene")
        {
            healthBar = GameObject.FindWithTag("Health").GetComponent<Slider>();
            LoadPlayerData();
            playerNameText.text = player.GetPlayerName();

            currentPoints = 0;
            healthBar.value = healthPoints;

            GameObject[] blaster = GameObject.FindGameObjectsWithTag("Blaster");

            foreach (GameObject item in blaster)
            {
                item.GetComponent<MeshRenderer>().material = blasterMaterial[player.currentBlaster];
            }
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        if (IsServer)
        {
            healthBar.value = healthPoints;
            
        }

        if (IsClient)
        {
            

            if (healthPoints <= 0 && !isGameOver)
            {
                healthPoints = 0;
                UpdateTotalPoints();
                SavePlayerData();
                GameManager.Instance.GameOver();
                isGameOver = true;
            }
        }
        else if(SceneLoader.GetCurrentScene() == "GameScene")
        {
            healthBar.value = healthPoints;

            if (healthPoints <= 0 && !isGameOver)
            {
                healthPoints = 0;
                UpdateTotalPoints();
                SavePlayerData();
                GameManager.Instance.GameOver();
                isGameOver = true;
            }
        }
        
    }

    public static void Damage(float damageTaken)
    {
        healthPoints -= damageTaken;
    }

    [ServerRpc(RequireOwnership = false)]
    public void NetworkDamageServerRpc(float damageTaken)
    {
        healthPoints -= damageTaken;
    }

    private void UpdateTotalPoints()
    {
        player.points += GameManager.Instance.currentPoints;
    }

    public void SavePlayerData()
    {
        SaveSystem.SavePlayer(GetComponent<Player>());
    }

    public void LoadPlayerData()
    {
        SaveData data = SaveSystem.LoadPlayer();

        player.playerName = data.playerName;
        player.coins = data.coins;
        player.points = data.points;
        player.highestWave = data.highestWave;
        player.currentBlaster = data.currentBlaster;
        player.ownedBlasters = data.ownedBlasters;
    }

}
