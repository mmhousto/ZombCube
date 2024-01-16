using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Com.GCTC.ZombCube
{
    public class NetworkFullyAuto : NetworkShootProjectile
    {
        protected int ammoCap = 120;
        protected int clipSize = 30;
        public int reserveAmmo = 30;
        public int currentAmmoInClip = 30;
        protected bool reloading = false;

        // Start is called before the first frame update
        void Start()
        {
            if (photonView.IsMine)
            {
                playerManager = GetComponent<NetworkPlayerManager>();
                audioSource = GetComponent<AudioSource>();
                launchVector = new Vector3(0, 0, launchVelocity);
                //isFiring = true;
                fireRate = 0.2f;
                ammoCap = 120;
                clipSize = 30;
                reserveAmmo = 30;
                currentAmmoInClip = 30;
            }
        }

        private void OnEnable()
        {
            if (photonView.IsMine)
            {
                fireRate = 0.2f;
                // Get the PlayerInput component
                PlayerInput playerInput = GetComponent<PlayerInput>();
                if (playerInput != null)
                {
                    // Find the fire action
                    fireAction = playerInput.actions.FindAction("Fire");
                    if (fireAction != null)
                    {
                        // Enable the fire action and attach the callback
                        fireAction.Enable();
                        fireAction.performed += OnFired;
                        fireAction.canceled += OnFired;
                    }
                    else
                    {
                        Debug.LogError("Fire action not found.");
                    }
                }
                else
                {
                    Debug.LogError("PlayerInput component not found.");
                }
            }
        }

        private void OnDisable()
        {
            if (photonView.IsMine && fireAction != null)
            {
                // Disable the fire action
                fireAction.Disable();
                fireAction.performed -= OnFired;
                fireAction.canceled -= OnFired;
            }
        }

        // Update is called once per frame
        void Update()
        {
            CheckCanFire();
            CheckForFiring();
        }

        public override void LaunchProjectile()
        {
            if (photonView.IsMine && playerManager.isInputDisabled == false && currentAmmoInClip > 0 && reloading == false)
            {
                audioSource.Play();
                anim.SetTrigger("IsFiring");
                muzzle.Play();
                currentAmmoInClip--;

                GameObject clone = PhotonNetwork.Instantiate(projectile.name, firePosition.position, firePosition.rotation);
                clone.GetComponent<Rigidbody>().AddRelativeForce(launchVector);

                if (Player.Instance != null)
                {
                    Player.Instance.totalProjectilesFired++;
                    CheckForTriggerHappyAchievements();
                }

            }
            else ReloadWeapon();

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

        public void ReloadWeapon()
        {
            if (currentAmmoInClip != clipSize && (reserveAmmo > clipSize || (currentAmmoInClip + reserveAmmo) > clipSize) && reloading == false && photonView.IsMine && this.enabled)
            {
                //reload clip
                StartCoroutine(Reload());
                anim.SetTrigger("IsReloading");
                reloading = true;
                reserveAmmo += currentAmmoInClip;
                currentAmmoInClip = clipSize;
                reserveAmmo -= clipSize;

            }
            else if (currentAmmoInClip != clipSize && reserveAmmo > 0 && reloading == false && photonView.IsMine && this.enabled)
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
                // NO AMMO
                anim.SetTrigger("IsOut");
            }
        }

        public virtual void GetAmmo()
        {
            if (currentAmmoInClip == 0 && reserveAmmo == 0)
            {
                // GOT AMMO
                anim.SetTrigger("GotAmmo");
            }

            currentAmmoInClip = clipSize;
            reserveAmmo = 90;
        }
    }
}
