using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using System.Threading.Tasks;
using UnityEngine.EventSystems;

namespace Com.GCTC.ZombCube
{

    public class InventoryManager : MonoBehaviour, IBuyable, IUseable
    {
        [Tooltip("The cosmetic blaster colors you can buy with coins or points.")]
        public GameObject[] blasterItems;
        [Tooltip("The cosmetic skin colors you can buy with coins or points.")]
        public GameObject[] skinItems;

        public GameObject notEnoughPointsPanel, closeButton, convertConfirm, noButton;

        [Tooltip("The players data object.")]
        private Player player;

        private void Start()
        {
            player = Player.Instance;

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

        public void CanConvert()
        {
            if(player.points >= 10_000)
            {
                convertConfirm.SetActive(true);
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(noButton);
            }
            else
            {
                NotEnough();
            }
        }

        public void ConvertCoin()
        {
            if (player.points >= 10_000)
            {
                player.points -= 10_000;
                player.coins++;
                TrySavePlayerData();
            }
            else
            {
                convertConfirm.SetActive(false);
                NotEnough();
            }
        }

        public void BuyBlaster(int index)
        {
            var price = 0;
            if (index >= 8)
            {
                price = 20;
                if (price <= player.coins)
                {
                    player.ownedBlasters[index] = 1;
                    player.coins -= price;
                }
                else
                {
                    NotEnough();
                }            }
            else if (index >= 5)
            {
                price = 10;
                if (price <= player.coins)
                {
                    player.ownedBlasters[index] = 1;
                    player.coins -= price;
                }
                else
                {
                    NotEnough();
                }            }
            else
            {
                price = 500;
                if (price <= player.points)
                {
                    player.ownedBlasters[index] = 1;
                    player.points -= price;
                }
                else
                {
                    NotEnough();
                }
            }

        }

        public void Use(int index)
        {
            player.currentBlaster = index;
        }

        public void SelectBlaster(int index)
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

            TrySavePlayerData();

        }

        public void BuySkin(int index)
        {
            var price = 0;
            if (index >= 8)
            {
                price = 100;
                if (price <= player.coins)
                {
                    player.ownedSkins[index] = 1;
                    player.coins -= price;
                }
                else
                {
                    NotEnough();
                }            }
            else if (index >= 5)
            {
                price = 50;
                if (price <= player.coins)
                {
                    player.ownedSkins[index] = 1;
                    player.coins -= price;
                }
                else
                {
                    NotEnough();
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
                else
                {
                    NotEnough();
                }
            }

        }

        public void UseSkin(int index)
        {
            player.currentSkin = index;
        }

        public void SelectSkin(int index)
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

            TrySavePlayerData();
        }

        private void TrySavePlayerData()
        {
            try
            {
                SaveSystem.SavePlayer(player);
            }
            catch
            {
                Debug.Log("Failed to save local data.");
            }

            try
            {
                CloudSaveLogin.Instance.SaveCloudData();
            }
            catch
            {
                Debug.Log("Failed to save cloud data.");
            }
        }

        private void NotEnough()
        {
            notEnoughPointsPanel.SetActive(true);
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(closeButton);
        }

    }
}
