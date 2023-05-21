using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Com.GCTC.ZombCube
{

    public class TripleShot : ShootProjectile
    {
        [SerializeField]
        private float offset = 15f;


        // Start is called before the first frame update
        void Start()
        {
            audioSource = GetComponent<AudioSource>();
            fireRate = 1f;
            launchVector = new Vector3(0, 0, launchVelocity);
        }

        private void Update()
        {
            CheckCanFire();

            CheckForFiring();
        }

        public override void LaunchProjectile()
        {
            audioSource.Play();
            anim.SetTrigger("IsFiring");
            muzzle.Play();
            GameObject clone = Instantiate(projectile, firePosition.position, Quaternion.AngleAxis(offset, Vector3.up) * firePosition.rotation);
            GameObject clone2 = Instantiate(projectile, firePosition.position, Quaternion.AngleAxis(-offset, Vector3.up) * firePosition.rotation);
            GameObject clone3 = Instantiate(projectile, firePosition.position, firePosition.rotation);

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
}
