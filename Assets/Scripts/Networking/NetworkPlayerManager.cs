using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;

namespace Com.MorganHouston.ZombCube
{

    public class NetworkPlayerManager : MonoBehaviour
    {

        public Material[] blasterMaterial;

        private Player player;

        private PhotonView view;

        private GameObject onScreenControls;

        public static int currentPoints = 0;

        private TextMeshProUGUI scoreText;
        private Slider healthBar;

        private static float healthPoints = 100f;
        private bool isGameOver;

        // Start is called before the first frame update
        void Start()
        {
            view = GetComponent<PhotonView>();
            onScreenControls = GameObject.FindWithTag("ScreenControls");

#if UNITY_ANDROID
    onScreenControls.SetActive(true);

#elif UNITY_IOS
    onScreenControls.SetActive(true);

#else
            onScreenControls.SetActive(false);

#endif

            player = GetComponent<Player>();
            LoadPlayerData();
            healthBar = GameObject.FindWithTag("Health").GetComponent<Slider>();
            scoreText = GameObject.FindWithTag("Score").GetComponent<TextMeshProUGUI>();
            healthPoints = 100f;
            currentPoints = 0;
            healthBar.value = healthPoints;
            scoreText.text = "Score: " + currentPoints.ToString();

            GameObject[] blaster = GameObject.FindGameObjectsWithTag("Blaster");

            foreach (GameObject item in blaster)
            {
                item.GetComponent<MeshRenderer>().material = blasterMaterial[player.currentBlaster];
            }
        }

        // Update is called once per frame
        void Update()
        {
            healthBar.value = healthPoints;
            scoreText.text = "Score: " + currentPoints.ToString();

            if (healthPoints <= 0 && !isGameOver)
            {
                healthPoints = 0;
                NetworkGameManager.Instance.EliminatePlayer();
                UpdateTotalPoints();
                SavePlayerData();
                if (NetworkGameManager.Instance.playersEliminated == PhotonNetwork.CurrentRoom.PlayerCount)
                {
                    NetworkSpawner.Instance.gameOver = true;
                    NetworkGameManager.Instance.CallGameOver();
                    isGameOver = true;
                }
                
            }
        }

        public static void AddPoints(int pointsToAdd)
        {
            currentPoints += pointsToAdd;
        }

        public static void Damage(float damageTaken)
        {
            healthPoints -= damageTaken;
        }

        private void UpdateTotalPoints()
        {
            player.points += currentPoints;
        }

        public void SavePlayerData()
        {
            SaveSystem.SavePlayer(player);
        }

        public void LoadPlayerData()
        {
            SaveData data = SaveSystem.LoadPlayer();

            player.playerName = data.playerName;
            player.coins = data.coins;
            player.points = data.points;
            player.highestWave = data.highestWave;
            player.currentBlaster = data.currentBlaster;
            player.ownedBlasters = data.ownedBlasters;
        }


    }

}
