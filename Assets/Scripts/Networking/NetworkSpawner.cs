using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace Com.MorganHouston.ZombCube
{

    public class NetworkSpawner : MonoBehaviour
    {
        private static NetworkSpawner _instance;

        public static NetworkSpawner Instance { get { return _instance; } }

        private int cubesToSpawn = 5;
        public GameObject[] spawnPoints;
        public GameObject enemy;

        public bool hasStarted = false;
        public bool gameOver = false;

        /// <summary>
        /// Singleton Pattern
        /// </summary>
        private void Awake()
        {
            if(_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
            }else
            {
                _instance = this;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            cubesToSpawn = 5;
            hasStarted = false;
            cubesToSpawn *= PhotonNetwork.CurrentRoom.PlayerCount;
            spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        }

        // Update is called once per frame
        void Update()
        {
            CheckForEnemies();
        }

        private void CheckForEnemies()
        {
            if (!GameObject.FindWithTag("Enemy") && hasStarted == true && gameOver == false)
            {
                cubesToSpawn += 5;
                NetworkGameManager.Instance.NextWave();
                Spawn();
            }
        }

        public void Spawn()
        {
            for (int i = 0; i < cubesToSpawn; i++)
            {
                int j = Random.Range(0, spawnPoints.Length);
                GameObject enemyClone = PhotonNetwork.Instantiate("NetworkEnemy", spawnPoints[j].transform.position, spawnPoints[j].transform.rotation);
            }

        }


    }

}

