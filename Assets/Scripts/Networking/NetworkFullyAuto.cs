using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    public class NetworkFullyAuto : NetworkShootProjectile
    {
        private NetworkShootProjectile shootProjectile;
        private NetworkTripleShot tripleShot;

        // Start is called before the first frame update
        void Start()
        {
            if (photonView.IsMine)
            {
                playerManager = GetComponent<NetworkPlayerManager>();
                shootProjectile = GetComponent<NetworkShootProjectile>();
                tripleShot = GetComponent<NetworkTripleShot>();
                audioSource = GetComponent<AudioSource>();

                //isFiring = true;
                fireRate = 0.2f;
                launchVector = new Vector3(0, 0, launchVelocity);

                if (shootProjectile != null)
                    shootProjectile.fireRate = fireRate;

                if (tripleShot != null)
                    tripleShot.fireRate = fireRate;

                //shootProjectile.enabled = false;
                StartCoroutine(EndPowerup());
            }
        }

        private void OnEnable()
        {
            if (photonView.IsMine)
            {
                if (shootProjectile != null)
                    shootProjectile.fireRate = fireRate;
                if (tripleShot != null)
                    tripleShot.fireRate = fireRate;

                //shootProjectile.enabled = false;
                StartCoroutine(EndPowerup());
            }
        }

        // Update is called once per frame
        void Update()
        {
            CheckCanFire();

            CheckForFiring();
        }

        IEnumerator EndPowerup()
        {
            yield return new WaitForSeconds(25f);
            this.enabled = false;
            shootProjectile.fireRate = 0.8f;
            tripleShot.fireRate = 0.8f;
            //shootProjectile.enabled = true;
        }
    }
}
