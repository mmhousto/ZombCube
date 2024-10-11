
#if UNITY_PS5 || UNITY_PS4
using Unity.PSN.PS5.Aysnc;
using Unity.PSN.PS5.Checks;
using Unity.PSN.PS5.Dialogs;
#endif


namespace Com.GCTC.ZombCube
{
#if UNITY_PS5 || UNITY_PS4
    public static class PSOnlineSafety
    {
        public static void GetCRStatus()
        {
            OnlineSafety.GetCommunicationRestrictionStatusRequest request = new OnlineSafety.GetCommunicationRestrictionStatusRequest()
            {
                UserId = PSGamePad.activeGamePad.loggedInUser.userId
            };

            var requestOp = new AsyncRequest<OnlineSafety.GetCommunicationRestrictionStatusRequest>(request).ContinueWith((antecedent) =>
            {
                if (PSNManager.CheckAysncRequestOK(antecedent))
                {
                    OnScreenLog.Add("CR Status = " + antecedent.Request.Status);
                    if(antecedent.Request.Status == OnlineSafety.CRStatus.Restricted)
                    {
                        ShowCRMessage(antecedent.Request.UserId);
                        CloudSaveLogin.Instance.restricted = true;
                    }
                }
            });

            OnlineSafety.Schedule(requestOp);
        }

        public static void ShowCRMessage(int userID)
        {
            MessageDialogSystem.SystemMsgParams msgParams = new MessageDialogSystem.SystemMsgParams()
            {
                MsgType = MessageDialogSystem.SystemMsgParams.SystemMessageTypes.PSNCommunicationRestriction
            };

            MessageDialogSystem.OpenMsgDialogRequest request = new MessageDialogSystem.OpenMsgDialogRequest()
            {
                UserId = userID, //PSGamePad.activeGamePad.loggedInUser.userId,
                Mode = MessageDialogSystem.OpenMsgDialogRequest.MsgModes.SystemMsg,
                SystemMsg = msgParams
            };

            var requestOp = new AsyncRequest<MessageDialogSystem.OpenMsgDialogRequest>(request).ContinueWith((antecedent) =>
            {
                if (PSNManager.CheckAysncRequestOK(antecedent))
                {
                    OnScreenLog.Add("Dialog Status : " + antecedent.Request.Status);
                    OnScreenLog.Add("Button Pressed : " + antecedent.Request.SelectedButton);

                    OnScreenLog.Add("Dialog Closed...");
                }
            });

            OnScreenLog.Add("Opening Dialog ...");

            DialogSystem.Schedule(requestOp);
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
