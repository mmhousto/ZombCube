using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

namespace Com.MorganHouston.ZombCube
{

    public class NetworkGameManager : MonoBehaviourPunCallbacks
    {
        private static NetworkGameManager _instance;

        public static NetworkGameManager Instance { get { return _instance; } }

        public Camera cam;

        private int CurrentRound { get; set; }

        public int playersEliminated = 0;

        public TextMeshProUGUI waveTxt;
        public GameObject gameOverScreen;

        public int playersSpawned = 0;

        public bool isGameOver = false;


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
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC(nameof(RPC_CreatePlayers), RpcTarget.AllBuffered);
            }
                gameOverScreen.SetActive(false);
                playersEliminated = 0;
                playersSpawned = 0;
                CurrentRound = 1;
                waveTxt.text = "Wave: " + CurrentRound.ToString();
                isGameOver = false;
                StartGame();
            
        }

        // Update is called once per frame
        void Update()
        {
            waveTxt.text = "Wave: " + CurrentRound.ToString();
        }

        [PunRPC]
        public void RPC_CreatePlayers()
        {
            PhotonNetwork.Instantiate("PhotonNetworkObject", transform.position, transform.rotation, 0);
        }

        public void CallEliminatePlayer()
        {
            photonView.RPC(nameof(RPC_EliminatePlayer), RpcTarget.All);
        }

        [PunRPC]
        public void RPC_EliminatePlayer()
        {
            playersEliminated++;
        }

        public void NextWaveCall()
        {
            photonView.RPC(nameof(NextWave), RpcTarget.All);
        }

        [PunRPC]
        public void NextWave()
        {
            CurrentRound += 1;
        }

        public void StartGame()
        {
            Time.timeScale = 1;
            NetworkSpawner.Instance.Spawn();
            NetworkSpawner.Instance.hasStarted = true;
        }

        public void CallGameOver()
        {
            photonView.RPC("GameOver", RpcTarget.All);
        }

        public void CallRestart()
        {
            if(PhotonNetwork.IsMasterClient)
                photonView.RPC("Restart", RpcTarget.All);
        }

        [PunRPC]
        public void GameOver()
        {
            isGameOver = true;
            NetworkSpawner.Instance.gameOver = isGameOver;
            NetworkEnemy.isGameOver = isGameOver;
            Cursor.lockState = CursorLockMode.Confined;
            gameOverScreen.SetActive(true);
        }

        [PunRPC]
        public void Restart()
        {
            CurrentRound = 1;
            Cursor.lockState = CursorLockMode.Locked;
            PhotonNetwork.LoadLevel("NetworkGameScene");
        }

        public void GoHome()
        {
            StartCoroutine(DisconnectAndLoad());
        }

        public void ActivateCamera()
        {
            cam.gameObject.SetActive(true);
        }

        public void DeactivateCamera()
        {
            cam.gameObject.SetActive(false);
        }

        IEnumerator DisconnectAndLoad()
        {
            PhotonNetwork.LeaveRoom();
            while (PhotonNetwork.InRoom)
                yield return null;
            Debug.Log("Disconnected from room!!!!!!");
        }

        public override void OnConnectedToMaster()
        {
            PhotonNetwork.Disconnect();
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            LeaveServer();
        }

        public void LeaveServer()
        {
            SceneLoader.ToMainMenu();
        }

        public void CallPlayerSpawned()
        {
            photonView.RPC(nameof(RPC_PlayerSpawned), RpcTarget.All);
        }

        [PunRPC]
        public void RPC_PlayerSpawned()
        {
            playersSpawned++;
        }

        public bool IsGameOver()
        {
            return isGameOver;
        }

    }
}
