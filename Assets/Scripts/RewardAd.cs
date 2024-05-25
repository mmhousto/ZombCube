using Com.GCTC.ZombCube;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.UI;

public class RewardAd : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [SerializeField] Button _showAdButton;
    [SerializeField] string _androidAdUnitId = "Rewarded_Android";
    [SerializeField] string _iOSAdUnitId = "Rewarded_iOS";
    string _adUnitId = null; // This will remain null for unsupported platforms
    private DateTime pressedTime, overTime;
    private bool buttonEnabled;
    private bool adLoaded = false;
    public TextMeshProUGUI buttonText;

    void Awake()
    {
        // Get the Ad Unit ID for the current platform:
#if UNITY_IOS
            _adUnitId = _iOSAdUnitId;
#elif UNITY_ANDROID
        _adUnitId = _androidAdUnitId;
#endif

        // Disable the button until the ad is ready to show:
        _showAdButton.interactable = false;

        overTime = Convert.ToDateTime(Player.Instance.RewardOvertime);
    }

    private void Update()
    {
        if (overTime <= DateTime.Now && adLoaded && buttonEnabled == false)
        {
            buttonEnabled = true;
            _showAdButton.interactable = buttonEnabled;
        }else if (overTime > DateTime.Now && buttonEnabled == true)
        {
            buttonEnabled = false;
            _showAdButton.interactable = false;
        }

        HandleButtonText();
    }

    private void HandleButtonText()
    {
        if (buttonEnabled == true && buttonText.text != "Watch Ad")
        {
            buttonText.text = "Watch Ad";
        }
        else if (buttonEnabled == false)
        {
            buttonText.text = $"{(overTime - DateTime.Now).ToString("hh\\:mm\\:ss")}";
        }
    }

    // Call this public method when you want to get an ad ready to show.
    public void LoadAd()
    {
        // IMPORTANT! Only load content AFTER initialization (in this example, initialization is handled in a different script).
        Advertisement.Load(_adUnitId, this);
    }

    // If the ad successfully loads, add a listener to the button and enable it:
    public void OnUnityAdsAdLoaded(string adUnitId)
    {

        if (adUnitId.Equals(_adUnitId))
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
            Advertisement.Show(_adUnitId, this);

            pressedTime = DateTime.Now;
            overTime = pressedTime.AddHours(2);

            switch (CloudSaveLogin.Instance.currentSSO)
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

    // Implement the Show Listener's OnUnityAdsShowComplete callback method to determine if the user gets a reward:
    public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
    {
        if (adUnitId.Equals(_adUnitId) && showCompletionState.Equals(UnityAdsShowCompletionState.COMPLETED))
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
                CloudSaveLogin.Instance.SaveCloudData();
            }
            catch
            {
                Debug.Log("Failed to save cloud data.");
            }


            // Grant a reward.
        }
    }

    // Implement Load and Show Listener error callbacks:
    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        Debug.Log($"Error loading Ad Unit {adUnitId}: {error.ToString()} - {message}");
        // Use the error details to determine whether to try to load another ad.
    }

    public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
    {
        Debug.Log($"Error showing Ad Unit {adUnitId}: {error.ToString()} - {message}");
        // Use the error details to determine whether to try to load another ad.
    }

    public void OnUnityAdsShowStart(string adUnitId) { }
    public void OnUnityAdsShowClick(string adUnitId) { }

    void OnDestroy()
    {
        // Clean up the button listeners:
        _showAdButton.onClick.RemoveAllListeners();
    }
}
