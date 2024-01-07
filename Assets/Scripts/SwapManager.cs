using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace Com.GCTC.ZombCube
{
    public class SwapManager : MonoBehaviour
    {
        public GameObject weaponSelectUI; // Weapon Select UI
        public Button[] weaponSelections; // Weapon Selection UI Buttons
        public Image[] weaponImages; // All weapon images
        public Image[] currentWeapons; // Current Weapons Player Has
        public List<GameObject> weapons = new List<GameObject>(); // Actual Physical Weapon

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
            blaster = GetComponent<ShootProjectile>();
            grenade = GetComponent<LaunchGrenade>();
            tripleShot = GetComponent<TripleShot>();
            fullyAuto = GetComponent<FullyAuto>();
            if(GameManager.mode == 0)
                weaponSelectUI = GameObject.Find("WeaponSelect");
            weaponSelectUI.SetActive(false);
        }

        private void Update()
        {
            if(grenade.grenadeCount == 0 && grenade.enabled == true)
            {
                SwapToNextWeapon();
            }
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
            // Implement your logic to swap to the next weapon in the list
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

        protected virtual void EnableDisableScriptComp(bool newState)
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
                        blaster.enabled = false;
                        grenade.enabled = newState;
                    }
                    else
                        SwapToNextWeapon();
                    break;
                case 2:
                    break;
                default:
                    blaster.enabled = true;
                    grenade.enabled = false;
                    break;
            }
        }
    }
}