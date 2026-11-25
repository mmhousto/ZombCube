#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
using Unity.Services.CloudCode;
#endif

namespace Com.GCTC.ZombCube
{
    /// <summary>
    /// Buys coin packs through Steam (in-game purchases / microtransactions).
    ///
    /// 1. Cloud Code "SteamInitPurchase" starts the order with Steam (the publisher key stays on the server).
    /// 2. Steam shows the purchase dialog in the overlay and sends MicroTxnAuthorizationResponse_t.
    /// 3. If approved, Cloud Code "SteamFinalizePurchase" captures the payment and returns the coins to grant.
    ///
    /// Orders that were approved but not finished (game closed, connection lost) are saved locally and
    /// finished on the next launch, so a paid purchase is never lost. Each order grants coins only once.
    /// See CloudCode/README.md for the server setup.
    /// </summary>
    public class SteamCoinStore : MonoBehaviour
    {
        public enum Result { Granted, Cancelled, Failed }

        private const string PendingKey = "steam_pending_orders";

        private static SteamCoinStore instance;

        /// <summary>True when the player is signed in with Steam and Steam is running.</summary>
        public static bool IsAvailable
        {
            get
            {
#if !DISABLESTEAMWORKS
                return SteamManager.IsReady && CloudSaveLogin.Instance != null
                    && CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Steam;
#else
                return false;
#endif
            }
        }

        /// <summary>True while a purchase is waiting for Steam or the server.</summary>
        public static bool IsBusy => instance != null && instance.busy;

        /// <summary>Coins granted by a Steam purchase (including ones finished on a later launch).</summary>
        public static event Action<int> CoinsGranted;

        private bool busy;

#if !DISABLESTEAMWORKS
        private Callback<MicroTxnAuthorizationResponse_t> authorizationCallback;
        private TaskCompletionSource<bool> waitingForAuthorization;
        private ulong waitingOrderId;
#endif

        public static SteamCoinStore Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("SteamCoinStore");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<SteamCoinStore>();
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
#if !DISABLESTEAMWORKS
            if (SteamManager.IsReady)
                authorizationCallback = Callback<MicroTxnAuthorizationResponse_t>.Create(OnAuthorization);
#endif
        }

        private void Start()
        {
            FinishPendingOrders();
        }

