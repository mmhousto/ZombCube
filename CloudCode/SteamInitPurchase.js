/*
 * ZombCube - Steam coin purchase, step 1 of 2 (Unity Cloud Code script "SteamInitPurchase").
 *
 * Starts a Steam microtransaction for a coin pack. Steam then shows the purchase dialog in the
 * player's Steam overlay; the game waits for MicroTxnAuthorizationResponse_t and calls
 * SteamFinalizePurchase.
 *
 * Secrets (Unity Secret Manager, with Cloud Code access allowed):
 *   STEAM_PUBLISHER_KEY  Steamworks Web API publisher key with Microtransaction permissions.
 *   STEAM_LIVE           Optional. "true" to charge real money; anything else (or missing) uses
 *                        the ISteamMicroTxnSandbox interface, so nobody is charged while testing.
 *
 * Prices and coin amounts live only here, so the game can't change what it pays or receives.
 */
const axios = require("axios-1.6");
const { DataApi } = require("@unity-services/cloud-save-1.4");

const APP_ID = "3035180";

// Cloud Code's JavaScript runtime has no URLSearchParams, so form bodies are encoded by hand.
function formBody(fields) {
  return Object.keys(fields)
    .map((k) => encodeURIComponent(k) + "=" + encodeURIComponent(String(fields[k])))
    .join("&");
}

// productId (same IDs as the iOS / Google products) -> Steam item id, coins granted, price in US cents.
// Steam converts USD to the player's wallet currency when the purchase is finalized.
const PRODUCTS = {
  "com.morganhouston.zombcube.coins_1": { itemId: 1, coins: 1, cents: 99, description: "1 Coin" },
  "com.morganhouston.zombcube.coins_5": { itemId: 5, coins: 5, cents: 299, description: "5 Coins" },
  "com.morganhouston.zombcube.coins_10": { itemId: 10, coins: 10, cents: 499, description: "10 Coins" },
  "com.morganhouston.zombcube.coins_25": { itemId: 25, coins: 25, cents: 999, description: "25 Coins" },
  "com.morganhouston.zombcube.coins_100": { itemId: 100, coins: 100, cents: 1999, description: "100 Coins" },
};

async function steamBase(secretManager) {
  let live = false;
  try {
    const flag = await secretManager.getSecret("STEAM_LIVE");
    live = flag && String(flag.value).toLowerCase() === "true";
  } catch (err) {
    live = false; // no STEAM_LIVE secret: stay in the sandbox
  }
  return `https://partner.steam-api.com/${live ? "ISteamMicroTxn" : "ISteamMicroTxnSandbox"}`;
}

module.exports = async ({ params, context, logger, secretManager }) => {
  const product = PRODUCTS[params.productId];
  if (!product) return { ok: false, error: "Unknown product" };
  if (!/^\d{17}$/.test(String(params.steamId || ""))) return { ok: false, error: "Invalid Steam ID" };

  let key;
  try {
    key = (await secretManager.getSecret("STEAM_PUBLISHER_KEY")).value;
  } catch (err) {
    logger.error("STEAM_PUBLISHER_KEY secret is missing or Cloud Code can't read it", { "error.message": err.message });
    return { ok: false, error: "Store is not configured" };
  }

  // 64-bit order id that fits in a JavaScript number: milliseconds * 1000 + random.
  const orderId = String(Date.now() * 1000 + Math.floor(Math.random() * 1000));

  const form = formBody({
    key,
    orderid: orderId,
    steamid: String(params.steamId),
    appid: APP_ID,
    itemcount: "1",
    language: "en",
    currency: "USD",
    usersession: "client",
    "itemid[0]": String(product.itemId),
    "qty[0]": "1",
    "amount[0]": String(product.cents),
    "description[0]": product.description,
    "category[0]": "Coins",
  });

  let response;
  try {
    const res = await axios.post(`${await steamBase(secretManager)}/InitTxn/v3/`, form, {
      headers: { "Content-Type": "application/x-www-form-urlencoded" },
      timeout: 15000,
    });
    response = res.data && res.data.response;
  } catch (err) {
    logger.error("InitTxn request failed", { "error.message": err.message });
    return { ok: false, error: "Couldn't reach Steam" };
  }

  if (!response || response.result !== "OK") {
    const error = response && response.error ? response.error : {};
    logger.warning("InitTxn refused", { errorcode: error.errorcode, errordesc: error.errordesc });
    return { ok: false, error: error.errordesc || "Steam refused the purchase" };
  }

  // Remember the order for this player, so only they can finalize it and coins are granted once.
  const cloudSave = new DataApi(context);
  await cloudSave.setItem(context.projectId, context.playerId, {
    key: `steamOrder_${orderId}`,
    value: { productId: params.productId, coins: product.coins, steamId: String(params.steamId), status: "init", created: new Date().toISOString() },
  });

  return { ok: true, orderId, coins: product.coins };
};

module.exports.params = {
  productId: { type: "String", required: true },
  steamId: { type: "String", required: true },
};
