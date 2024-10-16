using Photon.Pun;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Com.GCTC.ZombCube
{

    public class NetworkSpawner : MonoBehaviourPun
    {
        private static NetworkSpawner _instance;

        public static NetworkSpawner Instance { get { return _instance; } }

        private int cubesToSpawn = 5;
        public GameObject[] spawnPoints;
        public GameObject armor;
        public bool hasStarted = false;
        public bool gameOver = false;

        private int timeTilNextWave = 5;
        public TextMeshProUGUI countDownLabel;
        private bool isCountingDown = false;
        private float armorChance = 0.05f;
        private int enemiesInGame = 0;
        WaitForSeconds secondsToWait;

        /// <summary>
        /// Singleton Pattern
        /// </summary>
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {

                _instance = this;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            secondsToWait = new WaitForSeconds(1);
            hasStarted = false;
            spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
            if (!PhotonNetwork.IsMasterClient) { return; }
            cubesToSpawn *= PhotonNetwork.CurrentRoom.PlayerCount;
            gameOver = NetworkGameManager.Instance.IsGameOver();
            countDownLabel.gameObject.SetActive(true);
            isCountingDown = true;
            StartCoroutine(CountDownRound());
        }

        // Update is called once per frame
        void Update()
        {
            CheckForEnemies();
            HandleCountDownLabel();
        }

        private void CheckForEnemies()
        {

            if (PhotonNetwork.IsMasterClient)
            {
                gameOver = NetworkGameManager.Instance.IsGameOver();

                if (!GameObject.FindWithTag("Enemy") && hasStarted == true && gameOver == false && isCountingDown == false)
                {
                    isCountingDown = true;
                    cubesToSpawn += 5;
                    NetworkGameManager.Instance.NextWaveCall();

                    photonView.RPC(nameof(EnableCountDown), RpcTarget.All);

                    StartCoroutine(CountDownRound());
                    int currentRound = NetworkGameManager.Instance.CurrentRound;
                    if (currentRound > 5)
                        armorChance += 0.01f;
                }
            }

        }

        IEnumerator CountDownRound()
        {
            while (timeTilNextWave > 0)
            {
                if(PhotonNetwork.IsConnectedAndReady)
                    photonView.RPC(nameof(DecreaseTime), RpcTarget.All);
                yield return new WaitForSeconds(1);
            }

            if (PhotonNetwork.IsConnectedAndReady)
                photonView.RPC(nameof(DisableCountDown), RpcTarget.All);

            yield return null;
        }

        private void HandleCountDownLabel()
        {
            if (countDownLabel.text != $"Next Wave in {timeTilNextWave}...")
                countDownLabel.text = $"Next Wave in {timeTilNextWave}...";
        }

        public void CallSpawn()
        {
            //photonView.RPC(nameof(Spawn), RpcTarget.AllBuffered);
            StartCoroutine(Spawn());
        }

        
        public IEnumerator Spawn()
        {
            int currentRound = NetworkGameManager.Instance.CurrentRound;

            if (currentRound == 50 || currentRound == 60 || currentRound == 70 || currentRound == 80 || currentRound == 90 || currentRound == 100)
            {
                int j = Random.Range(0, spawnPoints.Length);
                GameObject cubeClone = PhotonNetwork.InstantiateRoomObject("NetworkBossCube",
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
                        if (enemiesInGame < 60)
                        {
                            break;
                        }
                        yield return secondsToWait;
                    }

                    int j = Random.Range(0, spawnPoints.Length);
                    GameObject fastCubeClone = PhotonNetwork.InstantiateRoomObject("NetworkFastCube",
                    spawnPoints[j].transform.position,
                    spawnPoints[j].transform.rotation);

                    if (currentRound > 7)
                    {
                        float randChance = Random.value;
                        if (randChance >= 1 - armorChance && fastCubeClone != null)
                        {
                            fastCubeClone.GetComponent<NetworkEnemy>().EnableDisableArmor();
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
                        if (enemiesInGame < 60)
                        {
                            break;
                        }
                        yield return secondsToWait;
                    }

                    int j = Random.Range(0, spawnPoints.Length);
                    GameObject dupeCubeClone = PhotonNetwork.InstantiateRoomObject("NetworkDupeCube",
                    spawnPoints[j].transform.position,
                    spawnPoints[j].transform.rotation);
                    if (currentRound > 9)
                    {
                        float randChance = Random.value;
                        if (randChance >= 1 - armorChance && dupeCubeClone != null)
                        {
                            dupeCubeClone.GetComponent<NetworkDupeCube>().EnableDisableArmor();
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
                        if (enemiesInGame < 60)
                        {
                            break;
                        }
                        yield return secondsToWait;
                    }

                    int j = Random.Range(0, spawnPoints.Length);
                    GameObject cubeClone = PhotonNetwork.InstantiateRoomObject("NetworkShieldedCube",
                    spawnPoints[j].transform.position,
                    spawnPoints[j].transform.rotation);
                    if (currentRound > 11)
                    {
                        float randChance = Random.value;
                        if (randChance >= 1 - armorChance && cubeClone != null)
                        {
                            cubeClone.GetComponent<NetworkShieldedCube>().EnableDisableArmor();
                        }
                    }
                }
            }

            for (int i = 0; i < cubesToSpawn; i++)
            {
                while (true)
                {
                    enemiesInGame = GameObject.FindGameObjectsWithTag("Enemy").Length;
                    if (enemiesInGame < 60)
                    {
                        break;
                    }
                    yield return secondsToWait;
                }

                int j = Random.Range(0, spawnPoints.Length);
                GameObject zombCubeClone = PhotonNetwork.InstantiateRoomObject("NetworkEnemy",
                    spawnPoints[j].transform.position,
                    spawnPoints[j].transform.rotation);
                if (currentRound > 5)
                {
                    float randChance = Random.value;
                    if (randChance >= 1 - armorChance && zombCubeClone != null)
                    {
                        zombCubeClone.GetComponent<NetworkEnemy>().EnableDisableArmor();
                    }
                }
            }

            yield return null;
        }

        [PunRPC]
        private void DecreaseTime()
        {
            timeTilNextWave--;
        }

        [PunRPC]
        private void DisableCountDown()
        {
            countDownLabel.gameObject.SetActive(false);
            NetworkGameManager.Instance.StartGame();
            isCountingDown = false;
        }

        [PunRPC]
        private void EnableCountDown()
        {
            timeTilNextWave = 5;
            countDownLabel.gameObject.SetActive(true);
        }


    }

}

