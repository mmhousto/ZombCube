using Unity.Services.LevelPlay;
using UnityEngine;

#if UNITY_IOS || UNITY_ANDROID

public class AdsInitializer : MonoBehaviour
{
    public static int timesPlayed = 0;
    public RewardAd rewardAd;



    void Awake()
    {
        /*if(timesPlayed % 3 != 0)*/
        LevelPlay.OnInitSuccess += OnInitializationComplete;
        LevelPlay.OnInitFailed += OnInitializationFailed;
        LevelPlay.Init("29e011b5d");
        //InitializeAds();
    }

    public void OnInitializationComplete(LevelPlayConfiguration config)
    {
        //interstitialAd.LoadAd();
        rewardAd.LoadAd();
    }

    public void OnInitializationFailed(LevelPlayInitError error)
    {
        Debug.Log($"Unity Ads Initialization Failed: {error}");
    }

}
#else

public class AdsInitializer : MonoBehaviour
{

    public static int timesPlayed = 0;

}

#endif
