using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PurchaseFulfillment : MonoBehaviour
{

    public void GrantCoins(int credits)
    {
        Player.Instance.coins += credits;
        SaveSystem.SavePlayer(Player.Instance);
        Debug.Log("You received " + credits + " Coins!");
    }
}
