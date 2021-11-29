using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;

namespace Com.MorganHouston.ZombCube
{

    public class Spawner : MonoBehaviour
    {

        private int cubesToSpawn;
        public GameObject[] spawnPoints;
        public GameObject enemy;


        // Start is called before the first frame update
        void Start()
        {
            cubesToSpawn = RemoteConfig.Instance.cubesToSpawn;
            spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
            Spawn();
        }

        // Update is called once per frame
        void Update()
        {
            if (!GameObject.FindWithTag("Enemy"))
            {
                cubesToSpawn += (cubesToSpawn / GameManager.Instance.CurrentRound);
                GameManager.Instance.NextWave();
                Spawn();
            }
        }

        private void Spawn()
        {
            for (int i = 0; i < cubesToSpawn; i++)
            {
                int j = Random.Range(0, spawnPoints.Length);
                GameObject enemyClone = Instantiate(enemy,
                    spawnPoints[j].transform.position,
                    spawnPoints[j].transform.rotation);
            }

        }


    }

}
