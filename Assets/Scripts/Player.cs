using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        public int[] ownedBlasters = { 1, 0, 0, 0, 0, 0, 0, 0 };
        public int[] ownedSkins = { 1, 0, 0, 0, 0, 0, 0, 0 };
        public int cubesEliminated = 0;
        public int totalPointsEarned = 0;
        public int totalProjectilesFired = 0;

        public Player(Player player)
        {
            userID = player.userID;
            userName = player.userName;
            points = player.points;
            coins = player.coins;
            currentBlaster = player.currentBlaster;
            currentSkin = player.currentBlaster;
            highestWave = player.highestWave;
            highestWaveParty = player.highestWaveParty;
            playerName = player.playerName;
            ownedBlasters = player.ownedBlasters;
            ownedSkins = player.ownedSkins;
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

    }

}
