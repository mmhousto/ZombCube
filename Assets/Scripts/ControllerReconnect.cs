using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Com.GCTC.ZombCube
{
    public class ControllerReconnect : MonoBehaviour
    {
        private static PlayerInput playerInput; // Reference to your PlayerInput component
        private static PSUser mainUser;

        private void Start()
        {
            playerInput = GetComponent<PlayerInput>();

        }

        public static void ConnectController(PSUser pSUser)
        {
#if UNITY_PS5 && !UNITY_EDITOR
                if (pSUser != null && pSUser.gamePad != null && pSUser.gamePad.currentGamepad != null)
                {
                    mainUser = pSUser;
                    playerInput.SwitchCurrentControlScheme(pSUser.gamePad.currentGamepad);
                }
#endif
        }

        private void Update()
        {
#if UNITY_PS5 && !UNITY_EDITOR
            if(Gamepad.current != mainUser.gamePad.currentGamepad)
            {
                foreach (PSUser user in PSUser.users)
                {
                    if (Gamepad.current == user.gamePad.currentGamepad && user.gamePad.IsMainPlayer)
                    {
                        ConnectController(user);
                    }
                }
            }
#endif
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
            if (change == InputDeviceChange.Reconnected)
            {

                // Manually reassign the device to the PlayerInput component
                playerInput.SwitchCurrentControlScheme(device);
            }
        }
    }
}
