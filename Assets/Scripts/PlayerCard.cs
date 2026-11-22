using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Com.GCTC.ZombCube
{

    public class PlayerCard : MonoBehaviour
    {
        [Tooltip("GameObject array that holds blaster components.")] public GameObject[] blaster;
        public MeshRenderer playerSkin;

        [Header("Panels")]
        [SerializeField] [Tooltip("UI GameObject that holds 'Waiting For Player' text.")] private GameObject waitingForPlayerPanel;
        [SerializeField] [Tooltip("UI GameObject that holds player data.")] private GameObject playerDataPanel;

        [Header("Data Display")]
        [SerializeField] [Tooltip("UI Text that holds player's name.")] private TextMeshProUGUI playerName;
        [SerializeField] [Tooltip("UI Toggle that tells if player is ready or not.")] private Toggle isReadyToggle;

        /// <summary>
        /// Updates the Player Card with the players data.
        /// </summary>
        /// <param name="player">Player properties to update and display</param>
        public void UpdateDisplay(Photon.Realtime.Player player)
        {
            // Properties may not be set yet when a player has just joined the room.
            var props = player.CustomProperties;
            playerName.text = (props["PlayerName"] as string) + "<br>" + (props["UserName"] as string);
            isReadyToggle.isOn = props.TryGetValue("IsReady", out object ready) && ready is bool isReady && isReady;

            if (props.TryGetValue("Skin", out object skin) && skin is int skinIndex)
                playerSkin.material = CosmeticDatabase.MaterialAt(skinIndex);

            if (props.TryGetValue("Blaster", out object blasterProp) && blasterProp is int blasterIndex)
            {
                foreach (GameObject item in blaster)
                {
                    item.GetComponent<MeshRenderer>().material = CosmeticDatabase.MaterialAt(blasterIndex);
                }
            }

            waitingForPlayerPanel.SetActive(false);
            playerDataPanel.SetActive(true);
        }

        /// <summary>
        /// Updates the Player Card with the players data.
        /// </summary>
        /// <param name="player">Player properties to update and display</param>
        public void UpdateDisplay(int playerNum)
        {
            playerName.text = $"P{playerNum}";

            playerSkin.material = CosmeticDatabase.MaterialAt(Player.Instance.currentSkin);

            foreach (GameObject item in blaster)
            {
                item.GetComponent<MeshRenderer>().material = CosmeticDatabase.MaterialAt(Player.Instance.currentBlaster);
            }

            waitingForPlayerPanel.SetActive(false);
            playerDataPanel.SetActive(true);
        }

        /// <summary>
        /// Disables the display and sets the waiting for player text to active.
        /// </summary>
        public void DisableDisplay()
        {
            waitingForPlayerPanel.SetActive(true);
            playerDataPanel.SetActive(false);
        }

    }
}
