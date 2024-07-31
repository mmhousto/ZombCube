using UnityEngine;
using TMPro;
using UnityEngine.UI;

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
#if UNITY_PS5 && !UNITY_EDITOR
            if (CloudSaveLogin.Instance.restricted)
                playerName.text = (string)player.CustomProperties["UserName"];
            else
                playerName.text = (string)player.CustomProperties["PlayerName"] + "<br>" + (string)player.CustomProperties["UserName"];

            var blockedUsers = PSUserProfiles.GetBlockedUsers();
            if(blockedUsers != null)
            {
                foreach(var blockedUser in blockedUsers)
                {
                    if((string)player.CustomProperties["AccountID"] == blockedUser.ToString())
                    {
                        playerName.text = (string)player.CustomProperties["UserName"];
                    }
                }
            }
#else
            playerName.text = (string)player.CustomProperties["PlayerName"] + "<br>" + (string)player.CustomProperties["UserName"];
#endif
            isReadyToggle.isOn = (bool)player.CustomProperties["IsReady"];

            playerSkin.material = MaterialSelector.Instance.materials[(int)player.CustomProperties["Skin"]];

            foreach (GameObject item in blaster)
            {
                item.GetComponent<MeshRenderer>().material = MaterialSelector.Instance.materials[(int)player.CustomProperties["Blaster"]];
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

            playerSkin.material = MaterialSelector.Instance.materials[Player.Instance.currentSkin];

            foreach (GameObject item in blaster)
            {
                item.GetComponent<MeshRenderer>().material = MaterialSelector.Instance.materials[Player.Instance.currentBlaster];
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
