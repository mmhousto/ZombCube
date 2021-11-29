using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.MorganHouston.ZombCube
{

    public class PurchaseFulfillment : MonoBehaviour
    {
        public GameObject restoreButton;

        public Player player;

        private void Awake()
        {
#if UNITY_IOS
            restoreButton.SetActive(true);
#else
            restoreButton.SetActive(false);
#endif
        }

        public void GrantCoins(int credits)
        {
            player.coins += credits;
            SaveSystem.SavePlayer(player);
            Debug.Log("You received " + credits + " Coins!");
        }

        public void CallIAPAnalyticsEvent()
        {
            CustomAnalytics.SendIAPComplete();
        }
    }

}
