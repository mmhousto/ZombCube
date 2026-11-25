/*
 * ZombCube - Steam coin purchase, step 2 of 2 (Unity Cloud Code script "SteamFinalizePurchase").
 *
 * Called after Steam reports that the player approved the purchase (MicroTxnAuthorizationResponse_t),
 * or on the next launch for an order the game didn't get to finish. Captures the payment with
 * FinalizeTxn and tells the game how many coins to grant. Each order grants coins once.
 *
 * Uses the same secrets as SteamInitPurchase (STEAM_PUBLISHER_KEY, optional STEAM_LIVE).
 *
 * Returns { ok, coins, status }:
 *   ok true,  coins > 0   payment captured now: grant the coins.
 *   ok true,  coins 0     already granted earlier (nothing to do).
 *   ok false, status ...  not paid ("cancelled", "pending", "unknown", "error"): grant nothing.
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

async function steamBase(secretManager) {
  let live = false;
  try {
    const flag = await secretManager.getSecret("STEAM_LIVE");
    live = flag && String(flag.value).toLowerCase() === "true";
  } catch (err) {
    live = false;
  }
  return `https://partner.steam-api.com/${live ? "ISteamMicroTxn" : "ISteamMicroTxnSandbox"}`;
}

async function queryStatus(base, key, orderId) {
  const res = await axios.get(`${base}/QueryTxn/v3/`, { params: { key, appid: APP_ID, orderid: orderId }, timeout: 15000 });
  const response = res.data && res.data.response;
  return response && response.result === "OK" && response.params ? response.params.status : null;
}

module.exports = async ({ params, context, logger, secretManager }) => {
  const orderId = String(params.orderId || "");
  if (!/^\d+$/.test(orderId)) return { ok: false, status: "unknown", coins: 0 };

  // The order must have been started by this player.
  const cloudSave = new DataApi(context);
  const recordKey = `steamOrder_${orderId}`;
  let order;
  try {
    const saved = await cloudSave.getItems(context.projectId, context.playerId, [recordKey]);
    const results = (saved && saved.data && saved.data.results) || [];
    order = results.length > 0 ? results[0].value : null;
  } catch (err) {
    logger.error("Couldn't read the order from Cloud Save", { "error.message": err.message });
    return { ok: false, status: "error", coins: 0 };
  }
  if (!order) return { ok: false, status: "unknown", coins: 0 };
  if (order.status === "granted") return { ok: true, status: "granted", coins: 0 };
  if (order.status === "cancelled") return { ok: false, status: "cancelled", coins: 0 };

  let key;
  try {
    key = (await secretManager.getSecret("STEAM_PUBLISHER_KEY")).value;
  } catch (err) {
    logger.error("STEAM_PUBLISHER_KEY secret is missing or Cloud Code can't read it", { "error.message": err.message });
    return { ok: false, status: "error", coins: 0 };
  }
  const base = await steamBase(secretManager);

  let finalized = false;
  try {
    const form = formBody({ key, orderid: orderId, appid: APP_ID });
    const res = await axios.post(`${base}/FinalizeTxn/v2/`, form, {
      headers: { "Content-Type": "application/x-www-form-urlencoded" },
      timeout: 15000,
    });
    const response = res.data && res.data.response;
    finalized = !!response && response.result === "OK";
    if (!finalized && response && response.error)
      logger.warning("FinalizeTxn refused", { errorcode: response.error.errorcode, errordesc: response.error.errordesc });
  } catch (err) {
    logger.warning("FinalizeTxn request failed; checking the order status", { "error.message": err.message });
  }

  // If FinalizeTxn didn't say OK (e.g. a retry after a lost response), ask Steam what happened.
  if (!finalized) {
    let status = null;
    try {
      status = await queryStatus(base, key, orderId);
    } catch (err) {
      logger.error("QueryTxn request failed", { "error.message": err.message });
      return { ok: false, status: "error", coins: 0 };
    }
    if (status === "Succeeded") {
      finalized = true;
    } else if (status === "Init" || status === "Approved" || status === "InProgress") {
      return { ok: false, status: "pending", coins: 0 };
    } else {
      order.status = "cancelled";
      await cloudSave.setItem(context.projectId, context.playerId, { key: recordKey, value: order });
      return { ok: false, status: "cancelled", coins: 0 };
    }
  }

  order.status = "granted";
  order.granted = new Date().toISOString();
  await cloudSave.setItem(context.projectId, context.playerId, { key: recordKey, value: order });
  return { ok: true, status: "granted", coins: order.coins };
};

module.exports.params = {
  orderId: { type: "String", required: true },
};
