using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Com.GCTC.ZombCube
{

    public class ShootProjectile : MonoBehaviour
    {

        #region Variables


        public Transform firePosition;
        public GameObject projectile;

        [SerializeField]
        protected bool isFiring;
        [SerializeField]
        protected bool canFire = true;
        protected float fireTime = 0f;
        protected float fireRate = 0.8f;
        protected float launchVelocity = 5000f;
        protected Vector3 launchVector;


        #endregion


        #region Monobehavior Methods


        // Start is called before the first frame update
        void Start()
        {
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
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(firePosition.position, firePosition.forward * launchVelocity);
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
        protected void CheckForFiring()
        {
            if (isFiring & canFire)
            {
                LaunchProjectile();
                fireTime = fireRate;
            }
        }

        private void CheckForTriggerHappyAchievements()
        {
            if (Social.localUser.authenticated)
            {
                if (Player.Instance.totalProjectilesFired == 100_000)
                {
                    LeaderboardManager.UnlockTriggerHappyI();
                }

                if (Player.Instance.totalProjectilesFired == 1_000_000)
                {
                    LeaderboardManager.UnlockTriggerHappyII();
                }

                if (Player.Instance.totalProjectilesFired == 10_000_000)
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
            GameObject clone = Instantiate(projectile, firePosition.position, firePosition.rotation);
            clone.GetComponent<Rigidbody>().AddRelativeForce(launchVector);
            Player.Instance.totalProjectilesFired++;

            CheckForTriggerHappyAchievements();
        }

        #endregion


        #region Public Methods

        /// <summary>
        /// Dynamic callback to see if player performed Fire player input action.
        /// </summary>
        /// <param name="context"></param>
        public void Fire(InputAction.CallbackContext context)
        {
            FireInput(context.ReadValueAsButton());
        }

        public void FireInput(bool newValue)
        {
            isFiring = newValue;
        }

        #endregion

    }

}
