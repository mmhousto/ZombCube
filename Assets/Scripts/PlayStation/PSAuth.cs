#if !UNITY_PS5 && !UNITY_PS4
using UnityEngine;

public class PSAuth : MonoBehaviour
{}

#endif

#if UNITY_PS5 || UNITY_PS4
using Com.GCTC.ZombCube;
using System.Collections;
using Unity.PSN.PS5.Auth;
using Unity.PSN.PS5.Aysnc;
using UnityEngine;
using UnityEngine.PS5;
#endif


#if UNITY_PS5 || UNITY_PS4
public class PSAuth : MonoBehaviour
{
    public string userID, iDToken, authCode;
    public bool initialized;
    private bool calledSignIn;
    private int tries = 0;
    private void Update()
    {
        if(initialized && calledSignIn == false)
        {
            calledSignIn = true;
            CloudSaveLogin.Instance.SignInPS(userID, iDToken, authCode);
        }
    }

    public void Initialize()
    {
        if (PSGamePad.activeGamePad.loggedInUser.userName.Contains("Guest"))
        {
            CloudSaveLogin.Instance.isSigningIn = false;
            CloudSaveLogin.Instance.SignInAnonymously();
            return;
        }
        GetAuthCode();

    }

    public void SignIn()
    {
        if (PSGamePad.activeGamePad.loggedInUser.userName.Contains("Guest"))
        {
            CloudSaveLogin.Instance.isSigningIn = false;
            CloudSaveLogin.Instance.SignInAnonymously();
            return;
        }
        CloudSaveLogin.Instance.SignInPS(userID, iDToken, authCode);
    }

    private void GetAuthCode()
    {
        try
        { 

            Authentication.GetAuthorizationCodeRequest request = new Authentication.GetAuthorizationCodeRequest()
            {
                UserId = PSGamePad.activeGamePad.loggedInUser.userId,
#if UNITY_PS5
                ClientId = "686986a6-3b34-4a42-89d1-b4ba193bc80f",
#elif UNITY_PS4
                    ClientId = "c5806b90-16f4-4086-9b43-665b69654b05",
#endif
                Scope = "psn:s2s"
            };

            var requestOp = new AsyncRequest<Authentication.GetAuthorizationCodeRequest>(request).ContinueWith((antecedent) =>
            {
                if (PSNManager.CheckAysncRequestOK(antecedent))
                {
                    /*OnScreenLog.Add("GetAuthorizationCodeRequest:");
                    OnScreenLog.Add("  ClientId = " + antecedent.Request.ClientId);
                    OnScreenLog.Add("  Scope = " + antecedent.Request.Scope);
                    OnScreenLog.Add("  AuthCode = " + antecedent.Request.AuthCode);
                    OnScreenLog.Add("  IssuerId = " + antecedent.Request.IssuerId);*/
                    authCode = antecedent.Request.AuthCode;
                    GetIDToken();
                }
            });

            Authentication.Schedule(requestOp);
        }
        catch
        {
            tries++;
            Debug.Log("Failed to Auth, Tries: " + tries);
            if(tries == 5)
                CloudSaveLogin.Instance.SignInAnonymously();
            else
                GetAuthCode();
        }

        
    }

    private void GetIDToken()
    {
        Authentication.GetIdTokenRequest request = new Authentication.GetIdTokenRequest()
        {
            UserId = PSGamePad.activeGamePad.loggedInUser.userId,
#if UNITY_PS5
            ClientId = "686986a6-3b34-4a42-89d1-b4ba193bc80f",
            ClientSecret = "U3ILASyaI0Y3s0l5",
#elif UNITY_PS4
                    ClientId = "c5806b90-16f4-4086-9b43-665b69654b05",
                    ClientSecret = "NhpNTk6BygPyw7hw",
#endif

            Scope = "openid id_token:psn.basic_claims"
        };

        var requestOp = new AsyncRequest<Authentication.GetIdTokenRequest>(request).ContinueWith((antecedent) =>
        {
            if (PSNManager.CheckAysncRequestOK(antecedent))
            {
                /*OnScreenLog.Add("GetIdTokenRequest:");
                OnScreenLog.Add("  ClientId = " + antecedent.Request.ClientId);
                OnScreenLog.Add("  ClientSecret = " + antecedent.Request.ClientSecret);
                OnScreenLog.Add("  Scope = " + antecedent.Request.Scope);
                OnScreenLog.Add("  IdToken = " + antecedent.Request.IdToken);*/
                iDToken = antecedent.Request.IdToken;
                userID = antecedent.Request.UserId.ToString();
                CloudSaveLogin.Instance.userID = userID;
                CloudSaveLogin.Instance.userName = PSUser.GetActiveUserName;
                initialized = true;
                
            }
        });

        Authentication.Schedule(requestOp);

    }

    public void ResetInit()
    {
        initialized = false;
        calledSignIn = false;
    }

}
#endif

