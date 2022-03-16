using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using StarterAssets;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace Com.MorganHouston.ZombCube
{

    public class NetworkGameManager : MonoBehaviourPunCallbacks
    {
        private static NetworkGameManager _instance;

        public static NetworkGameManager Instance { get { return _instance; } }

        public Camera eliminatedCam;

        public int CurrentRound { get; set; }
        public TextMeshProUGUI waveTxt;
        public GameObject gameOverScreen, restart, pauseMenu, settingsButton, settingsMenu;

        public int playersEliminated = 0;

        public int playersSpawned = 0;

        public bool isGameOver = false;

        GameObject myPlayer;


        #region MonoBehaviour Methods


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
            gameOverScreen.SetActive(false);
            pauseMenu.SetActive(false);

            playersSpawned = 0;
            playersEliminated = 0;
            CurrentRound = 1;
            waveTxt.text = "Wave: " + CurrentRound.ToString();
            isGameOver = false;

            Cursor.lockState = CursorLockMode.Locked;

            if (PhotonNetwork.IsMasterClient)
            {
                this.photonView.RPC(nameof(RPC_CreatePlayers), RpcTarget.AllBuffered);
                StartGame();
            }

            myPlayer = FindPlayer.GetPlayer();
        }

        // Update is called once per frame
        void Update()
        {
            waveTxt.text = "Wave: " + CurrentRound.ToString();
        }


        #endregion


        #region Public Methods

        public void PauseGame() 
        {
            pauseMenu.SetActive(true);
            SelectObject(settingsButton);
        }

        public void ResumeGame()
        {
            pauseMenu.SetActive(false);
            settingsMenu.SetActive(false);
        }

        public void StartGame()
        {
            NetworkSpawner.Instance.Spawn();
            NetworkSpawner.Instance.hasStarted = true;
            CustomAnalytics.SendGameStart();
            
        }

        public void GoHome()
        {
            StartCoroutine(DisconnectAndLoad());
        }

        public void ActivateCamera()
        {
            eliminatedCam.gameObject.SetActive(true);
        }

        public void DeactivateCamera()
        {
            eliminatedCam.gameObject.SetActive(false);
        }

        private void LeaveServer()
        {
            SceneLoader.ToMainMenu();
        }

        public bool IsGameOver()
        {
            return isGameOver;
        }

        public void SelectObject(GameObject uiElement)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(uiElement);
        }


        // START RPC Remote Calls ---------------------------------------------------------------
        public void CallPlayerSpawned()
        {
            photonView.RPC(nameof(RPC_PlayerSpawned), RpcTarget.AllBuffered);
        }

        public void CallGameOver()
        {
            photonView.RPC("GameOver", RpcTarget.All);
        }

        public void CallRestart()
        {
            if (PhotonNetwork.IsMasterClient)
                photonView.RPC("Restart", RpcTarget.All);
        }

        public void CallEliminatePlayer()
        {
            this.photonView.RPC(nameof(RPC_EliminatePlayer), RpcTarget.All);
        }

        public void NextWaveCall()
        {
            photonView.RPC(nameof(NextWave), RpcTarget.All);
        }
        // END RPC Remote Calls -----------------------------------------------------------------


        #endregion


        #region Private Methods

        IEnumerator DisconnectAndLoad()
        {
            PhotonNetwork.LeaveRoom();
            while (PhotonNetwork.InRoom)
                yield return null;
            Debug.Log("Disconnected from room!!!!!!");
        }


        #endregion


        #region PunCallbacks

        public override void OnConnectedToMaster()
        {
            PhotonNetwork.Disconnect();
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            LeaveServer();
        }

        #endregion


        #region PunRPCs

        [PunRPC]
        public void RPC_CreatePlayers()
        {
            PhotonNetwork.Instantiate("PhotonNetworkObject", transform.position, transform.rotation);
        }

        [PunRPC]
        public void RPC_PlayerSpawned()
        {
            playersSpawned++;
        }

        [PunRPC]
        public void GameOver()
        {
            isGameOver = true;
            SelectObject(restart);
            Cursor.lockState = CursorLockMode.None;
            gameOverScreen.SetActive(isGameOver);
            pauseMenu.SetActive(false);
            settingsMenu.SetActive(false);
            CustomAnalytics.SendGameOver();
        }

        [PunRPC]
        public void NextWave()
        {
            CurrentRound += 1;
        }

        [PunRPC]
        public void Restart()
        {
            CurrentRound = 1;
            playersSpawned = 0;
            playersEliminated = 0;
            Cursor.lockState = CursorLockMode.Locked;
            PhotonNetwork.LoadLevel("NetworkGameScene");
        }

        [PunRPC]
        public void RPC_EliminatePlayer()
        {
            playersEliminated++;
        }

        #endregion

    }
}
