using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Com.GCTC.ZombCube
{
    public class NetworkSniper : NetworkFullyAuto
    {

        // Start is called before the first frame update
        void Start()
        {
            if (photonView.IsMine)
            {
                playerManager = GetComponent<NetworkPlayerManager>();
                audioSource = GetComponent<AudioSource>();
                launchVelocity = 15000f;
                launchVector = new Vector3(0, 0, launchVelocity);
                //isFiring = true;
                fireRate = 1f;
                ammoCap = 25;
                clipSize = 5;
                reserveAmmo = 20;
                currentAmmoInClip = clipSize;
            }
        }

        private void OnEnable()
        {
            if (photonView.IsMine)
            {
                fireRate = 1f;
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

                if (audioSource == null) audioSource = GetComponent<AudioSource>();

                audioSource.clip = fireSound;
                audioSource.pitch = audioPitch;
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
                clone.GetComponent<Rigidbody>().AddForce(clone.transform.forward * launchVelocity);

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
