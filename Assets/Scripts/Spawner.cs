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
        public GameObject[] enemies;

        private int timeTilNextWave = 5;
        public TextMeshProUGUI countDownLabel;
        private bool isCountingDown = false;


        // Start is called before the first frame update
        void Start()
        {
            cubesToSpawn = (GameObject.Find("CoopManager") != null) ? GameObject.Find("CoopManager").GetComponent<CouchCoopManager>().joinedPlayerIDs.Count*5 : 5;
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
                GameObject enemyClone = Instantiate(enemies[0],
                    spawnPoints[j].transform.position,
                    spawnPoints[j].transform.rotation);
            }

            if(GameManager.Instance.CurrentRound > 2)
            {
                for(int i = 0; i < cubesToSpawn/10; i++)
                {
                    int j = Random.Range(0, spawnPoints.Length);
                    Instantiate(enemies[1],
                    spawnPoints[j].transform.position,
                    spawnPoints[j].transform.rotation);
                }
            }

            if (GameManager.Instance.CurrentRound > 4)
            {
                for (int i = 0; i < cubesToSpawn / 20; i++)
                {
                    int j = Random.Range(0, spawnPoints.Length);
                    Instantiate(enemies[2],
                    spawnPoints[j].transform.position,
                    spawnPoints[j].transform.rotation);
                }
            }

        }


    }

}
