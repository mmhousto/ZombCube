using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using System;
using System.Threading.Tasks;
using StarterAssets;

namespace Com.MorganHouston.ZombCube
{

    public class PlayerManager : MonoBehaviour
    {

        public Material[] blasterMaterial;

        private Player player;

        public static int currentPoints = 0;

        public TextMeshProUGUI scoreText;
        private Slider healthBar;

        private float healthPoints = 100f;
        private bool isGameOver;

        // Start is called before the first frame update
        void Start()
        {
            player = Player.Instance;
            isGameOver = false;
            healthBar = GameObject.FindWithTag("Health").GetComponent<Slider>();
            healthPoints = 100f;
            currentPoints = 0;
            healthBar.value = healthPoints;
            scoreText.text = "Score: " + currentPoints.ToString();

            GetComponent<MeshRenderer>().material = blasterMaterial[player.currentSkin];

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

            if (healthPoints <= 0 && isGameOver == false)
            {
                healthPoints = 0;

                //Update player stats and save to cloud and disk.
                UpdateTotalPoints();
                UpdateHighestWave();

                try
                {
                    SavePlayerData();
                }
                catch
                {
                    Debug.Log("Failed to save local data");
                }

                try
                {
                    CloudSaveLogin.Instance.SaveCloudData();
                }
                catch
                {
                    Debug.Log("Failed to save cloud data");
                }


                GameManager.Instance.GameOver();
                isGameOver = GameManager.Instance.isGameOver;
            }
        }

        public static void AddPoints(int pointsToAdd)
        {
            currentPoints += pointsToAdd;
            Player.Instance.totalPointsEarned += pointsToAdd;
        }

        public void Damage(float damageTaken)
        {
            healthPoints -= damageTaken;
        }

        private void UpdateTotalPoints()
        {
            player.points += currentPoints;
        }

        private void UpdateHighestWave()
        {
            int endingRound = GameManager.Instance.CurrentRound;
            if (player.highestWave < endingRound)
            {
                player.highestWave = endingRound;
            }
        }

        public void SavePlayerData()
        {
            SaveSystem.SavePlayer(player);
        }


    }

}
