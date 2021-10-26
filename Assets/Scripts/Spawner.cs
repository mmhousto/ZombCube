using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;

public class Spawner : NetworkBehaviour
{

    [SerializeField] private int cubesToSpawn = 5;
    public GameObject[] spawnPoints;
    public GameObject enemy;
    public GameObject networkEnemy;


    // Start is called before the first frame update
    void Start()
    {
        spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        if (SceneLoader.GetCurrentScene() == "GameScene")
            Spawn();
        else if (SceneLoader.GetCurrentScene() == "NetworkGameScene" && IsServer)
            NetworkSpawnServerRpc();
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameObject.FindWithTag("Enemy"))
        {
            
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
    }

    [ServerRpc(RequireOwnership = false)]
    private void IncreaseCubesServerRpc()
    {
        cubesToSpawn += 5;
    }


}
