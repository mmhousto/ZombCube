#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using System.Threading.Tasks;
using Unity.Services.Core;
//using Facebook.Unity;
using System;
using AppleAuth;
using AppleAuth.Native;
using AppleAuth.Enums;
using AppleAuth.Extensions;
using AppleAuth.Interfaces;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Security.Principal;
using PSNSample;

#if UNITY_ANDROID
using GooglePlayGames.BasicApi;
using GooglePlayGames;
using WebSocketSharp;
#endif
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace Com.GCTC.ZombCube
{

    public class CloudSaveLogin : MonoBehaviour
    {

        #region Fields/Variables

        private static CloudSaveLogin instance;

        public static CloudSaveLogin Instance { get { return instance; } }

        // What SSO Option the use is using atm.
        public enum ssoOption { Anonymous, Facebook, Google, Apple, Steam, PS }

        // Player Data Object
        public Player player;

        public ssoOption currentSSO = ssoOption.Anonymous;

        private IAppleAuthManager appleAuthManager;

        private bool triedQuickLogin = false;

        public bool isSteam = false;
        public bool devModeActivated = false;
        public bool gameCenterSignedIn = false;
        public bool loggedIn = false;
        public bool isSigningIn = false;

        // User Info.
        public string userName, userID;

#if !DISABLESTEAMWORKS
        public GameObject steamStats;

        Callback<GetTicketForWebApiResponse_t> m_AuthTicketForWebApiResponseCallback;
        string m_SessionTicket;
        string identity = "unityauthenticationservice";
#endif


        #endregion


        #region MonoBehaviour Methods


        // Start is called before the first frame update
        async void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                instance = this;
                DontDestroyOnLoad(Instance.gameObject);
            }

            AdsInitializer.timesPlayed = 0;

            if(Application.internetReachability == NetworkReachability.NotReachable)
            {
                isSigningIn = true;

                currentSSO = ssoOption.Anonymous;

                userID = "OfflineMode";
                userName = "Guest_" + userID;

                SetPlayer(userID);

                Login();
                return;
            }

            if (UnityServices.State == ServicesInitializationState.Initialized)
            {

            }
            else
            {
                await UnityServices.InitializeAsync();
            }

            /*
            if (!FB.IsInitialized)
            {
                // Initialize the Facebook SDK
                FB.Init(InitCallback, OnHideUnity);
            }
            else
            {
                // Already initialized, signal an app activation App Event
                FB.ActivateApp();
            }*/


#if UNITY_WSA
            isSigningIn = false;
#elif UNITY_ANDROID
            // Initializes Google Play Games Login
            InitializePlayGamesLogin();
#elif UNITY_PS5 || UNITY_PS4
            isSigningIn = true;
            GetComponent<PSNManager>().Initialize();
#else
            if (isSteam && SteamManager.Initialized)
            {
                isSigningIn = false;

            }
            else
                isSigningIn = false;
#endif
        }

        private void Start()
        {
            StartCoroutine(checkInternetConnection((isConnected) => OfflineLogin(isConnected)));

            // If the current platform is supported initialize apple authentication.
            if (AppleAuthManager.IsCurrentPlatformSupported)
            {
                // Creates a default JSON deserializer, to transform JSON Native responses to C# instances
                IPayloadDeserializer deserializer = new PayloadDeserializer();
                // Creates an Apple Authentication manager with the deserializer
                appleAuthManager = new AppleAuthManager(deserializer);
            }
        }

        void Update()
        {
#if (UNITY_IOS || UNITY_STANDALONE_OSX)
            // Updates the AppleAuthManager instance to execute
            // pending callbacks inside Unity's execution loop
            if (appleAuthManager != null)
            {
                appleAuthManager.Update();
            }

            // Tries to quick login on Apple, if user previously logged in.
            if (triedQuickLogin == false && appleAuthManager != null)
            {
                GetCredentialState();
                triedQuickLogin = true;
            }
#endif

        }

        /// <summary>
        /// Saves data on application exit.
        /// </summary>
        private void OnApplicationQuit()
        {
            SaveCloudData();

        }

        /// <summary>
        /// Saves data to cloud on application pause or swipe out.
        /// </summary>
        /// <param name="pause"></param>
        private void OnApplicationPause(bool pause)
        {
#if (UNITY_IOS || UNITY_ANDROID)
        if(pause)
            SaveCloudData();
#endif
        }


        #endregion

        IEnumerator checkInternetConnection(Action<bool> action)
        {
            WWW www = new WWW("http://google.com");
            yield return www;
            if (www.error != null)
            {
                action(false);
            }
            else
            {
                action(true);
            }
        }

        #region Public Sign In/Out Methods


        public async void DeleteAccount()
        {
            if (currentSSO == ssoOption.PS)
            {
                SaveSystem.DeletePlayer();
                userID = "";
                userName = "";
#if UNITY_PS5
                GetComponent<PSAuth>().ResetInit();
#endif
            }
            else
            {
                await ForceDeleteSpecificData(userID);
                SaveSystem.DeletePlayer();
                await AuthenticationService.Instance.DeleteAccountAsync();

                userID = "";
                userName = "";

                if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Apple)
                {
                    this.appleAuthManager.SetCredentialsRevokedCallback(result =>
                    {
                        // Sign in with Apple Credentials were revoked.
                        // Discard credentials/user id and go to login screen.

                        PlayerPrefs.SetString("AppleUserIdKey", "");
                        PlayerPrefs.SetString("AppleUserNameKey", "");
                        PlayerPrefs.SetString("AppleTokenIdKey", "");
                    });
                }
            }


            LogoutScreenActivate();
        }



        /// <summary>
        /// Signs user into an anonymous account.
        /// </summary>
        public async void SignInAnonymously()
        {
            StartCoroutine(checkInternetConnection((isConnected) => OfflineLogin(isConnected)));
            if (isSigningIn) return;
            isSigningIn = true;

            currentSSO = ssoOption.Anonymous;

            AuthenticationService.Instance.SwitchProfile("default");

            // Cloud Save needs to be initialized along with the other Unity Services that
            // it depends on (namely, Authentication), and then the user must sign in.
            if (UnityServices.State == ServicesInitializationState.Initialized)
            {
                Debug.Log("Services are already Initialized");
            }
            else
                await UnityServices.InitializeAsync();


            if (AuthenticationService.Instance.IsSignedIn)
            {
                userID = AuthenticationService.Instance.PlayerId;
                userName = "Guest_" + userID;
                SetPlayer(userID);

                Login();
            }
            else
                await SignInAnonymouslyAsync();

        }

        /// <summary>
        /// Signs user into dev account.
        /// </summary>
        public async void SignInDeveloper()
        {
            if (isSigningIn) return;
            isSigningIn = true;

            devModeActivated = true;
            await SignInAnonymouslyAsync();
        }

