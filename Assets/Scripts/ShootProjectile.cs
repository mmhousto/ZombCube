using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Com.MorganHouston.ZombCube
{

    public class ShootProjectile : MonoBehaviour
    {
        public Transform firePosition;
        private bool isFiring;
        private bool canFire = true;
        private float fireTime = 0f;
        private float fireRate = 1.5f;
        private float launchVelocity = 5000f;

        public GameObject projectile;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
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
                GameObject clone = Instantiate(projectile, firePosition.position, firePosition.rotation);
                clone.GetComponent<Rigidbody>().AddRelativeForce(new Vector3(0, 0, launchVelocity));
                fireTime = fireRate;
            }
        }

        public void Fire(InputAction.CallbackContext context)
        {
            isFiring = context.ReadValueAsButton();
        }
    }

}
