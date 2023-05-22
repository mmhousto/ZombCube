using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using System;
using System.Threading.Tasks;
using StarterAssets;

namespace Com.GCTC.ZombCube
{

    public class PlayerManager : MonoBehaviour
    {

        public Material[] blasterMaterial;

        private Player player;

        public static int currentPoints = 0;

        public TextMeshProUGUI scoreText;
        private GameObject contextPrompt;
        private TextMeshProUGUI contextPromptText;
        private Slider healthBar;

        private float healthPoints = 100f;
        private bool isGameOver;
        private bool pressedUse;

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
            contextPrompt = GameObject.FindWithTag("ContextPrompt");
            contextPromptText = contextPrompt.GetComponent<TextMeshProUGUI>();
            contextPrompt.SetActive(false);

            GetComponent<MeshRenderer>().material = blasterMaterial[(player != null) ? player.currentSkin : 0];

            GameObject[] blaster = GameObject.FindGameObjectsWithTag("Blaster");

            foreach (GameObject item in blaster)
            {
                item.GetComponent<MeshRenderer>().material = blasterMaterial[(player != null) ? player.currentBlaster : 0];
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
                UpdateLeaderboards();

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

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("HealthPack"))
            {
                contextPrompt.SetActive(false);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if(other.CompareTag("HealthPack") && other.GetComponent<HealthPack>().isUsable)
            {
                contextPrompt.SetActive(true);
                contextPromptText.text = other.GetComponent<HealthPack>().contextPrompt;
            }

            if(other.CompareTag("HealthPack") && other.GetComponent<HealthPack>().isUsable && pressedUse && healthPoints <= 99)
            {
                other.GetComponent<HealthPack>().StartResetHealthPack();

                Damage(-20);
                SpendPoints(500);

                if(healthPoints >= 100) { healthPoints = 100; }
            }
        }

        public void SpendPoints(int pointsToSpend)
        {
            currentPoints -= pointsToSpend;
        }

        public static void AddPoints(int pointsToAdd)
        {
            currentPoints += pointsToAdd;
            if(Player.Instance != null)
                Player.Instance.totalPointsEarned += pointsToAdd;
        }

        public void Damage(float damageTaken)
        {
            healthPoints -= damageTaken;
        }

        private void UpdateLeaderboards()
        {
            if (Social.localUser.authenticated)
            {
                LeaderboardManager.UpdateMostPointsLeaderboard();
                LeaderboardManager.UpdateSoloHighestWaveLeaderboard();
                LeaderboardManager.UpdateCubesDestroyedLeaderboard();
                LeaderboardManager.UpdateAccuracyLeaderboard();
            }
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

        public void OnInteract(InputAction.CallbackContext context)
        {
            pressedUse = context.performed;
        }


    }

}
