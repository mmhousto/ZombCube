using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace Com.MorganHouston.ZombCube
{

    public class ConnectToServer : MonoBehaviourPunCallbacks
    {
        [Tooltip("The players data object.")]
        public Player player;

        #region MonoBehaviour Methods


        /// <summary>
        /// Loads player data and connects to server using specified settings.
        /// </summary>
        void Start()
        {
            LoadPlayerData();

            PhotonNetwork.AutomaticallySyncScene = true;
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
            PhotonNetwork.JoinLobby();
        }

        /// <summary>
        /// Loads the lobby scene and sets NickName to player's name, after joining successfully.
        /// </summary>
        public override void OnJoinedLobby()
        {
            SceneLoader.ToLobby();
            PhotonNetwork.NickName = player.playerName;
            Debug.Log(PhotonNetwork.NickName);
        }


        #endregion

    }

}
