using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Com.GCTC.ZombCube
{
    public class NetworkShotblaster : NetworkFullyAuto
    {
        private ShotblasterReload reload;

        // Start is called before the first frame update
        void Start()
        {
            if (photonView.IsMine)
            {
                reload = GetComponentInChildren<ShotblasterReload>();
                playerManager = GetComponent<NetworkPlayerManager>();
                audioSource = GetComponent<AudioSource>();
                launchVelocity = 5000f;
                launchVector = new Vector3(0, 0, launchVelocity);
                //isFiring = true;
                fireRate = 0.4f;
                ammoCap = 40;
                clipSize = 5;
                reserveAmmo = 15;
                currentAmmoInClip = 5;
            }
        }

        private void OnEnable()
        {
            if (photonView.IsMine)
            {
                fireRate = 0.4f;
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

            if (audioSource == null) audioSource = GetComponent<AudioSource>();

            audioSource.clip = fireSound;
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

                foreach (Projectile p in clone.GetComponentsInChildren<Projectile>())
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

        protected override void ReloadWeapon()
        {
            if (currentAmmoInClip != clipSize && (reserveAmmo > clipSize || (currentAmmoInClip + reserveAmmo) > clipSize) && reloading == false && this.enabled)
            {
                //reload clip
                reload.SetAmmo(currentAmmoInClip);
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
                reload.SetAmmo(currentAmmoInClip);
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
    }
}