#if UNITY_PS5
        public void PSAuthInit()
        {
            GetComponent<PSAuth>().Initialize();
        }

        public void PSSignIn()
        {
            GetComponent<PSAuth>().SignIn();
        }

        public void SignInPS(string psnUserID, string tokenID, string authCode)
        {
            currentSSO = ssoOption.PS;

            userID = PSUser.GetActiveUserId.ToString();
            userName = psnUserID;

            if (PSSaveData.singleton.initialized)
                PSSaveData.singleton.StartAutoSaveLoad();
            else
                PSSaveData.singleton.InitializeSaveData();

            PSTrophies.Initialize();

            //SetPlayer(psnUserID, psnUserID);


            //Login();

        }
#endif

        /*
        /// <summary>
        /// Signs user into facebook account with authentication from Facebook.
        /// </summary>
        public void SignInFacebook()
        {
            currentSSO = ssoOption.Facebook;
            AuthenticationService.Instance.SwitchProfile("facebook");

#if UNITY_ANDROID
        FB.Android.RetrieveLoginStatus(LoginStatusCallback);
#else
            var perms = new List<string>() { "public_profile" };
            FB.LogInWithReadPermissions(perms, AuthCallback);
#endif

        }*/

        /// <summary>
        /// Signs user into Apple with Auth from Apple.
        /// </summary>
        public async void SignInApple()
        {
            if (isSigningIn || AuthenticationService.Instance.IsSignedIn || AppleAuthManager.IsCurrentPlatformSupported == false) return;
            isSigningIn = true;

            currentSSO = ssoOption.Apple;
            if (!AuthenticationService.Instance.IsSignedIn)
                AuthenticationService.Instance.SwitchProfile("apple");

            var idToken = await GetAppleIdTokenAsync();

            await AuthenticationService.Instance.SignInWithAppleAsync(idToken);

            SetPlayer(AuthenticationService.Instance.PlayerId, userName);

            SignInGameCenter();

            Login();

        }

        public void SignInWithSteam()
        {
#if !DISABLESTEAMWORKS
            StartCoroutine(checkInternetConnection((isConnected) => OfflineLogin(isConnected)));
            if (isSigningIn || AuthenticationService.Instance.IsSignedIn ||  SteamManager.Initialized == false) return;
            isSigningIn = true;

            currentSSO = ssoOption.Steam;
            AuthenticationService.Instance.SwitchProfile("steam");

            // It's not necessary to add event handlers if they are 
            // already hooked up.
            // Callback.Create return value must be assigned to a 
            // member variable to prevent the GC from cleaning it up.
            // Create the callback to receive events when the session ticket
            // is ready to use in the web API.
            // See GetAuthSessionTicket document for details.
            m_AuthTicketForWebApiResponseCallback = Callback<GetTicketForWebApiResponse_t>.Create(OnAuthCallback);

            SteamUser.GetAuthTicketForWebApi(identity);
#endif
        }

