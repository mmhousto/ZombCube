using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DapperDino.UMT.Lobby.UI;

public class PlayerCard : MonoBehaviour
{
    [Tooltip("GameObject array that holds blaster components.")] public GameObject[] blaster;

    [Header("Panels")]
    [SerializeField] [Tooltip("UI GameObject that holds 'Waiting For Player' text.")] private GameObject waitingForPlayerPanel;
    [SerializeField] [Tooltip("UI GameObject that holds player data.")] private GameObject playerDataPanel;

    [Header("Data Display")]
    [SerializeField] [Tooltip("UI Text that holds player's name.")] private TextMeshProUGUI playerName;
    [SerializeField] [Tooltip("UI Toggle that tells if player is ready or not.")] private Toggle isReadyToggle;

    /// <summary>
    /// Updates the Player Card with the players data.
    /// </summary>
    /// <param name="lobbyPlayerState"></param>
    public void UpdateDisplay(LobbyPlayerState lobbyPlayerState)
    {
        playerName.text = lobbyPlayerState.PlayerName;
        isReadyToggle.isOn = lobbyPlayerState.IsReady;

        foreach (GameObject item in blaster)
        {
            item.GetComponent<MeshRenderer>().material = Player.Instance.materials[lobbyPlayerState.CurrentBlaster];
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
