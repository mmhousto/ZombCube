using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.MorganHouston.ZombCube
{

    public class Player : MonoBehaviour
    {

        public int points = 0;
        public int coins = 0;
        public int currentBlaster = 0;
        public int highestWave = 0;
        public string playerName = "PlayerName";
        public int[] ownedBlasters = { 1, 0, 0, 0, 0, 0, 0 };
        public Material[] materials;

        /// <summary>
        /// Sets playerName to name and saves the player data.
        /// </summary>
        /// <param name="name">The Name Input Field</param>
        public void SetPlayerName(string name)
        {
            playerName = name;
            SaveSystem.SavePlayer(this);
        }


    }

}