#if !DISABLESTEAMWORKS
        void OnAuthCallback(GetTicketForWebApiResponse_t callback)
        {
            m_SessionTicket = BitConverter.ToString(callback.m_rgubTicket).Replace("-", string.Empty);
            m_AuthTicketForWebApiResponseCallback.Dispose();
            m_AuthTicketForWebApiResponseCallback = null;

            CSteamID cSteamID = SteamUser.GetSteamID();

            userID = cSteamID.m_SteamID.ToString();
            userName = SteamFriends.GetPersonaName();
            // Call Unity Authentication SDK to sign in or link with Steam, displayed in the following examples, using the same identity string and the m_SessionTicket.
            CallSignInSteam(m_SessionTicket);
        }

        private async void CallSignInSteam(string sessionTicket)
        {
            await SignInWithSteamAsync(m_SessionTicket);
        }

#endif

        /// <summary>
        /// Saves player data to cloud if user is signed in.
        /// </summary>
        public async void SaveCloudData()
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                SaveData data = new SaveData(player);
                await ForceSaveObjectData(player.userID, data);
            }
        }

        /// <summary>
        /// Logs out the user, unless logged in with Apple account will display message.
        /// </summary>
        public void Logout()
        {
            SaveLogout();

            LogoutScreenActivate();

        }

        /// <summary>
        /// Developer sign out option to delete all player data and sign out.
        /// </summary>
        public async void DevSignOut()
        {

            await DeleteEverythingSignOut();
        }

        /*/// <summary>
        /// Logs out of facebook.
        /// </summary>
        public void FacebookLogout()
        {
            FB.LogOut();
        }*/


#endregion


#region Private Login/Logout Methods

        private void OfflineLogin(bool isConnected)
        {
            if (!isConnected)
            {
                currentSSO = ssoOption.Anonymous;

                userID = "OfflineMode";
                userName = "Guest_" + userID;

                SetPlayer(userID);

                Login();
            }
        }

        /// <summary>
        /// Loads the Main Menu Scene.
        /// </summary>
        public void Login()
        {
            loggedIn = true;
            isSigningIn = false;
            SceneLoader.LoadThisScene(1);
        }

        /// <summary>
        /// Loads the Sign-In Scene.
        /// </summary>
        private void LogoutScreenActivate()
        {
            loggedIn = false;
            SceneLoader.LoadThisScene(0);
        }

        /// <summary>
        /// Saves and logs out of user and resets player data.
        /// </summary>
        private void SaveLogout()
        {
            SaveCloudData();

            /*if (FB.IsLoggedIn)
            {
                FacebookLogout();
            }*/

            if (AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignOut();
            }

#if UNITY_ANDROID
            if(PlayGamesPlatform.Instance.IsAuthenticated())
                PlayGamesPlatform.Instance.SignOut();
#endif

#if UNITY_PS5 || UNITY_PS4
            GetComponent<PSAuth>().ResetInit();
#endif

            ResetPlayer();
        }


#endregion


