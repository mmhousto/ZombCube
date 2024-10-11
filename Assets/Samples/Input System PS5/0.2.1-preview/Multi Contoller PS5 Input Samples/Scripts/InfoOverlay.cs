using System;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine.InputSystem.PS5.ControllerSample
{
    public class InfoOverlay : MonoBehaviour
    {
        [SerializeField] SampleGamepad sampleGamepad;

        [Header("UI")]
        [SerializeField] GameObject uiParent;
        [SerializeField] Text infoText;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void OnEnable()
        {
            if(sampleGamepad.gamepadControls?.Main.OpenDebugMenu != null)
                sampleGamepad.gamepadControls.Main.OpenDebugMenu.performed += ToggleInfoMenu;
        }

        void OnDisable()
        {
            if(sampleGamepad.gamepadControls?.Main.OpenDebugMenu != null)
                sampleGamepad.gamepadControls.Main.OpenDebugMenu.performed -= ToggleInfoMenu;
        }

        void ToggleInfoMenu(InputAction.CallbackContext obj)
        {
            SetActive(!uiParent.activeSelf);

            if (!uiParent.activeSelf)
            {
                return;
            }

            var gamepad = sampleGamepad.gamepadDevicePS5;
            if (gamepad == null)
            {
                infoText.text = "Please run on device";
            }

            #if !UNITY_PS5_ENHANCED_INPUT_SYSTEM
            infoText.text = $"<b>UserID</b> {gamepad.ps5UserId}\n" +
            $"<b>SlotID</b> {gamepad.slotIndex}\n";
            #else
            infoText.text = $"<b>UserID</b> {gamepad.ps5UserId}\n" +
                $"<b>SlotID</b> {gamepad.slotIndex}\n" +
                $"<b>Connection Type</b> {gamepad.connectionType}\n" +
                $"<b>Device Class</b> {gamepad.deviceClass}\n" +
                $"<b>Touchpad</b> (<b>DPI:</b>  {gamepad.touchPadInformation.pixelDensity}, <b>Resolution:</b> {gamepad.touchPadInformation.resolutionX} x {gamepad.touchPadInformation.resolutionY})\n" +
                $"<b>Stick Deadzones</b> (<b>Left:</b> {gamepad.stickInfo.leftStickDeadzone}, <b>Right:</b> {gamepad.stickInfo.rightStickDeadzone})";
            #endif
        }

        public void SetActive(bool active)
        {
            uiParent.SetActive(active);
        }
    }
}

