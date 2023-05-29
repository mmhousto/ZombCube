using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Com.GCTC.ZombCube
{
    public class NetworkFullyAuto : MonoBehaviourPun
    {
        private NetworkShootProjectile shootProjectile;
        private NetworkTripleShot tripleShot;
        private float fireRate = 0.2f;

        // Start is called before the first frame update
        void Start()
        {
            if (photonView.IsMine)
            {
                shootProjectile = GetComponent<NetworkShootProjectile>();
                tripleShot = GetComponent<NetworkTripleShot>();

                //isFiring = true;
                fireRate = 0.2f;

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
                fireRate = 0.2f;
                if (shootProjectile != null)
                    shootProjectile.fireRate = fireRate;
                if (tripleShot != null)
                    tripleShot.fireRate = fireRate;

                //shootProjectile.enabled = false;
                StartCoroutine(EndPowerup());
            }
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
