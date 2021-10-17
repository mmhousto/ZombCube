using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DapperDino.UMT.Lobby.UI;

public class PlayerCard : MonoBehaviour
{
    public GameObject[] blaster;

    [Header("Panels")]
    [SerializeField] private GameObject waitingForPlayerPanel;
    [SerializeField] private GameObject playerDataPanel;

    [Header("Data Display")]
    [SerializeField] private TextMeshProUGUI playerName;
    [SerializeField] private Toggle isReadyToggle;

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

    public void DisableDisplay()
    {
        waitingForPlayerPanel.SetActive(true);
        playerDataPanel.SetActive(false);
    }

    // Start is called before the first frame update
    void Start()
    {

        
    
    }

}
