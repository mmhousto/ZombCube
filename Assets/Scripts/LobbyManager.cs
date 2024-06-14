using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace Com.GCTC.ZombCube
{
    public class LobbyManager : MonoBehaviour
    {

        private TouchScreenKeyboard keyboard;

        public TMP_InputField nameTextField;

        public GameObject playButton;

        public void SelectObject(GameObject uiElement)
        {
            if (EventSystem.current.alreadySelecting == true) { }
            else
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(uiElement);
            }

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
            SelectObject(playButton);
        }
    }
}
