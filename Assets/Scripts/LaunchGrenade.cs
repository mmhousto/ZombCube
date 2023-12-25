using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Com.GCTC.ZombCube
{
    public class LaunchGrenade : ShootProjectile
    {
        public static int grenadeCount = 2;
        public GameObject grenade;
        [SerializeField]
        private float launchPower = 0f;
        private const float maxLaunchPower = 4f; // Adjust this value as needed
        [SerializeField]
        private bool pulledPin;

        // Start is called before the first frame update
        void Start()
        {
            audioSource = GetComponent<AudioSource>();
            // Assignes launchVector
            launchVector = new Vector3(0, 5, launchVelocity);
            grenadeCount = 2;
        }

        // Update is called once per frame
        void Update()
        {
            CheckCanFire();
        }

        /// <summary>
        /// Draws line to show where player is aiming in editor.
        /// </summary>
        public override void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(firePosition.position, firePosition.position + firePosition.forward * (launchVelocity + launchPower));
        }

        /// <summary>
        /// Checks to see if the player is firing and can fire, if so launches a projectile and resets fireTime.
        /// </summary>
        protected override void CheckForFiring()
        {
            if (canFire && grenadeCount > 0 && this.enabled == true)
            {
                LaunchProjectile();
                fireTime = fireRate;
            }
        }

        IEnumerator ReEnableGernade()
        {
            while(grenade.activeInHierarchy == false)
            {
                yield return new WaitForSeconds(1);
                grenade.SetActive(true);
            }
            
        }

        /// <summary>
        /// Launches one projectile forward from player based on launchVector.
        /// </summary>
        public override void LaunchProjectile()
        {
            //audioSource.Play();
            grenade.SetActive(false);
            GameObject clone = Instantiate(projectile, firePosition.position, firePosition.rotation);
            clone.GetComponent<Rigidbody>().AddForce(firePosition.forward * (launchVelocity + launchPower*5), ForceMode.Impulse);
            clone.GetComponent<Grenade>().timeTicked = launchPower;
            grenadeCount--;
            /*if (Player.Instance != null)
            {
                Player.Instance.totalProjectilesFired++;
                CheckForTriggerHappyAchievements();
            }*/

            if(grenadeCount > 0)
            {
                StartCoroutine(ReEnableGernade());
            }
            else
            {
                // switch weapons
            }

        }

        public override void FireInput(bool newValue)
        {
            if (grenadeCount > 0)
            {

                isFiring = newValue;

                if (isFiring && pulledPin == false)
                {
                    pulledPin = true;
                    StartCoroutine(ChargeLaunchPower());
                }
            }
        }

        IEnumerator ChargeLaunchPower()
        {
            while (isFiring && launchPower < maxLaunchPower)
            {
                launchPower += Time.deltaTime; // Increase launch power over time
                yield return null;
            }
            CheckForFiring();
            launchPower = 0f; // Reset launch power after launching
            pulledPin = false;
        }

    }
}
