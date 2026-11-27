using Com.GCTC.ZombCube;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.Services.LevelPlay;

public class RewardAd : MonoBehaviour
{
    [SerializeField] Button _showAdButton;
    string _adUnitId = null; // This will remain null for unsupported platforms
    private DateTime pressedTime, overTime;
    private bool buttonEnabled;
    private bool adLoaded = false;
    public TextMeshProUGUI buttonText;
    public TextMeshProUGUI labelText;
    public GameObject buttonToSelect;

    private LevelPlayRewardedAd rewardedAd;

    void Awake()
    {
        // Get the Ad Unit ID for the current platform:
#if UNITY_IOS
            _adUnitId = "e9mwai9q9pp28zfb";
#elif UNITY_ANDROID
        _adUnitId = "e9mwai9q9pp28zfb";
#endif

        CreateAd();

        // Disable the button until the ad is ready to show:
        buttonEnabled = false;
        _showAdButton.interactable = false;

        overTime = Convert.ToDateTime(Player.Instance.RewardOvertime);

#if (!UNITY_IOS && !UNITY_ANDROID)
        labelText.text = "Free 1,000 Points";
        _showAdButton.onClick.AddListener(GetPoints);
#endif
    }

    private void Update()
    {
#if (UNITY_IOS && UNITY_ANDROID)
        if (overTime <= DateTime.Now && adLoaded && buttonEnabled == false)
        {
            buttonEnabled = true;
            _showAdButton.interactable = buttonEnabled;
        }
#else
        if (overTime <= DateTime.Now && buttonEnabled == false)
        {
            buttonEnabled = true;
            _showAdButton.interactable = buttonEnabled;
        }
#endif
        else if (overTime > DateTime.Now && buttonEnabled == true)
        {
            buttonEnabled = false;
            _showAdButton.interactable = false;
        }

        HandleButtonText();
    }

    private void HandleButtonText()
    {
#if (UNITY_IOS && UNITY_ANDROID)
        if (buttonEnabled == true && buttonText.text != "Watch Ad")
        {
            buttonText.text = "Watch Ad";
        }
#else
        if (buttonEnabled == true && buttonText.text != "Free Points")
        {
            buttonText.text = "Free Points";
        }
#endif
        else if (buttonEnabled == false)
        {
            buttonText.text = $"{(overTime - DateTime.Now).ToString("hh\\:mm\\:ss")}";
        }
    }

    public void CreateAd()
    {
        rewardedAd = new LevelPlayRewardedAd(_adUnitId);
        rewardedAd.OnAdLoaded += OnAdLoaded;
        rewardedAd.OnAdLoadFailed += OnAdLoadFailed;
        rewardedAd.OnAdDisplayed += OnAdDisplayed;
        rewardedAd.OnAdRewarded += OnAdRewarded;
        rewardedAd.OnAdClicked += OnAdClicked;
        rewardedAd.OnAdClosed += OnAdClosed;
        rewardedAd.OnAdInfoChanged += OnAdInfoChanged;
    }

    #region NEW LEVELPLAY
    
    void ShowRewardedAd(string placementName = null)
    {
        if (rewardedAd.IsAdReady() && !LevelPlayRewardedAd.IsPlacementCapped(placementName))
        {
            rewardedAd.ShowAd(placementName);
        }
    }
    
    bool CheckIfRewardedAdIsReady()
    {
        return rewardedAd.IsAdReady();
    }
    
    bool CheckIfPlacementIsCapped(string placementName)
    {
        return LevelPlayRewardedAd.IsPlacementCapped(placementName);
    }
    
    void OnAdLoaded(LevelPlayAdInfo adInfo)
    {
        adLoaded = true;
            // Configure the button to call the ShowAd() method when clicked:
            _showAdButton.onClick.AddListener(ShowAd);
            // Enable the button for users to click:
            if (overTime <= DateTime.Now && adLoaded && buttonEnabled == false)
            {
                buttonEnabled = true;
                _showAdButton.interactable = buttonEnabled;
            }
            else if (buttonEnabled == true)
            {
                buttonEnabled = false;
                _showAdButton.interactable = buttonEnabled;
            }
    }
    
