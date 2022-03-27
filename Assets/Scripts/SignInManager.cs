using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SignInManager : MonoBehaviour
{
    public GameObject exitButton, xboxSignIn, googleSignIn;//, appleSignIn;

    private void Start()
    {
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE || UNITY_WSA)
            if(exitButton)
                exitButton.SetActive(true);
#else
        if (exitButton)
            exitButton.SetActive(false);
#endif
    }

    public void SelectObject(GameObject uiElement)
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(uiElement);
    }

    public void ExitApplication()
    {
        Application.Quit();
    }
}
