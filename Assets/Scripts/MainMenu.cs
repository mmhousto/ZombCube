using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace Com.MorganHouston.ZombCube
{

    public class MainMenu : MonoBehaviour
    {
        private TouchScreenKeyboard keyboard;

        public TMP_InputField nameTextField;

        public GameObject exitButton;

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

        public void SelectObject(GameObject uiElement)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(uiElement);
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
        }
    }
}
