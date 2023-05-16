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
        private NetworkPlayerManager playerManager;
        private AudioSource audioSource;
        private bool isFiring;
        private bool canFire = true;
        private float fireTime = 0f;
        private float fireRate = 0.8f;
        private float launchVelocity = 5000f;
        public Animator anim;
        public GameObject projectile;
        public ParticleSystem muzzle;

        // Start is called before the first frame update
        void Start()
        {
            playerManager = GetComponent<NetworkPlayerManager>();
            audioSource = GetComponent<AudioSource>();
        }

        // Update is called once per frame
        void Update()
        {
            LaunchProjectile();
        }

        public void LaunchProjectile()
        {
            if (photonView.IsMine && playerManager.isInputDisabled == false)
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

                if (isFiring & canFire)
                {
                    audioSource.Play();
                    anim.SetTrigger("IsFiring");
                    muzzle.Play();
                    GameObject clone = PhotonNetwork.Instantiate(projectile.name, firePosition.position, firePosition.rotation);
                    clone.GetComponent<Rigidbody>().AddRelativeForce(new Vector3(0, 0, launchVelocity));
                    fireTime = fireRate;
                    isFiring = false;
                    Player.Instance.totalProjectilesFired++;

                    CheckForTriggerHappyAchievements();
                }
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
        /// Dynamic callback to see if player performed Fire player input action.
        /// </summary>
        /// <param name="context"></param>
        public void OnFire(InputValue value)
        {
            FireInput(value.isPressed);
        }

        public void FireInput(bool newValue)
        {
            isFiring = newValue;
        }
    }

}