#region Apple Auth
        
        /// <summary>
        /// Performs continue with Apple login.
        /// </summary>
        public async void QuickLoginApple()
        {
            if (appleAuthManager == null) return;

            currentSSO = ssoOption.Apple;
            if (!AuthenticationService.Instance.IsSignedIn)
                AuthenticationService.Instance.SwitchProfile("apple");

            var quickLoginArgs = new AppleAuthQuickLoginArgs();

            this.appleAuthManager.QuickLogin(
                quickLoginArgs,
                credential =>
                {
                    // Received a valid credential!
                    // Try casting to IAppleIDCredential or IPasswordCredential

                    // Previous Apple sign in credential
                    var appleIdCredential = credential as IAppleIDCredential;

                    // Saved Keychain credential (read about Keychain Items)
                    var passwordCredential = credential as IPasswordCredential;

                    if (appleIdCredential != null)
                    {
                        userID = PlayerPrefs.GetString("AppleUserIdKey", "");
                        userName = PlayerPrefs.GetString("AppleUserNameKey", "");

                    }

                },
                error =>
                {
                    Debug.Log("Quick Login Apple Failed");
                    return;
                    // Quick login failed. The user has never used Sign in With Apple on your app. Go to login screen
                });

            var idToken = PlayerPrefs.GetString("AppleTokenIdKey");

            await AuthenticationService.Instance.SignInWithAppleAsync(idToken);

            SetPlayer(AuthenticationService.Instance.PlayerId, userName);

            SignInGameCenter();

            Login();
        }

        public void SignInGameCenter()
        {
            Social.localUser.Authenticate(success =>
            {
                if (success)
                {
                    gameCenterSignedIn = true;
                    SetPlayer(AuthenticationService.Instance.PlayerId, Social.localUser.userName);
                }
                else
                {
                    gameCenterSignedIn = false;
                }
            });
        }

        /// <summary>
        /// Checks if user has logged in with apple before on device, if so continues to quick login.
        /// </summary>
        public void GetCredentialState()
        {
            userID = PlayerPrefs.GetString("AppleUserIdKey");
            this.appleAuthManager.GetCredentialState(
                        userID,
                        state =>
                        {
                            switch (state)
                            {
                                case CredentialState.Authorized:
                                    // User ID is still valid. Login the user.
                                    Debug.Log("User ID is valid!");
                                    QuickLoginApple();
                                    break;

                                case CredentialState.Revoked:
                                    // User ID was revoked. Go to login screen.
                                    Debug.Log("User ID was revoked.");
                                    PlayerPrefs.SetString("AppleUserIdKey", "");
                                    PlayerPrefs.SetString("AppleUserNameKey", "");
                                    PlayerPrefs.SetString("AppleTokenIdKey", "");
                                    if (AuthenticationService.Instance.IsSignedIn)
                                    {
                                        AuthenticationService.Instance.SignOut();
                                    }
                                    isSigningIn = false;
                                    break;

                                case CredentialState.NotFound:
                                    // User ID was not found. Go to login screen.
                                    isSigningIn = false;
                                    break;
                            }
                        },
                        error =>
                        {
                            // Something went wrong- FAILED
                            if (AuthenticationService.Instance.IsSignedIn)
                            {
                                AuthenticationService.Instance.SignOut();
                            }
                            isSigningIn = false;
                            return;
                        });
        }


        /// <summary>
        /// Gets the Apple Identity Token to Authenticate the user.
        /// Stores username, userID, Identity Token and email in Player Prefs.
        /// Returns the Identity Token.
        /// </summary>
        /// <returns></returns>
        private Task<string> GetAppleIdTokenAsync()
        {
            var tcs = new TaskCompletionSource<string>();

            if (appleAuthManager == null) return null;

            var loginArgs = new AppleAuthLoginArgs(LoginOptions.IncludeFullName);

            this.appleAuthManager.LoginWithAppleId(
                loginArgs,
                credential =>
                {
                    //      Obtained credential, cast it to IAppleIDCredential
                    var appleIdCredential = credential as IAppleIDCredential;
                    if (appleIdCredential != null)
                    {
                        // Apple User ID
                        // You should save the user ID somewhere in the device
                        if(appleIdCredential.User != null)
                        {
                            userID = appleIdCredential.User;
                            PlayerPrefs.SetString("AppleUserIdKey", userID);
                        }
                        else
                        {
                            userID = PlayerPrefs.GetString("AppleUserIdKey", "");
                        }
                        

                        // Email (Received ONLY in the first login)
                        /*email = appleIdCredential.Email;
                            PlayerPrefs.SetString("AppleUserEmailKey", email);*/

                        // Full name (Received ONLY in the first login)
                        if(appleIdCredential.FullName != null)
                        {
                            userName = appleIdCredential.FullName.ToLocalizedString();
                            PlayerPrefs.SetString("AppleUserNameKey", userName);
                        }
                        else
                        {
                            userName = PlayerPrefs.GetString("AppleUserNameKey", "");
                        }
                            
                        

                        // Identity token
                        var idToken = Encoding.UTF8.GetString(
                                appleIdCredential.IdentityToken,
                                0,
                                appleIdCredential.IdentityToken.Length);

                        tcs.SetResult(idToken);

                        PlayerPrefs.SetString("AppleTokenIdKey", idToken);

                        // Authorization code
                        var AuthCode = Encoding.UTF8.GetString(
                                        appleIdCredential.AuthorizationCode,
                                        0,
                                        appleIdCredential.AuthorizationCode.Length);

                        // And now you have all the information to create/login a user in your system

                    }
                    else
                    {
                        tcs.SetException(new Exception("Retrieving Apple Id Token failed."));
                    }
                },
                error =>
                {
                    // Something went wrong
                    tcs.SetException(new Exception("Retrieving Apple Id Token failed."));
                    var authorizationErrorCode = error.GetAuthorizationErrorCode();
                    return;
                });

            return tcs.Task;

        }


        #endregion


        #region Facebook Auth
        /*
        /// <summary>
        /// Initializes Facebook SDK
        /// </summary>
        private void InitCallback()
        {
            if (FB.IsInitialized)
            {
                // Signal an app activation App Event
                FB.ActivateApp();
                // Continue with Facebook SDK
                // ...
            }
            else
            {
                Debug.Log("Failed to Initialize the Facebook SDK");
            }
        }

        private void OnHideUnity(bool isGameShown)
        {
            if (!isGameShown)
            {
                // Pause the game - we will need to hide
                Time.timeScale = 0;
            }
            else
            {
                // Resume the game - we're getting focus again
                Time.timeScale = 1;
            }
        }

        /// <summary>
        /// Callback to get player info on login.
        /// </summary>
        /// <param name="result"></param>
        private async void AuthCallback(ILoginResult result)
        {
            if (FB.IsLoggedIn)
            {
                // AccessToken class will have session details
                var aToken = AccessToken.CurrentAccessToken;
                // Print current access token's User ID
                Debug.Log(aToken.UserId);
                userID = aToken.UserId;

                FB.API("me?fields=id,name", HttpMethod.GET, AssignInfo);



                await SignInWithFacebookAsync(aToken.TokenString);

            }
            else
            {
                Debug.Log("User cancelled login");
            }
        }

        /// <summary>
        /// Assigns player info on login.
        /// </summary>
        /// <param name="result"></param>
        void AssignInfo(IGraphResult result)
        {
            if (result.Error != null)
            {
                Debug.Log("Error: " + result.Error);
            }
            else if (!FB.IsLoggedIn)
                Debug.Log("Login Canceled By Player");
            else
            {
                userID = result.ResultDictionary["id"].ToString();
                userName = result.ResultDictionary["name"].ToString();
            }
        }

        /// <summary>
        /// Signs the player into Unity Services and sets player data.
        /// </summary>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        async Task SignInWithFacebookAsync(string accessToken)
        {
            try
            {
                await AuthenticationService.Instance.SignInWithFacebookAsync(accessToken);
                Debug.Log("Sign-In With Facebook is successful.");

                SetPlayer(AuthenticationService.Instance.PlayerId, userName);

                Login();
            }
            catch (AuthenticationException ex)
            {
                // Compare error code to AuthenticationErrorCodes
                // Notify the player with the proper error message
                Debug.LogException(ex);
            }
            catch (RequestFailedException ex)
            {
                // Compare error code to CommonErrorCodes
                // Notify the player with the proper error message
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// Logs the player in, if they were logged in before.
        /// </summary>
        /// <param name="result"></param>
        private async void LoginStatusCallback(ILoginStatusResult result)
        {
            if (!string.IsNullOrEmpty(result.Error))
            {
                Debug.Log("Error: " + result.Error);

                var perms = new List<string>() { "public_profile" };
                FB.LogInWithReadPermissions(perms, AuthCallback);
            }
            else if (result.Failed)
            {
                Debug.Log("Failure: Access Token could not be retrieved");

                var perms = new List<string>() { "public_profile" };
                FB.LogInWithReadPermissions(perms, AuthCallback);
            }
            else
            {
                // Successfully logged user in
                // A popup notification will appear that says "Logged in as <User Name>"
                Debug.Log("Success: " + result.AccessToken.UserId);

                await SignInWithFacebookAsync(result.AccessToken.TokenString);

            }
        }

        */
        #endregion


        #region Google Play Auth

