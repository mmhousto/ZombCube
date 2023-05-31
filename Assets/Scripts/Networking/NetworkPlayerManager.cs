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

        public float healthPoints = 100f;
        private bool isGameOver;
        private bool pressedUse;

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
                healthBar.value = healthPoints;
                playerHealth.value = healthPoints;
                scoreText.text = "Score: " + currentPoints.ToString();

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

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("HealthPack") && photonView.IsMine)
            {
                contextPrompt.SetActive(false);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("HealthPack") && other.GetComponent<NetworkHealthPack>().isUsable && photonView.IsMine)
            {
                contextPrompt.SetActive(true);
                contextPromptText.text = other.GetComponent<NetworkHealthPack>().contextPrompt;
            }

            if (other.CompareTag("HealthPack") && other.GetComponent<NetworkHealthPack>().isUsable && pressedUse && healthPoints <= 99 && currentPoints >= 500 && photonView.IsMine)
            {
                other.GetComponent<NetworkHealthPack>().StartResetHealthPack();

                Damage(-20);
                SpendPoints(500);

                if (healthPoints >= 100) { healthPoints = 100; }
            }
        }


        #endregion


        #region Public Methods

        public void SpendPoints(int pointsToSpend)
        {
            currentPoints -= pointsToSpend;
        }

        public void DamagePlayerCall()
        {
            photonView.RPC(nameof(Damage), RpcTarget.All, 20f);
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

        public void OnInteract(InputValue value)
        {
            pressedUse = value.isPressed;
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

            GetComponent<MeshRenderer>().material = blasterMaterial[skinIndex];

            MeshRenderer[] blaster = GetComponentsInChildren<MeshRenderer>();

            for(int i = 1; i < blaster.Length; i++)
            {
                if(blaster[i].tag == "Blaster")
                    blaster[i].material = blasterMaterial[blasterIndex];
            }
        }

#endregion


    }

}
