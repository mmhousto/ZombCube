using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Com.MorganHouston.ZombCube
{

    public class DoubleShot : ShootProjectile
    {
        private Vector3 offset;


        // Start is called before the first frame update
        void Start()
        {
            fireRate = 1f;
            offset = new Vector3(1f, 0, 0);
            launchVector = new Vector3(0, 0, launchVelocity);
        }

        private void Update()
        {
            CheckCanFire();

            CheckForFiring();
        }

        public override void LaunchProjectile()
        {
            GameObject clone = Instantiate(projectile, firePosition.position + offset, firePosition.rotation);
            GameObject clone2 = Instantiate(projectile, firePosition.position - offset, firePosition.rotation);

            clone.GetComponent<Rigidbody>().AddRelativeForce(launchVector);
            clone2.GetComponent<Rigidbody>().AddRelativeForce(launchVector);
        }

    }
}
