#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using StarterAssets;
using UnityEngine.InputSystem;

#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace Com.GCTC.ZombCube
{

    public class NetworkPlayerManager : MonoBehaviourPunCallbacks, IPunObservable
    {

        public Material[] blasterMaterial;

        public bool isInputDisabled = false;

        private NetworkSwapManager swapManager;
        private NetworkFullyAuto fullyAutoSMB;
        private HandlePowerups powerups;
        private NetworkAB aB;
        private NetworkShotblaster shotblaster;
        private NetworkSniper sniper;
        public NetworkLaunchGrenade grenade;
        private Player player;
        private GameObject onScreenControls;
        private Image[] onScreenControlButtons;
        private GameObject currentPlayer;
        private UICanvasControllerInput uiInput;
        private MobileDisableAutoSwitchControls mobileControls;
        public PlayerInput playerInput;
        private GameObject contextPrompt;
        private TextMeshProUGUI contextPromptText;
        private GameObject contextPromptImage;

        public static int currentPoints = 0;

        public NetworkMouseLook mouseLook;

        public TextMeshProUGUI playerNameText;

        public string playerName;

        public Slider playerHealth;

        private TextMeshProUGUI scoreText;
        private Slider healthBar;
        private TextMeshProUGUI ammoText;

        public float healthPoints = 100f;
        private bool isGameOver;
        protected float holdTime;
        [SerializeField]
        protected bool isInteractHeld;
        [SerializeField]
        protected bool isInteracting;
        [SerializeField]
        protected bool startedHold;

        private bool isAlive = true, isPaused = false;
        private bool isControllerConnected = false;
        private bool isSteamOverlayActive = false;

#if !DISABLESTEAMWORKS
        protected Callback<GameOverlayActivated_t> overlayIsOn;
#endif


        #region MonoBehaviour Methods


        // Start is called before the first frame update
        void Start()
        {
            swapManager = GetComponent<NetworkSwapManager>();

            if (!photonView.IsMine)
            {
                NetworkSpectatorManager.playerUserNames.Add((string)photonView.Owner.CustomProperties["UserName"]);
            }

            // Remote players are drawn by the local Main Camera (behind walls); the local WeaponCam draws only our gun.
            if (photonView.IsMine) PlayerViewLayers.ApplyLocal(gameObject, 0);
            else PlayerViewLayers.ApplyRemote(gameObject);

            if (photonView.IsMine)
            {
#if !DISABLESTEAMWORKS
                overlayIsOn = Callback<GameOverlayActivated_t>.Create(PauseGameIfSteamOverlayOn);
#endif

                // Check if a controller is initially connected
                if (Gamepad.current != null)
                {
                    isControllerConnected = true;
                }

                isAlive = true;
                isPaused = false;
                isInputDisabled = false;
                isGameOver = NetworkGameManager.Instance.IsGameOver();

                player = GameObject.FindWithTag("PlayerData").GetComponent<Player>();
                playerInput = GetComponent<PlayerInput>();
                
                fullyAutoSMB = GetComponent<NetworkFullyAuto>();
                powerups = GetComponent<HandlePowerups>();
                aB = GetComponent<NetworkAB>();
                shotblaster = GetComponent<NetworkShotblaster>();
                sniper = GetComponent<NetworkSniper>();
                grenade = GetComponent<NetworkLaunchGrenade>();

                if (GetComponent<NetworkTripleShot>())
                {
                    GetComponent<NetworkTripleShot>().enabled = false;
                }

#if (UNITY_IOS || UNITY_ANDROID)
                currentPlayer = FindPlayer.GetPlayer();
                onScreenControls = GameObject.FindGameObjectWithTag("ScreenControls");
                onScreenControlButtons = onScreenControls.GetComponentsInChildren<Image>(true);

                foreach (Image button in onScreenControlButtons)
                {
                    button.gameObject.SetActive(true);
                }

                uiInput = onScreenControls.GetComponent<UICanvasControllerInput>();
                mobileControls = onScreenControls.GetComponent<MobileDisableAutoSwitchControls>();

                uiInput.GetPlayer(currentPlayer);
                mobileControls.GetPlayer(currentPlayer);

#endif

                photonView.RPC(nameof(SetPlayerInfo), RpcTarget.AllBuffered, player.playerName, player.userName, null, player.currentBlaster, player.currentSkin);
                healthBar = GameObject.FindWithTag("Health").GetComponent<Slider>();
                scoreText = GameObject.FindWithTag("Score").GetComponent<TextMeshProUGUI>();
                healthPoints = 100f;
                // Points carry over when respawning; NetworkGameManager resets them when a game starts.
                holdTime = 0;
                healthBar.value = healthPoints;
                playerHealth.value = healthPoints;
                scoreText.text = "Score: " + currentPoints.ToString();

                if (ammoText == null && GameObject.FindWithTag("Ammo") != null)
                {
                    ammoText = GameObject.FindWithTag("Ammo").GetComponent<TextMeshProUGUI>();
                }

                if (ammoText != null)
                    ammoText.text = "";

                if (GameObject.Find("ContextPrompt") != null)
                {
                    contextPrompt = GameObject.Find("ContextPrompt");
                    //contextPromptText = contextPrompt.GetComponentInChildren<TextMeshProUGUI>();
                    contextPromptText = contextPrompt.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
                    contextPromptText.gameObject.SetActive(false);
                    contextPromptImage = contextPrompt.transform.GetChild(0).GetComponent<Image>().gameObject;
                    contextPromptImage.SetActive(false);
                }

            }
        }

        // Update is called once per frame
        void Update()
        {
            if (this.photonView.IsMine && GameObject.FindWithTag("ContextPrompt") && contextPrompt == null)
            {
                contextPrompt = GameObject.FindWithTag("ContextPrompt");
                contextPromptText = contextPrompt.GetComponentInChildren<TextMeshProUGUI>();
                contextPromptText.gameObject.SetActive(false);
                contextPromptImage = contextPrompt.GetComponentInChildren<Image>().gameObject;
                contextPromptImage.SetActive(false);
            }

            CheckControllerConnection();
            CheckIfAlive();
            UpdateStats();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Armor") && other.transform.root.TryGetComponent(out NetworkEnemy enemy) && photonView.IsMine)
            {
                enemy.photonView.RPC("DestroyEnemy", RpcTarget.MasterClient);
                DamagePlayerCall(20);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if ((other.CompareTag("HealthPack") || other.CompareTag("SMB") || other.CompareTag("AB") || other.CompareTag("Shotblaster") || other.CompareTag("Sniper")) && photonView.IsMine)
            {
                contextPromptText.gameObject.SetActive(false);
                contextPromptImage.SetActive(false);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (photonView.IsMine)
            {

                NetworkHealthPack hp;
                other.TryGetComponent<NetworkHealthPack>(out hp);

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
                        sniper.GetAmmo(60);
                    }
                    else
                    {
                        swapManager.GetWeapon(5);
                        sniper.GetAmmo(50);
                    }

                    contextPromptText.gameObject.SetActive(false);
                    contextPromptImage.SetActive(false);
                }
            }
        }

        private new void OnEnable()
        {
            NetworkGameManager.endGame += CallSaveDataEndGame;
        }

        private new void OnDisable()
        {
            NetworkGameManager.endGame -= CallSaveDataEndGame;
        }

#endregion


        #region Public Methods

        public void SpendPoints(int pointsToSpend)
        {
            currentPoints -= pointsToSpend;
        }

        public void DamagePlayerCall(float damage)
        {
            photonView.RPC(nameof(Damage), RpcTarget.All, damage);
        }

        public static void AddPoints(int pointsToAdd)
        {
            currentPoints += pointsToAdd;
            Player.Instance.totalPointsEarned += pointsToAdd;
        }

        public void SavePlayerData()
        {
            SaveSystem.SavePlayer(player);
        }

        public void CallSaveDataEndGame()
        {
            if (PhotonNetwork.IsMasterClient)
                photonView.RPC(nameof(SaveDataEndGame), RpcTarget.AllBuffered);
        }

        [PunRPC]
        public void SaveDataEndGame()
        {
            SaveEndOfGame(NetworkGameManager.Instance.CurrentRound);

#if (UNITY_IOS || UNITY_ANDROID)
                    
            foreach(Image button in onScreenControlButtons)
            {
                button.gameObject.SetActive(false);
            }
                    
#endif


            //NetworkGameManager.Instance.ActivateCamera();

            NetworkGameManager.Instance.CallGameOver();
            isGameOver = true;
            isPaused = false;
            Cursor.lockState = CursorLockMode.None;

            if (this.gameObject != null && photonView.IsMine)
                PhotonNetwork.Destroy(this.gameObject);
        }

        // Input for Pausing -------------------------------------------------------

        public void PauseForContinue()
        {
            isInputDisabled = true;

            Cursor.lockState = CursorLockMode.None;

#if (UNITY_IOS || UNITY_ANDROID)
            foreach (Image button in onScreenControlButtons)
            {
                button.gameObject.SetActive(false);
            }
#endif
        }

        public void DisableMobileButtons()
        {
#if (UNITY_IOS || UNITY_ANDROID)
            foreach (Image button in onScreenControlButtons)
                {
                    button.gameObject.SetActive(false);
                }
#endif
        }

        public void PauseInput()
        {
            // Pausing only affects this player in Party (time keeps running for everyone). The Continue screen after
            // the boss already stops time for the whole room; Resume would restart it for this player only, so the
            // pause menu stays closed until everyone has continued.
            if (!isPaused && NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsContinue) return;

            isPaused = !isPaused;
            isGameOver = NetworkGameManager.Instance.IsGameOver();
            if (isPaused == true && isGameOver == false)
            {
                isInputDisabled = true;

                Cursor.lockState = CursorLockMode.None;

                NetworkGameManager.Instance.PauseGame();

#if (UNITY_IOS || UNITY_ANDROID)
                foreach (Image button in onScreenControlButtons)
                {
                    button.gameObject.SetActive(false);
                }
#endif
            }
            else if (isPaused == false && isGameOver == false)
            {
                isInputDisabled = false;

                Cursor.lockState = CursorLockMode.Locked;

                NetworkGameManager.Instance.ResumeGame();

#if (UNITY_IOS || UNITY_ANDROID)
                foreach (Image button in onScreenControlButtons)
                {
                    button.gameObject.SetActive(true);
                }
#endif
            }
            else if (isGameOver == true)
            {
                isInputDisabled = true;
                Cursor.lockState = CursorLockMode.None;

#if (UNITY_IOS || UNITY_ANDROID)
                foreach (Image button in onScreenControlButtons)
                {
                    button.gameObject.SetActive(false);
                }
#endif
            }
        }

        public void OnGamePause(InputValue value)
        {
            Pause();
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

        public void EnableInputResumeButton()
        {
            isPaused = false;
            isInputDisabled = false;

            Cursor.lockState = CursorLockMode.Locked;

#if (UNITY_IOS || UNITY_ANDROID)
            foreach (Image button in onScreenControlButtons)
            {
                button.gameObject.SetActive(true);
            }
#endif
        }

        // END Input for Pausing -------------------------------------------------------------------


        #endregion


        #region Private Methods

        private void CheckControllerConnection()
        {
            if (this.photonView.IsMine)
            {
                // Check if a controller was connected and gets disconnected
                if (isControllerConnected && Gamepad.all.Count <= 0 && !isPaused && !NetworkGameManager.Instance.isGameOver)
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
        }

#if !DISABLESTEAMWORKS
        void PauseGameIfSteamOverlayOn(GameOverlayActivated_t callback)
        {
            if (!isPaused && !NetworkGameManager.Instance.isGameOver && this.photonView.IsMine)
            {
                Pause();
            }
        }
#endif

        private void CheckIfAlive()
        {
            if (this.photonView.IsMine)
            {
                if (healthPoints <= 0 && !isGameOver && isAlive == true)
                {
                    // Points stay with the player through respawns; the profile is saved once, when the game ends.

#if (UNITY_IOS || UNITY_ANDROID)

                    foreach (Image button in onScreenControlButtons)
                    {
                        button.gameObject.SetActive(false);
                    }
#endif



                    healthPoints = 0;
                    NetworkGameManager.Instance.CallEliminatePlayer();
                    isAlive = false;
                    NetworkSpectatorManager.isAlive = false;

                    /*if(PhotonNetwork.CurrentRoom.PlayerCount == 1)
                    {
                        NetworkGameManager.Instance.ActivateCamera();
                    }
                    else if (NetworkGameManager.Instance.playersEliminated != PhotonNetwork.CurrentRoom.PlayerCount)
                    {
                        NetworkSpectatorManager.ActivateSpectatorCamera(mouseLook.GetCamera());
                    }*/

                    NetworkSpectatorManager.EnableElimCam();

                    if (this.gameObject != null)
                        PhotonNetwork.Destroy(this.gameObject);

                    // Everyone is out, or Hardcore Party (one player out ends the game for everyone).
                    if (NetworkGameManager.Instance.playersEliminated >= PhotonNetwork.CurrentRoom.PlayerCount || RoomLevelKeys.IsHardcoreRoom)
                    {
                        NetworkGameManager.Instance.ActivateCamera();
                        NetworkGameManager.Instance.CallGameOver();
                        isGameOver = true;
                        isPaused = false;
                        Cursor.lockState = CursorLockMode.None;
                    }

                }
            }
            
        }

        private void UpdateStats()
        {
            if (this.photonView.IsMine)
            {
                healthBar.value = healthPoints;
                playerHealth.value = healthPoints;
                scoreText.text = "Score: " + currentPoints.ToString();

                if (ammoText != null && powerups != null && powerups.FullAutoActive)
                    ammoText.text = "";
                else if (ammoText != null && fullyAutoSMB != null && fullyAutoSMB.enabled == true)
                    ammoText.text = $"{fullyAutoSMB.currentAmmoInClip}/{fullyAutoSMB.reserveAmmo}";
                else if (ammoText != null && aB != null && aB.enabled == true)
                    ammoText.text = $"{aB.currentAmmoInClip}/{aB.reserveAmmo}";
                else if (ammoText != null && shotblaster != null && shotblaster.enabled == true)
                    ammoText.text = $"{shotblaster.currentAmmoInClip}/{shotblaster.reserveAmmo}";
                else if (ammoText != null && sniper != null && sniper.enabled == true)
                    ammoText.text = $"{sniper.currentAmmoInClip}/{sniper.reserveAmmo}";
                else if (ammoText != null)
                    ammoText.text = "";
            }
        }

        private static bool endOfGameSaved;

        /// <summary>A new Party game: points start at 0 and the end-of-game save can run again.</summary>
        public static void ResetForNewGame()
        {
            currentPoints = 0;
            endOfGameSaved = false;
        }

        /// <summary>
        /// Adds this game's points to the profile, records the wave the party reached, reports leaderboards and
        /// saves, once per game (called on every client when the game ends, even if this player was eliminated).
        /// </summary>
        public static void SaveEndOfGame(int wave)
        {
            if (endOfGameSaved) return;
            endOfGameSaved = true;

            Player player = Player.Instance;
            if (player == null) return;

            player.points += currentPoints;
            WaveMode mode = RoomLevelKeys.IsHardcoreRoom ? WaveMode.PartyHardcore : WaveMode.Party;
            player.RecordWave(GameScenes.ActiveLevelId, mode, wave);

            bool signedIn = Social.localUser.authenticated || CloudSaveLogin.CurrentSSO == CloudSaveLogin.ssoOption.Steam;
            if (signedIn && player.totalPointsEarned >= 100000) LeaderboardManager.UnlockPointRackerI();
            if (signedIn && player.totalPointsEarned >= 1000000) LeaderboardManager.UnlockPointRackerII();
            if (signedIn && player.totalPointsEarned >= 10000000) LeaderboardManager.UnlockPointRackerIII();

            if (Social.localUser.authenticated || SteamManager.IsReady)
            {
                LeaderboardManager.UpdateMostPointsLeaderboard();
                LeaderboardManager.UpdateWaveLeaderboard(GameScenes.ActiveLevelId, mode);
                LeaderboardManager.UpdateCubesDestroyedLeaderboard();
                LeaderboardManager.UpdateAccuracyLeaderboard();
            }

            try { SaveSystem.SavePlayer(player); }
            catch { Debug.Log("Failed to save local data"); }
            try { CloudSaveLogin.SaveCloudDataIfSignedIn(); }
            catch { Debug.Log("Failed to save cloud data"); }
        }

        private void Pause()
        {
            if (!NetworkGameManager.Instance.isGameOver)
            {
                PauseInput();
            }

        }


        #endregion


        #region Pun Methods


        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                //We own this player: send the others our data
                stream.SendNext(healthPoints);
            }
            else
            {
                //Network player, receive data
                healthPoints = (float)stream.ReceiveNext();
            }
        }

        public void CallDamageRPC(float damageTaken)
        {
            photonView.RPC(nameof(Damage), RpcTarget.AllBuffered, damageTaken);
        }

        [PunRPC]
        public void Damage(float damageTaken)
        {
            healthPoints -= damageTaken;
            playerHealth.value = healthPoints;
        }

        [PunRPC]
        public void SetPlayerInfo(string name, string username, string accountID, int blasterIndex, int skinIndex)
        {
            playerName = name;

            playerNameText.text = playerName + "<br>" + username;

            GetComponentInChildren<MeshRenderer>().material = CosmeticDatabase.MaterialAt(skinIndex);

            MeshRenderer[] blaster = GetComponentsInChildren<MeshRenderer>();

            for(int i = 0; i < blaster.Length; i++)
            {
                if(blaster[i].tag == "Blaster")
                    blaster[i].material = CosmeticDatabase.MaterialAt(blasterIndex);

            }

            if(swapManager != null)
                swapManager.CallDisableWeapons();
        }

#endregion


    }

}
