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
        private NetworkShootProjectile blasterN;
        private NetworkLaunchGrenade grenadeN;
        private NetworkTripleShot tripleShotN;
        private NetworkFullyAuto fullyAutoN; // SMB
        private NetworkAB assaultBlasterN; // AB
        private NetworkShotblaster shotblasterN; // Shotblaster
        private NetworkSniper sniperN; // Sniper
        // Start is called before the first frame update
        void Start()
        {
            if (photonView.IsMine)
            {

                holdTime = 0;
                currentWeapon = weapons[0];
                currentWeaponImages = new List<Image>();
                currentWeaponIndexes = new List<int>();
                blasterN = GetComponent<NetworkShootProjectile>();
                grenadeN = GetComponent<NetworkLaunchGrenade>();
                tripleShotN = GetComponent<NetworkTripleShot>();
                fullyAutoN = GetComponent<NetworkFullyAuto>();
                assaultBlasterN = GetComponent<NetworkAB>();
                shotblasterN = GetComponent<NetworkShotblaster>();
                sniperN = GetComponent<NetworkSniper>();

                weaponSelectUI = GameObject.Find("WeaponSelect").transform.GetChild(0).gameObject;
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
            if (photonView.IsMine == false) return;

            if (grenadeN.grenadeCount == 0 && grenadeN.enabled == true && photonView.IsMine)
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
            if ((weaponToSwapTo == 1 && grenadeN.grenadeCount <= 0) || (currentWeapon == weapons[1] && weaponToSwapTo == 1)) return; // Dont swap to nades
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
                    if (tripleShotN.enabled == true)
                    {
                        blasterN.enabled = false;
                        tripleShotN.projectile = blasterN.projectile;
                        tripleShotN.fireRate = blasterN.fireRate;
                        tripleShotN.firePosition = blasterN.firePosition;
                        tripleShotN.fireSound = blasterN.fireSound;
                        tripleShotN.muzzle = blasterN.muzzle;
                        tripleShotN.anim = blasterN.anim;
                        tripleShotN.launchVelocity = 5000;
                        tripleShotN.SetFireSound();
                    }
                    else
                    {
                        blasterN.enabled = newState;
                    }
                    grenadeN.enabled = false;
                    break;
                case 1:// Grenade
                    if (newState == true && grenadeN.grenadeCount > 0) // if has grenades switch
                    {
                        blasterN.enabled = false;
                        grenadeN.enabled = newState;
                        tripleShotN.enabled = false;
                    }
                    else if (newState == false) // disable
                    {
                        blasterN.enabled = false;
                        grenadeN.enabled = newState;
                    }
                    else
                        SwapToNextWeapon(); // out of nades
                    break;
                case 2:// SMB
                    if (tripleShotN.enabled == true)
                    {
                        fullyAutoN.enabled = false;
                        tripleShotN.projectile = fullyAutoN.projectile;
                        tripleShotN.fireRate = fullyAutoN.fireRate;
                        tripleShotN.firePosition = fullyAutoN.firePosition;
                        tripleShotN.fireSound = fullyAutoN.fireSound;
                        tripleShotN.muzzle = fullyAutoN.muzzle;
                        tripleShotN.anim = fullyAutoN.anim;
                        tripleShotN.launchVelocity = 5000;
                        tripleShotN.SetFireSound();
                    }
                    else if (newState == true || newState == false)
                    {
                        fullyAutoN.enabled = newState;
                        blasterN.enabled = false;
                        grenadeN.enabled = false;
                    }
                    break;
                case 3:// AB
                    if (tripleShotN.enabled == true)
                    {
                        assaultBlasterN.enabled = false;
                        tripleShotN.projectile = assaultBlasterN.projectile;
                        tripleShotN.fireRate = assaultBlasterN.fireRate;
                        tripleShotN.firePosition = assaultBlasterN.firePosition;
                        tripleShotN.fireSound = assaultBlasterN.fireSound;
                        tripleShotN.muzzle = assaultBlasterN.muzzle;
                        tripleShotN.anim = assaultBlasterN.anim;
                        tripleShotN.launchVelocity = 10000;
                        tripleShotN.SetFireSound();
                    }
                    else if (newState == true || newState == false)
                    {
                        assaultBlasterN.enabled = newState;
                        blasterN.enabled = false;
                        grenadeN.enabled = false;
                    }
                    break;
                case 4:// Shotblaster
                    if (tripleShotN.enabled == true)
                    {
                        shotblasterN.enabled = false;
                        tripleShotN.projectile = shotblasterN.projectile;
                        tripleShotN.fireRate = shotblasterN.fireRate;
                        tripleShotN.firePosition = shotblasterN.firePosition;
                        tripleShotN.fireSound = shotblasterN.fireSound;
                        tripleShotN.muzzle = shotblasterN.muzzle;
                        tripleShotN.anim = shotblasterN.anim;
                        tripleShotN.launchVelocity = 5000;
                        tripleShotN.SetFireSound();
                    }
                    else if (newState == true || newState == false)
                    {
                        shotblasterN.enabled = newState;
                        blasterN.enabled = false;
                        grenadeN.enabled = false;
                    }
                    break;
                case 5:// Sniper
                    if (tripleShotN.enabled == true)
                    {
                        sniperN.enabled = false;
                        tripleShotN.projectile = sniperN.projectile;
                        tripleShotN.fireRate = sniperN.fireRate;
                        tripleShotN.firePosition = sniperN.firePosition;
                        tripleShotN.fireSound = sniperN.fireSound;
                        tripleShotN.muzzle = sniperN.muzzle;
                        tripleShotN.anim = sniperN.anim;
                        tripleShotN.launchVelocity = 15000;
                        tripleShotN.SetFireSound();
                    }
                    else if (newState == true || newState == false)
                    {
                        sniperN.enabled = newState;
                        blasterN.enabled = false;
                        grenadeN.enabled = false;
                    }
                    break;
                default:
                    blasterN.enabled = true;
                    grenadeN.enabled = false;
                    break;
            }

        }

        public void CallDisableWeapons()
        {
            photonView.RPC(nameof(DisableNetworkWeapons), RpcTarget.AllBuffered);
        }

        [PunRPC]
        public void DisableNetworkWeapons()
        {
            for (int i = 2; i < weapons.Count; i++)
            {
                weapons[i].SetActive(false);
            }
        }
    }
}
