using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Com.GCTC.ZombCube
{
    public class SwapManager : MonoBehaviour
    {
        protected float holdTime;
        protected bool isSwapWeaponsHeld;
        public List<GameObject> weapons = new List<GameObject>();
        protected GameObject currentWeapon;
        private ShootProjectile blaster;
        private LaunchGrenade grenade;
        protected bool isSwapping;
        protected bool startedHold;

        private void Start()
        {
            holdTime = 0;
            currentWeapon = weapons[0];
            blaster = GetComponent<ShootProjectile>();
            grenade = GetComponent<LaunchGrenade>();
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

            if (isSwapping && startedHold == false)
            {
                startedHold = true;
                StartCoroutine(ChargeHoldTime());
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


        }

        protected virtual void EnableDisableScriptComp(bool newState)
        {
            switch (weapons.IndexOf(currentWeapon))
            {
                case 0:
                    blaster.enabled = newState;
                    break;
                case 1:
                    if (newState == true && grenade.grenadeCount > 0 || newState == false)
                        grenade.enabled = newState;
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