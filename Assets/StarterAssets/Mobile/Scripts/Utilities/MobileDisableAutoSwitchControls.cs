/*
The PlayerInput component has an auto-switch control scheme action that allows automatic changing of connected devices.
IE: Switching from Keyboard to Gamepad in-game.
When built to a mobile phone; in most cases, there is no concept of switching connected devices as controls are typically driven through what is on the device's hardware (Screen, Tilt, etc)
In Input System 1.0.2, if the PlayerInput component has Auto Switch enabled, it will search the mobile device for connected devices; which is very costly and results in bad performance.
This is fixed in Input System 1.1.
For the time-being; this script will disable a PlayerInput's auto switch control schemes; when project is built to mobile.
*/

using Com.GCTC.ZombCube;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class MobileDisableAutoSwitchControls : MonoBehaviourPun
{

#if (UNITY_IOS || UNITY_ANDROID)

    [Header("Target")]
    public PlayerInput playerInput;

    private GameObject currentPlayer, touchZone, lookStick;

    public void GetPlayer(GameObject player)
    {
        currentPlayer = player;
        playerInput = currentPlayer.GetComponent<PlayerInput>();
    }

    void Start()
    {
        touchZone = GameObject.Find("UI_Virtual_TouchZone");
        lookStick = GameObject.Find("UI_Virtual_Joystick_Look");
        if (PlayerPrefs.GetInt("SwipeToLook", 0) == 0)
        {
            EnableStickLook();
        }
        else if (PlayerPrefs.GetInt("SwipeToLook", 0) == 1)
        {
            EnableSwipe();
        }

        HandleMobileControls();
        
        
    }

    private void Update()
    {
        //SetSwipeOrStickLook();
        //HandleMobileControls();
    }

    public void SetSwipeOrStickLook()
    {
        if (PlayerPrefs.GetInt("SwipeToLook", 0) == 0 && !lookStick.activeInHierarchy)
        {
            EnableStickLook();
        }
        else if (PlayerPrefs.GetInt("SwipeToLook", 0) == 1 && !touchZone.activeInHierarchy)
        {
            EnableSwipe();
        }
    }

    public void EnableSwipe()
    {
        touchZone.SetActive(true);
        lookStick.SetActive(false);
    }

    public void EnableStickLook()
    {
        touchZone.SetActive(false);
        lookStick.SetActive(true);
    }

    void HandleMobileControls()
    {
        if (playerInput != null)
        {
            if (playerInput.currentControlScheme != "Touch")
                this.gameObject.SetActive(false);
            if (playerInput.currentControlScheme == "Touch")
                this.gameObject.SetActive(true);
        }
        
        

    }

#else

    private void Start()
    {
        if (SceneLoader.GetCurrentScene().name == "NetworkGameScene")
        {
            gameObject.SetActive(false);
            //Destroy(this.gameObject);
        }
        else
            Destroy(this.gameObject);
    }

    public void SetSwipeOrStickLook()
    {
        
    }


#endif

}
