using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System;
using TMPro;

namespace Com.GCTC.ZombCube
{

    public class ConnectToServer : MonoBehaviourPunCallbacks
    {
        [Tooltip("The players data object.")]
        private Player player;

        public TextMeshProUGUI loadingText;

        private string gameVersion = "0.2";

        private int dots = 1;

        #region MonoBehaviour Methods


        private void Awake()
        {
            player = GameObject.FindWithTag("PlayerData").GetComponent<Player>();
            Time.timeScale = 1.0f;
        }

        /// <summary>
        /// Loads player data and connects to server using specified settings.
        /// </summary>
        void Start()
        {
            InvokeRepeating(nameof(UpdateLoadingText), 0f, 1f);

            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.GameVersion = gameVersion;
            PhotonNetwork.ConnectUsingSettings();

            Invoke(nameof(CouldNotConnect), 15f);
        }


        #endregion

        /// <summary>
        /// Sets data loaded from player to player variable.
        /// </summary>
        public void LoadPlayerData()
        {
            SaveData data = SaveSystem.LoadPlayer();

            player.playerName = data.playerName;
            player.coins = data.coins;
            player.points = data.points;
            player.highestWave = data.highestWave;
            player.currentBlaster = data.currentBlaster;
            player.ownedBlasters = data.ownedBlasters;
            player.currentSkin = data.currentSkin;
            player.ownedSkins = data.ownedSkins;
        }

        void CouldNotConnect()
        {
            PhotonNetwork.Disconnect();
        }

        void UpdateLoadingText()
        {
            dots++;
            if (dots > 3)
            {
                dots = 1;
            }

            switch (dots)
            {
                case 1:
                    loadingText.text = "Connecting.";
                    break;
                case 2:
                    loadingText.text = "Connecting..";
                    break;
                case 3:
                    loadingText.text = "Connecting...";
                    break;
                default:
                    loadingText.text = "Connecting...";
                    break;
            }

        }


        #region Pun Callbacks

        /// <summary>
        /// Sends player to Main Menu scene after disconnecting.
        /// </summary>
        /// <param name="cause"></param>
        public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
        {
            //Debug.Log(cause);
            ErrorManager.Instance.StartErrorMessage("Network Error: Player disconnected from the internet.");
            SceneLoader.ToMainMenu();
        }

        /// <summary>
        /// Joins the lobby after connecting successfully.
        /// </summary>
        public override void OnConnectedToMaster()
        {
            //Debug.Log("Connected to Master " + PhotonNetwork.ServerAddress);
            PhotonNetwork.JoinLobby();
            PhotonNetwork.AutomaticallySyncScene = true;
        }

        /// <summary>
        /// Loads the lobby scene and sets NickName to player's name, after joining successfully.
        /// </summary>
        public override void OnJoinedLobby()
        {
            SceneLoader.ToLobby();
            PhotonNetwork.NickName = player.playerName;
            //Debug.Log(PhotonNetwork.ServerAddress);
        }


        #endregion

    }

}
