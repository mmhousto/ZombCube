/*
The PlayerInput component has an auto-switch control scheme action that allows automatic changing of connected devices.
IE: Switching from Keyboard to Gamepad in-game.
When built to a mobile phone; in most cases, there is no concept of switching connected devices as controls are typically driven through what is on the device's hardware (Screen, Tilt, etc)
In Input System 1.0.2, if the PlayerInput component has Auto Switch enabled, it will search the mobile device for connected devices; which is very costly and results in bad performance.
This is fixed in Input System 1.1.
For the time-being; this script will disable a PlayerInput's auto switch control schemes; when project is built to mobile.
*/

using UnityEngine;
using UnityEngine.InputSystem;

public class MobileDisableAutoSwitchControls : MonoBehaviour
{

#if (UNITY_IOS || UNITY_ANDROID)

    [Header("Target")]
    public PlayerInput playerInput;

    private GameObject currentPlayer;

    public void GetPlayer(GameObject player)
    {
        currentPlayer = player;
        playerInput = currentPlayer.GetComponent<PlayerInput>();
    }

    void Start()
    {
        DisableAutoSwitchControls();
    }


    void DisableAutoSwitchControls()
    {
        if(playerInput != null)
            playerInput.neverAutoSwitchControlSchemes = true;
    }

#else
    private void Start()
    {
        this.gameObject.SetActive(false);
    }
    

#endif

}