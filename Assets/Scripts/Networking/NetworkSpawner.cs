using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace Com.MorganHouston.ZombCube
{

    public class NetworkSpawner : MonoBehaviourPun
    {
        private static NetworkSpawner _instance;

        public static NetworkSpawner Instance { get { return _instance; } }

        private int cubesToSpawn = 5;
        public GameObject[] spawnPoints;

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
            spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
            if (!PhotonNetwork.IsMasterClient) { return; }
            cubesToSpawn *= PhotonNetwork.CurrentRoom.PlayerCount;
            gameOver = NetworkGameManager.Instance.IsGameOver();
            
        }

        // Update is called once per frame
        void Update()
        {
            CheckForEnemies();
        }

        private void CheckForEnemies()
        {

            if (PhotonNetwork.IsMasterClient)
            {
                gameOver = NetworkGameManager.Instance.IsGameOver();

                if (!GameObject.FindWithTag("Enemy") && hasStarted == true && gameOver == false)
                {
                    cubesToSpawn += 5;
                    NetworkGameManager.Instance.NextWaveCall();
                    Spawn();
                }
            }
            
        }

        public void Spawn()
        {
                for (int i = 0; i < cubesToSpawn; i++)
                {
                    int j = Random.Range(0, spawnPoints.Length);
                    PhotonNetwork.InstantiateRoomObject("NetworkEnemy",
                        spawnPoints[j].transform.position,
                        spawnPoints[j].transform.rotation);
                }
            

        }


    }

}

