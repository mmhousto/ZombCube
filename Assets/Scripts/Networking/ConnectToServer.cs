using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System;

namespace Com.MorganHouston.ZombCube
{

    public class ConnectToServer : MonoBehaviourPunCallbacks
    {
        [Tooltip("The players data object.")]
        private Player player;

        private string gameVersion = "0.1";

        #region MonoBehaviour Methods


        private void Awake()
        {
            player = GameObject.FindWithTag("PlayerData").GetComponent<Player>();
        }

        /// <summary>
        /// Loads player data and connects to server using specified settings.
        /// </summary>
        void Start()
        {
            try
            {
                LoadPlayerData();
            }
            catch(Exception e)
            {
                Debug.Log("No data found!");
            }

            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.GameVersion = gameVersion;
            PhotonNetwork.ConnectUsingSettings();
            
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
        }


        #region Pun Callbacks

        /// <summary>
        /// Sends player to Main Menu scene after disconnecting.
        /// </summary>
        /// <param name="cause"></param>
        public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
        {
            Debug.Log(cause);
            SceneLoader.ToMainMenu();
        }

        /// <summary>
        /// Joins the lobby after connecting successfully.
        /// </summary>
        public override void OnConnectedToMaster()
        {
            Debug.Log("Connected to Master " + PhotonNetwork.ServerAddress);
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
            Debug.Log(PhotonNetwork.ServerAddress);
        }


        #endregion

    }

}