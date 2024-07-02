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
        public GameObject signingIn, buttonsPanel;

        private void Start()
        {
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE || UNITY_WSA)
            if(exitButton)
                exitButton.SetActive(true);
#else
            if (exitButton)
                exitButton.SetActive(false);
#endif

#if UNITY_IOS
                appleSignIn.SetActive(true);
                playButton.SetActive(false);
#else
                appleSignIn.SetActive(false);
#endif

#if UNITY_ANDROID
            //googleSignIn.SetActive(true);
            playButton.SetActive(true);
            SignInAnon();
#endif
            googleSignIn.SetActive(false);
        }

        private void Update()
        {
            if(CloudSaveLogin.Instance.isSigningIn && !signingIn.activeInHierarchy)
            {
                signingIn.SetActive(true);
                buttonsPanel.SetActive(false);
            }
            else if (!CloudSaveLogin.Instance.isSigningIn && !buttonsPanel.activeInHierarchy)
            {
                signingIn.SetActive(false);
                buttonsPanel.SetActive(true);
            }
        }

        public void SignInAuto()
        {
#if UNITY_ANDROID
            SignInAnon();
#elif UNITY_IOS
            SignInApple();
#else
            if(CloudSaveLogin.Instance.isSteam && Application.internetReachability != NetworkReachability.NotReachable)
                SignInSteam();
            else
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

        public void SignInSteam()
        {
            CloudSaveLogin.Instance.SignInWithSteam();
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
