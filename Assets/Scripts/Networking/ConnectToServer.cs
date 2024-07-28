using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System;
using TMPro;
using UnityEngine.UI;
using Photon.Realtime;

namespace Com.GCTC.ZombCube
{

    public class ConnectToServer : MonoBehaviourPunCallbacks
    {
        [Tooltip("The players data object.")]
        private Player player;

        public TextMeshProUGUI loadingText;

        public TMP_Dropdown regionList;

        public string region;

        private string gameVersion = "0.2";

        private int dots = 1;

        private AppSettings serverSettings;

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
            loadingText.gameObject.SetActive(false);

            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.GameVersion = gameVersion;
            serverSettings = new AppSettings();
            serverSettings.UseNameServer = true;
            serverSettings.AppIdRealtime = "6ebea3a1-0375-4762-8828-5a4b07a80f6a";
            serverSettings.AppVersion = "1";
            region = PlayerPrefs.GetString("Region", "ussc");
            switch (region)
            {
                case "usw":
                    regionList.SetValueWithoutNotify(1);
                    break;
                case "us":
                    regionList.SetValueWithoutNotify(2);
                    break;
                case "ussc":
                    regionList.SetValueWithoutNotify(0);
                    break;
                case "sa":
                    regionList.SetValueWithoutNotify(3);
                    break;
                case "asia":
                    regionList.SetValueWithoutNotify(4);
                    break;
                case "au":
                    regionList.SetValueWithoutNotify(5);
                    break;
                case "cae":
                    regionList.SetValueWithoutNotify(6);
                    break;
                case "eu":
                    regionList.SetValueWithoutNotify(7);
                    break;
                case "in":
                    regionList.SetValueWithoutNotify(8);
                    break;
                case "jp":
                    regionList.SetValueWithoutNotify(9);
                    break;
                case "za":
                    regionList.SetValueWithoutNotify(10);
                    break;
                default:
                    regionList.SetValueWithoutNotify(0);
                    break;
            }

            serverSettings.FixedRegion = region;
        }


        #endregion

        public void RegionSelect(int regionIndex)
        {
            switch (regionList.options[regionIndex].text)
            {
                case "US West":
                    region = "usw";
                    break;
                case "US East":
                    region = "us";
                    break;
                case "US Central":
                    region = "ussc";
                    break;
                case "South America":
                    region = "sa";
                    break;
                case "Asia":
                    region = "asia";
                    break;
                case "Austrailia":
                    region = "au";
                    break;
                case "Canada":
                    region = "cae";
                    break;
                case "Europe":
                    region = "eu";
                    break;
                case "India":
                    region = "in";
                    break;
                case "Japan":
                    region = "jp";
                    break;
                case "South Africa":
                    region = "za";
                    break;
                default:
                    region = "ussc";
                    break;
            }
            PlayerPrefs.SetString("Region", region);
            serverSettings.FixedRegion = region;
        }

        public void Connect()
        {
            InvokeRepeating(nameof(UpdateLoadingText), 0f, 1f);
            loadingText.gameObject.SetActive(true);
            PhotonNetwork.ConnectUsingSettings(serverSettings);

            Invoke(nameof(CouldNotConnect), 15f);
        }

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

        public void BackToMainMenu()
        {
            SceneLoader.ToMainMenu();
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
