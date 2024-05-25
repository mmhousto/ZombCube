using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;

namespace Com.GCTC.ZombCube
{

    public class Player : MonoBehaviour
    {
        private static Player _instance;

        public static Player Instance { get { return _instance; } }

        public string userID;
        public string userName;
        public int points = 0;
        public int coins = 0;
        public int currentBlaster = 0;
        public int currentSkin = 0;
        public int highestWave = 0;
        public int highestWaveParty = 0;
        public string playerName = "PlayerName";
        public string RewardOvertime { get; private set; }
        public int[] ownedBlasters = { 1, 0, 0, 0, 0, 0, 0, 0, 0 };
        public int[] ownedSkins = { 1, 0, 0, 0, 0, 0, 0, 0, 0 };
        public int cubesEliminated = 0;
        public int totalPointsEarned = 0;
        public int totalProjectilesFired = 0;

        public Player(Player player)
        {
            userID = player.userID;
            userName = player.userName;
            points = player.points;
            coins = player.coins;
            highestWave = player.highestWave;
            highestWaveParty = player.highestWaveParty;
            playerName = player.playerName;

            if(RewardOvertime == null || RewardOvertime == "")
                RewardOvertime = DateTime.Now.AddHours(-2).ToString();
            else
                RewardOvertime = player.RewardOvertime;

            currentBlaster = player.currentBlaster;
            if (player.ownedBlasters == null)
            {
                ownedBlasters = new int[] { 1, 0, 0, 0, 0, 0, 0, 0, 0 };
            }
            else if (player.ownedBlasters.Length != ownedBlasters.Length)
            {
                int[] temp = new int[9];
                ownedBlasters.CopyTo(temp, 0);
                ownedBlasters = temp;
                ownedBlasters = player.ownedBlasters;
            }
            else
            {
                ownedBlasters = player.ownedBlasters;
            }

            currentSkin = player.currentSkin;
            if (player.ownedSkins == null)
            {
                player.ownedSkins = new int[] { 1, 0, 0, 0, 0, 0, 0, 0, 0 };
            }
            else if (player.ownedSkins.Length != ownedSkins.Length)
            {
                int[] temp = new int[9];
                ownedSkins.CopyTo(temp, 0);
                ownedSkins = temp;
                ownedSkins = player.ownedSkins;

            }
            else
            {
                ownedSkins = player.ownedSkins;
            }

            cubesEliminated = player.cubesEliminated;
            totalPointsEarned = player.totalPointsEarned;
            totalProjectilesFired = player.totalProjectilesFired;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                _instance = this;
            }
            DontDestroyOnLoad(this.gameObject);
        }

        public void GainPoints()
        {
            points += 1000;
        }

        public void SetRewardOvertime(string time)
        {
            RewardOvertime = time;
        }

    }

}
