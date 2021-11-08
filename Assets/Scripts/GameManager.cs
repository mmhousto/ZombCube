using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Com.MorganHouston.ZombCube
{

    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;

        public static GameManager Instance { get { return _instance; } }
        private static int CurrentRound { get; set; }
        public TextMeshProUGUI waveTxt;
        public GameObject onScreenControls;
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
        }

        // Update is called once per frame
        void Update()
        {
            waveTxt.text = "Wave: " + CurrentRound.ToString();
        }

        public void NextWave()
        {
            CurrentRound += 1;
        }

        public void GameOver()
        {
            Cursor.lockState = CursorLockMode.Confined;
            Time.timeScale = 0;
            gameOverScreen.SetActive(true);
        }

        public void Restart()
        {
            SceneLoader.PlayGame();
        }

        public void GoHome()
        {
            SceneLoader.ToMainMenu();
        }
    }
}
