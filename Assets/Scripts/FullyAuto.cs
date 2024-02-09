using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Com.GCTC.ZombCube
{
    public class FullyAuto : ShootProjectile
    {
        protected int ammoCap = 120;
        protected int clipSize = 30;
        public int reserveAmmo = 30;
        public int currentAmmoInClip = 30;
        protected bool reloading = false;

        // Start is called before the first frame update
        void Start()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
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
            if (audioSource == null) audioSource = GetComponent<AudioSource>();

            audioSource.clip = fireSound;
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
                clone.GetComponent<Rigidbody>().AddForce(clone.transform.forward * launchVelocity);

                if (Player.Instance != null)
                {
                    Player.Instance.totalProjectilesFired++;
                    CheckForTriggerHappyAchievements();
                }
            }
            else
            {
                ReloadWeapon();
            }
        }

        protected IEnumerator Reload()
        {
            yield return new WaitForSeconds(1);
            reloading = false;
        }

        public void OnReload(InputAction.CallbackContext context)
        {
            ReloadInput(context.ReadValueAsButton());
        }

        public void OnReload(InputValue context)
        {
            ReloadInput(context.isPressed);
        }

        public virtual void ReloadInput(bool newValue)
        {
            ReloadWeapon();
        }

        protected virtual void ReloadWeapon()
        {
            if (currentAmmoInClip != clipSize && (reserveAmmo > clipSize || (currentAmmoInClip + reserveAmmo) > clipSize) && reloading == false && this.enabled)
            {
                //reload clip
                StartCoroutine(Reload());
                anim.SetTrigger("IsReloading");
                reloading = true;
                reserveAmmo += currentAmmoInClip;
                currentAmmoInClip = clipSize;
                reserveAmmo -= clipSize;

            }
            else if (currentAmmoInClip != clipSize && reserveAmmo > 0 && reloading == false && this.enabled)
            {
                //reload left
                StartCoroutine(Reload());
                anim.SetTrigger("IsReloading");
                reloading = true;
                reserveAmmo += currentAmmoInClip;
                currentAmmoInClip = reserveAmmo;
                reserveAmmo = 0;
            }
            else if (currentAmmoInClip == 0 && reserveAmmo == 0)
            {
                // No AMMO
                anim.SetTrigger("IsOut");
            }
        }

        public void GetAmmo(int reserve)
        {
            if (currentAmmoInClip == 0 && reserveAmmo == 0)
            {
                // GOT AMMO
                anim.SetTrigger("GotAmmo");
            }

            currentAmmoInClip = clipSize;
            reserveAmmo = reserve;
        }

    }
}