        /// <summary>
        /// Buys a coin pack by its product ID (the same IDs as the mobile stores). Progress and problems
        /// are shown with the game's ErrorMessage banner. <paramref name="done"/> gets the result and coins.
        /// </summary>
        public async void Buy(string productId, Action<Result, int> done = null)
        {
#if !DISABLESTEAMWORKS
            if (busy) return;
            if (!IsAvailable)
            {
                Notify("Sign in with Steam to buy coins.");
                done?.Invoke(Result.Failed, 0);
                return;
            }
            if (!SteamUtils.IsOverlayEnabled())
            {
                // Steam asks the player to approve in-game purchases in the overlay, so it must be on.
                Notify("Steam purchases need the Steam overlay. Turn on \"Enable the Steam Overlay while in-game\" in Steam Settings > In Game, then restart the game.");
                done?.Invoke(Result.Failed, 0);
                return;
            }

            busy = true;
            try
            {
                if (authorizationCallback == null)
                    authorizationCallback = Callback<MicroTxnAuthorizationResponse_t>.Create(OnAuthorization);

                Notify("Contacting Steam...");
                InitResponse init;
                try
                {
                    init = await CloudCodeService.Instance.CallEndpointAsync<InitResponse>("SteamInitPurchase",
                        new Dictionary<string, object>
                        {
                            { "productId", productId },
                            { "steamId", SteamUser.GetSteamID().m_SteamID.ToString() }
                        });
                }
                catch (Exception e)
                {
                    Debug.LogWarning("SteamInitPurchase failed: " + e);
                    Notify("Store server error: " + Short(e.Message) + " You weren't charged.");
                    done?.Invoke(Result.Failed, 0);
                    return;
                }

                if (init == null || !init.ok || !ulong.TryParse(init.orderId, out ulong orderId))
                {
                    string reason = init == null ? "no response from the store server" : (string.IsNullOrEmpty(init.error) ? "unknown error" : init.error);
                    Debug.LogWarning("Steam purchase couldn't start: " + reason);
                    Notify("Steam purchase failed: " + reason + ". You weren't charged.");
                    done?.Invoke(Result.Failed, 0);
                    return;
                }

                // Steam's overlay now asks the player to approve the purchase.
                Notify("Approve the purchase in the Steam overlay.");
                waitingOrderId = orderId;
                waitingForAuthorization = new TaskCompletionSource<bool>();
                Task<bool> approval = waitingForAuthorization.Task;
                bool answered = await Task.WhenAny(approval, Task.Delay(TimeSpan.FromMinutes(5))) == approval;
                waitingForAuthorization = null;
                if (!answered)
                {
                    // No answer from Steam. Check the order again next launch in case it was approved.
                    AddPending(init.orderId);
                    Notify("Steam didn't answer. If you approved the purchase, your coins will be added next time you start the game.");
                    done?.Invoke(Result.Failed, 0);
                    return;
                }
                if (!approval.Result)
                {
                    Notify("Purchase cancelled. You weren't charged.");
                    done?.Invoke(Result.Cancelled, 0);
                    return;
                }

                AddPending(init.orderId);   // paid for: finish it even if the game closes now
                int coins = await Finalize(init.orderId);
                if (coins > 0)
                    done?.Invoke(Result.Granted, coins);   // Grant() shows the thank-you message
                else
                {
                    Notify(coins < 0
                        ? "Your purchase is still processing. Your coins will be added as soon as Steam confirms it."
                        : "Steam couldn't complete the payment. You weren't charged.");
                    done?.Invoke(Result.Failed, 0);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Steam purchase failed: " + e);
                Notify("Steam purchase failed: " + Short(e.Message));
                done?.Invoke(Result.Failed, 0);
            }
            finally
            {
                busy = false;
            }
#else
            await Task.Yield();
            done?.Invoke(Result.Failed, 0);
#endif
        }

        private static void Notify(string message)
        {
            if (ErrorManager.Instance != null) ErrorManager.Instance.StartErrorMessage(message);
            else Debug.Log(message);
        }

        // Cloud Code exceptions can carry long JSON; keep the banner readable.
        private static string Short(string message)
        {
            if (string.IsNullOrEmpty(message)) return "unknown error.";
            message = System.Text.RegularExpressions.Regex.Replace(message, @"\s+", " ").Trim();
            return message.Length > 140 ? message.Substring(0, 140) + "..." : message;
        }

#if !DISABLESTEAMWORKS
        private void OnAuthorization(MicroTxnAuthorizationResponse_t response)
        {
            if (response.m_unAppID != SteamUtils.GetAppID().m_AppId) return;
            bool approved = response.m_bAuthorized != 0;

            if (waitingForAuthorization != null && response.m_ulOrderID == waitingOrderId)
            {
                waitingForAuthorization.TrySetResult(approved);
                return;
            }

            // Approval for an order this run isn't waiting on (e.g. after a restart): finish it now.
            if (approved)
            {
                AddPending(response.m_ulOrderID.ToString());
                FinishPendingOrders();
            }
        }

        /// <summary>Finalizes an order on the server and grants its coins once. Returns the coins granted.</summary>
        private async Task<int> Finalize(string orderId)
        {
            FinalizeResponse result;
            try
            {
                result = await CloudCodeService.Instance.CallEndpointAsync<FinalizeResponse>("SteamFinalizePurchase",
                    new Dictionary<string, object> { { "orderId", orderId } });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Steam order {orderId} couldn't be finalized yet: {e.Message}");
                return -1;   // keep it pending; it's retried on the next launch
            }

            if (result == null) return -1;
            if (result.ok)
            {
                RemovePending(orderId);
                if (result.coins > 0) Grant(result.coins);
                return result.coins;
            }

            // "pending" (Steam hasn't finished approving) and "error" stay pending for a retry.
            if (result.status == "cancelled" || result.status == "unknown")
            {
                RemovePending(orderId);
                return 0;
            }
            return -1;
        }
#endif

        private void Grant(int coins)
        {
            Player player = Player.Instance;
            if (player == null) return;
            player.coins += coins;
            try { SaveSystem.SavePlayer(player); } catch { Debug.Log("Failed to save local data."); }
            try { CloudSaveLogin.Instance.SaveCloudData(); } catch { Debug.Log("Failed to save cloud data."); }
            CustomAnalytics.SendIAPComplete();
            Notify($"Thanks! +{coins} {(coins == 1 ? "Coin" : "Coins")}");
            CoinsGranted?.Invoke(coins);
        }

        /// <summary>Finishes purchases that were approved but not completed on an earlier run.</summary>
        public async void FinishPendingOrders()
        {
#if !DISABLESTEAMWORKS
            if (!IsAvailable) return;
            foreach (string orderId in LoadPending())
            {
                try { await Finalize(orderId); }
                catch (Exception e) { Debug.LogWarning("Pending Steam order failed: " + e.Message); }
            }
#else
            await Task.Yield();
#endif
        }

        // ------------------------------------------------------------------ pending orders (PlayerPrefs)

        private static List<string> LoadPending()
        {
            string saved = PlayerPrefs.GetString(PendingKey, "");
            var list = new List<string>();
            foreach (string id in saved.Split(','))
                if (!string.IsNullOrEmpty(id) && !list.Contains(id)) list.Add(id);
            return list;
        }

        private static void SavePending(List<string> orders)
        {
            PlayerPrefs.SetString(PendingKey, string.Join(",", orders));
            PlayerPrefs.Save();
        }

        private static void AddPending(string orderId)
        {
            List<string> orders = LoadPending();
            if (!orders.Contains(orderId)) orders.Add(orderId);
            SavePending(orders);
        }

        private static void RemovePending(string orderId)
        {
            List<string> orders = LoadPending();
            if (orders.Remove(orderId)) SavePending(orders);
        }

        // Cloud Code responses (see CloudCode/*.js).
        [Serializable]
        private class InitResponse
        {
            public bool ok;
            public string orderId;
            public int coins;
            public string error;
        }

        [Serializable]
        private class FinalizeResponse
        {
            public bool ok;
            public int coins;
            public string status;
        }
    }
}
