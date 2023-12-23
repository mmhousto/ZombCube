using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Com.GCTC.ZombCube
{
    public class LaunchGrenade : ShootProjectile
    {
        public int grenadeCount = 1;

        // Start is called before the first frame update
        void Start()
        {
            audioSource = GetComponent<AudioSource>();
            // Assignes launchVector
            launchVector = new Vector3(0, 5, launchVelocity);
        }

        // Update is called once per frame
        void Update()
        {
            CheckCanFire();

            CheckForFiring();
        }

        /// <summary>
        /// Checks to see if the player is firing and can fire, if so launches a projectile and resets fireTime.
        /// </summary>
        protected override void CheckForFiring()
        {
            if (isFiring & canFire)
            {
                LaunchProjectile();
                fireTime = fireRate;
            }
        }

        /// <summary>
        /// Launches one projectile forward from player based on launchVector.
        /// </summary>
        public override void LaunchProjectile()
        {
            audioSource.Play();
            GameObject clone = Instantiate(projectile, firePosition.position, firePosition.rotation);
            clone.GetComponent<Rigidbody>().AddForce(launchVector, ForceMode.Impulse);

            if (Player.Instance != null)
            {
                Player.Instance.totalProjectilesFired++;
                CheckForTriggerHappyAchievements();
            }



        }
    }
}
