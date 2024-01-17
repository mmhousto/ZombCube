using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Security.Cryptography;

namespace Com.GCTC.ZombCube
{

    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;

        public static GameManager Instance { get { return _instance; } }
        public static int mode;
        public GameObject playerPrefab;
        public int CurrentRound { get; set; }
        public GameObject[] grenades;
        public TextMeshProUGUI waveTxt;
        public GameObject gameOverScreen, pauseScreen, resume, restart, settingsScreen, onScreenControls, weaponSelect;
        public Camera eliminatedCam;
        private PlayerInput playerInput;
        private PlayerInputManager playerInputManager;
        private CouchCoopManager couchCoopManager;
        private GameObject player;
        [SerializeField] private bool overrideCursor = false;

        private bool isPaused = false;
        public int numOfPlayers;

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

            Time.timeScale = 1;

            if(mode == 1)
            {
                playerInputManager = GameObject.Find("CoopManager").GetComponent<PlayerInputManager>();
                couchCoopManager = playerInputManager.gameObject.GetComponent<CouchCoopManager>();
                numOfPlayers = couchCoopManager.GetNumOfPlayers();
                weaponSelect.SetActive(false);
            }
            else
            {
                Transform sp1 = GameObject.Find("SP1").transform;
                player = Instantiate(playerPrefab, sp1.position, sp1.rotation);
                playerInput = player.GetComponent<PlayerInput>();
                EnableDisableElimCam(false);

                if (grenades != null)
                {
                    grenades[2].transform.GetChild(0).gameObject.SetActive(false);
                    grenades[3].transform.GetChild(0).gameObject.SetActive(false);
                }
                numOfPlayers = 1;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            gameOverScreen.SetActive(false);
            pauseScreen.SetActive(false);
            isGameOver = false;

            CurrentRound = 1;
            waveTxt.text = "Wave: " + CurrentRound.ToString();

            CustomAnalytics.SendGameStart();

            if (mode == 1)
            {
                couchCoopManager.EnableDisableInput(true);
            }
            else if (!playerInput.actions.enabled)
                playerInput.actions.Enable();

            if(overrideCursor == false)
                Cursor.lockState = CursorLockMode.Locked;
            else
                Cursor.lockState = CursorLockMode.None;

            Projectile.pointsToAdd = 10;
        }

        // Update is called once per frame
        void Update()
        {
            CheckForPause();
            SetCursorState();

            if (grenades != null && grenades.Length > 0 && mode == 0)
            {
                for (int i = 0; i < grenades.Length; i++)
                {
                    if (i < player.GetComponent<PlayerManager>().grenade.grenadeCount)
                        grenades[i].transform.GetChild(0).gameObject.SetActive(true);
                    else grenades[i].transform.GetChild(0).gameObject.SetActive(false);
                }
            }
        }

        public GameObject GetPlayer()
        {
            return player;
        }

        public void EnableDisableElimCam(bool newState)
        {
            eliminatedCam.enabled = newState;
        }

        public void SetElimCameraForThreePlayers()
        {
            eliminatedCam.rect = new Rect(.5f, 0, .5f, .5f);
            eliminatedCam.enabled = true;

        }

        public void NextWave()
        {
            CurrentRound += 1;
            waveTxt.text = "Wave: " + CurrentRound.ToString();

            if (CurrentRound == 50 && Social.localUser.authenticated)
            {
                LeaderboardManager.UnlockStayinAlive();
            }
        }

        public void SetCursorState()
        {
            if (isPaused == true && isGameOver == false)
                Cursor.lockState = CursorLockMode.None;
            else if (overrideCursor)
                Cursor.lockState = CursorLockMode.None;
            else if (isPaused == false && isGameOver == false)
                Cursor.lockState = CursorLockMode.Locked;
            else
                Cursor.lockState = CursorLockMode.None;
        }

        public void GameOver()
        {
            AdsInitializer.timesPlayed++;
            //playerInput.SwitchCurrentActionMap("UI");
            if (mode == 1)
            {
                couchCoopManager.EnableDisableInput(false);
            }
            else
                playerInput.actions.Disable();

            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(restart);

            isGameOver = true;
            isPaused = false;
            Time.timeScale = 0;

#if (UNITY_IOS || UNITY_ANDROID)
            onScreenControls.SetActive(false);
#endif
            gameOverScreen.SetActive(true);
            pauseScreen.SetActive(false);
            settingsScreen.SetActive(false);
            CustomAnalytics.SendGameOver();
        }

        public void Restart()
        {
            if (mode == 1)
            {
                playerInputManager.splitScreen = false;
                playerInputManager.GetComponent<CouchCoopManager>().DestroyPlayerObjects();
                EnableDisableElimCam(true);
            }
            SceneLoader.PlayGame();
        }

        public void Resume()
        {
#if (UNITY_IOS || UNITY_ANDROID)
            onScreenControls.SetActive(true);
#endif
            if (mode == 1)
            {
                couchCoopManager.EnableDisableInput(true);
            }
            else
                playerInput.actions.Enable();

            isPaused = false;
        }

        public void GoHome()
        {
            if (mode == 1)
            {
                playerInputManager.GetComponent<CouchCoopManager>().DestroyPlayers();
                EnableDisableElimCam(true);
            }

            SceneLoader.ToMainMenu();

            if(playerInputManager != null)
                Destroy(playerInputManager.gameObject);

            Time.timeScale = 1;
        }

        public void PauseInput()
        {
            isPaused = !isPaused;
            if (isPaused == true && isGameOver == false)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(resume);
            }
        }

        public void Pause(InputAction.CallbackContext context)
        {
            PauseInput();
        }

        private void CheckForPause()
        {
            if (isPaused == true && isGameOver == false)
            {
                if (mode == 1)
                {
                    couchCoopManager.EnableDisableInput(false);
                }
                else
                    playerInput.actions.Disable();

                pauseScreen.SetActive(true);

#if (UNITY_IOS || UNITY_ANDROID)
                onScreenControls.SetActive(false);
#endif

                Time.timeScale = 0;
            }
            else if (isPaused == false && isGameOver == false)
            {
                if (mode == 1)
                {
                    couchCoopManager.EnableDisableInput(true);
                }
                else
                    playerInput.actions.Enable();

                pauseScreen.SetActive(false);
                settingsScreen.SetActive(false);

#if (UNITY_IOS || UNITY_ANDROID)
                onScreenControls.SetActive(true);
#endif

                Time.timeScale = 1;
            } else if (isGameOver == true)
            {
#if (UNITY_IOS || UNITY_ANDROID)
                onScreenControls.SetActive(false);
#endif

                isPaused = false;

                if (mode == 1)
                {
                    couchCoopManager.EnableDisableInput(false);
                }
                else
                    playerInput.actions.Disable();

                pauseScreen.SetActive(false);
                settingsScreen.SetActive(false);
            }
        }
    }
}
