using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Com.GCTC.ZombCube
{
    public class FullyAuto : ShootProjectile
    {
        int ammoCap = 120;
        int clipSize = 30;
        public int reserveAmmo = 30;
        public int currentAmmoInClip = 30;

        // Start is called before the first frame update
        void Start()
        {
            audioSource = GetComponent<AudioSource>();

            //isFiring = true;
            fireRate = 0.2f;
            launchVector = new Vector3(0, 0, launchVelocity);
            //shootProjectile.enabled = false;
            ammoCap = 120;
            clipSize = 30;
            reserveAmmo = 30;
            currentAmmoInClip = 30;
    }

        private void OnEnable()
        {
            //shootProjectile.enabled = false;
        }

        // Update is called once per frame
        void Update()
        {
            CheckCanFire();

            CheckForFiring();
        }

        public override void LaunchProjectile()
        {
            if(currentAmmoInClip > 0)
            {
                audioSource.Play();
                anim.SetTrigger("IsFiring");
                muzzle.Play();
                GameObject clone = Instantiate(projectile, firePosition.position, firePosition.rotation);
                clone.GetComponent<Rigidbody>().AddRelativeForce(launchVector);

                if (Player.Instance != null)
                {
                    Player.Instance.totalProjectilesFired++;
                    CheckForTriggerHappyAchievements();
                }
            }
            else if (reserveAmmo > clipSize)
            {
                //reload clip
                currentAmmoInClip = clipSize;
                reserveAmmo -= clipSize;
            }else if (reserveAmmo > 0)
            {
                //reload left
                currentAmmoInClip = reserveAmmo;
                reserveAmmo = 0;
            }
            


        }

        public void GetAmmo()
        {
            currentAmmoInClip = clipSize;
            reserveAmmo = 90;
        }

    }
}
