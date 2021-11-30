using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

namespace Com.MorganHouston.ZombCube
{

    public class NetworkPlayerManager : MonoBehaviourPunCallbacks
    {

        public Material[] blasterMaterial;

        private Player player;

        public static int currentPoints = 0;

        public TextMeshProUGUI playerNameText;

        public string playerName;

        public Slider playerHealth;

        private TextMeshProUGUI scoreText;
        private Slider healthBar;

        public float healthPoints = 100f;
        private bool isGameOver;

        private bool isAlive = true;


        // Start is called before the first frame update
        void Start()
        {
            

            if (photonView.IsMine)
            {
                isAlive = true;

                player = GetComponent<Player>();
                LoadPlayerData();
                playerName = PhotonNetwork.LocalPlayer.NickName;
                playerNameText.text = playerName;
                Debug.Log("Loaded Player Data");
                healthBar = GameObject.FindWithTag("Health").GetComponent<Slider>();
                scoreText = GameObject.FindWithTag("Score").GetComponent<TextMeshProUGUI>();
                healthPoints = 100f;
                currentPoints = 0;
                healthBar.value = healthPoints;
                playerHealth.value = healthPoints;
                scoreText.text = "Score: " + currentPoints.ToString();

                GameObject[] blaster = GameObject.FindGameObjectsWithTag("Blaster");

                foreach (GameObject item in blaster)
                {
                    item.GetComponent<MeshRenderer>().material = blasterMaterial[(int)PhotonNetwork.LocalPlayer.CustomProperties["Blaster"]];
                }
            }
            
        }

        // Update is called once per frame
        void Update()
        {
            UpdateStats();
            CheckIfAlive();
        }

        public void CheckIfAlive()
        {
            if (this.photonView.IsMine)
            {
                if (healthPoints <= 0 && !isGameOver && isAlive == true)
                {
                    healthPoints = 0;
                    NetworkGameManager.Instance.CallEliminatePlayer();
                    Debug.Log(NetworkGameManager.Instance.playersEliminated);
                    isAlive = false;
                    UpdateTotalPoints();
                    SavePlayerData();
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

        public void UpdateStats()
        {
            if (this.photonView.IsMine)
            {
                healthBar.value = healthPoints;
                playerHealth.value = healthPoints;
                scoreText.text = "Score: " + currentPoints.ToString();
            }
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                //We own this player: send the others our data
                stream.SendNext(healthPoints);
                stream.SendNext(playerName);
            }
            else
            {
                //Network player, receive data
                this.healthPoints = (int)stream.ReceiveNext();
                this.playerName = (string)stream.ReceiveNext();
            }
        }

        public static void AddPoints(int pointsToAdd)
        {
            currentPoints += pointsToAdd;
        }

        public void Damage(float damageTaken)
        {
            healthPoints -= damageTaken;
        }

        private void UpdateTotalPoints()
        {
            player.points += currentPoints;
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


    }

}