#if UNITY_ANDROID

        void InitializePlayGamesLogin()
        {
            // Requests an ID token be generated.  
            // This OAuth token can be used to
            // identify the player to other services such as Firebase.
            /*var config = new PlayGamesClientConfiguration.Builder()
                .RequestIdToken()
                .Build();

            PlayGamesPlatform.InitializeInstance(config);
            PlayGamesPlatform.DebugLogEnabled = true;
            PlayGamesPlatform.Activate();*/
            //LoginGooglePlayGames();
        }

        public void LoginGooglePlayGames()
        {
            currentSSO = ssoOption.Google;
            AuthenticationService.Instance.SwitchProfile("google");
            try
            {
                //PlayGamesPlatform.Instance.Authenticate((success) => { OnGooglePlayGamesLogin(success); });
                //PlayGamesPlatform.Instance.ManuallyAuthenticate(OnGooglePlayGamesLogin);
                Social.localUser.Authenticate(OnGooglePlayGamesLogin);
            }
            catch (Exception e)
            {
                Debug.Log("ERROR: " + e);
            }

        }

        async void OnGooglePlayGamesLogin(SignInStatus status)
        {
            if (status == SignInStatus.Success)
            {
                // Call Unity Authentication SDK to sign in or link with Google.
                var idToken = "";

                idToken = ((PlayGamesLocalUser)Social.localUser).GetIdToken();

                userID = Social.localUser.id;
                userName = Social.localUser.userName;

                await SignInWithGoogleAsync(idToken);

            }
            else if (status == SignInStatus.InternalError)
            {
                Debug.Log("Unsuccessful login");
                isSigningIn = false;
            }
            else
            {
                Debug.Log("Unsuccessful login");
                isSigningIn = false;
            }
        }

        async void OnGooglePlayGamesLogin(bool status)
        {
            if (status == true)
            {
                // Call Unity Authentication SDK to sign in or link with Google.
                var idToken = "";

                idToken = Social.localUser.id;

                userID = Social.localUser.id;
                userName = Social.localUser.userName;

                await SignInWithGoogleAsync(idToken);

            }
            else if (status == false)
            {
                Debug.Log("Unsuccessful login");
                isSigningIn = false;
            }
            else
            {
                Debug.Log("Unsuccessful login");
                isSigningIn = false;
            }
        }

        async Task SignInWithGoogleAsync(string idToken)
        {
            try
            {
                await AuthenticationService.Instance.SignInWithGoogleAsync(idToken);

                SetPlayer(AuthenticationService.Instance.PlayerId, userName);

                Login();
            }
            catch (AuthenticationException ex)
            {
                // Compare error code to AuthenticationErrorCodes
                // Notify the player with the proper error message
                Debug.LogException(ex);
            }
            catch (RequestFailedException ex)
            {
                // Compare error code to CommonErrorCodes
                // Notify the player with the proper error message
                Debug.LogException(ex);
            }
            isSigningIn = false;
        }
        
