using System.Collections.Generic;
using System.Reflection;
using TMPro;
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
        private TextMeshProUGUI steamMessage;
        private float hideMessageAt;

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

        private void OnEnable()
        {
            SteamCoinStore.CoinsGranted += OnSteamCoinsGranted;
        }

        private void OnDisable()
        {
            SteamCoinStore.CoinsGranted -= OnSteamCoinsGranted;
        }

        private void Update()
        {
            if (steamButtons.Count == 0) return;

            // Unity IAP has no store on Steam builds and may grey the buttons out; keep them usable.
            bool usable = !SteamCoinStore.IsBusy;
            foreach (Button b in steamButtons)
                if (b != null && b.interactable != usable) b.interactable = usable;

            if (steamMessage != null && steamMessage.gameObject.activeSelf && Time.unscaledTime > hideMessageAt && !SteamCoinStore.IsBusy)
                steamMessage.gameObject.SetActive(false);
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

        private void BuyOnSteam(string productId)
        {
            if (SteamCoinStore.IsBusy) return;
            ShowMessage("Complete the purchase in the Steam overlay...", 60f);
            SteamCoinStore.Instance.Buy(productId, (result, coins) =>
            {
                switch (result)
                {
                    case SteamCoinStore.Result.Granted:
                        // OnSteamCoinsGranted shows the message.
                        break;
                    case SteamCoinStore.Result.Cancelled:
                        ShowMessage("Purchase cancelled.", 3f);
                        break;
                    default:
                        ShowMessage("The purchase couldn't be completed. You weren't charged.", 5f);
                        break;
                }
            });
        }

        private void OnSteamCoinsGranted(int coins)
        {
            ShowMessage($"Thanks! +{coins} {(coins == 1 ? "Coin" : "Coins")}", 4f);
        }

        private void ShowMessage(string text, float seconds)
        {
            if (steamMessage == null)
            {
                TextMeshProUGUI template = GetComponentInChildren<TextMeshProUGUI>(true);
                if (template == null) return;
                steamMessage = Instantiate(template, transform);
                steamMessage.name = "SteamPurchaseMessage";
                steamMessage.raycastTarget = false;
                steamMessage.alignment = TextAlignmentOptions.Center;
                steamMessage.enableAutoSizing = true;
                steamMessage.fontSizeMin = 18f;
                steamMessage.fontSizeMax = 40f;
                RectTransform rt = steamMessage.rectTransform;
                rt.anchorMin = new Vector2(0.1f, 0f);
                rt.anchorMax = new Vector2(0.9f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 30f);
                rt.sizeDelta = new Vector2(0f, 60f);
                rt.localScale = Vector3.one;
            }
            steamMessage.text = text;
            steamMessage.gameObject.SetActive(true);
            steamMessage.transform.SetAsLastSibling();
            hideMessageAt = Time.unscaledTime + seconds;
        }
    }

}
