using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using StarterAssets;
using UnityEngine.InputSystem;

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
        public NetworkLaunchGrenade grenade;
        private Player player;
        private GameObject onScreenControls;
        private GameObject currentPlayer;
        private UICanvasControllerInput uiInput;
        private MobileDisableAutoSwitchControls mobileControls;
        public PlayerInput playerInput;
        private GameObject contextPrompt;
        private TextMeshProUGUI contextPromptText;

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


        #region MonoBehaviour Methods


        // Start is called before the first frame update
        void Start()
        {
            if (photonView.IsMine)
            {
                isAlive = true;
                isPaused = false;
                isInputDisabled = false;
                isGameOver = NetworkGameManager.Instance.IsGameOver();

                player = GameObject.FindWithTag("PlayerData").GetComponent<Player>();
                playerInput = GetComponent<PlayerInput>();
                swapManager = GetComponent<NetworkSwapManager>();
                fullyAutoSMB = GetComponent<NetworkFullyAuto>();
                aB = GetComponent<NetworkAB>();
                shotblaster = GetComponent<NetworkShotblaster>();
                grenade = GetComponent<NetworkLaunchGrenade>();

                if (GetComponent<NetworkTripleShot>())
                {
                    GetComponent<NetworkTripleShot>().enabled = false;
                }

#if (UNITY_IOS || UNITY_ANDROID)
                currentPlayer = FindPlayer.GetPlayer();
                onScreenControls = GameObject.FindGameObjectWithTag("ScreenControls");
                uiInput = onScreenControls.GetComponent<UICanvasControllerInput>();
                mobileControls = onScreenControls.GetComponent<MobileDisableAutoSwitchControls>();

                uiInput.GetPlayer(currentPlayer);
                mobileControls.GetPlayer(currentPlayer);

#endif

                photonView.RPC(nameof(SetPlayerInfo), RpcTarget.AllBuffered, player.playerName, player.currentBlaster, player.currentSkin);
                
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

                contextPrompt = GameObject.Find("ContextPrompt");
                contextPromptText = contextPrompt.GetComponent<TextMeshProUGUI>();
                contextPrompt.SetActive(false);
            }
            
        }

        // Update is called once per frame
        void Update()
        {
            CheckIfAlive();
            UpdateStats();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Armor"))
            {
                other.transform.root.GetComponent<NetworkEnemy>().photonView.RPC("DestroyEnemy", RpcTarget.MasterClient);
                DamagePlayerCall(20);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if ((other.CompareTag("HealthPack") || other.CompareTag("SMB") || other.CompareTag("AB") || other.CompareTag("Shotblaster")) && photonView.IsMine)
            {
                contextPrompt.SetActive(false);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            NetworkHealthPack hp;
            other.TryGetComponent<NetworkHealthPack>(out hp);

            if (other.CompareTag("HealthPack") && hp.isUsable && photonView.IsMine)
            {
                contextPrompt.SetActive(true);
                contextPromptText.text = hp.contextPrompt;
            }

            if (other.CompareTag("HealthPack") && hp.isUsable && isInteractHeld && healthPoints <= 99 && currentPoints >= 500 && photonView.IsMine)
            {
                hp.StartResetHealthPack();

                Damage(-20);
                SpendPoints(500);

                if (healthPoints >= 100) { healthPoints = 100; }
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
                    fullyAutoSMB.GetAmmo();
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
                    aB.GetAmmo();
                }
            }

            if (other.CompareTag("Shotblaster") && wp.isUsable)
            {
                contextPrompt.SetActive(true);
                contextPromptText.text = wp.contextPrompt;
            }

            if (other.CompareTag("Shotblaster") && wp.isUsable && isInteractHeld && currentPoints >= 10)
            {
                wp.StartResetWeapon();

                SpendPoints(10);

                if (swapManager.HasWeapon(4))
                {
                    shotblaster.GetAmmo();
                }
                else
                {
                    swapManager.GetWeapon(4);
                    shotblaster.GetAmmo();
                }
            }
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

        // Input for Pausing -------------------------------------------------------

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
                onScreenControls.SetActive(false);
#endif
            }
            else if (isPaused == false && isGameOver == false)
            {
                isInputDisabled = false;

                Cursor.lockState = CursorLockMode.Locked;

                NetworkGameManager.Instance.ResumeGame();

#if (UNITY_IOS || UNITY_ANDROID)
                onScreenControls.SetActive(true);
#endif
            }
            else if (isGameOver == true)
            {
                isInputDisabled = true;
                Cursor.lockState = CursorLockMode.None;

#if (UNITY_IOS || UNITY_ANDROID)
                onScreenControls.SetActive(false);
#endif
            }
        }

        public void OnGamePause(InputValue value)
        {
            PauseInput();

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

        public void EnableInputResumeButton()
        {
            isPaused = false;
            isInputDisabled = false;

            Cursor.lockState = CursorLockMode.Locked;

#if (UNITY_IOS || UNITY_ANDROID)
            onScreenControls.SetActive(true);
#endif
        }

        // END Input for Pausing -------------------------------------------------------------------


        #endregion


        #region Private Methods


        private void CheckIfAlive()
        {
            if (this.photonView.IsMine)
            {
                if (healthPoints <= 0 && !isGameOver && isAlive == true)
                {
                    UpdateTotalPoints();
                    UpdateHighestWave();

#if (UNITY_IOS || UNITY_ANDROID)
                    UpdateLeaderboards();
                    onScreenControls.SetActive(false);
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

                    NetworkGameManager.Instance.ActivateCamera();

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
                LeaderboardManager.UpdateMostPointsLeaderboard();
                LeaderboardManager.UpdatePartyHighestWaveLeaderboard();
                LeaderboardManager.UpdateCubesDestroyedLeaderboard();
                LeaderboardManager.UpdateAccuracyLeaderboard();
        }



        private void UpdateStats()
        {
            if (this.photonView.IsMine)
            {
                healthBar.value = healthPoints;
                playerHealth.value = healthPoints;
                scoreText.text = "Score: " + currentPoints.ToString();

                if (ammoText != null && fullyAutoSMB.enabled == true)
                    ammoText.text = $"{fullyAutoSMB.currentAmmoInClip}/{fullyAutoSMB.reserveAmmo}";
                else if (ammoText != null && aB.enabled == true)
                    ammoText.text = $"{aB.currentAmmoInClip}/{aB.reserveAmmo}";
                else if (ammoText != null)
                    ammoText.text = "";
            }
        }

        private void UpdateTotalPoints()
        {
            player.points += currentPoints;
        }

        private void UpdateHighestWave()
        {
            int endingRound = NetworkGameManager.Instance.CurrentRound;
            if (player.highestWaveParty < endingRound)
            {
                player.highestWaveParty = endingRound;
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

        [PunRPC]
        public void Damage(float damageTaken)
        {
            healthPoints -= damageTaken;
            playerHealth.value = healthPoints;
        }

        [PunRPC]
        public void SetPlayerInfo(string name, int blasterIndex, int skinIndex)
        {
            playerName = name;
            playerNameText.text = playerName;

            GetComponentInChildren<MeshRenderer>().material = blasterMaterial[skinIndex];

            MeshRenderer[] blaster = GetComponentsInChildren<MeshRenderer>();

            for(int i = 0; i < blaster.Length; i++)
            {
                if(blaster[i].tag == "Blaster")
                    blaster[i].material = blasterMaterial[blasterIndex];
            }

            swapManager.DisableWeapons();
        }

#endregion


    }

}
