using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    public class NetworkSwapManager : SwapManager
    {
        private NetworkShootProjectile blaster;
        private NetworkLaunchGrenade grenade;
        // Start is called before the first frame update
        void Start()
        {
            holdTime = 0;
            currentWeapon = weapons[0];
            blaster = GetComponent<NetworkShootProjectile>();
            grenade = GetComponent<NetworkLaunchGrenade>();
        }

        // Update is called once per frame
        void Update()
        {
            if (grenade.grenadeCount == 0 && grenade.enabled == true)
            {
                SwapToNextWeapon();
            }
        }

        protected override void EnableDisableScriptComp(bool newState)
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
