using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using System.Threading.Tasks;

namespace Com.MorganHouston.ZombCube
{

    public class InventoryManager : MonoBehaviour, IBuyable, IUseable
    {
        [Tooltip("The cosmetic blaster colors you can buy with coins or points.")]
        public GameObject[] blasterItems;
        [Tooltip("The cosmetic skin colors you can buy with coins or points.")]
        public GameObject[] skinItems;

        [Tooltip("The players data object.")]
        private Player player;

        private void Start()
        {
            player = GameObject.FindWithTag("PlayerData").GetComponent<Player>();

            for (int i = 0; i < blasterItems.Length; i++)
            {
                if (player.currentBlaster == i)
                {
                    blasterItems[i].GetComponentInChildren<TextMeshProUGUI>().text = "USING";
                }
                else if (player.ownedBlasters[i] == 1)
                {
                    blasterItems[i].GetComponentInChildren<TextMeshProUGUI>().text = "USE";
                }
                else if (player.ownedBlasters[i] == 0)
                {
                    blasterItems[i].GetComponentInChildren<TextMeshProUGUI>().text = "BUY";
                }
            }

            for (int i = 0; i < skinItems.Length; i++)
            {
                if (player.currentSkin == i)
                {
                    skinItems[i].GetComponentInChildren<TextMeshProUGUI>().text = "USING";
                }
                else if (player.ownedSkins[i] == 1)
                {
                    skinItems[i].GetComponentInChildren<TextMeshProUGUI>().text = "USE";
                }
                else if (player.ownedSkins[i] == 0)
                {
                    skinItems[i].GetComponentInChildren<TextMeshProUGUI>().text = "BUY";
                }
            }

        }

        void Update()
        {
            for (int i = 0; i < blasterItems.Length; i++)
            {
                if (player.currentBlaster == i)
                {
                    blasterItems[i].GetComponentInChildren<TextMeshProUGUI>().text = "USING";
                }
                else if (player.ownedBlasters[i] == 1)
                {
                    blasterItems[i].GetComponentInChildren<TextMeshProUGUI>().text = "USE";
                }
                else if (player.ownedBlasters[i] == 0)
                {
                    blasterItems[i].GetComponentInChildren<TextMeshProUGUI>().text = "BUY";
                }
            }

            for (int i = 0; i < skinItems.Length; i++)
            {
                if (player.currentSkin == i)
                {
                    skinItems[i].GetComponentInChildren<TextMeshProUGUI>().text = "USING";
                }
                else if (player.ownedSkins[i] == 1)
                {
                    skinItems[i].GetComponentInChildren<TextMeshProUGUI>().text = "USE";
                }
                else if (player.ownedSkins[i] == 0)
                {
                    skinItems[i].GetComponentInChildren<TextMeshProUGUI>().text = "BUY";
                }
            }
        }

        public void BuyBlaster(int index)
        {
            var price = 0;
            if (index >= 5)
            {
                price = 10;
                if (price <= player.coins)
                {
                    player.ownedBlasters[index] = 1;
                    player.coins -= price;
                }
            }
            else
            {
                price = 500;
                if (price <= player.points)
                {
                    player.ownedBlasters[index] = 1;
                    player.points -= price;
                }
            }

        }

        public void Use(int index)
        {
            player.currentBlaster = index;
        }

        public async void SelectBlaster(int index)
        {
            if (player.ownedBlasters[index] == 1)
            {
                Use(index);
                blasterItems[index].GetComponentInChildren<TextMeshProUGUI>().text = "USING";
            }
            else if (player.ownedBlasters[index] == 0)
            {
                BuyBlaster(index);
                blasterItems[index].GetComponentInChildren<TextMeshProUGUI>().text = "USE";
            }
            SaveSystem.SavePlayer(player);

            await CloudSaveSample.CloudSaveSample.Instance.SavePlayerData(SaveSystem.LoadPlayer());
        }

        public void BuySkin(int index)
        {
            var price = 0;
            if (index >= 5)
            {
                price = 50;
                if (price <= player.coins)
                {
                    player.ownedSkins[index] = 1;
                    player.coins -= price;
                }
            }
            else
            {
                price = 5000;
                if (price <= player.points)
                {
                    player.ownedSkins[index] = 1;
                    player.points -= price;
                }
            }

        }

        public void UseSkin(int index)
        {
            player.currentSkin = index;
        }

        public async void SelectSkin(int index)
        {
            if (player.ownedSkins[index] == 1)
            {
                UseSkin(index);
                skinItems[index].GetComponentInChildren<TextMeshProUGUI>().text = "USING";
            }
            else if (player.ownedSkins[index] == 0)
            {
                BuySkin(index);
                skinItems[index].GetComponentInChildren<TextMeshProUGUI>().text = "USE";
            }
            SaveSystem.SavePlayer(player);

            await CloudSaveSample.CloudSaveSample.Instance.SavePlayerData(SaveSystem.LoadPlayer());
        }

    }
}
