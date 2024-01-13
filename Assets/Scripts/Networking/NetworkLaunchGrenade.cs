using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Com.GCTC.ZombCube
{
    public class NetworkLaunchGrenade : NetworkShootProjectile
    {
        public int grenadeCount = 1;
        public GameObject grenade;
        public GameObject pin;
        [SerializeField]
        private float launchPower = 0f;
        private const float maxLaunchPower = 4f; // Adjust this value as needed
        [SerializeField]
        private bool pulledPin;

        // Start is called before the first frame update
        void Start()
        {
            if (photonView.IsMine)
            {
                launchVelocity = 15f;
                playerManager = GetComponent<NetworkPlayerManager>();
                audioSource = GetComponent<AudioSource>();
                launchVector = new Vector3(0, 5, launchVelocity);
            }
        }

        private void OnEnable()
        {
            if (photonView.IsMine)
            {
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
        }

        /// <summary>
        /// Checks to see if the player is firing and can fire, if so launches a projectile and resets fireTime.
        /// </summary>
        protected override void CheckForFiring()
        {
            if (canFire && grenadeCount > 0 && this.enabled == true)
            {
                LaunchProjectile();
                fireTime = fireRate;
            }
        }

        IEnumerator ReEnableGernade()
        {
            while (grenade.activeInHierarchy == false)
            {
                yield return new WaitForSeconds(1);
                if(this.enabled == true)
                {
                    grenade.SetActive(true);
                    pin.SetActive(true);
                }
                
            }

        }

        /// <summary>
        /// Launches one projectile forward from player based on launchVector.
        /// </summary>
        public override void LaunchProjectile()
        {
            //audioSource.Play();
            grenade.SetActive(false);
            GameObject clone = PhotonNetwork.Instantiate(projectile.name, firePosition.position, firePosition.rotation);
            clone.GetComponent<Rigidbody>().AddForce(firePosition.forward * (launchVelocity + launchPower * 5), ForceMode.Impulse);
            clone.GetComponent<Grenade>().timeTicked = launchPower;
            grenadeCount--;
            /*if (Player.Instance != null)
            {
                Player.Instance.totalProjectilesFired++;
                CheckForTriggerHappyAchievements();
            }*/

            if (grenadeCount > 0)
            {
                StartCoroutine(ReEnableGernade());
            }
            else
            {
                // switch weapons
            }

        }

        /// <summary>
        /// Dynamic callback to see if player performed Fire player input action.
        /// </summary>
        /// <param name="context"></param>
        private void OnFired(InputAction.CallbackContext context)
        {
            FireInput(context.ReadValueAsButton());
        }

        public override void FireInput(bool newValue)
        {
            if (grenadeCount > 0)
            {

                isFiring = newValue;

                if (isFiring && pulledPin == false)
                {
                    pin.SetActive(false);
                    pulledPin = true;
                    StartCoroutine(ChargeLaunchPower());
                }
            }
        }

        IEnumerator ChargeLaunchPower()
        {
            while (isFiring && launchPower < maxLaunchPower)
            {
                launchPower += Time.deltaTime; // Increase launch power over time
                yield return null;
            }
            CheckForFiring();
            launchPower = 0f; // Reset launch power after launching
            pulledPin = false;
        }
    }
}
