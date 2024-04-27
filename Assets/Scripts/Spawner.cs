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
        public GameObject armor;
        private int timeTilNextWave = 5;
        public TextMeshProUGUI countDownLabel;
        private bool isCountingDown = false;
        private float armorChance = 0.05f;


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
            int currentRound = GameManager.Instance.CurrentRound;

            if (!GameObject.FindWithTag("Enemy") && isCountingDown == false)
            {
                isCountingDown = true;
                cubesToSpawn += (cubesToSpawn / currentRound);
                GameManager.Instance.NextWave();

                timeTilNextWave = 5;
                countDownLabel.gameObject.SetActive(true);
                
                StartCoroutine(CountDownRound());

                if (currentRound > 5)
                    armorChance += 0.01f;
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
            int currentRound = GameManager.Instance.CurrentRound;

            for (int i = 0; i < cubesToSpawn; i++)
            {
                int j = Random.Range(0, spawnPoints.Length);
                GameObject zombCubeClone = Instantiate(enemies[0],
                    spawnPoints[j].transform.position,
                    spawnPoints[j].transform.rotation);
                if(currentRound > 5)
                {
                    float randChance = Random.value;
                    if(randChance >= 1 - armorChance)
                    {
                        Instantiate(armor, zombCubeClone.transform);
                    }
                }
            }

            if(currentRound > 2)
            {
                for(int i = 0; i < cubesToSpawn/10; i++)
                {
                    int j = Random.Range(0, spawnPoints.Length);
                    GameObject fastCubeClone = Instantiate(enemies[1],
                    spawnPoints[j].transform.position,
                    spawnPoints[j].transform.rotation);
                    if (currentRound > 7)
                    {
                        float randChance = Random.value;
                        if (randChance >= 1 - armorChance)
                        {
                            Instantiate(armor, fastCubeClone.transform);
                        }
                    }
                }

            }

            if (currentRound > 4)
            {
                for (int i = 0; i < cubesToSpawn / 20; i++)
                {
                    int j = Random.Range(0, spawnPoints.Length);
                    GameObject dupeCubeClone = Instantiate(enemies[2],
                    spawnPoints[j].transform.position,
                    spawnPoints[j].transform.rotation);
                    if (currentRound > 9)
                    {
                        float randChance = Random.value;
                        if (randChance >= 1 - armorChance)
                        {
                            Instantiate(armor, dupeCubeClone.transform);
                        }
                    }
                }
            }

            if (currentRound > 6)
            {
                for (int i = 0; i < cubesToSpawn / 30; i++)
                {
                    int j = Random.Range(0, spawnPoints.Length);
                    GameObject cubeClone = Instantiate(enemies[3],
                    spawnPoints[j].transform.position,
                    spawnPoints[j].transform.rotation);
                    if (currentRound > 11)
                    {
                        float randChance = Random.value;
                        if (randChance >= 1 - armorChance)
                        {
                            Instantiate(armor, cubeClone.transform);
                        }
                    }
                }
            }

        }


    }

}
