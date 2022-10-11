using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;
using TMPro;

namespace Com.GCTC.ZombCube
{

    public class Spawner : MonoBehaviour
    {

        private int cubesToSpawn;
        public GameObject[] spawnPoints;
        public GameObject enemy;

        private int timeTilNextWave = 5;
        public TextMeshProUGUI countDownLabel;
        private bool isCountingDown = false;


        // Start is called before the first frame update
        void Start()
        {
            cubesToSpawn = RemoteConfig.Instance.cubesToSpawn;
            spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
            timeTilNextWave = 5;
            countDownLabel.gameObject.SetActive(true);
            isCountingDown = true;
            StartCoroutine(CountDownRound());
        }

        // Update is called once per frame
        void Update()
        {
            if (!GameObject.FindWithTag("Enemy") && isCountingDown == false)
            {
                isCountingDown = true;
                cubesToSpawn += (cubesToSpawn / GameManager.Instance.CurrentRound);
                GameManager.Instance.NextWave();

                timeTilNextWave = 5;
                countDownLabel.gameObject.SetActive(true);
                
                StartCoroutine(CountDownRound());
            }

            HandleCountDownLabel();
        }

        IEnumerator CountDownRound()
        {
            while (timeTilNextWave > 0)
            {
                timeTilNextWave--;
                yield return new WaitForSeconds(1);
            }
            countDownLabel.gameObject.SetActive(false);
            Spawn();
            isCountingDown = false;
            yield return null;
        }

        private void HandleCountDownLabel()
        {
            if (countDownLabel.text != $"Next Wave in {timeTilNextWave}...")
                countDownLabel.text = $"Next Wave in {timeTilNextWave}...";
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
