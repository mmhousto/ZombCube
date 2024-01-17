using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Com.GCTC.ZombCube
{

    public class PlayerManager : MonoBehaviour
    {

        public Material[] blasterMaterial;

        private Player player;

        public static int currentPoints = 0;

        private SwapManager swapManager;
        private FullyAuto fullyAutoSMB;
        private AssaultBlaster aB;

        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI waveText;
        public GameObject contextPrompt;
        private TextMeshProUGUI contextPromptText;
        public Slider healthBar;
        public Camera minimapCam;

        private float healthPoints = 100f;
        private bool isGameOver;
        private bool pressedUse;
        protected float holdTime;
        [SerializeField]
        protected bool isInteractHeld;
        [SerializeField]
        protected bool isInteracting;
        [SerializeField]
        protected bool startedHold;

        // Start is called before the first frame update
        void Start()
        {
            player = Player.Instance;
            isGameOver = false;

            swapManager = GetComponent<SwapManager>();
            fullyAutoSMB = GetComponent<FullyAuto>();
            aB = GetComponent<AssaultBlaster>();

            if(healthBar == null && GameObject.FindWithTag("Health") != null)
                healthBar = GameObject.FindWithTag("Health").GetComponent<Slider>();

            healthPoints = 100f;
            currentPoints = 0;
            holdTime = 0;

            if(healthBar != null)
                healthBar.value = healthPoints;

            if(scoreText == null && GameObject.FindWithTag("Score") != null)
                scoreText = GameObject.FindWithTag("Score").GetComponent<TextMeshProUGUI>();

            if(scoreText != null)
                scoreText.text = "Score: " + currentPoints.ToString();

            if(contextPrompt == null && GameObject.FindWithTag("ContextPrompt") != null)
                contextPrompt = GameObject.FindWithTag("ContextPrompt");

            if(contextPrompt != null)
            {
                contextPromptText = contextPrompt.GetComponent<TextMeshProUGUI>();
                contextPrompt.SetActive(false);
            }
            GetComponentInChildren<MeshRenderer>().material = blasterMaterial[(player != null) ? player.currentSkin : 0];

            /*if (blaster == null)
                blaster = GameObject.FindGameObjectsWithTag("Blaster");*/

            MeshRenderer[] blasters = GetComponentsInChildren<MeshRenderer>();

            for(int i = 0; i < blasters.Length; i++)
            {
                if(blasters[i].tag == "Blaster")
                    blasters[i].material = blasterMaterial[(player != null) ? player.currentBlaster : 0];
            }

            swapManager.DisableWeapons();

            /*foreach (GameObject item in blaster)
            {
                item.GetComponent<MeshRenderer>().material = blasterMaterial[(player != null) ? player.currentBlaster : 0];
            }*/
        }

        // Update is called once per frame
        void Update()
        {
            if(healthBar != null)
                healthBar.value = healthPoints;

            if (scoreText != null)
                scoreText.text = "Score: " + currentPoints.ToString();

            if(waveText != null && waveText?.text != GameManager.Instance?.waveTxt.text)
                waveText.text = GameManager.Instance?.waveTxt.text;

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
            if (other.CompareTag("HealthPack") || other.CompareTag("SMB") || other.CompareTag("AB"))
            {
                contextPrompt.SetActive(false);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            HealthPack hp;
            other.TryGetComponent<HealthPack>(out hp);

            if(other.CompareTag("HealthPack") && hp.isUsable)
            {
                contextPrompt.SetActive(true);
                contextPromptText.text = hp.contextPrompt;
            }

            if(other.CompareTag("HealthPack") && hp.isUsable && isInteractHeld && healthPoints <= 99 && currentPoints >= 500)
            {
                hp.StartResetHealthPack();

                Damage(-20);
                SpendPoints(500);

                if(healthPoints >= 100) { healthPoints = 100; }
            }

            WeaponPickup wp;
            other.TryGetComponent<WeaponPickup>(out wp);

            if (other.CompareTag("SMB") && wp.isUsable)
            {
                contextPrompt.SetActive(true);
                contextPromptText.text = wp.contextPrompt;
            }

            if (other.CompareTag("SMB") && wp.isUsable && isInteractHeld && currentPoints >= 2500)
            {
                wp.StartResetWeapon();

                SpendPoints(2500);

                if (swapManager.HasWeapon(2))
                {
                    fullyAutoSMB.GetAmmo();
                }
                else
                {
                    swapManager.GetWeapon(2);
                }
            }

            if (other.CompareTag("AB") && wp.isUsable)
            {
                contextPrompt.SetActive(true);
                contextPromptText.text = wp.contextPrompt;
            }

            if (other.CompareTag("AB") && wp.isUsable && isInteractHeld && currentPoints >= 2500)
            {
                wp.StartResetWeapon();

                SpendPoints(2500);

                if (swapManager.HasWeapon(3))
                {
                    aB.GetAmmo();
                }
                else
                {
                    swapManager.GetWeapon(3);
                }
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

        public void ResetPlayer()
        {
            healthPoints = 100;
            currentPoints = 0;
            healthBar.value = healthPoints;
        }

        public void SavePlayerData()
        {
            SaveSystem.SavePlayer(player);
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            InteractInput(context.performed);
        }

        public void OnInteract(InputValue context)
        {
            InteractInput(context.isPressed);
        }

        public void InteractInput(bool newValue)
        {
            isInteracting = newValue;

            if (isInteracting && startedHold == false && isInteractHeld == false)
            {
                startedHold = true;
                StartCoroutine(ChargeHoldTime());
            }
            else if (isInteracting == false)
            {
                isInteractHeld = false;
            }
        }

        protected IEnumerator ChargeHoldTime()
        {
            while (isInteracting && holdTime < 0.5f)
            {
                holdTime += Time.deltaTime; // Increase launch power over time
                yield return null;
            }

            if (holdTime < 0.5)
            {
                isInteractHeld = false;
                Debug.Log("Not Holding!");
            }
            else
            {
                // interacting
                Debug.Log("Holding!");
                isInteractHeld = true;
            }
            yield return new WaitForSeconds(0.5f);

            holdTime = 0f; // Reset
            startedHold = false;
            isInteractHeld = false;
            isInteracting = false;
        }

        public void OnGamePause(InputAction.CallbackContext context)
        {
            GameManager.Instance.PauseInput();
        }

        public void OnGamePause(InputValue context)
        {
            GameManager.Instance.PauseInput();
        }

    }

}