#endif

        #endregion


        #region Steam Auth

#if !DISABLESTEAMWORKS
        async Task SignInWithSteamAsync(string ticket)
        {
            try
            {
                await AuthenticationService.Instance.SignInWithSteamAsync(ticket, identity);

                SetPlayer(userID, userName);

                Login();

                steamStats.SetActive(true);

            }
            catch (AuthenticationException ex)
            {
                // Compare error code to AuthenticationErrorCodes
                // Notify the player with the proper error message
                //Debug.Log(ex);
                OfflineLogin(false);
            }
            catch (RequestFailedException ex)
            {
                // Compare error code to CommonErrorCodes
                // Notify the player with the proper error message
                //Debug.Log(ex);
                OfflineLogin(false);
            }
            isSigningIn = false;
        }
#endif

        #endregion


        #region Custom ID Auth

        async Task SignInWithCustomIDAsync(string authCode, string idToken)
        {
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                SetPlayer(userID, userName);

                Login();

            }
            catch (AuthenticationException ex)
            {
                // Compare error code to AuthenticationErrorCodes
                // Notify the player with the proper error message
                //Debug.Log(ex);
                OfflineLogin(false);
            }
            catch (RequestFailedException ex)
            {
                // Compare error code to CommonErrorCodes
                // Notify the player with the proper error message
                //Debug.Log(ex);
                OfflineLogin(false);
            }
            isSigningIn = false;
        }

        #endregion


        #region Private Methods

        /// <summary>
        /// Signs in an anonymous player.
        /// </summary>
        /// <returns></returns>
        async Task SignInAnonymouslyAsync()
        {
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                userID = AuthenticationService.Instance.PlayerId;
                userName = "Guest_" + userID;

                SetPlayer(userID);

                Login();

            }
            catch (AuthenticationException ex)
            {
                // Compare error code to AuthenticationErrorCodes
                // Notify the player with the proper error message
                OfflineLogin(false);
            }
            catch (RequestFailedException exception)
            {
                // Compare error code to CommonErrorCodes
                // Notify the player with the proper error message

                OfflineLogin(false);
            }
        }

        /// <summary>
        /// Signs in an anonymous player for PSN.
        /// </summary>
        /// <returns></returns>
        async Task SignInAnonymouslyAsync(string userID)
        {
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                SetPlayer(AuthenticationService.Instance.PlayerId, userID);

                Login();

                Debug.Log("Signed In Anon PS");

            }
            catch (AuthenticationException ex)
            {
                AuthenticationService.Instance.ClearSessionToken();
                // Compare error code to AuthenticationErrorCodes
                // Notify the player with the proper error message
                Debug.Log(ex);

                isSigningIn = false;
            }
            catch (RequestFailedException exception)
            {
                // Compare error code to CommonErrorCodes
                // Notify the player with the proper error message
                Debug.Log(exception);

                isSigningIn = false;
            }
        }

        /// <summary>
        /// Loads player data from cloud or creates a new anonymous player.
        /// </summary>
        /// <param name="id"></param>
        private async void SetPlayer(string id)
        {
            SaveData incomingSample = await RetrieveSpecificData<SaveData>(id);

            if (incomingSample != null)
            {
                LoadPlayerData(incomingSample);
            }
            else
            {
                LoadPlayerData();
            }


        }

        /// <summary>
        /// Loads player data from cloud or creates a new player with login details.
        /// </summary>
        /// <param name="id"></param>
        private async void SetPlayer(string id, string name)
        {
            SaveData incomingSample = await RetrieveSpecificData<SaveData>(id);

            if (incomingSample != null)
                LoadPlayerData(incomingSample);
            else
            {
                LoadPlayerData(id, name);
            }

            player.userName = name;
        }

        /// <summary>
        /// List all the cloud save keys/players.
        /// </summary>
        /// <returns></returns>
        private async Task ListAllKeys()
        {
            try
            {
                var keys = await CloudSaveService.Instance.Data.RetrieveAllKeysAsync();

                Debug.Log($"Keys count: {keys.Count}\n" +
                          $"Keys: {String.Join(", ", keys)}");
            }
            catch (CloudSaveValidationException e)
            {
                Debug.LogError(e);
            }
            catch (CloudSaveException e)
            {
                Debug.LogError(e);
            }
        }

        /// <summary>
        /// Saves a Single Item to cloud.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private async Task ForceSaveSingleData(string key, string value)
        {
            try
            {
                Dictionary<string, object> oneElement = new Dictionary<string, object>();

                // It's a text input field, but let's see if you actually entered a number.
                if (Int32.TryParse(value, out int wholeNumber))
                {
                    oneElement.Add(key, wholeNumber);
                }
                else if (Single.TryParse(value, out float fractionalNumber))
                {
                    oneElement.Add(key, fractionalNumber);
                }
                else
                {
                    oneElement.Add(key, value);
                }

                await CloudSaveService.Instance.Data.ForceSaveAsync(oneElement);

                Debug.Log($"Successfully saved {key}:{value}");
            }
            catch (CloudSaveValidationException e)
            {
                Debug.LogError(e);
            }
            catch (CloudSaveException e)
            {
                Debug.LogError(e);
            }
        }

        /// <summary>
        /// Save an object to cloud.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private async Task ForceSaveObjectData(string key, SaveData value)
        {
            try
            {
                // Although we are only saving a single value here, you can save multiple keys
                // and values in a single batch.
                Dictionary<string, object> oneElement = new Dictionary<string, object>
                {
                    { key, value }
                };

                await CloudSaveService.Instance.Data.ForceSaveAsync(oneElement);

            }
            catch (CloudSaveValidationException e)
            {
                Debug.LogError(e);
            }
            catch (CloudSaveException e)
            {
                Debug.LogError(e);
            }
        }

        /// <summary>
        /// Get data from the cloud.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        private async Task<T> RetrieveSpecificData<T>(string key)
        {
            try
            {
                var results = await CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { key });

                if (results.TryGetValue(key, out string value))
                {
                    return JsonUtility.FromJson<T>(value);
                }
                else
                {
                    Debug.Log($"There is no such key as {key}!");
                }
            }
            catch (CloudSaveValidationException e)
            {
                Debug.LogError(e);
            }
            catch (CloudSaveException e)
            {
                Debug.LogError(e);
            }

            return default;
        }

        /// <summary>
        /// Deletes everything and signs out.
        /// </summary>
        /// <returns></returns>
        private async Task DeleteEverythingSignOut()
        {
            try
            {
                // If you wish to load only a subset of keys rather than everything, you
                // can call a method LoadAsync and pass a HashSet of keys into it.
                var results = await CloudSaveService.Instance.Data.LoadAllAsync();

                foreach (var element in results)
                {
                    await ForceDeleteSpecificData(element.Key);
                }

                AuthenticationService.Instance.SignOut();
            }
            catch (CloudSaveValidationException e)
            {
                Debug.LogError(e);
            }
            catch (CloudSaveException e)
            {
                Debug.LogError(e);
            }
        }

        /// <summary>
        /// Deletes a specific key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private async Task ForceDeleteSpecificData(string key)
        {
            try
            {
                await CloudSaveService.Instance.Data.ForceDeleteAsync(key);

            }
            catch (CloudSaveValidationException e)
            {
                Debug.LogError(e);
            }
            catch (CloudSaveException e)
            {
                Debug.LogError(e);
            }
        }

        /// <summary>
        /// Creates new player with login details.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        private void LoadPlayerData(string id, string name)
        {
            player.userID = id;
            player.userName = name;
            player.playerName = "PlayerName";
            player.SetRewardOvertime(DateTime.Now.AddHours(-2).ToString());
            player.coins = 0;
            player.points = 0;
            player.highestWave = 0;
            player.cubesEliminated = 0;
            player.totalPointsEarned = 0;
            player.totalProjectilesFired = 0;
            player.currentBlaster = 0;
            player.currentSkin = 0;
            player.ownedBlasters = new int[9];
            player.ownedSkins = new int[9];
        }

        /// <summary>
        /// Loads the players local data and sets it to the player.
        /// </summary>
        public void LoadPlayerData()
        {
            SaveData data = SaveSystem.LoadPlayer();

            if (data.userID == "")
            {
                player.userID = userID;
            }
            else
            {
                player.userID = data.userID;
            }

            if (data.userName == "")
            {
                player.userName = userName;
            }
            else
            {
                player.userName = data.userName;
            }

            player.playerName = data.playerName;

            if (data.rewardOverTime == null || data.rewardOverTime == "")
                player.SetRewardOvertime(DateTime.Now.AddHours(-2).ToString());
            else
                player.SetRewardOvertime(data.rewardOverTime);

            player.coins = data.coins;
            player.points = data.points;
            player.highestWave = data.highestWave;
            player.cubesEliminated = data.cubesEliminated;
            player.totalPointsEarned = data.totalPointsEarned;
            player.totalProjectilesFired = data.totalProjectilesFired;

            player.currentBlaster = data.currentBlaster;
            if (data.ownedBlasters == null)
            {
                player.ownedBlasters = new int[] { 1, 0, 0, 0, 0, 0, 0, 0, 0 };
            }
            else if (data.ownedBlasters.Length != 9)
            {
                int[] temp = new int[9];
                data.ownedBlasters.CopyTo(temp, 0);
                data.ownedBlasters = temp;
                player.ownedBlasters = data.ownedBlasters;
            }
            else
            {
                player.ownedBlasters = data.ownedBlasters;
            }

            player.currentSkin = data.currentSkin;

            if (data.ownedSkins == null)
            {
                player.ownedSkins = new int[] { 1, 0, 0, 0, 0, 0, 0, 0, 0 };
            }
            else if (data.ownedSkins.Length != 9)
            {
                int[] temp = new int[9];
                data.ownedSkins.CopyTo(temp, 0);
                data.ownedSkins = temp;
                player.ownedSkins = data.ownedSkins;

            }
            else
            {
                player.ownedSkins = data.ownedSkins;
            }
        }

        /// <summary>
        /// Loads the players cloud data and sets it to the player.
        /// </summary>
        /// <param name="data"></param>
        public void LoadPlayerData(SaveData data)
        {
            if(data.userID == "")
            {
                player.userID = userID;
            }
            else
            {
                player.userID = data.userID;
            }

            if(data.userName == "")
            {
                player.userName = userName;
            }
            else
            {
                player.userName = data.userName;
            }
            
            player.playerName = data.playerName;

            if (data.rewardOverTime == null || data.rewardOverTime == "")
                player.SetRewardOvertime(DateTime.Now.AddHours(-2).ToString());
            else
                player.SetRewardOvertime(data.rewardOverTime);

            player.coins = data.coins;
            player.points = data.points;
            player.highestWave = data.highestWave;
            player.cubesEliminated = data.cubesEliminated;
            player.totalPointsEarned = data.totalPointsEarned;
            player.totalProjectilesFired = data.totalProjectilesFired;

            player.currentBlaster = data.currentBlaster;
            if (data.ownedBlasters == null)
            {
                player.ownedBlasters = new int[] { 1, 0, 0, 0, 0, 0, 0, 0, 0 };
            }
            else if (data.ownedBlasters.Length != 9)
            {
                int[] temp = new int[9];
                data.ownedBlasters.CopyTo(temp, 0);
                data.ownedBlasters = temp;
                player.ownedBlasters = data.ownedBlasters;
            }
            else
            {
                player.ownedBlasters = data.ownedBlasters;
            }

            player.currentSkin = data.currentSkin;

            if (data.ownedSkins == null)
            {
                player.ownedSkins = new int[] { 1, 0, 0, 0, 0, 0, 0, 0, 0 };
            }
            else if (data.ownedSkins.Length != 9)
            {
                int[] temp = new int[9];
                data.ownedSkins.CopyTo(temp, 0);
                data.ownedSkins = temp;
                player.ownedSkins = data.ownedSkins;

            }
            else
            {
                player.ownedSkins = data.ownedSkins;
            }
        }

        /// <summary>
        /// Resets player data on logout.
        /// </summary>
        private void ResetPlayer()
        {
            player.userID = "";
            player.userName = "";
            player.playerName = "";
            player.SetRewardOvertime(DateTime.Now.AddHours(-2).ToString());
            player.coins = 0;
            player.points = 0;
            player.highestWave = 0;
            player.cubesEliminated = 0;
            player.totalPointsEarned = 0;
            player.totalProjectilesFired = 0;
            player.currentBlaster = 0;
            player.currentSkin = 0;
            player.ownedBlasters = new int[9];
            player.ownedSkins = new int[9];
        }

#endregion


    }
}

