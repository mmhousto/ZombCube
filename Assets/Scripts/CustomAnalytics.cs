using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;

static public class CustomAnalytics
{
    static public void SendGameStart()
    {
        AnalyticsEvent.GameStart();
    }

    static public void SendGameOver()
    {
        AnalyticsEvent.GameOver();
    }

    static public void SendAdStarted()
    {
        AnalyticsEvent.AdStart(false, AdvertisingNetwork.UnityAds);
    }

    static public void SendAdCompleted()
    {
        AnalyticsEvent.AdComplete(false, AdvertisingNetwork.UnityAds);
    }

    static public void SendIAPComplete()
    {
        AnalyticsEvent.IAPTransaction("Coins_10", 0.99f, "com.morganhouston.zombcube.coins_10");
    }

    static public void StoreVisit()
    {
        AnalyticsEvent.ScreenVisit("Store");
    }

    static public void SendPlayerName(Dictionary<string, object> customParameters)
    {
        AnalyticsEvent.Custom("Player Name", customParameters);
    }
}
