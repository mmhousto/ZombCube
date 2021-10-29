using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;

public class Spawner : NetworkBehaviour
{
    private static Spawner _instance;

    public static Spawner Instance { get { return _instance; } }

    [SerializeField] private int cubesToSpawn = 5;
    public GameObject[] spawnPoints;
    public GameObject enemy;
    public GameObject networkEnemy;

    private bool spawned = false;

    private bool spawning = false;

    private bool started;

    private void Awake()
    {
        if(_instance != this && _instance != null)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }

        spawned = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        Debug.Log(GameManager.Instance.gameStarted);
        
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance.gameStarted == false)
        {
            Debug.Log("Not Started!");
            return;
        }
            

        if (spawned == false)
        {
            if (SceneLoader.GetCurrentScene() == "GameScene")
            {
                spawning = true;
                Spawn();
            }
            else if (SceneLoader.GetCurrentScene() == "NetworkGameScene" && IsServer)
            {
                spawning = true;
                NetworkSpawnServerRpc();
            }
            spawned = true;
        }

        if (!GameObject.FindWithTag("Enemy") && spawned == true && spawning == false)
        {
            Debug.Log("WHY?!?!?!");
            GameManager.Instance.NextWave();
            if (SceneLoader.GetCurrentScene() == "GameScene")
            {
                cubesToSpawn += 5;
                Spawn();
            }
                
            else if (SceneLoader.GetCurrentScene() == "NetworkGameScene" && IsServer)
            {
                IncreaseCubesServerRpc();
                NetworkSpawnServerRpc();
            }
                
        }
    }

    private void Spawn()
    {
        for(int i = 0; i < cubesToSpawn; i++)
        {
            int j = Random.Range(0, spawnPoints.Length);
            GameObject enemyClone = Instantiate(enemy, spawnPoints[j].transform.position, spawnPoints[j].transform.rotation);
        }
        spawning = false;
        
    }

    [ServerRpc(RequireOwnership = false)]
    private void NetworkSpawnServerRpc()
    {
        for (int i = 0; i < cubesToSpawn; i++)
        {
            int j = Random.Range(0, spawnPoints.Length);
            GameObject enemyClone = Instantiate(networkEnemy, spawnPoints[j].transform.position, spawnPoints[j].transform.rotation);
            enemyClone.GetComponent<NetworkObject>().Spawn();
        }
        spawning = false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void IncreaseCubesServerRpc()
    {
        cubesToSpawn += 5;
    }


}
