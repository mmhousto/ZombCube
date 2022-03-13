using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using StarterAssets;
using UnityEngine.InputSystem;

namespace Com.MorganHouston.ZombCube
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
        private PlayerInput playerInput;

        public static int currentPoints = 0;

        public TextMeshProUGUI playerNameText;

        public string playerName;

        public Slider playerHealth;

        private TextMeshProUGUI scoreText;
        private Slider healthBar;

        public float healthPoints = 100f;
        private bool isGameOver;

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

                photonView.RPC(nameof(SetPlayerInfo), RpcTarget.AllBuffered, player.playerName, player.currentBlaster);
                
                healthBar = GameObject.FindWithTag("Health").GetComponent<Slider>();
                scoreText = GameObject.FindWithTag("Score").GetComponent<TextMeshProUGUI>();
                healthPoints = 100f;
                currentPoints = 0;
                healthBar.value = healthPoints;
                playerHealth.value = healthPoints;
                scoreText.text = "Score: " + currentPoints.ToString();

                
            }
            
        }

        // Update is called once per frame
        void Update()
        {
            CheckIfAlive();
            UpdateStats();
        }


#endregion


#region Public Methods

        public void DamagePlayerCall()
        {
            photonView.RPC(nameof(Damage), RpcTarget.All, 20f);
        }

        public static void AddPoints(int pointsToAdd)
        {
            currentPoints += pointsToAdd;
        }

        public void SavePlayerData()
        {
            SaveSystem.SavePlayer(player);
        }

        public void LoadPlayerData()
        {
            SaveData data = SaveSystem.LoadPlayer();

            player.playerName = PhotonNetwork.LocalPlayer.NickName;
            player.coins = data.coins;
            player.points = data.points;
            player.highestWave = data.highestWave;
            player.currentBlaster = (int)PhotonNetwork.LocalPlayer.CustomProperties["Blaster"];
            player.ownedBlasters = data.ownedBlasters;
        }

        // Input for Pausing -------------------------------------------------------

        public void OnGamePause(InputValue value)
        {
            Debug.Log("Pause Button Pressed");
            if (isPaused == false)
            {
                isPaused = true;
                isInputDisabled = true;
                Debug.Log("Paused");

#if (UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_STANDALONE || UNITY_EDITOR)
                Cursor.lockState = CursorLockMode.Confined;
#endif

                NetworkGameManager.Instance.PauseGame();
            }else if (isPaused == true)
            {
                isPaused = false;
                isInputDisabled = false;
                Debug.Log("Resumed");

#if (UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_STANDALONE || UNITY_EDITOR)
                Cursor.lockState = CursorLockMode.Locked;
#endif

                NetworkGameManager.Instance.ResumeGame();
            }
                
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
                    SavePlayerData();
                    CloudSaveSample.CloudSaveSample.Instance.SavePlayerData(SaveSystem.LoadPlayer());
                    healthPoints = 0;
                    NetworkGameManager.Instance.CallEliminatePlayer();
                    isAlive = false;
                    if(this.gameObject != null)
                        PhotonNetwork.Destroy(this.gameObject);

                    NetworkGameManager.Instance.ActivateCamera();
                    if (NetworkGameManager.Instance.playersEliminated == PhotonNetwork.CurrentRoom.PlayerCount)
                    {
                        NetworkGameManager.Instance.CallGameOver();
                        isGameOver = true;
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
            }
        }

        private void UpdateTotalPoints()
        {
            player.points += currentPoints;
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
        public void SetPlayerInfo(string name, int blasterIndex)
        {
            playerName = name;
            playerNameText.text = playerName;

            MeshRenderer[] blaster = GetComponentsInChildren<MeshRenderer>();

            for(int i = 1; i < blaster.Length; i++)
            {
                blaster[i].material = blasterMaterial[blasterIndex];
            }
        }

#endregion


    }

}
