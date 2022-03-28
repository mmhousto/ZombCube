using AppleAuth;
using AppleAuth.Native;
using AppleAuth.Enums;
using AppleAuth.Extensions;
using AppleAuth.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Com.MorganHouston.ZombCube
{

    public class SignInManager : MonoBehaviour
    {
        public GameObject exitButton, xboxSignIn, googleSignIn, appleSignIn;

        private void Start()
        {
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE || UNITY_WSA)
            if(exitButton)
                exitButton.SetActive(true);
#else
            if (exitButton)
                exitButton.SetActive(false);
#endif

            if (AppleAuthManager.IsCurrentPlatformSupported)
            {
                appleSignIn.SetActive(true);
            }
            else
            {
                appleSignIn.SetActive(false);
            }

#if UNITY_ANDROID
            googleSignIn.SetActive(true);
#else
            googleSignIn.SetActive(false);
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

}
