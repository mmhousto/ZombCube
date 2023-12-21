using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Com.GCTC.ZombCube
{
    public class CouchCoopManager : MonoBehaviour
    {
        private static CouchCoopManager _instance;

        public static CouchCoopManager Instance { get { return _instance; } }
        public List<int> joinedPlayerIDs = new List<int>();
        public List<GameObject> joinedPlayers = new List<GameObject>();
        public GameObject playerPrefab;
        public RenderTexture[] minimaps;
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

        public void DestroyPlayerObjects()
        {
            foreach (GameObject player in joinedPlayers)
            {
                Destroy(player.transform.GetChild(0).gameObject);
            }
        }

        public void DestroyPlayers()
        {
            foreach(GameObject player in joinedPlayers)
            {
                Destroy(player);
            }
        }

        public void EnableDisableInput(bool newState)
        {
            foreach(GameObject playerInput in joinedPlayers)
            {
                if (newState == true)
                {
                    playerInput.GetComponent<PlayerInput>().actions.Enable();
                }
                else
                {
                    playerInput.GetComponent<PlayerInput>().actions.Disable();
                }
            }
        }

        // called second
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.buildIndex == 5 && GameManager.mode == 1)
            {
                // Disable Elimination camera
                GameManager.Instance.EnableDisableElimCam(false);

                // Sets up canvas
                var canvas = GameObject.FindWithTag("Canvas");
                canvas.transform.GetChild(0).gameObject.SetActive(false);
                canvas.transform.GetChild(1).gameObject.SetActive(false);
                canvas.transform.GetChild(2).gameObject.SetActive(false);
                canvas.transform.GetChild(6).gameObject.SetActive(false);
                canvas.transform.GetChild(7).gameObject.SetActive(false);
                canvas.transform.GetChild(9).gameObject.SetActive(false);

                played = true;
                // Gets spawn points
                spawnPoints[0] = GameObject.Find("SP1").transform;
                spawnPoints[1] = GameObject.Find("SP2").transform;
                spawnPoints[2] = GameObject.Find("SP3").transform;
                spawnPoints[3] = GameObject.Find("SP4").transform;
                for (int i = 0; i < joinedPlayerIDs.Count; i++)
                {
                    // spawns player
                    GameObject clone = Instantiate(playerPrefab, spawnPoints[i].position, spawnPoints[i].rotation, joinedPlayers[i].transform);

                    // Sets up player input
                    var playerInput = joinedPlayers[i].GetComponent<PlayerInput>();
                    playerInput.camera = clone.GetComponentInChildren<Camera>();
                    playerInput.uiInputModule = clone.GetComponentInChildren<InputSystemUIInputModule>();
                    playerInput.currentActionMap = playerInput.actions.FindActionMap("Player");
                    playerInput.actions.Enable();

                    // Assign minimap texture to camera and minimap
                    clone.GetComponentInChildren<RawImage>().texture = minimaps[i];
                    clone.GetComponent<PlayerManager>().minimapCam.targetTexture = minimaps[i];
                    clone.GetComponentInChildren<Canvas>().transform.parent = joinedPlayers[i].transform;

                    // Destroy audio listeners that are not p1
                    if(i > 0)
                        Destroy(clone.GetComponentInChildren<AudioListener>());
                }

                // enable split-screen
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
