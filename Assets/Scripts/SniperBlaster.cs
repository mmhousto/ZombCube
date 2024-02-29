using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    public class SniperBlaster : FullyAuto
    {
        // Start is called before the first frame update
        void Start()
        {
            audioSource = GetComponent<AudioSource>();

            reloading = false;
            //isFiring = true;
            fireRate = 1.0f;
            launchVelocity = 15000f;
            launchVector = new Vector3(0, 0, launchVelocity);
            //shootProjectile.enabled = false;
            ammoCap = 25;
            clipSize = 5;
            reserveAmmo = 20;
            currentAmmoInClip = 5;
        }

        private void OnEnable()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();

            audioSource.clip = fireSound;
            audioSource.pitch = audioPitch;
        }

        // Update is called once per frame
        void Update()
        {
            CheckCanFire();

            CheckForFiring();
        }

        public override void LaunchProjectile()
        {
            if (currentAmmoInClip > 0 && reloading == false)
            {
                audioSource.Play();
                anim.SetTrigger("IsFiring");
                muzzle.Play();
                currentAmmoInClip--;

                GameObject clone = Instantiate(projectile, firePosition.position, firePosition.rotation);
                clone.GetComponent<Rigidbody>().AddRelativeForce(launchVector);

                if (Player.Instance != null)
                {
                    Player.Instance.totalProjectilesFired++;
                    CheckForTriggerHappyAchievements();
                }
            }
            else ReloadWeapon();
        }

    }
}
