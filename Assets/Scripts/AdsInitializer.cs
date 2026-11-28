using Unity.Services.LevelPlay;
using UnityEngine;

#if UNITY_IOS || UNITY_ANDROID

public class AdsInitializer : MonoBehaviour
{
    public static int timesPlayed = 0;
    public RewardAd rewardAd;



    private static bool initStarted, initialized;

    void Awake()
    {
        // The main menu loads many times; LevelPlay is initialized once per app run.
        if (initialized)
        {
            if (rewardAd != null) rewardAd.LoadAd();
            return;
        }

        LevelPlay.OnInitSuccess += OnInitializationComplete;
        LevelPlay.OnInitFailed += OnInitializationFailed;
        if (!initStarted)
        {
            initStarted = true;
            LevelPlay.Init("29e011b5d");
        }
        //InitializeAds();
    }

    private void OnDestroy()
    {
        LevelPlay.OnInitSuccess -= OnInitializationComplete;
        LevelPlay.OnInitFailed -= OnInitializationFailed;
    }

    public void OnInitializationComplete(LevelPlayConfiguration config)
    {
        initialized = true;
        //interstitialAd.LoadAd();
        if (rewardAd != null) rewardAd.LoadAd();
    }

    public void OnInitializationFailed(LevelPlayInitError error)
    {
        Debug.Log($"Unity Ads Initialization Failed: {error}");
        initStarted = false;   // let the next main menu visit try again
    }

}
#else

public class AdsInitializer : MonoBehaviour
{

    public static int timesPlayed = 0;

}

#endif
