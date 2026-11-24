using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace Com.GCTC.ZombCube
{

    public class PurchaseFulfillment : MonoBehaviour
    {
        public GameObject restoreButton;

        private Player player;

        // Steam: the coin pack buttons buy through SteamCoinStore instead of Unity IAP.
        private readonly List<Button> steamButtons = new List<Button>();

        private void Awake()
        {
#if UNITY_IOS
            restoreButton.SetActive(true);
#else
            restoreButton.SetActive(false);
#endif
            player = Player.Instance;
        }

        private void Start()
        {
            if (SteamCoinStore.IsAvailable)
                UseSteamForCoinButtons();
        }

        private void Update()
        {
            if (steamButtons.Count == 0) return;

            // Unity IAP has no store on Steam builds and may grey the buttons out; keep them usable.
            bool usable = !SteamCoinStore.IsBusy;
            foreach (Button b in steamButtons)
                if (b != null && b.interactable != usable) b.interactable = usable;
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

        // Every Unity IAP coin button (CodelessIAPButton) on this panel buys its product through Steam instead.
        private void UseSteamForCoinButtons()
        {
            SteamCoinStore store = SteamCoinStore.Instance;   // also finishes purchases left from an earlier run
            const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (MonoBehaviour iap in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (iap == null || iap.GetType().Name != "CodelessIAPButton") continue;
                string productId = iap.GetType().GetField("productId", Fields)?.GetValue(iap) as string;
                Button button = iap.GetType().GetField("button", Fields)?.GetValue(iap) as Button;
                if (button == null) button = iap.GetComponent<Button>();
                if (string.IsNullOrEmpty(productId) || button == null) continue;

                iap.enabled = false;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => BuyOnSteam(productId));
                steamButtons.Add(button);
            }
        }

        // Progress, problems and the thank-you message are shown with the ErrorMessage banner.
        private void BuyOnSteam(string productId)
        {
            if (!SteamCoinStore.IsBusy)
                SteamCoinStore.Instance.Buy(productId);
        }
    }

}
