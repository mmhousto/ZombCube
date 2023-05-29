using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

namespace Com.GCTC.ZombCube
{
    public class NetworkTripleShot : NetworkShootProjectile
    {
        [SerializeField]
        private float offset = 15f;
        private NetworkShootProjectile shootProjectile;

        // Start is called before the first frame update
        void Start()
        {
            if (photonView.IsMine)
            {
                playerManager = GetComponent<NetworkPlayerManager>();
                audioSource = GetComponent<AudioSource>();
                fireRate = 0.8f;
                launchVector = new Vector3(0, 0, launchVelocity);
            }
        }

        private void OnEnable()
        {
            if (photonView.IsMine)
            {
                if (shootProjectile == null)
                    shootProjectile = GetComponent<NetworkShootProjectile>();
                
                if(shootProjectile != null)
                    shootProjectile.enabled = false;

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
                
                StartCoroutine(EndPowerup());
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
                shootProjectile.enabled = true;
            }
        }

        private void Update()
        {
            CheckCanFire();

            CheckForFiring();
        }

        public override void LaunchProjectile()
        {
            if (photonView.IsMine && playerManager.isInputDisabled == false)
            {
                audioSource.Play();
                anim.SetTrigger("IsFiring");
                muzzle.Play();
                GameObject clone = PhotonNetwork.Instantiate(projectile.name, firePosition.position, Quaternion.AngleAxis(offset, Vector3.up) * firePosition.rotation);
                GameObject clone2 = PhotonNetwork.Instantiate(projectile.name, firePosition.position, Quaternion.AngleAxis(-offset, Vector3.up) * firePosition.rotation);
                GameObject clone3 = PhotonNetwork.Instantiate(projectile.name, firePosition.position, firePosition.rotation);

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
        }

        /// <summary>
        /// Dynamic callback to see if player performed Fire player input action.
        /// </summary>
        /// <param name="context"></param>
        private void OnFired(InputAction.CallbackContext context)
        {
            isFiring = context.ReadValueAsButton();
        }

        IEnumerator EndPowerup()
        {
            yield return new WaitForSeconds(25f);
            this.enabled = false;
        }
    }
}
