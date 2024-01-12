using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Com.GCTC.ZombCube
{
    public class NetworkSwapManager : SwapManager
    {
        private NetworkShootProjectile blaster;
        private NetworkLaunchGrenade grenade;
        private NetworkTripleShot tripleShot;
        private NetworkFullyAuto fullyAuto;
        // Start is called before the first frame update
        void Start()
        {
            holdTime = 0;
            currentWeapon = weapons[0];
            currentWeaponImages = new Image[4];
            currentWeaponIndexes = new List<int>();
            blaster = GetComponent<NetworkShootProjectile>();
            grenade = GetComponent<NetworkLaunchGrenade>();
            tripleShot = GetComponent<NetworkTripleShot>();
            fullyAuto = GetComponent<NetworkFullyAuto>();

            weaponSelectUI = GameObject.Find("WeaponSelect");

            if (weaponSelectUI != null)
            {
                weaponSelections = weaponSelectUI.GetComponentsInChildren<Button>();
                int i = 0;
                foreach (Button b in weaponSelections)
                {
                    currentWeaponImages[i] = b.transform.GetChild(0).GetComponent<Image>();
                    i++;
#if (UNITY_IOS || UNITY_ANDROID)
                    b.interactable = true;
#endif
                }
                currentWeaponImages[0].sprite = weaponImages[0];
                currentWeaponImages[1].sprite = weaponImages[1];
                currentWeaponImages[2].sprite = null;
                currentWeaponImages[3].sprite = null;
                currentWeaponIndexes.Add(0);
                currentWeaponIndexes.Add(1);
                currentWeaponIndex = currentWeaponIndexes[0];

                weaponSelectUI.SetActive(false);
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (grenade.grenadeCount == 0 && grenade.enabled == true)
            {
                SwapToNextWeapon();
            }
        }

        protected override void SwapToWeapon(int weaponToSwapTo)
        {
            if ((weaponToSwapTo == 1 && grenade.grenadeCount <= 0) || (currentWeapon == weapons[1] && weaponToSwapTo == 1)) return; // Dont swap to nades
            if (currentWeapon == weapons[0] && weaponToSwapTo == 0) return; // Dont swap to pistol if has pistol
            currentWeaponIndex = weaponToSwapTo;

            if (currentWeaponImages[weaponToSwapTo].sprite != null)
            {
                Debug.Log("Swapping to weapon: " + weaponToSwapTo);
                // Disable current weapon
                EnableDisableScriptComp(false);
                currentWeapon.SetActive(false);

                int weaponIndex = weaponImages.IndexOf(currentWeaponImages[weaponToSwapTo].sprite);
                // switch to selected weapon
                currentWeapon = weapons[weaponIndex];

                //enable weapon
                currentWeapon.SetActive(true);
                EnableDisableScriptComp(true);
            }

            // Disable UI if enabled
            if (weaponSelectUI.activeInHierarchy)
            {
                isSwapWeaponsHeld = false;
                weaponSelectUI.SetActive(false);
            }
        }

        protected override void EnableDisableScriptComp(bool newState)
        {
            fullyAuto.enabled = false;
            tripleShot.enabled = false;
            switch (weapons.IndexOf(currentWeapon))
            {
                case 0:
                    blaster.enabled = newState;
                    grenade.enabled = false;
                    break;
                case 1:
                    if (newState == true && grenade.grenadeCount > 0 || newState == false)
                    {
                        grenade.enabled = newState;
                        blaster.enabled = false;
                    }
                    else
                        SwapToNextWeapon();
                    break;
                case 2:
                    break;
                default:
                    blaster.enabled = newState;
                    break;
            }
        }
    }
}
