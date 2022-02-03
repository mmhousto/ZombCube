using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Com.MorganHouston.ZombCube
{

    public class DataManager : MonoBehaviour
    {
        private static DataManager _instance;

        public static DataManager Instance { get { return _instance; } }

        [Tooltip("The players data object.")]
        private Player player;

        [Tooltip("The text that displays number of coins the player owns.")]
        public TextMeshProUGUI coinsText;
        [Tooltip("The text that displays number of points the player owns.")]
        public TextMeshProUGUI pointsText;
        [Tooltip("The input field players type their name in.")]
        public TMP_InputField nameInputField;

        /// <summary>
        /// Singleton implementation
        /// </summary>
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

        }

        // Start is called before the first frame update
        void Start()
        {
            player = Player.Instance;

            if (coinsText)
            {
                coinsText.text = "Coins: " + player.coins;
            }

            if (pointsText)
            {
                pointsText.text = "Points: " + player.points;
            }

            if (nameInputField)
                nameInputField.text = player.playerName;

        }

        // Update is called once per frame
        void Update()
        {

            if (coinsText)
            {
                coinsText.text = "Coins: " + player.coins;
            }

            if (pointsText)
            {
                pointsText.text = "Points: " + player.points;
            }

        }


    }

}
