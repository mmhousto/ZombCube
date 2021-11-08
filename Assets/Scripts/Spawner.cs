using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.MorganHouston.ZombCube
{

    public class Spawner : MonoBehaviour
    {

        private int cubesToSpawn = 5;
        public GameObject[] spawnPoints;
        public GameObject enemy;


        // Start is called before the first frame update
        void Start()
        {
            spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
            Spawn();
        }

        // Update is called once per frame
        void Update()
        {
            if (!GameObject.FindWithTag("Enemy"))
            {
                cubesToSpawn += 5;
                GameManager.Instance.NextWave();
                Spawn();
            }
        }

        private void Spawn()
        {
            for (int i = 0; i < cubesToSpawn; i++)
            {
                int j = Random.Range(0, spawnPoints.Length);
                GameObject enemyClone = Instantiate(enemy, spawnPoints[j].transform.position, spawnPoints[j].transform.rotation);
            }

        }


    }

}
