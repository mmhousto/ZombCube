using AppleAuth;
using AppleAuth.Native;
using AppleAuth.Enums;
using AppleAuth.Extensions;
using AppleAuth.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Com.GCTC.ZombCube
{

    public class SignInManager : MonoBehaviour
    {
        public GameObject exitButton, googleSignIn, appleSignIn, playButton;

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
                playButton.SetActive(false);
            }
            else
            {
                appleSignIn.SetActive(false);
            }

#if UNITY_ANDROID
            googleSignIn.SetActive(true);
            playButton.SetActive(false);
#else
            googleSignIn.SetActive(false);
#endif

        }

        public void SignInAuto()
        {
#if UNITY_ANDROID
            SignInGoogle();
#elif UNITY_IOS
            SignInApple();
#else
            SignInAnon();
#endif
        }

        public void SignInAnon()
        {
            CloudSaveLogin.Instance.SignInAnonymously();
        }

        public void SignInApple()
        {
            CloudSaveLogin.Instance.SignInApple();
        }

        public void SignInGoogle()
        {
#if UNITY_ANDROID
            CloudSaveLogin.Instance.LoginGooglePlayGames();
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
