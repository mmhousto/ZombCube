using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Com.MorganHouston.ZombCube
{

    public class PurchaseFulfillment : MonoBehaviour
    {
        public GameObject restoreButton;

        private Player player;

        private void Awake()
        {
#if UNITY_IOS
            restoreButton.SetActive(true);
#else
            restoreButton.SetActive(false);
#endif
            player = GameObject.FindWithTag("PlayerData").GetComponent<Player>();
        }

        public void GrantCoins(int credits)
        {
            player.coins += credits;

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
            Debug.Log("You received " + credits + " Coins!");

        }

        public void CallIAPAnalyticsEvent()
        {
            CustomAnalytics.SendIAPComplete();
        }

    }

}
