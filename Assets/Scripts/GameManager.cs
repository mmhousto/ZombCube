using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

namespace Com.MorganHouston.ZombCube
{

    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;

        public static GameManager Instance { get { return _instance; } }
        public int CurrentRound { get; set; }
        public TextMeshProUGUI waveTxt;
        public GameObject gameOverScreen, pauseScreen;

        private bool isPaused = false;

        private bool isGameOver = false;


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
            CurrentRound = 1;
            waveTxt.text = "Wave: " + CurrentRound.ToString();
            CustomAnalytics.SendGameStart();
        }

        // Update is called once per frame
        void Update()
        {
            CheckForPause(); 
           
        }

        public void NextWave()
        {
            CurrentRound += 1;
            waveTxt.text = "Wave: " + CurrentRound.ToString();
        }

        public async void GameOver()
        {
            isGameOver = true;
            Cursor.lockState = CursorLockMode.Confined;
            Time.timeScale = 0;
            gameOverScreen.SetActive(true);
            CustomAnalytics.SendGameOver();

            await CloudSaveSample.CloudSaveSample.Instance.SavePlayerData(SaveSystem.LoadPlayer());
        }

        public void Restart()
        {
            SceneLoader.PlayGame();
        }

        public void Resume()
        {
            isPaused = false;
        }

        public void GoHome()
        {
            SceneLoader.ToMainMenu();
        }

        public void Pause(InputAction.CallbackContext context)
        {
            isPaused = !isPaused;
        }

        private void CheckForPause()
        {
            if (isPaused == true)
            {
                pauseScreen.SetActive(true);
                Time.timeScale = 0;
                Cursor.lockState = CursorLockMode.None;
            }
            else if (isPaused == false && isGameOver == false)
            {
                pauseScreen.SetActive(false);
                Time.timeScale = 1;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }
}
