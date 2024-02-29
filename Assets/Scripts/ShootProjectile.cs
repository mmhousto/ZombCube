using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Com.GCTC.ZombCube
{

    public class ShootProjectile : MonoBehaviour, ISwappable
    {

        #region Variables


        public Transform firePosition;
        public GameObject projectile;
        public ParticleSystem muzzle;
        public Animator anim;
        public AudioClip fireSound;
        protected AudioSource audioSource;

        [SerializeField]
        protected bool isFiring;
        [SerializeField]
        protected bool canFire = true;
        protected float fireTime = 0f;
        public float fireRate = 0.8f;
        public float launchVelocity = 5000f;
        public float audioPitch = 1f;
        protected Vector3 launchVector;


        #endregion


        #region Monobehavior Methods


        // Start is called before the first frame update
        void Start()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            // Assignes launchVector
            launchVector = new Vector3(0, 0, launchVelocity);
        }

        // Update is called once per frame
        void Update()
        {
            CheckCanFire();

            CheckForFiring();
        }

        /// <summary>
        /// Draws line to show where player is aiming in editor.
        /// </summary>
        public virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(firePosition.position, firePosition.forward * launchVelocity);
        }

        private void OnEnable()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();

            audioSource.clip = fireSound;
            audioSource.pitch = audioPitch;
        }

        #endregion


        #region Private Methods

        /// <summary>
        /// Checks to see if the fire time is less than or qual to zero. If so the player can fire, if it is greater the player cannot fire.
        /// </summary>
        protected void CheckCanFire()
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
        
        /// <summary>
        /// Checks to see if the player is firing and can fire, if so launches a projectile and resets fireTime.
        /// </summary>
        protected virtual void CheckForFiring()
        {
            if (isFiring & canFire)
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
        /// Launches one projectile forward from player based on launchVector.
        /// </summary>
        public virtual void LaunchProjectile()
        {
            audioSource.Play();
            anim.SetTrigger("IsFiring");
            muzzle.Play();
            GameObject clone = Instantiate(projectile, firePosition.position, firePosition.rotation);
            clone.GetComponent<Rigidbody>().AddForce(clone.transform.forward * launchVelocity);

            if (Player.Instance != null)
            {
                Player.Instance.totalProjectilesFired++;
                CheckForTriggerHappyAchievements();
            }
                

            
        }

        #endregion


        #region Public Methods

        public void SetFireSound()
        {
            audioSource.clip = fireSound;
        }

        /// <summary>
        /// Dynamic callback to see if player performed Fire player input action.
        /// </summary>
        /// <param name="context"></param>
        public void Fire(InputAction.CallbackContext context)
        {
            FireInput(context.ReadValueAsButton());
        }

        /// <summary>
        /// Dynamic callback to see if player performed Fire player input action.
        /// </summary>
        /// <param name="context"></param>
        public void OnFire(InputValue context)
        {
            FireInput(context.isPressed);
        }

        public virtual void FireInput(bool newValue)
        {
            isFiring = newValue;
        }

        public void SwapTo()
        {
            this.enabled = true;
        }

        public void SwapOut()
        {
            this.enabled = false;
        }

        #endregion

    }

}
