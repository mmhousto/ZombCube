using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

namespace Com.GCTC.ZombCube
{
    public class NetworkTripleShot : NetworkShootProjectile
    {
        [SerializeField]
        private float offset = 15f;
        private NetworkShootProjectile shootProjectile;

        // Start is called before the first frame update
        void Start()
        {
            if (photonView.IsMine)
            {
                playerManager = GetComponent<NetworkPlayerManager>();
                shootProjectile = GetComponent<NetworkShootProjectile>();
                audioSource = GetComponent<AudioSource>();
                fireRate = 0.8f;
                launchVector = new Vector3(0, 0, launchVelocity);
            }
        }

        private void OnEnable()
        {
            if (photonView.IsMine)
            {
                if (shootProjectile == null)
                {
                    shootProjectile = GetComponent<NetworkShootProjectile>();
                }
                shootProjectile.enabled = false;
                StartCoroutine(EndPowerup());
            }
        }

        private void Update()
        {
            CheckCanFire();

            CheckForFiring();
        }

        public override void LaunchProjectile()
        {
            if (photonView.IsMine)
            {
                audioSource.Play();
                anim.SetTrigger("IsFiring");
                muzzle.Play();
                GameObject clone = PhotonNetwork.Instantiate(projectile.name, firePosition.position, Quaternion.AngleAxis(offset, Vector3.up) * firePosition.rotation);
                GameObject clone2 = PhotonNetwork.Instantiate(projectile.name, firePosition.position, Quaternion.AngleAxis(-offset, Vector3.up) * firePosition.rotation);
                GameObject clone3 = PhotonNetwork.Instantiate(projectile.name, firePosition.position, firePosition.rotation);

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

        IEnumerator EndPowerup()
        {
            yield return new WaitForSeconds(25f);
            this.enabled = false;
            shootProjectile.enabled = true;
        }
    }
}
