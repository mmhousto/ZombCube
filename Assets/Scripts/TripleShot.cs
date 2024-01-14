using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Com.GCTC.ZombCube
{

    public class TripleShot : ShootProjectile
    {
        [SerializeField]
        private float offset = 15f;
        private ShootProjectile blaster;
        private FullyAuto smb; //SMB
        private LaunchGrenade grenade; // Grenade
        private SwapManager swapManager;

        // Start is called before the first frame update
        void Start()
        {
            swapManager = GetComponent<SwapManager>();
            blaster = GetComponent<ShootProjectile>();
            smb = GetComponent<FullyAuto>();
            grenade = GetComponent<LaunchGrenade>();
            audioSource = GetComponent<AudioSource>();
            fireRate = 0.8f;
            launchVector = new Vector3(0, 0, launchVelocity);
        }

        private void OnEnable()
        {
            if (grenade != null && grenade.enabled == true)
            {
                swapManager.SwapToNextWeapon();
            }

            if (blaster != null && blaster.enabled == true)
            {
                blaster.enabled = false;
                fireRate = blaster.fireRate;
            }
            else if (smb != null && smb.enabled == true)
            {
                smb.enabled = false;
                fireRate = smb.fireRate;
            }

            StartCoroutine(EndPowerup());
        }

        private void OnDisable()
        {
            switch (swapManager.GetCurrentWeaponIndex())
            {
                case 0:// Pistol
                    blaster.enabled = true;
                    grenade.enabled = false;
                    break;
                case 1:// Grenade
                    if (grenade.grenadeCount > 0) // if has grenades switch, else swap to next weapon
                    {
                        blaster.enabled = false;
                        grenade.enabled = true;
                    }
                    else
                        swapManager.SwapToNextWeapon();
                    break;
                case 2:// SMB
                    if (smb.currentAmmoInClip > 0 || smb.reserveAmmo > 0)
                    {
                        smb.enabled = true;
                        blaster.enabled = false;
                        grenade.enabled = false;
                    }
                    else
                    {
                        swapManager.SwapToNextWeapon();
                    }

                    break;
                case 3:// Shotblaster
                    break;
                default:
                    blaster.enabled = true;
                    smb.enabled = false;
                    grenade.enabled = false;
                    break;
            }
        }

        private void Update()
        {
            CheckCanFire();

            CheckForFiring();
        }

        public override void LaunchProjectile()
        {
            audioSource.Play();
            anim.SetTrigger("IsFiring");
            muzzle.Play();
            GameObject clone = Instantiate(projectile, firePosition.position, Quaternion.AngleAxis(offset, Vector3.up) * firePosition.rotation);
            GameObject clone2 = Instantiate(projectile, firePosition.position, Quaternion.AngleAxis(-offset, Vector3.up) * firePosition.rotation);
            GameObject clone3 = Instantiate(projectile, firePosition.position, firePosition.rotation);

            clone.GetComponent<Rigidbody>().AddRelativeForce(launchVector);
            clone2.GetComponent<Rigidbody>().AddRelativeForce(launchVector);
            clone3.GetComponent<Rigidbody>().AddRelativeForce(launchVector);

            if (Player.Instance != null)
            {
                Player.Instance.totalProjectilesFired++;
                Player.Instance.totalProjectilesFired++;
                Player.Instance.totalProjectilesFired++;

                CheckForTriggerHappyAchievements();
            }
        }

        IEnumerator EndPowerup()
        {
            yield return new WaitForSeconds(25f);
            this.enabled = false;
        }

    }
}
