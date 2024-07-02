#if UNITY_PS5 || UNITY_PS4
using Com.GCTC.ZombCube;
using Unity.PSN.PS5.Auth;
using Unity.PSN.PS5.Aysnc;
using Unity.PSN.PS5.Checks;
#endif


#if UNITY_PS5 || UNITY_PS4
    public static class PSAuth
    {
        public static string userID, iDToken, authCode;
        public static bool initialized;

        public static void Initialize()
        {
            GetAuthCode();
            GetIDToken();
            initialized = true;
            CloudSaveLogin.Instance.SignInPS(userID, iDToken, authCode);
        }

        private static void GetAuthCode()
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
                }
            });

            Authentication.Schedule(requestOp);
        }

        private static void GetIDToken()
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

                }
            });

            Authentication.Schedule(requestOp);
        }

    }
#endif