    void OnAdLoadFailed(LevelPlayAdError adError)
    {
        Debug.Log($"Rewarded ad failed to load with ad error {adError}");
    }
   
    void OnAdDisplayed(LevelPlayAdInfo adInfo)
    {
        CustomAnalytics.SendAdStarted();
    }

    void OnAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward adReward)
    {
        Player player = Player.Instance;

        player.SetRewardOvertime(overTime.ToString());
        player.GainPoints();

        try
        {
            SaveSystem.SavePlayer(player);
        }
        catch
        {
            Debug.Log("Failed to save local data.");
        }

        try
        {
            CloudSaveLogin.SaveCloudDataIfSignedIn();
        }
        catch
        {
            Debug.Log("Failed to save cloud data.");
        }
            
            CustomAnalytics.SendAdCompleted();
    }
    
    void OnAdClicked(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"Rewarded ad clicked with ad info {adInfo}");
    }
    
    void OnAdClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"Rewarded ad closed with ad info {adInfo}");
    }
    
    void OnAdInfoChanged(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"Rewarded ad info changed with ad info {adInfo}");
    }

    #endregion

    // Call this public method when you want to get an ad ready to show.
    public void LoadAd()
    {
        // IMPORTANT! Only load content AFTER initialization (in this example, initialization is handled in a different script).
        //Advertisement.Load(_adUnitId, this);
        rewardedAd.LoadAd();
    }

    private void GetPoints()
    {
        // Disable the button:
        buttonEnabled = false;
        _showAdButton.interactable = false;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(buttonToSelect);

        pressedTime = DateTime.Now;
        overTime = pressedTime.AddHours(24);

        switch (CloudSaveLogin.CurrentSSO)
        {
            case CloudSaveLogin.ssoOption.Anonymous:
                PlayerPrefs.SetString("RewardOverTime", overTime.ToString());
                break;
            case CloudSaveLogin.ssoOption.Facebook:
                PlayerPrefs.SetString("RewardOverTimeFB", overTime.ToString());
                break;
            case CloudSaveLogin.ssoOption.Google:
                PlayerPrefs.SetString("RewardOverTimeG", overTime.ToString());
                break;
            case CloudSaveLogin.ssoOption.Apple:
                PlayerPrefs.SetString("RewardOverTimeA", overTime.ToString());
                break;
            default:
                PlayerPrefs.SetString("RewardOverTime", overTime.ToString());
                break;
        }

        Player player = Player.Instance;

        player.SetRewardOvertime(overTime.ToString());
        player.GainPoints();

        try
        {
            SaveSystem.SavePlayer(player);
        }
        catch
        {
            Debug.Log("Failed to save local data.");
        }

        try
        {
            CloudSaveLogin.SaveCloudDataIfSignedIn();
        }
        catch
        {
            Debug.Log("Failed to save cloud data.");
        }
    }

    // Implement a method to execute when the user clicks the button:
    public void ShowAd()
    {
        if(adLoaded == true)
        {
            // Disable the button:
            buttonEnabled = false;
            _showAdButton.interactable = false;
            // Then show the ad:
            //Advertisement.Show(_adUnitId, this);
            ShowRewardedAd("Main_Menu");

            pressedTime = DateTime.Now;
            overTime = pressedTime.AddHours(2);

            switch (CloudSaveLogin.CurrentSSO)
            {
                case CloudSaveLogin.ssoOption.Anonymous:
                    PlayerPrefs.SetString("RewardOverTime", overTime.ToString());
                    break;
                case CloudSaveLogin.ssoOption.Facebook:
                    PlayerPrefs.SetString("RewardOverTimeFB", overTime.ToString());
                    break;
                case CloudSaveLogin.ssoOption.Google:
                    PlayerPrefs.SetString("RewardOverTimeG", overTime.ToString());
                    break;
                case CloudSaveLogin.ssoOption.Apple:
                    PlayerPrefs.SetString("RewardOverTimeA", overTime.ToString());
                    break;
                default:
                    PlayerPrefs.SetString("RewardOverTime", overTime.ToString());
                    break;
            }

        }
        else
        {
            LoadAd();
        }
        
    }
    
    void OnDestroy()
    {
        // Clean up the button listeners:
        _showAdButton.onClick.RemoveAllListeners();
    }
}
