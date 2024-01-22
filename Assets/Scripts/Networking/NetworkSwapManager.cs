using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

namespace Com.GCTC.ZombCube
{
    public class NetworkSwapManager : SwapManager
    {
        private NetworkShootProjectile blaster;
        private NetworkLaunchGrenade grenade;
        private NetworkTripleShot tripleShot;
        private NetworkFullyAuto fullyAuto;
        private NetworkAB assaultBlaster; // AB
        private NetworkShotblaster shotblaster; // Shotblaster
        // Start is called before the first frame update
        void Start()
        {
            if (photonView.IsMine)
            {

                holdTime = 0;
                currentWeapon = weapons[0];
                currentWeaponImages = new List<Image>();
                currentWeaponIndexes = new List<int>();
                blaster = GetComponent<NetworkShootProjectile>();
                grenade = GetComponent<NetworkLaunchGrenade>();
                tripleShot = GetComponent<NetworkTripleShot>();
                fullyAuto = GetComponent<NetworkFullyAuto>();
                assaultBlaster = GetComponent<NetworkAB>();
                shotblaster = GetComponent<NetworkShotblaster>();

                weaponSelectUI = GameObject.Find("WeaponSelect");
#if (UNITY_IOS || UNITY_ANDROID)
                touchZone = GameObject.FindWithTag("TouchZone");
#endif

                if (weaponSelectUI != null)
                {
                    weaponSelections = weaponSelectUI.GetComponentsInChildren<Button>();
                    int i = 0;
                    foreach (Button b in weaponSelections)
                    {
                        currentWeaponImages.Add(b.transform.GetChild(0).GetComponent<Image>());
                        
#if (UNITY_IOS || UNITY_ANDROID)
                    b.interactable = true;
                        switch (i)
                        {
                            case 0:
                                b.onClick.AddListener(delegate { WeaponUpInput(true); });
                                break;
                            case 1:
                                b.onClick.AddListener(delegate { WeaponRightInput(true); });
                                break;
                            case 2:
                                b.onClick.AddListener(delegate { WeaponDownInput(true); });
                                break;
                            case 3:
                                b.onClick.AddListener(delegate { WeaponLeftInput(true); });
                                break;
                        }
#endif
                        i++;
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
        }

        // Update is called once per frame
        void Update()
        {
            if (grenade.grenadeCount == 0 && grenade.enabled == true && photonView.IsMine)
            {
                SwapToNextWeapon();
            }

            if((NetworkGameManager.Instance.IsGameOver() == true || NetworkGameManager.Instance.pauseMenu.activeInHierarchy == true || NetworkGameManager.Instance.settingsMenu.activeInHierarchy == true) && photonView.IsMine)
            {
                // Disable UI if enabled
                if (weaponSelectUI.activeInHierarchy == true)
                {
                    isSwapWeaponsHeld = false;
                    weaponSelectUI.SetActive(false);
                }
            }
        }

        private void OnDisable()
        {
            // Disable UI if enabled
            if (weaponSelectUI.activeInHierarchy == true)
            {
                isSwapWeaponsHeld = false;
                weaponSelectUI.SetActive(false);
            }
        }

        protected override void SwapToWeapon(int weaponToSwapTo)
        {
            if (!photonView.IsMine) { return; }
            if ((weaponToSwapTo == 1 && grenade.grenadeCount <= 0) || (currentWeapon == weapons[1] && weaponToSwapTo == 1)) return; // Dont swap to nades
            if (currentWeaponIndex == weaponToSwapTo) return; // Dont swap to current weapon
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
#if (UNITY_IOS || UNITY_ANDROID)
                if (touchZone == null)
                    touchZone = GameObject.FindWithTag("TouchZone");

                if (touchZone!=null)
                    touchZone.SetActive(true);
#endif
            }
        }

        protected override void EnableDisableScriptComp(bool newState)
        {
            if (!photonView.IsMine) { return; }
            
            switch (weapons.IndexOf(currentWeapon))
            {
                case 0:// Blaster
                    blaster.enabled = newState;
                    grenade.enabled = false;
                    break;
                case 1:// Grenade
                    if (newState == true && grenade.grenadeCount > 0)
                    {
                        grenade.enabled = newState;
                        blaster.enabled = false;
                        tripleShot.enabled = false;
                    }
                    else if (newState == false)
                    {
                        blaster.enabled = false;
                        grenade.enabled = newState;
                    }
                    else
                        SwapToNextWeapon();
                    break;
                case 2:// SMB
                    if (newState == true || newState == false)
                    {
                        fullyAuto.enabled = newState;
                        blaster.enabled = false;
                        grenade.enabled = false;
                    }
                    break;
                case 3:// AB
                    if (newState == true || newState == false)
                    {
                        assaultBlaster.enabled = newState;
                        blaster.enabled = false;
                        grenade.enabled = false;
                    }
                    break;
                case 4:// Shotblaster
                    if (newState == true || newState == false)
                    {
                        shotblaster.enabled = newState;
                        blaster.enabled = false;
                        grenade.enabled = false;
                    }
                    break;
                default:
                    blaster.enabled = newState;
                    break;
            }
        }
    }
}
