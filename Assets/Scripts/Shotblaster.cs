using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Com.GCTC.ZombCube
{
    public class Shotblaster : FullyAuto
    {
        // Start is called before the first frame update
        void Start()
        {
            audioSource = GetComponent<AudioSource>();

            reloading = false;
            //isFiring = true;
            fireRate = 0.4f;
            launchVelocity = 5000f;
            launchVector = new Vector3(0, 0, launchVelocity);
            //shootProjectile.enabled = false;
            ammoCap = 40;
            clipSize = 5;
            reserveAmmo = 15;
            currentAmmoInClip = 5;
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
            if (currentAmmoInClip > 0 && reloading == false)
            {
                audioSource.Play();
                anim.SetTrigger("IsFiring");
                muzzle.Play();
                currentAmmoInClip--;

                GameObject clone = Instantiate(projectile, firePosition.position, firePosition.rotation);
                foreach(Projectile p in clone.GetComponentsInChildren<Projectile>())
                {
                    if (p.name.Contains("Blast")) continue;
                    float x = Random.Range(-6f, 6f);
                    float y = Random.Range(-6f, 6f);
                    p.transform.SetParent(null);
                    p.transform.localRotation *= Quaternion.Euler(new Vector3(x, y, 0));
                    p.GetComponent<Rigidbody>().AddForce(p.transform.forward * launchVelocity);


                }
                float cx = Random.Range(-6f, 6f);
                float cy = Random.Range(-6f, 6f);
                clone.transform.localRotation *= Quaternion.Euler(new Vector3(cx, cy, 0));
                clone.GetComponent<Rigidbody>().AddForce(clone.transform.forward * launchVelocity);

                if (Player.Instance != null)
                {
                    Player.Instance.totalProjectilesFired++;
                    CheckForTriggerHappyAchievements();
                }
            }
            else ReloadWeapon();
        }

        public override void GetAmmo()
        {
            currentAmmoInClip = clipSize;
            reserveAmmo = 35;
        }

    }
}
