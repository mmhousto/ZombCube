using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.InputSystem;

namespace Com.GCTC.ZombCube
{

    public class MainMenu : MonoBehaviour
    {
        private TouchScreenKeyboard keyboard;

        public TMP_InputField nameTextField;

        public PlayerInput playerInput;

        public GameObject exitButton, playButton;

        private InputAction back;

        private void Start()
        {
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE || UNITY_WSA)
            if(exitButton)
                exitButton.SetActive(true);
#else
            if(exitButton)
                exitButton.SetActive(false);
#endif
        }

        private void Update()
        {
            if (playButton.activeInHierarchy && !back.enabled)
                back.Enable();
            else if (!playButton.activeInHierarchy && back.enabled)
                back.Disable();
        }

        private void OnEnable()
        {
            if(back == null) back = playerInput.currentActionMap.FindAction("DeSelect");

            back.performed += _ => Back();
        }

        private void OnDisable()
        {
            back.performed -= _ => Back();
        }

        public void SelectObject(GameObject uiElement)
        {
            if (EventSystem.current.alreadySelecting == false)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(uiElement);
            }
        }

        public void ChangeHorizontalSens(float sensitivty)
        {
            PreferencesManager.SetHorizontalSens(sensitivty);
        }

        public void ChangeVerticalSens(float sensitivity)
        {
            PreferencesManager.SetVerticalSens(sensitivity);
        }

        public void ExitApplication()
        {
            Application.Quit();
        }

        public void OpenKeyboard()
        {
#if UNITY_XBOX
        keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default, false, false);
        keyboard.text = nameTextField.text;
        keyboard.characterLimit = 25;
        keyboard.active = true;

#endif
        }

        public void CloseKeyboard()
        {
#if UNITY_XBOX
        keyboard.active = false;
#endif
            if (playButton != null)
                SelectObject(playButton);
        }

        public void Back()
        {
            if(playButton != null)
                SelectObject(playButton);
        }
    }
}
