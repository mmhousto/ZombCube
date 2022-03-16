using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace Com.MorganHouston.ZombCube
{

    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;

        public static GameManager Instance { get { return _instance; } }
        public int CurrentRound { get; set; }
        public TextMeshProUGUI waveTxt;
        public GameObject gameOverScreen, pauseScreen, resume, restart, settingsScreen;

        public PlayerInput playerInput;

        private bool isPaused = false;

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

            if(!playerInput.actions.enabled)
                playerInput.actions.Enable();

            Cursor.lockState = CursorLockMode.Locked;
        }

        // Update is called once per frame
        void Update()
        {
            CheckForPause();
            SetCursorState();
           
        }

        public void NextWave()
        {
            CurrentRound += 1;
            waveTxt.text = "Wave: " + CurrentRound.ToString();
        }

        public void SetCursorState()
        {
            if (isPaused == true && isGameOver == false)
            {
                Cursor.lockState = CursorLockMode.None;
            }else if (isPaused == false && isGameOver == false)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }else
            {
                Cursor.lockState = CursorLockMode.None;
            }
        }

        public async void GameOver()
        {
            //playerInput.SwitchCurrentActionMap("UI");
            playerInput.actions.Disable();
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(restart);

            isGameOver = true;
            isPaused = false;
            Time.timeScale = 0;

            gameOverScreen.SetActive(true);
            pauseScreen.SetActive(false);
            settingsScreen.SetActive(false);
            CustomAnalytics.SendGameOver();

            await CloudSaveSample.CloudSaveSample.Instance.SavePlayerData(SaveSystem.LoadPlayer());
        }

        public void Restart()
        {
            SceneLoader.PlayGame();
        }

        public void Resume()
        {
            playerInput.actions.Enable();
            isPaused = false;
        }

        public void GoHome()
        {
            SceneLoader.ToMainMenu();
        }

        public void Pause(InputAction.CallbackContext context)
        {
            isPaused = !isPaused;
            if(isPaused == true && isGameOver == false)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(resume);
            }
        }

        private void CheckForPause()
        {
            if (isPaused == true && isGameOver == false)
            {
                playerInput.actions.Disable();
                pauseScreen.SetActive(true);
                Time.timeScale = 0;
            }
            else if (isPaused == false && isGameOver == false)
            {
                playerInput.actions.Enable();
                pauseScreen.SetActive(false);
                settingsScreen.SetActive(false);
                
                Time.timeScale = 1;
            } else if (isGameOver == true)
            {
                isPaused = false;
                playerInput.actions.Disable();
                pauseScreen.SetActive(false);
                settingsScreen.SetActive(false);
            }
        }
    }
}
