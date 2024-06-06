using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

namespace Com.GCTC.ZombCube
{

    public class NetworkShootProjectile : MonoBehaviourPun
    {
        public Transform firePosition;
        protected NetworkPlayerManager playerManager;
        protected AudioSource audioSource;
        [SerializeField]
        protected bool isFiring;
        [SerializeField]
        protected bool canFire = true;
        protected float fireTime = 0f;
        public float fireRate = 0.8f;
        public AudioClip fireSound;
        public float launchVelocity = 5000f;
        public float audioPitch = 1f;
        protected Vector3 launchVector;
        public Animator anim;
        public GameObject projectile;
        public ParticleSystem muzzle;
        protected InputAction fireAction;
        protected BulletPool pool;

        private void Start()
        {
            if (photonView.IsMine)
            {
                playerManager = GetComponent<NetworkPlayerManager>();
                audioSource = GetComponent<AudioSource>();
                launchVector = new Vector3(0, 0, launchVelocity);
                pool = BulletPool.instance;
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

        public virtual void LaunchProjectile()
        {
            if (photonView.IsMine && playerManager.isInputDisabled == false)
            {
                audioSource.Play();
                anim.SetTrigger("IsFiring");
                muzzle.Play();

                var clone = pool.bulletPool.Get(); //Instantiate(projectile, firePosition.position, firePosition.rotation);
                Rigidbody cloneRb = clone.GetComponent<Rigidbody>();
                cloneRb.velocity = Vector3.zero;
                clone.transform.localPosition = Vector3.zero;
                clone.transform.position = firePosition.position;
                clone.transform.rotation = firePosition.rotation;
                cloneRb.AddForce(clone.transform.forward * launchVelocity);

                if (Player.Instance != null)
                {
                    Player.Instance.totalProjectilesFired++;
                    CheckForTriggerHappyAchievements();
                }
            }
        }

        /// <summary>
        /// Checks to see if the fire time is less than or qual to zero. If so the player can fire, if it is greater the player cannot fire.
        /// </summary>
        protected void CheckCanFire()
        {
            if (photonView.IsMine)
            {
                fireTime -= Time.deltaTime;

                if (fireTime <= 0)
                {
                    canFire = true;
                    fireTime = 0;
                }
                else
                {
                    canFire = false;
                }
            }
        }

        /// <summary>
        /// Checks to see if the player is firing and can fire, if so launches a projectile and resets fireTime.
        /// </summary>
        protected virtual void CheckForFiring()
        {
            if (isFiring & canFire && photonView.IsMine)
            {
                LaunchProjectile();
                fireTime = fireRate;
            }
        }

        protected void CheckForTriggerHappyAchievements()
        {
            if (Social.localUser.authenticated)
            {
                if (Player.Instance.totalProjectilesFired >= 100_000)
                {
                    LeaderboardManager.UnlockTriggerHappyI();
                }

                if (Player.Instance.totalProjectilesFired >= 1_000_000)
                {
                    LeaderboardManager.UnlockTriggerHappyII();
                }

                if (Player.Instance.totalProjectilesFired >= 10_000_000)
                {
                    LeaderboardManager.UnlockTriggerHappyIII();
                }
            }
        }

        /// <summary>
        /// Dynamic callback to see if player performed Fire player input action.
        /// </summary>
        /// <param name="context"></param>
        protected void OnFired(InputAction.CallbackContext context)
        {
            FireInput(context.ReadValueAsButton());
        }

        public virtual void FireInput(bool newValue)
        {
            isFiring = newValue;
        }

        public void SetFireSound()
        {
            audioSource.clip = fireSound;
        }
    }

}