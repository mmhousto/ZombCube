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
        private bool isFiring;
        private bool canFire = true;
        private float fireTime = 0f;
        private float fireRate = 0.8f;
        private float launchVelocity = 5000f;

        public GameObject projectile;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            LaunchProjectile();
        }

        public void LaunchProjectile()
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

                if (isFiring & canFire)
                {
                    GameObject clone = PhotonNetwork.Instantiate(projectile.name, firePosition.position, firePosition.rotation);
                    clone.GetComponent<Rigidbody>().AddRelativeForce(new Vector3(0, 0, launchVelocity));
                    fireTime = fireRate;
                }
            }
        }

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
    }

}