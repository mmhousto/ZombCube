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
        private NetworkFullyAuto fullyAuto; // SMB
        private NetworkAB assaultBlaster; // AB
        private NetworkShotblaster shotblaster; // Shotblaster
        private NetworkSniper sniper; // Sniper
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
                sniper = GetComponent<NetworkSniper>();

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
            if (weaponSelectUI != null && weaponSelectUI.activeInHierarchy == true)
            {
                isSwapWeaponsHeld = false;
                weaponSelectUI.SetActive(false);
            }
        }

        protected override void SwapToWeapon(int weaponToSwapTo)
        {
            if (!photonView.IsMine) { return; }
            if ((weaponToSwapTo == 1 && grenade.grenadeCount <= 0) || (currentWeapon == weapons[1] && weaponToSwapTo == 1)) return; // Dont swap to nades
            //if (currentWeaponIndex == weaponToSwapTo) return; // Dont swap to current weapon
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
                case 0:// Pistol
                    if (tripleShot.enabled == true)
                    {
                        blaster.enabled = false;
                        tripleShot.projectile = blaster.projectile;
                        tripleShot.fireRate = blaster.fireRate;
                        tripleShot.firePosition = blaster.firePosition;
                        tripleShot.fireSound = blaster.fireSound;
                        tripleShot.muzzle = blaster.muzzle;
                        tripleShot.anim = blaster.anim;
                        tripleShot.launchVelocity = 5000;
                        tripleShot.SetFireSound();
                    }
                    else
                    {
                        blaster.enabled = newState;
                    }
                    grenade.enabled = false;
                    break;
                case 1:// Grenade
                    if (newState == true && grenade.grenadeCount > 0) // if has grenades switch
                    {
                        blaster.enabled = false;
                        grenade.enabled = newState;
                        tripleShot.enabled = false;
                    }
                    else if (newState == false) // disable
                    {
                        blaster.enabled = false;
                        grenade.enabled = newState;
                    }
                    else
                        SwapToNextWeapon(); // out of nades
                    break;
                case 2:// SMB
                    if (tripleShot.enabled == true)
                    {
                        fullyAuto.enabled = false;
                        tripleShot.projectile = fullyAuto.projectile;
                        tripleShot.fireRate = fullyAuto.fireRate;
                        tripleShot.firePosition = fullyAuto.firePosition;
                        tripleShot.fireSound = fullyAuto.fireSound;
                        tripleShot.muzzle = fullyAuto.muzzle;
                        tripleShot.anim = fullyAuto.anim;
                        tripleShot.launchVelocity = 5000;
                        tripleShot.SetFireSound();
                    }
                    else if (newState == true || newState == false)
                    {
                        fullyAuto.enabled = newState;
                        blaster.enabled = false;
                        grenade.enabled = false;
                    }
                    break;
                case 3:// AB
                    if (tripleShot.enabled == true)
                    {
                        assaultBlaster.enabled = false;
                        tripleShot.projectile = assaultBlaster.projectile;
                        tripleShot.fireRate = assaultBlaster.fireRate;
                        tripleShot.firePosition = assaultBlaster.firePosition;
                        tripleShot.fireSound = assaultBlaster.fireSound;
                        tripleShot.muzzle = assaultBlaster.muzzle;
                        tripleShot.anim = assaultBlaster.anim;
                        tripleShot.launchVelocity = 10000;
                        tripleShot.SetFireSound();
                    }
                    else if (newState == true || newState == false)
                    {
                        assaultBlaster.enabled = newState;
                        blaster.enabled = false;
                        grenade.enabled = false;
                    }
                    break;
                case 4:// Shotblaster
                    if (tripleShot.enabled == true)
                    {
                        shotblaster.enabled = false;
                        tripleShot.projectile = shotblaster.projectile;
                        tripleShot.fireRate = shotblaster.fireRate;
                        tripleShot.firePosition = shotblaster.firePosition;
                        tripleShot.fireSound = shotblaster.fireSound;
                        tripleShot.muzzle = shotblaster.muzzle;
                        tripleShot.anim = shotblaster.anim;
                        tripleShot.launchVelocity = 5000;
                        tripleShot.SetFireSound();
                    }
                    else if (newState == true || newState == false)
                    {
                        shotblaster.enabled = newState;
                        blaster.enabled = false;
                        grenade.enabled = false;
                    }
                    break;
                case 5:// Sniper
                    if (tripleShot.enabled == true)
                    {
                        sniper.enabled = false;
                        tripleShot.projectile = sniper.projectile;
                        tripleShot.fireRate = sniper.fireRate;
                        tripleShot.firePosition = sniper.firePosition;
                        tripleShot.fireSound = sniper.fireSound;
                        tripleShot.muzzle = sniper.muzzle;
                        tripleShot.anim = sniper.anim;
                        tripleShot.launchVelocity = 15000;
                        tripleShot.SetFireSound();
                    }
                    else if (newState == true || newState == false)
                    {
                        sniper.enabled = newState;
                        blaster.enabled = false;
                        grenade.enabled = false;
                    }
                    break;
                default:
                    blaster.enabled = true;
                    grenade.enabled = false;
                    break;
            }
        }
    }
}
