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

        public async void GrantCoins(int credits)
        {
            player.coins += credits;
            SaveSystem.SavePlayer(player);
            Debug.Log("You received " + credits + " Coins!");

            await CloudSaveSample.CloudSaveSample.Instance.SavePlayerData(SaveSystem.LoadPlayer());
        }

        public void CallIAPAnalyticsEvent()
        {
            CustomAnalytics.SendIAPComplete();
        }

    }

}
