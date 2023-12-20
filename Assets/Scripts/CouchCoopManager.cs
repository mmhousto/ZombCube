using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace Com.GCTC.ZombCube
{
    public class CouchCoopManager : MonoBehaviour
    {
        private static CouchCoopManager _instance;

        public static CouchCoopManager Instance { get { return _instance; } }
        public List<int> joinedPlayerIDs = new List<int>();
        public List<GameObject> joinedPlayers = new List<GameObject>();
        public GameObject playerPrefab;
        private Transform[] spawnPoints = new Transform[4];
        private PlayerInputManager playerInputManager;
        private bool played;
        private int spawnedInPlayers = 0;

        private void Awake()
        {
            if (_instance != null && _instance == this)
            {
                Destroy(this.gameObject);
            }

                _instance = this;
                DontDestroyOnLoad(gameObject);
            
            playerInputManager = GetComponent<PlayerInputManager>();
            spawnedInPlayers = 0;
        }

        public void DestroyPlayers()
        {
            foreach(GameObject player in joinedPlayers)
            {
                Destroy(player);
            }
        }

        // called second
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if(scene.buildIndex == 1 && played == true)
            {
                Destroy(this.gameObject);
            }

            if (scene.buildIndex == 5 && GameManager.mode == 1)
            {
                var canvas = GameObject.FindWithTag("Canvas");
                canvas.transform.GetChild(0).gameObject.SetActive(false);
                canvas.transform.GetChild(1).gameObject.SetActive(false);
                canvas.transform.GetChild(2).gameObject.SetActive(false);
                canvas.transform.GetChild(6).gameObject.SetActive(false);
                canvas.transform.GetChild(7).gameObject.SetActive(false);
                canvas.transform.GetChild(9).gameObject.SetActive(false);
                GameObject.Find("Player").SetActive(false);

                played = true;
                spawnPoints[0] = GameObject.Find("SP1").transform;
                spawnPoints[1] = GameObject.Find("SP2").transform;
                spawnPoints[2] = GameObject.Find("SP3").transform;
                spawnPoints[3] = GameObject.Find("SP4").transform;
                for (int i = spawnedInPlayers; i < joinedPlayerIDs.Count; i++)
                {
                    GameObject clone = Instantiate(playerPrefab, spawnPoints[i].position, spawnPoints[i].rotation, joinedPlayers[i].transform);
                    var playerInput = joinedPlayers[i].transform.GetComponent<PlayerInput>();
                    playerInput.camera = clone.GetComponentInChildren<Camera>();
                    playerInput.uiInputModule = clone.GetComponentInChildren<InputSystemUIInputModule>();
                    playerInput.currentActionMap = playerInput.actions.FindActionMap("Player");
                    playerInput.actions.Enable();
                    spawnedInPlayers++;

                    if(i > 0)
                        clone.GetComponentInChildren<AudioListener>().enabled = false;
                }

                foreach (var player in joinedPlayers)
                {
                    player.GetComponentInChildren<PlayerManager>().ResetPlayer();
                }

                playerInputManager.splitScreen = true;
            }
            else if (scene.buildIndex == 5) Destroy(gameObject);
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        // called when the game is terminated
        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}
