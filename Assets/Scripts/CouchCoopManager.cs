using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
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
        private Transform[] spawnPoints = new Transform[3];
        private PlayerInputManager playerInputManager;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            
            playerInputManager = GetComponent<PlayerInputManager>();
        }

        // called second
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if(scene.buildIndex == 5)
            {
                
                spawnPoints[0] = GameObject.Find("SP2").transform;
                spawnPoints[1] = GameObject.Find("SP3").transform;
                spawnPoints[2] = GameObject.Find("SP4").transform;
                for(int i = 1; i < joinedPlayerIDs.Count; i++)
                {
                    GameObject clone = Instantiate(playerPrefab, spawnPoints[i - 1].position, spawnPoints[i - 1].rotation);
                    
                }
                playerInputManager.splitScreen = true;
            }
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
