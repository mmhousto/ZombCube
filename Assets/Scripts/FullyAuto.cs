using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Com.GCTC.ZombCube
{
    public class FullyAuto : ShootProjectile
    {
        private ShootProjectile shootProjectile;
        private TripleShot tripleShot;

        // Start is called before the first frame update
        void Start()
        {
            shootProjectile = GetComponent<ShootProjectile>();
            tripleShot = GetComponent<TripleShot>();
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

        private void OnEnable()
        {
            if(shootProjectile != null)
                shootProjectile.fireRate = fireRate;
            if(tripleShot != null)
                tripleShot.fireRate = fireRate;

            //shootProjectile.enabled = false;
            StartCoroutine(EndPowerup());
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
