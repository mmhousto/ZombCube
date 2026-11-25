using UnityEngine;

#if (UNITY_IOS || UNITY_ANDROID)

public class InterstitialAd : MonoBehaviour
{
    void Awake()
    {

    }

    void Start()
    {

    }

    // Load content to the Ad Unit:
    public void LoadAd()
    {
    }

    // Show the loaded content in the Ad Unit: 
    public void ShowAd()
    {
    }

}
#else
public class InterstitialAd : MonoBehaviour
{

}

#endif