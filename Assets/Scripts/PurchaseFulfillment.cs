using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PurchaseFulfillment : MonoBehaviour
{
    public GameObject restoreButton;

    private void Awake()
    {
        #if UNITY_IOS
            restoreButton.SetActive(true);
        #else
            restoreButton.SetActive(false);
        #endif
    }

    public void GrantCoins(int credits)
    {
        Player.Instance.coins += credits;
        SaveSystem.SavePlayer(Player.Instance);
        Debug.Log("You received " + credits + " Coins!");
    }
}
