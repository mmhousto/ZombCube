
#if UNITY_PS5 || UNITY_PS4
using Unity.PSN.PS5.Aysnc;
using Unity.PSN.PS5.Checks;
#endif


namespace Com.GCTC.ZombCube
{
#if UNITY_PS5 || UNITY_PS4
    public static class PSOnlineSafety
    {
        public static bool GetCRStatus()
        {
            OnlineSafety.GetCommunicationRestrictionStatusRequest request = new OnlineSafety.GetCommunicationRestrictionStatusRequest()
            {
                UserId = PSGamePad.activeGamePad.loggedInUser.userId
            };

            bool restricted = false;

            var requestOp = new AsyncRequest<OnlineSafety.GetCommunicationRestrictionStatusRequest>(request).ContinueWith((antecedent) =>
            {
                if (PSNManager.CheckAysncRequestOK(antecedent))
                {
                    OnScreenLog.Add("CR Status = " + antecedent.Request.Status);
                    if(antecedent.Request.Status == OnlineSafety.CRStatus.Restricted)
                    {
                        restricted = true;
                    }
                    else
                    {
                        restricted = false;
                    }
                }
            });

            OnlineSafety.Schedule(requestOp);
            return restricted;
        }

        public static void FilterProfanity(string profanity)
        {
            OnlineSafety.FilterProfanityRequest request = new OnlineSafety.FilterProfanityRequest()
            {
                UserId = PSGamePad.activeGamePad.loggedInUser.userId,
                Locale = "en-US",
                TextToFilter = profanity
            };

            OnScreenLog.Add("Filtering : " + request.TextToFilter);

            var requestOp = new AsyncRequest<OnlineSafety.FilterProfanityRequest>(request).ContinueWith((antecedent) =>
            {
                if (PSNManager.CheckAysncRequestOK(antecedent))
                {
                    OnScreenLog.Add("Filtered Text = " + antecedent.Request.FilteredText);
                }
            });

            OnlineSafety.Schedule(requestOp);
        }

        /*public void MenuOnlineSafety(MenuStack menuStack)
        {
            m_MenuOnlineSafety.Update();

            bool enabled = true;

            if (m_MenuOnlineSafety.AddItem("CR Status", "Get Communication Restriction Status for current user", enabled))
            {
                OnlineSafety.GetCommunicationRestrictionStatusRequest request = new OnlineSafety.GetCommunicationRestrictionStatusRequest()
                {
                    UserId = GamePad.activeGamePad.loggedInUser.userId
                };

                var requestOp = new AsyncRequest<OnlineSafety.GetCommunicationRestrictionStatusRequest>(request).ContinueWith((antecedent) =>
                {
                    if (SonyNpMain.CheckAysncRequestOK(antecedent))
                    {
                        OnScreenLog.Add("CR Status = " + antecedent.Request.Status);
                    }
                });

                OnlineSafety.Schedule(requestOp);
            }

            if (m_MenuOnlineSafety.AddItem("Filter Profanity", "Replace profanity in text with *", enabled))
            {
                OnlineSafety.FilterProfanityRequest request = new OnlineSafety.FilterProfanityRequest()
                {
                    UserId = GamePad.activeGamePad.loggedInUser.userId,
                    Locale = "en-US",
                    TextToFilter = "This is a test of a shit string"
                };

                OnScreenLog.Add("Filtering : " + request.TextToFilter);

                var requestOp = new AsyncRequest<OnlineSafety.FilterProfanityRequest>(request).ContinueWith((antecedent) =>
                {
                    if (SonyNpMain.CheckAysncRequestOK(antecedent))
                    {
                        OnScreenLog.Add("Filtered Text = " + antecedent.Request.FilteredText);
                    }
                });

                OnlineSafety.Schedule(requestOp);
            }

            if (m_MenuOnlineSafety.AddItem("Test Profanity", "Mark profanity in text with []", enabled))
            {
                OnlineSafety.FilterProfanityRequest request = new OnlineSafety.FilterProfanityRequest()
                {
                    UserId = GamePad.activeGamePad.loggedInUser.userId,
                    Locale = "en-US",
                    TextToFilter = "This is a test of a shit string",
                    FilterType = OnlineSafety.ProfanityFilterType.MarkProfanity
                };

                OnScreenLog.Add("Filtering : " + request.TextToFilter);

                var requestOp = new AsyncRequest<OnlineSafety.FilterProfanityRequest>(request).ContinueWith((antecedent) =>
                {
                    if (SonyNpMain.CheckAysncRequestOK(antecedent))
                    {
                        OnScreenLog.Add("Filtered Text = " + antecedent.Request.FilteredText);
                    }
                });

                OnlineSafety.Schedule(requestOp);
            }

            if (m_MenuOnlineSafety.AddBackIndex("Back"))
            {
                menuStack.PopMenu();
            }
        }*/

    }
#endif
}
