using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;

namespace Com.GCTC.ZombCube
{

    public class NetworkSpawner : MonoBehaviourPun
    {
        private static NetworkSpawner _instance;

        public static NetworkSpawner Instance { get { return _instance; } }

        private int cubesToSpawn = 5;
        public GameObject[] spawnPoints;

        public bool hasStarted = false;
        public bool gameOver = false;

        private int timeTilNextWave = 5;
        public TextMeshProUGUI countDownLabel;
        private bool isCountingDown = false;

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
                }
            }
            
        }

        IEnumerator CountDownRound()
        {
            while (timeTilNextWave > 0)
            {
                photonView.RPC(nameof(DecreaseTime), RpcTarget.All);
                yield return new WaitForSeconds(1);
            }
            photonView.RPC(nameof(DisableCountDown), RpcTarget.All);
            yield return null;
        }

        private void HandleCountDownLabel()
        {
            if(countDownLabel.text != $"Next Wave in {timeTilNextWave}...")
                countDownLabel.text = $"Next Wave in {timeTilNextWave}...";
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

            if (NetworkGameManager.Instance.CurrentRound > 2)
            {
                for (int i = 0; i < cubesToSpawn / 10; i++)
                {
                    int j = Random.Range(0, spawnPoints.Length);
                    PhotonNetwork.InstantiateRoomObject("NetworkFastCube",
                    spawnPoints[j].transform.position,
                    spawnPoints[j].transform.rotation);
                }
            }

            if (GameManager.Instance.CurrentRound > 4)
            {
                for (int i = 0; i < cubesToSpawn / 20; i++)
                {
                    int j = Random.Range(0, spawnPoints.Length);
                    PhotonNetwork.InstantiateRoomObject("NetworkDupeCube",
                    spawnPoints[j].transform.position,
                    spawnPoints[j].transform.rotation);
                }
            }

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

