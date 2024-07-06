using System;
using UnityEngine;
using UnityEngine.InputSystem.Users;

#if UNITY_EDITOR && UNITY_PS5
using DualSensePad = UnityEngine.InputSystem.PS5.DualSenseGamepadPC;
#elif UNITY_PS5
using DualSensePad = UnityEngine.InputSystem.PS5.DualSenseGamepad;
#endif

namespace UnityEngine.InputSystem.PS5.ControllerSample
{
    public class SampleGamepad : MonoBehaviour
    {
        [SerializeField]
        GamepadDisplay gamepadDisplay;
        [SerializeField]
        GamepadTriggerEffects gamepadTriggerEffects;
        [SerializeField]
        GamepadHaptics gamepadHaptics;
        [SerializeField]
        InfoOverlay infoOverlay;

        public InputUser inputUser { get; private set; }
        public DualSensePad gamepadDevice { get; private set; } //Device on either platform (PS5 or PC)
        public DualSenseGamepad gamepadDevicePS5 => (Gamepad)gamepadDevice as DualSenseGamepad;  //Device on PS5
        public DualSenseGamepadPC gamepadDevicePC => (Gamepad)gamepadDevice as DualSenseGamepadPC;  //Device on PC
        public GamepadControls gamepadControls { get; private set; }
        public IInputActionCollection2 actionCollection => gamepadControls;

        void Awake()
        {
            gamepadControls = new GamepadControls();
        }

        void Start()
        {
            gamepadDisplay.Initalize();
        }

        public bool SetupUserWithDevice(InputDevice device)
        {
            if (device is not DualSensePad dualSenseGamepad)
            {
                Debug.LogWarning("Unexpected device type");
                return false;
            }

            inputUser = InputUser.PerformPairingWithDevice(dualSenseGamepad, options: InputUserPairingOptions.UnpairCurrentDevicesFromUser);
            inputUser.AssociateActionsWithUser(actionCollection);
            actionCollection.Enable();
            gamepadDevice = dualSenseGamepad;

            gamepadDisplay.enabled = true;
            gamepadTriggerEffects.enabled = true;
            gamepadHaptics.enabled = true;
            infoOverlay.enabled = true;

            return true;
        }


        public bool UnpairUser()
        {
            InputSystem.RemoveDevice(gamepadDevice);
            inputUser.UnpairDevices();
            actionCollection.Disable();
            gamepadDevice = null;
            gamepadDisplay.UpdateUsernameOutput();
            inputUser = default(InputUser);

            gamepadDisplay.enabled = false;
            gamepadTriggerEffects.enabled = false;
            gamepadHaptics.enabled = false;
            infoOverlay.enabled = false;
            infoOverlay.SetActive(false);

            return true;
        }
    }
}

