using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

namespace Com.MorganHouston.ZombCube
{

    public class NetworkShootProjectile : MonoBehaviourPun
    {
        public Transform firePosition;
        private NetworkPlayerManager playerManager;
        private bool isFiring;
        private bool canFire = true;
        private float fireTime = 0f;
        private float fireRate = 0.8f;
        private float launchVelocity = 5000f;

        public GameObject projectile;

        // Start is called before the first frame update
        void Start()
        {
            playerManager = GetComponent<NetworkPlayerManager>();
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
                    GameObject clone = PhotonNetwork.Instantiate(projectile.name, firePosition.position, firePosition.rotation);
                    clone.GetComponent<Rigidbody>().AddRelativeForce(new Vector3(0, 0, launchVelocity));
                    fireTime = fireRate;
                    isFiring = false;
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