using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

// The Cloud Code purchase script must sell the same coin packs as the mobile stores.
public class SteamCoinStoreTests
{
    [Test]
    public void SteamProductsMatchTheIapCatalog()
    {
        string script = File.ReadAllText(Path.Combine(Application.dataPath, "..", "CloudCode", "SteamInitPurchase.js"));
        string catalog = File.ReadAllText(Path.Combine(Application.dataPath, "Resources", "IAPProductCatalog.json"));

        MatchCollection products = Regex.Matches(catalog, "\"id\":\\s*\"(com\\.morganhouston\\.zombcube\\.coins_(\\d+))\"");
        Assert.Greater(products.Count, 0, "No coin products in the IAP catalog");
        foreach (Match product in products)
        {
            string id = product.Groups[1].Value;
            string coins = product.Groups[2].Value;
            Match entry = Regex.Match(script, "\"" + Regex.Escape(id) + "\":\\s*\\{[^}]*coins:\\s*(\\d+)");
            Assert.IsTrue(entry.Success, $"{id} is missing from SteamInitPurchase.js");
            Assert.AreEqual(coins, entry.Groups[1].Value, $"{id} grants a different number of coins on Steam");
        }
    }

    [Test]
    public void SteamDefaultsToTheSandbox()
    {
        string[] scripts = { "SteamInitPurchase.js", "SteamFinalizePurchase.js" };
        foreach (string name in scripts)
        {
            string script = File.ReadAllText(Path.Combine(Application.dataPath, "..", "CloudCode", name));
            StringAssert.Contains("ISteamMicroTxnSandbox", script, name);
            StringAssert.Contains("live = false", script, name + " must fall back to the sandbox without STEAM_LIVE");
            StringAssert.DoesNotContain("console.log(key", script, name);
            // Cloud Code's JavaScript runtime has no URLSearchParams (it failed with "URLSearchParams is not defined").
            StringAssert.DoesNotContain("new URLSearchParams", script, name);
        }
    }
}
