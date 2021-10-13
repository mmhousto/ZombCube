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
            if(DataManager.Instance.GetBlaster() == i)
            {
                blasterItems[i].GetComponentInChildren<TextMeshProUGUI>().text = "USING";
            }
            else if(DataManager.Instance.ownedBlasters[i] == 1)
            {
                blasterItems[i].GetComponentInChildren<TextMeshProUGUI>().text = "USE";
            } else if (DataManager.Instance.ownedBlasters[i] == 0)
            {
                blasterItems[i].GetComponentInChildren<TextMeshProUGUI>().text = "BUY";
            }
        }

    }

    void Update()
    {
        for (int i = 0; i < blasterItems.Length; i++)
        {
            if (DataManager.Instance.GetBlaster() == i)
            {
                blasterItems[i].GetComponentInChildren<TextMeshProUGUI>().text = "USING";
            }
            else if (DataManager.Instance.ownedBlasters[i] == 1)
            {
                blasterItems[i].GetComponentInChildren<TextMeshProUGUI>().text = "USE";
            }
            else if (DataManager.Instance.ownedBlasters[i] == 0)
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
            if (price <= DataManager.Instance.GetCoins())
            {
                PlayerPrefs.SetInt("Blaster" + index, 1);
                PlayerPrefs.SetInt("Coins", DataManager.Instance.GetCoins() - price);
            }
        } else
        {
            price = 500;
            if (price <= DataManager.Instance.GetPoints())
            {
                PlayerPrefs.SetInt("Blaster" + index, 1);
                PlayerPrefs.SetInt("Points", DataManager.Instance.GetPoints() - price);
            }
        }
        
    }

    public void Use(int index)
    {
        PlayerPrefs.SetInt("Blaster", index);
    }

    public void SelectBlaster(int index)
    {
        if (DataManager.Instance.ownedBlasters[index] == 1)
        {
            Use(index);
            blasterItems[index].GetComponentInChildren<TextMeshProUGUI>().text = "USING";
        }
        else if (DataManager.Instance.ownedBlasters[index] == 0)
        {
            BuyBlaster(index);
            blasterItems[index].GetComponentInChildren<TextMeshProUGUI>().text = "USE";
        }
    }

}
