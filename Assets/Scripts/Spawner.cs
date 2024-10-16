using System.Collections;
using TMPro;
using UnityEngine;

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
        private int enemiesInGame = 0;
        WaitForSeconds secondsToWait;


        // Start is called before the first frame update
        void Start()
        {
            secondsToWait = new WaitForSeconds(1);
            cubesToSpawn = (GameObject.Find("CoopManager") != null) ? GameObject.Find("CoopManager").GetComponent<CouchCoopManager>().joinedPlayerIDs.Count * 5 : 5;
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
            StartCoroutine(Spawn());
            isCountingDown = false;
            yield return null;
        }

        private void HandleCountDownLabel()
        {
            if (countDownLabel.text != $"Next Wave in {timeTilNextWave}...")
                countDownLabel.text = $"Next Wave in {timeTilNextWave}...";
        }

        private IEnumerator Spawn()
        {
            int currentRound = GameManager.Instance.CurrentRound;

            if (currentRound == 50 || currentRound == 60 || currentRound == 70 || currentRound == 80 || currentRound == 90 || currentRound == 100)
            {
                int j = Random.Range(0, spawnPoints.Length);
                GameObject cubeClone = Instantiate(enemies[4],
                spawnPoints[j].transform.position,
                spawnPoints[j].transform.rotation);
            }

            if (currentRound > 2)
            {
                for (int i = 0; i < cubesToSpawn / 10; i++)
                {
                    while (true)
                    {
                        enemiesInGame = GameObject.FindGameObjectsWithTag("Enemy").Length;
                        if (enemiesInGame < 500)
                        {
                            break;
                        }
                        yield return secondsToWait;
                    }

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
                    while (true)
                    {
                        enemiesInGame = GameObject.FindGameObjectsWithTag("Enemy").Length;
                        if (enemiesInGame < 500)
                        {
                            break;
                        }
                        yield return secondsToWait;
                    }

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
                    while (true)
                    {
                        enemiesInGame = GameObject.FindGameObjectsWithTag("Enemy").Length;
                        if (enemiesInGame < 500)
                        {
                            break;
                        }
                        yield return secondsToWait;
                    }

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

            for (int i = 0; i < cubesToSpawn; i++)
            {
                while (true)
                {
                    enemiesInGame = GameObject.FindGameObjectsWithTag("Enemy").Length;
                    if (enemiesInGame < 500)
                    {
                        break;
                    }
                    yield return secondsToWait;
                }

                int j = Random.Range(0, spawnPoints.Length);
                GameObject zombCubeClone = Instantiate(enemies[0],
                    spawnPoints[j].transform.position,
                    spawnPoints[j].transform.rotation);
                if (currentRound > 5)
                {
                    float randChance = Random.value;
                    if (randChance >= 1 - armorChance)
                    {
                        Instantiate(armor, zombCubeClone.transform);
                    }
                }
            }

            

            yield return null;

        }

    }

}
