using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Linq;

namespace Com.GCTC.ZombCube
{
    public class SwapManager : MonoBehaviour
    {
        public GameObject weaponSelectUI; // Weapon Select UI
        private Button[] weaponSelections; // Weapon Selection UI Buttons
        public List<Sprite> weaponImages; // All weapon images = 0:Pistol, 1:Grenade, 2:SMB, 3:AB, 4:Shotblaster, 5:SB, 6:Sword
        private Image[] currentWeapons; // Current Weapons Player Has
        public List<GameObject> weapons = new List<GameObject>(); // Actual Physical Weapon = 0:Pistol, 1:Grenade, 2:SMB, 3:AB, 4:Shotblaster, 5:SB, 6:Sword

        protected GameObject currentWeapon;
        protected float holdTime;
        protected bool isSwapWeaponsHeld;
        protected bool isSwapping;
        protected bool startedHold;

        private ShootProjectile blaster;
        private TripleShot tripleShot;
        private FullyAuto fullyAuto;
        private LaunchGrenade grenade;

        

        private void Start()
        {
            holdTime = 0;
            currentWeapon = weapons[0];
            currentWeapons = new Image[4];
            blaster = GetComponent<ShootProjectile>();
            grenade = GetComponent<LaunchGrenade>();
            tripleShot = GetComponent<TripleShot>();
            fullyAuto = GetComponent<FullyAuto>();
            if(GameManager.mode == 0)
                weaponSelectUI = GameObject.Find("WeaponSelect");
            if(weaponSelectUI != null )
            {
                weaponSelections = GetComponentsInChildren<Button>();
                int i = 0;
                foreach (Button b in weaponSelections)
                {
                    currentWeapons[i] = b.GetComponentInChildren<Image>();
                    i++;
#if (UNITY_IOS || UNITY_ANDROID)
                    b.interactable = true;
#endif
                }
                currentWeapons[0].sprite = weaponImages[0];
                currentWeapons[1].sprite = weaponImages[1];

                weaponSelectUI.SetActive(false);
            }
            
        }

        private void Update()
        {
            if(grenade.grenadeCount == 0 && grenade.enabled == true)
            {
                SwapToNextWeapon();
            }
        }

        public void OnWeaponUp(InputValue context)
        {
            WeaponUpInput(context.isPressed);
        }

        public void WeaponUpInput(bool newValue)
        {
            SwapToWeapon(0);
        }

        public void OnWeaponRight(InputValue context)
        {
            WeaponRightInput(context.isPressed);
        }

        public void WeaponRightInput(bool newValue)
        {
            SwapToWeapon(1);
        }

        public void OnWeaponDown(InputValue context)
        {
            WeaponDownInput(context.isPressed);
        }

        public void WeaponDownInput(bool newValue)
        {
            SwapToWeapon(2);
        }

        public void OnWeaponLeft(InputValue context)
        {
            WeaponLeftInput(context.isPressed);
        }

        public void WeaponLeftInput(bool newValue)
        {
            SwapToWeapon(3);
        }

        public void OnSwitchWeapons(InputValue context)
        {
            SwapInput(context.isPressed);
        }

        public void SwapInput(bool newValue)
        {
            isSwapping = newValue;

            if (isSwapping && startedHold == false && isSwapWeaponsHeld == false)
            {
                startedHold = true;
                StartCoroutine(ChargeHoldTime());
            }
            else if(isSwapping && isSwapWeaponsHeld == true && weaponSelectUI.activeInHierarchy)
            {
                weaponSelectUI.SetActive(false);
                isSwapWeaponsHeld = false;
            }
        }

        protected IEnumerator ChargeHoldTime()
        {
            while (isSwapping && holdTime < 0.75f)
            {
                holdTime += Time.deltaTime; // Increase launch power over time
                yield return null;
            }

            if (holdTime < 0.75)
            {
                SwapToNextWeapon();
            }
            else
            {
                // menu
                Debug.Log("Holding!");
                isSwapWeaponsHeld = true;
                weaponSelectUI.SetActive(true);
            }

            holdTime = 0f; // Reset launch power after launching
            startedHold = false;
        }

        protected void SwapToNextWeapon()
        {
            Debug.Log("Swapping to the next weapon");
            EnableDisableScriptComp(false);
            currentWeapon.SetActive(false);

            if (weapons.IndexOf(currentWeapon) < weapons.Count - 1)
            {
                currentWeapon = weapons[weapons.IndexOf(currentWeapon) + 1];
            }
            else
            {
                currentWeapon = weapons[0];
            }
            currentWeapon.SetActive(true);
            EnableDisableScriptComp(true);
            isSwapWeaponsHeld = false;

        }

        protected void SwapToWeapon(int weaponToSwapTo)
        {
            if (currentWeapons[weaponToSwapTo] != null)
            {
                Debug.Log("Swapping to weapon: " + weaponToSwapTo);
                // Disable current weapon
                EnableDisableScriptComp(false);
                currentWeapon.SetActive(false);

                int weaponIndex = weaponImages.IndexOf(currentWeapons[weaponToSwapTo].sprite);
                // switch to selected weapon
                currentWeapon = weapons[weaponIndex];

                //enable weapon
                currentWeapon.SetActive(true);
                EnableDisableScriptComp(true);
            }
            

        }

        protected virtual void EnableDisableScriptComp(bool newState)
        {
            fullyAuto.enabled = false;
            tripleShot.enabled = false;

            switch (weapons.IndexOf(currentWeapon))
            {
                case 0:// Pistol
                    blaster.enabled = newState;
                    grenade.enabled = false;
                    break;
                case 1:// Grenade
                    if (newState == true && grenade.grenadeCount > 0 || newState == false) // if has grenades switch, else swap to next weapon
                    {
                        blaster.enabled = false;
                        grenade.enabled = newState;
                    }
                    else
                        SwapToNextWeapon();
                    break;
                case 2:// Weapon Class 1
                    break;
                case 3:// Weapon Class 2
                    break;
                default:
                    blaster.enabled = true;
                    grenade.enabled = false;
                    break;
            }
        }
    }
}