#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace Com.GCTC.ZombCube
{

    public class PlayerManager : MonoBehaviour
    {

        public Material[] blasterMaterial;

        private Player player;

        public static int currentPoints = 0;

        /// <summary>Players in the current solo / co-op game.</summary>
        public static readonly System.Collections.Generic.List<PlayerManager> Players = new System.Collections.Generic.List<PlayerManager>();

        /// <summary>Chill Co-op: eliminated and waiting to respawn at the start of the next wave.</summary>
        public bool IsDown { get; private set; }
        private readonly System.Collections.Generic.List<Behaviour> disabledWhileDown = new System.Collections.Generic.List<Behaviour>();
        private readonly System.Collections.Generic.List<Renderer> hiddenWhileDown = new System.Collections.Generic.List<Renderer>();
        private readonly System.Collections.Generic.List<GameObject> untaggedWhileDown = new System.Collections.Generic.List<GameObject>();

        private SwapManager swapManager;
        private FullyAuto fullyAutoSMB;
        private AssaultBlaster aB;
        private Shotblaster shotblaster;
        private SniperBlaster sniperBlaster;
        public LaunchGrenade grenade;

        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI waveText;
        public TextMeshProUGUI ammoText;
        public GameObject[] grenades;
        public GameObject contextPrompt;
        private TextMeshProUGUI contextPromptText;
        private GameObject contextPromptImage;
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
        protected bool isControllerConnected = false;
        protected bool isSteamOverlayActive = false;

#if !DISABLESTEAMWORKS
        protected Callback<GameOverlayActivated_t> overlayIsOn;
#endif

        // Start is called before the first frame update
        void Start()
        {
#if !DISABLESTEAMWORKS
            // Only touch Steam when it is running; otherwise steam_api is loaded for nothing and
            // takes over crash handling. Each player (up to 4 in co-op) registers its own callback.
            if (SteamManager.IsReady)
                overlayIsOn = Callback<GameOverlayActivated_t>.Create(PauseGameIfSteamOverlayOn);
#endif
            // Check if a controller is initially connected
            if (Gamepad.current != null)
            {
                isControllerConnected = true;
            }

            player = Player.Instance;
            isGameOver = false;

            swapManager = GetComponent<SwapManager>();
            fullyAutoSMB = GetComponent<FullyAuto>();
            shotblaster = GetComponent<Shotblaster>();
            aB = GetComponent<AssaultBlaster>();
            sniperBlaster = GetComponent<SniperBlaster>();
            grenade = GetComponent<LaunchGrenade>();

            if (healthBar == null && GameObject.FindWithTag("Health") != null)
                healthBar = GameObject.FindWithTag("Health").GetComponent<Slider>();

            healthPoints = 100f;
            currentPoints = 0;
            holdTime = 0;

            if (healthBar != null)
                healthBar.value = healthPoints;

            if (scoreText == null && GameObject.FindWithTag("Score") != null)
                scoreText = GameObject.FindWithTag("Score").GetComponent<TextMeshProUGUI>();

            if (scoreText != null)
                scoreText.text = "Score: " + currentPoints.ToString();

            if (waveText == null && GameObject.FindWithTag("Wave") != null)
                waveText = GameObject.FindWithTag("Wave").GetComponent<TextMeshProUGUI>();

            if (waveText != null)
                waveText.text = "Wave: 1";

            if (ammoText == null && GameObject.FindWithTag("Ammo") != null)
            {
                ammoText = GameObject.FindWithTag("Ammo").GetComponent<TextMeshProUGUI>();
            }

            if (ammoText != null)
                ammoText.text = "";

            if (grenades != null && grenades.Length > 0)
            {
                grenades[2].transform.GetChild(0).gameObject.SetActive(false);
                grenades[3].transform.GetChild(0).gameObject.SetActive(false);
            }

            if (contextPrompt == null && GameObject.FindWithTag("ContextPrompt") != null)
                contextPrompt = GameObject.FindWithTag("ContextPrompt");

            if (contextPrompt != null)
            {
                contextPromptText = contextPrompt.GetComponentInChildren<TextMeshProUGUI>();
                contextPromptText.gameObject.SetActive(false);
                contextPromptImage = contextPrompt.GetComponentInChildren<Image>().gameObject;
                contextPromptImage.SetActive(false);
            }
            GetComponentInChildren<MeshRenderer>().material = CosmeticDatabase.MaterialAt((player != null) ? player.currentSkin : 0);

            /*if (blaster == null)
                blaster = GameObject.FindGameObjectsWithTag("Blaster");*/

            MeshRenderer[] blasters = GetComponentsInChildren<MeshRenderer>();

            for (int i = 0; i < blasters.Length; i++)
            {
                if (blasters[i].tag == "Blaster")
                    blasters[i].material = CosmeticDatabase.MaterialAt((player != null) ? player.currentBlaster : 0);
            }

            swapManager.DisableWeapons();

            /*foreach (GameObject item in blaster)
            {
                item.GetComponent<MeshRenderer>().material = CosmeticDatabase.MaterialAt((player != null) ? player.currentBlaster : 0);
            }*/
        }

        // Update is called once per frame
        void Update()
        {
            CheckControllerConnection();

            if (healthBar != null)
                healthBar.value = healthPoints;

            if (scoreText != null)
                scoreText.text = "Score: " + currentPoints.ToString();

            if (ammoText != null && fullyAutoSMB.enabled == true)
                ammoText.text = $"{fullyAutoSMB.currentAmmoInClip}/{fullyAutoSMB.reserveAmmo}";
            else if (ammoText != null && aB.enabled == true)
                ammoText.text = $"{aB.currentAmmoInClip}/{aB.reserveAmmo}";
            else if (ammoText != null && shotblaster.enabled == true)
                ammoText.text = $"{shotblaster.currentAmmoInClip}/{shotblaster.reserveAmmo}";
            else if (ammoText != null && sniperBlaster.enabled == true)
                ammoText.text = $"{sniperBlaster.currentAmmoInClip}/{sniperBlaster.reserveAmmo}";
            else if (ammoText != null)
                ammoText.text = "";

            if (grenades != null && grenades.Length > 0)
            {
                for (int i = 0; i < grenades.Length; i++)
                {
                    if (i < grenade.grenadeCount)
                        grenades[i].transform.GetChild(0).gameObject.SetActive(true);
                    else
                        grenades[i].transform.GetChild(0).gameObject.SetActive(false);
                }
            }

            if (waveText != null && waveText?.text != GameManager.Instance?.waveTxt.text)
                waveText.text = GameManager.Instance?.waveTxt.text;

            if (healthPoints <= 0 && isGameOver == false && !IsDown)
            {
                healthPoints = 0;

                // Chill Co-op: go down and respawn next wave while a teammate is still up.
                if (GameManager.mode == 1 && GameManager.chillCoop && AnyTeammateUp())
                    GoDown();
                else
                    SaveDataEndGame();
            }

        }

        private void OnEnable()
        {
            GameManager.endGame += SaveDataEndGame;
            GameManager.waveStarted += RespawnIfDown;
            if (!Players.Contains(this)) Players.Add(this);
        }

        private void OnDisable()
        {
            GameManager.endGame -= SaveDataEndGame;
            GameManager.waveStarted -= RespawnIfDown;
            Players.Remove(this);
        }

        private bool AnyTeammateUp()
        {
            foreach (PlayerManager p in Players)
                if (p != null && p != this && !p.IsDown && p.isActiveAndEnabled && p.healthPoints > 0) return true;
            return false;
        }

        // Down: hidden, ignored by the cubes (not tagged Player), can't move, shoot or pick things up.
        // The camera stays, so the player watches their team until the next wave.
        private void GoDown()
        {
            IsDown = true;
            disabledWhileDown.Clear();
            hiddenWhileDown.Clear();
            untaggedWhileDown.Clear();

            foreach (Behaviour b in GetComponents<Behaviour>())
                if (b != null && b.enabled && (b is PlayerMovement || b is ShootProjectile || b is SwapManager || b is HandlePowerups))
                {
                    b.enabled = false;
                    disabledWhileDown.Add(b);
                }
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
                if (r.enabled) { r.enabled = false; hiddenWhileDown.Add(r); }
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
                if (t.CompareTag("Player")) { t.tag = "Untagged"; untaggedWhileDown.Add(t.gameObject); }
            CharacterController body = GetComponent<CharacterController>();
            if (body != null) body.enabled = false;

            if (healthBar != null) healthBar.value = 0;
        }

        private void RespawnIfDown()
        {
            if (!IsDown || isGameOver) return;

            // Back at this player's co-op spawn point.
            int index = CouchCoopManager.Instance != null && transform.parent != null ? CouchCoopManager.Instance.GetPlayerIndex(transform.parent.gameObject) : 0;
            GameObject spawn = GameObject.Find("SP" + (Mathf.Clamp(index, 0, 3) + 1));
            if (spawn != null) transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);

            CharacterController body = GetComponent<CharacterController>();
            if (body != null) body.enabled = true;
            foreach (GameObject go in untaggedWhileDown) if (go != null) go.tag = "Player";
            foreach (Renderer r in hiddenWhileDown) if (r != null) r.enabled = true;
            foreach (Behaviour b in disabledWhileDown) if (b != null) b.enabled = true;

            healthPoints = 100f;
            if (healthBar != null) healthBar.value = healthPoints;
            IsDown = false;
        }

        private void OnDestroy()
        {
#if !DISABLESTEAMWORKS
            // Release the Steam callback so it doesn't outlive this player (Restart destroys players).
            overlayIsOn?.Dispose();
            overlayIsOn = null;
#endif
        }

        private void CheckControllerConnection()
        {
            // Check if a controller was connected and gets disconnected
            if (isControllerConnected && Gamepad.all.Count <= 0 && !GameManager.Instance.isPaused && !GameManager.Instance.isGameOver)
            {
                // Controller was just unplugged
                isControllerConnected = false;
                Pause();
            }
            else if (!isControllerConnected && Gamepad.all.Count > 0)
            {
                // Controller was just plugged in
                isControllerConnected = true;
            }
        }

#if !DISABLESTEAMWORKS
        void PauseGameIfSteamOverlayOn(GameOverlayActivated_t callback)
        {
            if (!GameManager.Instance.isPaused && !GameManager.Instance.isGameOver)
            {
                Pause();
            }
        }
#endif

        public void SaveDataEndGame()
        {
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
                CloudSaveLogin.SaveCloudDataIfSignedIn();
            }
            catch
            {
                Debug.Log("Failed to save cloud data");
            }


            GameManager.Instance.GameOver();
            isGameOver = GameManager.Instance.isGameOver;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsDown) return;
            if (other.CompareTag("Armor"))
            {
                Destroy(other.transform.root.gameObject);
                Damage(20);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("HealthPack") || other.CompareTag("SMB") || other.CompareTag("AB") || other.CompareTag("Shotblaster") || other.CompareTag("Sniper"))
            {
                contextPromptText.gameObject.SetActive(false);
                contextPromptImage.SetActive(false);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (IsDown) return;
            HealthPack hp;
            other.TryGetComponent<HealthPack>(out hp);

            if (other.CompareTag("HealthPack") && hp.isUsable)
            {
                contextPromptText.gameObject.SetActive(true);
                contextPromptImage.SetActive(true);
                contextPromptText.text = hp.contextPrompt;
            }

            if (other.CompareTag("HealthPack") && hp.isUsable && isInteractHeld && healthPoints <= 99 && currentPoints >= 500)
            {
                hp.StartResetHealthPack();

                Damage(-50);
                SpendPoints(500);

                if (healthPoints >= 100) { healthPoints = 100; }

                contextPromptText.gameObject.SetActive(false);
                contextPromptImage.SetActive(false);
            }

            WeaponPickup wp;
            other.TryGetComponent<WeaponPickup>(out wp);

            if (other.CompareTag("SMB") && wp.isUsable)
            {
                contextPromptText.gameObject.SetActive(true);
                contextPromptImage.SetActive(true);
                contextPromptText.text = wp.contextPrompt;
            }

            if (other.CompareTag("SMB") && wp.isUsable && isInteractHeld && currentPoints >= 1500)
            {
                wp.StartResetWeapon();

                SpendPoints(1500);

                if (swapManager.HasWeapon(2))
                {
                    fullyAutoSMB.GetAmmo(360);
                }
                else
                {
                    swapManager.GetWeapon(2);
                    fullyAutoSMB.GetAmmo(300);
                }

                contextPromptText.gameObject.SetActive(false);
                contextPromptImage.SetActive(false);
            }

            if (other.CompareTag("AB") && wp.isUsable)
            {
                contextPromptText.gameObject.SetActive(true);
                contextPromptImage.SetActive(true);
                contextPromptText.text = wp.contextPrompt;
            }

            if (other.CompareTag("AB") && wp.isUsable && isInteractHeld && currentPoints >= 1500)
            {
                wp.StartResetWeapon();

                SpendPoints(1500);

                if (swapManager.HasWeapon(3))
                {
                    aB.GetAmmo(360);
                }
                else
                {
                    swapManager.GetWeapon(3);
                    aB.GetAmmo(300);
                }

                contextPromptText.gameObject.SetActive(false);
                contextPromptImage.SetActive(false);
            }

            if (other.CompareTag("Shotblaster") && wp.isUsable)
            {
                contextPromptText.gameObject.SetActive(true);
                contextPromptImage.SetActive(true);
                contextPromptText.text = wp.contextPrompt;
            }

            if (other.CompareTag("Shotblaster") && wp.isUsable && isInteractHeld && currentPoints >= 1500)
            {
                wp.StartResetWeapon();

                SpendPoints(1500);

                if (swapManager.HasWeapon(4))
                {
                    shotblaster.GetAmmo(60);
                }
                else
                {
                    swapManager.GetWeapon(4);
                    shotblaster.GetAmmo(50);
                }

                contextPromptText.gameObject.SetActive(false);
                contextPromptImage.SetActive(false);
            }

            if (other.CompareTag("Sniper") && wp.isUsable)
            {
                contextPromptText.gameObject.SetActive(true);
                contextPromptImage.SetActive(true);
                contextPromptText.text = wp.contextPrompt;
            }

            if (other.CompareTag("Sniper") && wp.isUsable && isInteractHeld && currentPoints >= 1500)
            {
                wp.StartResetWeapon();

                SpendPoints(1500);

                if (swapManager.HasWeapon(5))
                {
                    sniperBlaster.GetAmmo(60);
                }
                else
                {
                    swapManager.GetWeapon(5);
                    sniperBlaster.GetAmmo(50);
                }

                contextPromptText.gameObject.SetActive(false);
                contextPromptImage.SetActive(false);
            }
        }

        public void SpendPoints(int pointsToSpend)
        {
            currentPoints -= pointsToSpend;
        }

        public static void AddPoints(int pointsToAdd)
        {
            currentPoints += pointsToAdd;
            if (Player.Instance != null)
                Player.Instance.totalPointsEarned += pointsToAdd;
        }

        public void Damage(float damageTaken)
        {
            healthPoints -= damageTaken;
        }

        private void UpdateLeaderboards()
        {
            if (Social.localUser.authenticated || SteamManager.IsReady)
            {
                LeaderboardManager.UpdateMostPointsLeaderboard();
                LeaderboardManager.UpdateWaveLeaderboard(GameScenes.ActiveLevelId, CurrentPlayMode);
                LeaderboardManager.UpdateCubesDestroyedLeaderboard();
                LeaderboardManager.UpdateAccuracyLeaderboard();

            }
        }

        private void UpdateTotalPoints()
        {
            player.points += currentPoints;

            if (player != null && player.totalPointsEarned >= 100000 && (Social.localUser.authenticated || CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Steam))
            {
                LeaderboardManager.UnlockPointRackerI();
            }
            if (player != null && player.totalPointsEarned >= 1000000 && (Social.localUser.authenticated || CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Steam))
            {
                LeaderboardManager.UnlockPointRackerII();
            }
            if (player != null && player.totalPointsEarned >= 10000000 && (Social.localUser.authenticated || CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Steam))
            {
                LeaderboardManager.UnlockPointRackerIII();
            }
        }

        // Couch Co-op has its own records and leaderboards, separate from Solo.
        private static WaveMode CurrentPlayMode => GameManager.mode == 1 ? WaveMode.Coop : WaveMode.Solo;

        private void UpdateHighestWave()
        {
            if (player == null) return;
            player.RecordWave(GameScenes.ActiveLevelId, CurrentPlayMode, GameManager.Instance.CurrentRound);
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
            while (isInteracting && holdTime < 0.15f)
            {
                holdTime += Time.deltaTime; // Increase launch power over time
                yield return null;
            }

            if (holdTime < 0.15f)
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
            Pause();
        }

        public void OnGamePause(InputValue context)
        {
            Pause();
        }

        private void Pause()
        {
            if (!GameManager.Instance.isGameOver)
            {
                GameManager.Instance.PauseInput();
            }

        }

    }

}
