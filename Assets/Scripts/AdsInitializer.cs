using UnityEngine;

#if (UNITY_IOS || UNITY_ANDROID)
using UnityEngine.Advertisements;
using UnityEngine.Purchasing;

public class AdsInitializer : MonoBehaviour, IUnityAdsInitializationListener, IUnityAdsListener
{
    [SerializeField] string _androidGameId;
    [SerializeField] string _iOsGameId;
    [SerializeField] bool _testMode = true;
    [SerializeField] bool _enablePerPlacementMode = true;
    private string _gameId;

    public InterstitialAd interstitialAd;

    void Awake()
    {

        Destroy(this.gameObject);
        return;

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
    private void Awake()
    {
        Destroy(this.gameObject);
    }
}

#endif
