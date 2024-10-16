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

#if UNITY_PS5 && !UNITY_EDITOR
                photonView.RPC(nameof(SetPlayerInfo), RpcTarget.AllBuffered, player.playerName, player.userName, PSUser.GetActiveUserAccountID.ToString(), player.currentBlaster, player.currentSkin);
#else
                photonView.RPC(nameof(SetPlayerInfo), RpcTarget.AllBuffered, player.playerName, player.userName, null, player.currentBlaster, player.currentSkin);
#endif
                healthBar = GameObject.FindWithTag("Health").GetComponent<Slider>();
                scoreText = GameObject.FindWithTag("Score").GetComponent<TextMeshProUGUI>();
                healthPoints = 100f;
                currentPoints = 0;
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

                    Damage(-20);
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
                        fullyAutoSMB.GetAmmo(90);
                    }
                    else
                    {
                        swapManager.GetWeapon(2);
                        fullyAutoSMB.GetAmmo(90);
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
                        aB.GetAmmo(210);
                    }
                    else
                    {
                        swapManager.GetWeapon(3);
                        aB.GetAmmo(210);
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
                        shotblaster.GetAmmo(35);
                    }
                    else
                    {
                        swapManager.GetWeapon(4);
                        shotblaster.GetAmmo(35);
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
                        sniper.GetAmmo(20);
                    }
                    else
                    {
                        swapManager.GetWeapon(5);
                        sniper.GetAmmo(20);
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
            UpdateTotalPoints();
            UpdateHighestWave();
            UpdateLeaderboards();

#if (UNITY_IOS || UNITY_ANDROID)
                    
            foreach(Image button in onScreenControlButtons)
            {
                button.gameObject.SetActive(false);
            }
                    
#endif

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
            while (isInteracting && holdTime < 0.25f)
            {
                holdTime += Time.deltaTime; // Increase launch power over time
                yield return null;
            }

            if (holdTime < 0.25f)
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
                    UpdateTotalPoints();
                    UpdateHighestWave();
                    UpdateLeaderboards();

#if (UNITY_IOS || UNITY_ANDROID)

                    foreach (Image button in onScreenControlButtons)
                    {
                        button.gameObject.SetActive(false);
                    }
#endif

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

                    if (NetworkGameManager.Instance.playersEliminated >= PhotonNetwork.CurrentRoom.PlayerCount)
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

        private void UpdateLeaderboards()
        {
            if (Social.localUser.authenticated || SteamManager.Initialized)
            {
                LeaderboardManager.UpdateMostPointsLeaderboard();
                LeaderboardManager.UpdatePartyHighestWaveLeaderboard();
                LeaderboardManager.UpdateCubesDestroyedLeaderboard();
                LeaderboardManager.UpdateAccuracyLeaderboard();
            }
#if UNITY_PS5 && !UNITY_EDITOR
                LeaderboardManager.UpdatePSNStats(player);
#endif
        }

        private void UpdateStats()
        {
            if (this.photonView.IsMine)
            {
                healthBar.value = healthPoints;
                playerHealth.value = healthPoints;
                scoreText.text = "Score: " + currentPoints.ToString();

                if (ammoText != null && fullyAutoSMB != null && fullyAutoSMB.enabled == true)
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

        private void UpdateTotalPoints()
        {
            if (player == null)
                player = Player.Instance;

            if(player != null)
                player.points += currentPoints;

            if (player != null && player.totalPointsEarned >= 100000 && (Social.localUser.authenticated || CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Steam || CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.PS))
            {
                LeaderboardManager.UnlockPointRackerI();
            }
            else if (player != null && player.totalPointsEarned >= 1000000 && (Social.localUser.authenticated || CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Steam || CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.PS))
            {
                LeaderboardManager.UnlockPointRackerII();
            }
            else if (player != null && player.totalPointsEarned >= 10000000 && (Social.localUser.authenticated || CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Steam || CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.PS))
            {
                LeaderboardManager.UnlockPointRackerIII();
            }
        }

        private void UpdateHighestWave()
        {
            if (player == null)
                player = Player.Instance;

            int endingRound = NetworkGameManager.Instance.CurrentRound;
            if (player != null && player.highestWaveParty < endingRound)
            {
                player.highestWaveParty = endingRound;
            }
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

#if UNITY_PS5 && !UNITY_EDITOR
            if (CloudSaveLogin.Instance.restricted)
                playerNameText.text = username;
            else
                playerNameText.text = playerName + "<br>" + username;

            PSUserProfiles.GetBlockedUsers();
            if(PSUserProfiles.blockedUsers.Count > 0 && accountID != null)
            {
                foreach(ulong blockedUser in PSUserProfiles.blockedUsers)
                {
                    if(accountID == blockedUser.ToString())
                    {
                        playerNameText.text = username;
                    }
                }
            }
#else
            playerNameText.text = playerName + "<br>" + username;
#endif

            GetComponentInChildren<MeshRenderer>().material = blasterMaterial[skinIndex];

            MeshRenderer[] blaster = GetComponentsInChildren<MeshRenderer>();

            for(int i = 0; i < blaster.Length; i++)
            {
                if(blaster[i].tag == "Blaster")
                    blaster[i].material = blasterMaterial[blasterIndex];

            }

            if(swapManager != null)
                swapManager.CallDisableWeapons();
        }

#endregion


    }

}
