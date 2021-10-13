using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PurchaseFulfillment : MonoBehaviour
{

    public void GrantCoins(int credits)
    {
        PlayerPrefs.SetInt("Coins", DataManager.Instance.GetCoins() + credits);
        Debug.Log("You received " + credits + " Coins!");
    }
}
