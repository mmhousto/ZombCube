using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class MainMenu : MonoBehaviour
{
    private TouchScreenKeyboard keyboard;

    public TMP_InputField nameTextField;

    public void SelectObject(GameObject uiElement)
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(uiElement);
    }

    public void OpenKeyboard()
    {
        keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default);
        keyboard.text = nameTextField.text;
        keyboard.characterLimit = 20;
        keyboard.active = true;
    }

    public void CloseKeyboard()
    {
        keyboard.active = false;
    }
}
