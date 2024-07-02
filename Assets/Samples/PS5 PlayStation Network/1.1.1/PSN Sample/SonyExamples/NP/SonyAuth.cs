
#if UNITY_PS5 || UNITY_PS4
using Unity.PSN.PS5.Auth;
using Unity.PSN.PS5.Aysnc;
using Unity.PSN.PS5.Checks;
#endif


namespace PSNSample
{
#if UNITY_PS5 || UNITY_PS4
    public class SonyAuth
    {
        public string userID, iDToken, authCode;
        public bool initialized;

        public SonyAuth()
        {
            Initialize();
        }

        public void Initialize()
        {
            GetAuthCode();
            GetIDToken();
            initialized = true;
            //CloudSaveLogin.Instance.SignInPS(userID, iDToken, authCode);
        }

        private void GetAuthCode()
        {
            Authentication.GetAuthorizationCodeRequest request = new Authentication.GetAuthorizationCodeRequest()
            {
                UserId = GamePad.activeGamePad.loggedInUser.userId,
#if UNITY_PS5
                ClientId = "686986a6-3b34-4a42-89d1-b4ba193bc80f",
#elif UNITY_PS4
                    ClientId = "c5806b90-16f4-4086-9b43-665b69654b05",
#endif
                Scope = "psn:s2s"
            };

            var requestOp = new AsyncRequest<Authentication.GetAuthorizationCodeRequest>(request).ContinueWith((antecedent) =>
            {
                if (SonyNpMain.CheckAysncRequestOK(antecedent))
                {
                    /*OnScreenLog.Add("GetAuthorizationCodeRequest:");
                    OnScreenLog.Add("  ClientId = " + antecedent.Request.ClientId);
                    OnScreenLog.Add("  Scope = " + antecedent.Request.Scope);
                    OnScreenLog.Add("  AuthCode = " + antecedent.Request.AuthCode);
                    OnScreenLog.Add("  IssuerId = " + antecedent.Request.IssuerId);*/
                    authCode = antecedent.Request.AuthCode;
                }
            });

            Authentication.Schedule(requestOp);
        }

        private void GetIDToken()
        {
            Authentication.GetIdTokenRequest request = new Authentication.GetIdTokenRequest()
            {
                UserId = GamePad.activeGamePad.loggedInUser.userId,
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
                if (SonyNpMain.CheckAysncRequestOK(antecedent))
                {
                    /*OnScreenLog.Add("GetIdTokenRequest:");
                    OnScreenLog.Add("  ClientId = " + antecedent.Request.ClientId);
                    OnScreenLog.Add("  ClientSecret = " + antecedent.Request.ClientSecret);
                    OnScreenLog.Add("  Scope = " + antecedent.Request.Scope);
                    OnScreenLog.Add("  IdToken = " + antecedent.Request.IdToken);*/
                    iDToken = antecedent.Request.IdToken;
                    userID = antecedent.Request.UserId.ToString();
                    
                }
            });

            Authentication.Schedule(requestOp);
        }

    }
#endif
}
