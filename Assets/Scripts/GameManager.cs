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
        public GameObject onScreenControls;
        public GameObject gameOverScreen;

        private bool isPaused = false;


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

#if UNITY_ANDROID
    onScreenControls.SetActive(true);

#elif UNITY_IOS
    onScreenControls.SetActive(true);

#else
            onScreenControls.SetActive(false);
#endif

            gameOverScreen.SetActive(false);
            CurrentRound = 1;
            waveTxt.text = "Wave: " + CurrentRound.ToString();
            CustomAnalytics.SendGameStart();
        }

        // Update is called once per frame
        void Update()
        {
            waveTxt.text = "Wave: " + CurrentRound.ToString();

            if (isPaused)
            {
                Time.timeScale = 0;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Time.timeScale = 1;
                Cursor.lockState = CursorLockMode.Locked;
            }
           
        }

        public void NextWave()
        {
            CurrentRound += 1;
        }

        public async void GameOver()
        {
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

        public void GoHome()
        {
            SceneLoader.ToMainMenu();
        }

        public void Pause(InputAction.CallbackContext context)
        {
            isPaused = !isPaused;
        }
    }
}
