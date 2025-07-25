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
#if UNITY_PS5 && !UNITY_EDITOR
        public static PSUser mainUser;
        public static PSGamePad mainPlayerGamePad;
#endif

        private void Start()
        {
            playerInput = GetComponent<PlayerInput>();
#if UNITY_PS5 && !UNITY_EDITOR
            mainUser = null;
            mainPlayerGamePad = null;
#endif
            //ConnectController(PSUser.GetActiveUser);
        }

        public static void ConnectController(PSUser pSUser)
        {
#if UNITY_PS5 && !UNITY_EDITOR
                if (pSUser != null)
                {
                    mainUser = pSUser;
                    mainPlayerGamePad = pSUser.gamePad;
                    playerInput.SwitchCurrentControlScheme(pSUser.gamePad.currentGamepad);
                }
#endif
        }

        private void Update()
        {
#if UNITY_PS5 && !UNITY_EDITOR
            if(mainUser != null && Gamepad.current != mainUser.gamePad.currentGamepad && mainPlayerGamePad != mainUser.gamePad )
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
