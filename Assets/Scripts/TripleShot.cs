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
        private ShootProjectile shootProjectile;

        // Start is called before the first frame update
        void Start()
        {
            shootProjectile = GetComponent<ShootProjectile>();
            audioSource = GetComponent<AudioSource>();
            fireRate = 0.8f;
            launchVector = new Vector3(0, 0, launchVelocity);
        }

        private void OnEnable()
        {
            if(shootProjectile == null)
            {
                shootProjectile = GetComponent<ShootProjectile>();
            }
            shootProjectile.enabled = false;
            StartCoroutine(EndPowerup());
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

        IEnumerator EndPowerup()
        {
            yield return new WaitForSeconds(25f);
            this.enabled = false;
            shootProjectile.enabled = true;
        }

    }
}
