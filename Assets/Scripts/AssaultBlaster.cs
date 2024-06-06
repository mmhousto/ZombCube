using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Com.GCTC.ZombCube
{
    public class AssaultBlaster : FullyAuto
    {
        // Start is called before the first frame update
        void Start()
        {
            audioSource = GetComponent<AudioSource>();
            pool = BulletPool.instance;
            reloading = false;
            //isFiring = true;
            fireRate = 0.3f;
            launchVelocity = 10000f;
            launchVector = new Vector3(0, 0, launchVelocity);
            //shootProjectile.enabled = false;
            ammoCap = 240;
            clipSize = 30;
            reserveAmmo = 60;
            currentAmmoInClip = 30;
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

                var clone = pool.bulletPool.Get(); //Instantiate(projectile, firePosition.position, firePosition.rotation);
                clone.transform.position = firePosition.position;
                clone.transform.rotation = firePosition.rotation;
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
