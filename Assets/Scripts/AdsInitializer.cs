using UnityEngine;

#if UNITY_IOS || UNITY_ANDROID
using UnityEngine.Advertisements;
using UnityEngine.Purchasing;

public class AdsInitializer : MonoBehaviour, IUnityAdsInitializationListener, IUnityAdsListener
{
    [SerializeField] string _androidGameId = "4415503";
    [SerializeField] string _iOsGameId = "4415502";
    [SerializeField] bool _testMode = false;
    [SerializeField] bool _enablePerPlacementMode = true;
    public static int timesPlayed = 0;
    private string _gameId;

    public InterstitialAd interstitialAd;

    void Awake()
    {
        if(timesPlayed % 3 != 0)
            InitializeAds();
    }

    public void InitializeAds()
    {
        _gameId = (Application.platform == RuntimePlatform.IPhonePlayer)
            ? _iOsGameId
            : _androidGameId;
        Advertisement.Initialize(_gameId, _testMode, _enablePerPlacementMode, this);
    }

    public void OnInitializationComplete()
    {
        Debug.Log("Unity Ads initialization complete.");
        interstitialAd.LoadAd();
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.Log($"Unity Ads Initialization Failed: {error.ToString()} - {message}");
    }

    public void OnUnityAdsReady(string placementId)
    {
        throw new System.NotImplementedException();
    }

    public void OnUnityAdsDidError(string message)
    {
        throw new System.NotImplementedException();
    }

    public void OnUnityAdsDidStart(string placementId)
    {
        CustomAnalytics.SendAdStarted();
    }

    public void OnUnityAdsDidFinish(string placementId, ShowResult showResult)
    {
        CustomAnalytics.SendAdCompleted();
    }

}
#else

public class AdsInitializer : MonoBehaviour
{

    public static int timesPlayed = 0;

    private void Awake()
    {
        Destroy(this.gameObject);
    }
}

#endif
