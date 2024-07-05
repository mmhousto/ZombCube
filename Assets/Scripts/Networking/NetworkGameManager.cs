using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Com.GCTC.ZombCube
{

    public class NetworkGameManager : MonoBehaviourPunCallbacks
    {
        private static NetworkGameManager _instance;

        public static NetworkGameManager Instance { get { return _instance; } }

        public delegate void EndGame();
        public static event EndGame endGame;

        public delegate void NextWaveEvent();
        public static event NextWaveEvent nextWave;

        public const byte EndGameEventCode = 2;

        public Camera eliminatedCam;

        public int CurrentRound { get; set; }
        public GameObject[] grenades;
        public TextMeshProUGUI waveTxt;
        public GameObject gameOverScreen, restart, pauseMenu, settingsButton, settingsMenu, continueScreen, continueButton, endButton, waitingText;

        public int playersEliminated = 0;

        public int playersSpawned = 0;

        public bool isGameOver = false;
        private bool isContinue = false;

        GameObject myPlayer;
        public static List<GameObject> players = new List<GameObject>();


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
            players.Clear();

            if (grenades != null)
            {
                grenades[2].transform.GetChild(0).gameObject.SetActive(false);
                grenades[3].transform.GetChild(0).gameObject.SetActive(false);
            }

            PhotonNetwork.MinimalTimeScaleToDispatchInFixedUpdate = 0;
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
                //StartGame();
            }

            myPlayer = FindPlayer.GetPlayer();

            Projectile.pointsToAdd = 10;
        }

        // Update is called once per frame
        void Update()
        {
            waveTxt.text = "Wave: " + CurrentRound.ToString();

            if (myPlayer == null) myPlayer = FindPlayer.GetPlayer();

            if (grenades != null && grenades.Length > 0 && myPlayer != null && myPlayer.GetComponent<NetworkPlayerManager>().grenade != null)
            {
                for (int i = 0; i < grenades.Length; i++)
                {
                    if (i < myPlayer.GetComponent<NetworkPlayerManager>().grenade.grenadeCount)
                        grenades[i].transform.GetChild(0).gameObject.SetActive(true);
                    else grenades[i].transform.GetChild(0).gameObject.SetActive(false);
                }
            }
        }

        private new void OnEnable()
        {
            base.OnEnable();
            NetworkBossCube.bossDead += CallPauseForContinue;
        }

        private new void OnDisable()
        {
            base.OnDisable();
            NetworkBossCube.bossDead -= CallPauseForContinue;
        }


        #endregion


        #region Public Methods

        public void PauseGame()
        {
            pauseMenu.SetActive(true);
            SelectObject(settingsButton);

            if (myPlayer == null)
                myPlayer = FindPlayer.GetPlayer();
            if (myPlayer != null)
                myPlayer.GetComponent<NetworkPlayerManager>().DisableMobileButtons();
        }

        public void ResumeGame()
        {
            pauseMenu.SetActive(false);
            settingsMenu.SetActive(false);
            isContinue = false;
            continueScreen.SetActive(false);
            if (myPlayer == null)
                myPlayer = FindPlayer.GetPlayer();
            if(myPlayer != null)
                myPlayer.GetComponent<NetworkPlayerManager>().EnableInputResumeButton();

            Time.timeScale = 1.0f;
        }

        public void StartGame()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                NetworkSpawner.Instance.CallSpawn();
            }

            if (NetworkSpawner.Instance.hasStarted == false)
            {
#if !UNITY_PLAYSTATION
                CustomAnalytics.SendGameStart();
#endif

                NetworkSpawner.Instance.hasStarted = true;
            }


        }

        public void GoHome()
        {
            StartCoroutine(DisconnectAndLoad());
        }

        public void ActivateCamera()
        {
            eliminatedCam.gameObject.SetActive(true);
            NetworkSpectatorManager.showNextPlayerText.SetActive(true);
        }

        public void DeactivateCamera()
        {
            eliminatedCam.gameObject.SetActive(false);
        }

        private void LeaveServer()
        {
            Time.timeScale = 1;
            SceneLoader.ToMainMenu();
            Debug.Log("left server");
        }

        private void LeaveRoom()
        {
            Time.timeScale = 1;
            //SceneLoader.ToLobby();
            PhotonNetwork.LoadLevel(3);
            Debug.Log("left room");
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
        public void CallPlayerSpawned(GameObject player)
        {
            players.Add(player);
            photonView.RPC(nameof(RPC_PlayerSpawned), RpcTarget.AllBuffered);
        }

        public void CallGameOver()
        {
            photonView.RPC("GameOver", RpcTarget.All);
            isGameOver = true;
        }

        public void CallRestart()
        {
            if (PhotonNetwork.IsMasterClient)
                photonView.RPC("Restart", RpcTarget.All);
        }

        public void CallEliminatePlayer()
        {
            this.photonView.RPC(nameof(RPC_EliminatePlayer), RpcTarget.AllBuffered);
        }

        public void NextWaveCall()
        {
            photonView.RPC(nameof(NextWave), RpcTarget.AllBuffered);
        }

        public void CallEndGame()
        {
            if (PhotonNetwork.IsMasterClient)
                photonView.RPC(nameof(EndTheGame), RpcTarget.AllBuffered);
        }

        public void CallPauseForContinue()
        {
            if (PhotonNetwork.IsMasterClient)
                photonView.RPC(nameof(PauseForContinue), RpcTarget.All);
        }

        public void CallResumeAfterGame()
        {
            if (PhotonNetwork.IsMasterClient)
                photonView.RPC(nameof(ResumeAfterGame), RpcTarget.AllBuffered);
        }

        // END RPC Remote Calls -----------------------------------------------------------------


        #endregion


        #region Private Methods

        IEnumerator DisconnectAndLoad()
        {
            pauseMenu.SetActive(false);

            if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
                while (PhotonNetwork.InRoom)
                    yield return null;
                Debug.Log("Disconnected from room!!!!!!");
            }

            if (players.Contains(myPlayer))
            {
                players.Remove(myPlayer);
            }


            /*if (PhotonNetwork.IsConnectedAndReady)
            {
                PhotonNetwork.Disconnect();
                while (PhotonNetwork.IsConnectedAndReady)
                    yield return null;
                Debug.Log("Disconnected from server!!!!!!");
            }*/


            yield return new WaitForSeconds(1);
            LeaveRoom();
        }


        #endregion


        #region PunCallbacks

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
        public void ResumeAfterGame()
        {
            Debug.Log("ResumeAfterGame RPC Called");
            pauseMenu.SetActive(false);
            settingsMenu.SetActive(false);
            isContinue = false;
            continueScreen.SetActive(false);
            if (myPlayer == null)
                myPlayer = FindPlayer.GetPlayer();
            myPlayer.GetComponent<NetworkPlayerManager>().EnableInputResumeButton();

            Time.timeScale = 1.0f;
        }

        [PunRPC]
        public void GameOver()
        {
            //if(myPlayer != null) { PhotonNetwork.Destroy(myPlayer); }

            Time.timeScale = 1.0f;
            ActivateCamera();
            AdsInitializer.timesPlayed++;
            isGameOver = true;
            SelectObject(restart);
            Cursor.lockState = CursorLockMode.None;
            gameOverScreen.SetActive(isGameOver);
            pauseMenu.SetActive(false);
            settingsMenu.SetActive(false);
            continueScreen.SetActive(false);
            isContinue = false;

#if !UNITY_PLAYSTATION
            CustomAnalytics.SendGameOver();
#endif
        }

        [PunRPC]
        public void NextWave()
        {
            playersSpawned = playersSpawned - playersEliminated;
            playersEliminated = 0;
            CurrentRound += 1;

            if (CurrentRound == 50 && (Social.localUser.authenticated || CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Steam))
            {
                LeaderboardManager.UnlockStayinAliveTogether();
            }

            nextWave();
        }

        [PunRPC]
        public void Restart()
        {
            Time.timeScale = 1.0f;
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

        [PunRPC]
        public void PauseForContinue()
        {
            isContinue = true;

            if (myPlayer == null)
                myPlayer = FindPlayer.GetPlayer();
            myPlayer.GetComponent<NetworkPlayerManager>().PauseForContinue();

            Time.timeScale = 0;
            continueScreen.SetActive(true);

            if (PhotonNetwork.IsMasterClient)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(endButton);
            }
            else
            {
                endButton.SetActive(false);
                continueButton.SetActive(false);
                waitingText.SetActive(true);
            }

        }

        [PunRPC]
        public void EndTheGame()
        {
            endGame();
        }

        #endregion

    }
}
