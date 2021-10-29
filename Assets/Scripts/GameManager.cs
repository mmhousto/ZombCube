using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MLAPI;
using MLAPI.Messaging;
using DapperDino.UMT.Lobby.Networking;

public class GameManager : NetworkBehaviour
{
    private static GameManager _instance;

    public static GameManager Instance { get { return _instance; } }
    private int CurrentRound { get; set; }
    public int currentPoints = 0;
    public TextMeshProUGUI waveTxt;
    private TextMeshProUGUI scoreText;
    public GameObject onScreenControls;
    public GameObject gameOverScreen;

    [SerializeField] public bool gameStarted = false;

    private bool gameOver = false;
   

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

        scoreText = GameObject.FindWithTag("Score").GetComponent<TextMeshProUGUI>();
        scoreText.text = "Score: " + currentPoints.ToString();

        gameOverScreen.SetActive(false);
        CurrentRound = 1;
        waveTxt.text = "Wave: " + CurrentRound.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        waveTxt.text = "Wave: " + CurrentRound.ToString();
        scoreText.text = "Score: " + currentPoints.ToString();

        if (gameStarted && !gameOver)
        {
            gameOverScreen.SetActive(false);
        }
        else if(gameOver && gameStarted)
        {
            gameOverScreen.SetActive(true);
        }
        
    }

    public void NextWave()
    {
        CurrentRound += 1;
    }

    public void GameStart()
    {
        gameStarted = true;
    }

    public void GameOver()
    {
        gameOver = true;
        Cursor.lockState = CursorLockMode.Confined;
        Time.timeScale = 0;
    }


    public void Restart()
    {
        gameOver = false;
        gameStarted = false;
        Destroy(this.gameObject);
        Destroy(Spawner.Instance.gameObject);
        SceneLoader.PlayGame();
    }

    public void GoHome()
    {
        gameOver = false;
        gameStarted = false;
        Destroy(this.gameObject);
        Destroy(Spawner.Instance.gameObject);
        SceneLoader.ToMainMenu();
    }

    public void AddPoints(int pointsToAdd)
    {
        currentPoints += pointsToAdd;
    }

    [ServerRpc]
    public void GameOverServerRpc()
    {
        gameOver = true;
        Cursor.lockState = CursorLockMode.Confined;
        Time.timeScale = 0;
        gameOverScreen.SetActive(true);
    }

    [ServerRpc]
    public void GameStartServerRpc()
    {
        gameStarted = true;
    }

    [ServerRpc]
    public void RestartServerRpc()
    {
        gameOver = false;
        gameStarted = false;
        Destroy(this.gameObject);
        Destroy(Spawner.Instance.gameObject);
        ServerGameNetPortal.Instance.StartGame();
    }

    [ServerRpc]
    public void GoHomeServerRpc()
    {
        gameOver = false;
        gameStarted = false;
        Destroy(this.gameObject);
        Destroy(Spawner.Instance.gameObject);
        ServerGameNetPortal.Instance.EndRound();
    }

    
}
