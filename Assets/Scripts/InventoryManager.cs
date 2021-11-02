using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InventoryManager : MonoBehaviour, IBuyable, IUseable
{

    public GameObject[] blasterItems;

    private void Start()
    {
        for(int i = 0; i < blasterItems.Length; i++)
        {
            if(Player.Instance.currentBlaster == i)
            {
                blasterItems[i].GetComponentInChildren<TextMeshProUGUI>().text = "USING";
            }
            else if(Player.Instance.ownedBlasters[i] == 1)
            {
                blasterItems[i].GetComponentInChildren<TextMeshProUGUI>().text = "USE";
            } else if (Player.Instance.ownedBlasters[i] == 0)
            {
                blasterItems[i].GetComponentInChildren<TextMeshProUGUI>().text = "BUY";
            }
        }

    }

    void Update()
    {
        for (int i = 0; i < blasterItems.Length; i++)
        {
            if (Player.Instance.currentBlaster == i)
            {
                blasterItems[i].GetComponentInChildren<TextMeshProUGUI>().text = "USING";
            }
            else if (Player.Instance.ownedBlasters[i] == 1)
            {
                blasterItems[i].GetComponentInChildren<TextMeshProUGUI>().text = "USE";
            }
            else if (Player.Instance.ownedBlasters[i] == 0)
            {
                blasterItems[i].GetComponentInChildren<TextMeshProUGUI>().text = "BUY";
            }
        }
    }

    public void BuyBlaster(int index)
    {
        var price = 0;
        if(index >= 5)
        {
            price = 10;
            if (price <= Player.Instance.coins)
            {
                Player.Instance.ownedBlasters[index] = 1;
                Player.Instance.coins -= price;
            }
        } else
        {
            price = 500;
            if (price <= Player.Instance.points)
            {
                Player.Instance.ownedBlasters[index] = 1;
                Player.Instance.points -= price;
            }
        }
        
    }

    public void Use(int index)
    {
        Player.Instance.currentBlaster = index;
    }

    public void SelectBlaster(int index)
    {
        if (Player.Instance.ownedBlasters[index] == 1)
        {
            Use(index);
            blasterItems[index].GetComponentInChildren<TextMeshProUGUI>().text = "USING";
        }
        else if (Player.Instance.ownedBlasters[index] == 0)
        {
            BuyBlaster(index);
            blasterItems[index].GetComponentInChildren<TextMeshProUGUI>().text = "USE";
        }
        SaveSystem.SavePlayer(Player.Instance);
    }

}
