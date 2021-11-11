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
        private static int CurrentRound { get; set; }

        public int playersEliminated = 0;

        public TextMeshProUGUI waveTxt;
        public GameObject gameOverScreen;


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
            Time.timeScale = 0;

        }

        // Start is called before the first frame update
        void Start()
        {
            gameOverScreen.SetActive(false);
            playersEliminated = 0;
            CurrentRound = 1;
            waveTxt.text = "Wave: " + CurrentRound.ToString();
            StartGame();
        }

        // Update is called once per frame
        void Update()
        {
            waveTxt.text = "Wave: " + CurrentRound.ToString();
        }

        public void EliminatePlayer()
        {
            playersEliminated++;
        }

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
            Cursor.lockState = CursorLockMode.Confined;
            Time.timeScale = 0;
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

        IEnumerator DisconnectAndLoad()
        {
            PhotonNetwork.LeaveRoom();
            PhotonNetwork.Disconnect();
            while (PhotonNetwork.IsConnected)
                yield return null;
            SceneLoader.ToMainMenu();
        }


    }
}
