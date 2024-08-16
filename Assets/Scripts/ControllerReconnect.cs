using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Com.GCTC.ZombCube
{
    public class ControllerReconnect : MonoBehaviour
    {
        private PlayerInput playerInput; // Reference to your PlayerInput component

        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
        }

        private void OnEnable()
        {
            InputSystem.onDeviceChange += OnDeviceChange;
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Reconnected && PSGamePad.activeGamePad.loggedInUser.primaryUser)
            {

                    // Manually reassign the device to the PlayerInput component
                    playerInput.SwitchCurrentControlScheme(device);
            }
        }
    }
}
