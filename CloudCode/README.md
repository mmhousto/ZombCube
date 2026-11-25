# ZombCube Cloud Code: Steam coin purchases

Steam in-game purchases need a server, because the Steam publisher key must never ship in the game.
These two Unity Cloud Code scripts are that server. The game calls them from `SteamCoinStore.cs`.

| Script | What it does |
| --- | --- |
| `SteamInitPurchase` | Starts a Steam order for a coin pack (`InitTxn`). Steam then shows the purchase dialog in the player's overlay. |
| `SteamFinalizePurchase` | After the player approves, captures the payment (`FinalizeTxn`) and returns how many coins to grant. Each order grants coins once. |

Prices and coin amounts are set in `SteamInitPurchase.js` (`PRODUCTS`), in US cents. Steam converts them to the
player's wallet currency. The product IDs match the iOS/Google coin products in `Assets/Resources/IAPProductCatalog.json`.

## Setup

1. **Steamworks:** in the Steamworks partner site, create a Web API **publisher key** (Users & Permissions > Manage Groups)
   with permission for microtransactions. Microtransactions must be enabled for app 3035180.
2. **Secret Manager** (Unity Dashboard > your project > Secret Manager):
   - Add `STEAM_PUBLISHER_KEY` with the publisher key, and allow **Cloud Code** to access it.
   - Leave `STEAM_LIVE` unset while testing: purchases go to Steam's sandbox (`ISteamMicroTxnSandbox`) and nobody is
     charged. Add `STEAM_LIVE` = `true` (with Cloud Code access) when you're ready to sell for real.
3. **Cloud Code** (Unity Dashboard > Cloud Code > Scripts): create two **JavaScript** scripts named exactly
   `SteamInitPurchase` and `SteamFinalizePurchase`, paste in the matching `.js` file and publish both.
   Add the parameters the scripts declare at the bottom (`module.exports.params`):
   - `SteamInitPurchase`: `productId` (String, required), `steamId` (String, required)
   - `SteamFinalizePurchase`: `orderId` (String, required)
   (Alternatively install the Deployment package and deploy them from the Unity Editor.)
4. Make sure Cloud Save is enabled for the project (the scripts record each order on the player).

## Testing

With `STEAM_LIVE` unset, sign in with Steam in a Windows build, open Store > Coins and buy a pack. The Steam overlay
shows a sandbox purchase dialog; approving it should add the coins. Cancelling grants nothing.

If the game closes after approval but before the coins arrive, the order is finished on the next launch.
