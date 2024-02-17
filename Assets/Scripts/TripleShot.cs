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
        private ShootProjectile blaster;
        private FullyAuto smb; //SMB
        private AssaultBlaster aB; // AB
        private Shotblaster shotblaster; // Shotblaster
        private SniperBlaster sniperBlaster; // Sniper
        private LaunchGrenade grenade; // Grenade
        private SwapManager swapManager;

        // Start is called before the first frame update
        void Start()
        {
            swapManager = GetComponent<SwapManager>();
            blaster = GetComponent<ShootProjectile>();
            smb = GetComponent<FullyAuto>();
            aB = GetComponent<AssaultBlaster>();
            shotblaster = GetComponent<Shotblaster>();
            sniperBlaster = GetComponent<SniperBlaster>();
            grenade = GetComponent<LaunchGrenade>();
            audioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            if (grenade == null) grenade = GetComponent<LaunchGrenade>();
            if (grenade != null && grenade.enabled == true)
            {
                swapManager.SwapToNextWeapon();
            }

            if (blaster == null) blaster = GetComponent<ShootProjectile>();
            if (smb == null) smb = GetComponent<FullyAuto>();
            if (aB == null) aB = GetComponent<AssaultBlaster>();
            if (shotblaster == null) shotblaster = GetComponent<Shotblaster>();
            if(sniperBlaster == null) sniperBlaster= GetComponent<SniperBlaster>();
            if (swapManager == null) swapManager = GetComponent<SwapManager>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();

            if (blaster != null && blaster.enabled == true)
            {
                blaster.enabled = false;
                fireRate = blaster.fireRate;
                firePosition = blaster.firePosition;
                fireSound = blaster.fireSound;
                muzzle = blaster.muzzle;
                anim = blaster.anim;
                projectile = blaster.projectile;
                launchVelocity = 5000;
                launchVector = new Vector3(0, 0, launchVelocity);
            }
            else if (smb != null && smb.enabled == true)
            {
                smb.enabled = false;
                fireRate = smb.fireRate;
                firePosition = smb.firePosition;
                fireSound = smb.fireSound;
                muzzle = smb.muzzle;
                anim = smb.anim;
                projectile = smb.projectile;
                launchVelocity = 5000;
                launchVector = new Vector3(0, 0, launchVelocity);
            }
            else if (aB != null && aB.enabled == true)
            {
                aB.enabled = false;
                fireRate = aB.fireRate;
                firePosition = aB.firePosition;
                fireSound = aB.fireSound;
                muzzle = aB.muzzle;
                anim = aB.anim;
                projectile = aB.projectile;
                launchVelocity = 10000;
                launchVector = new Vector3(0, 0, launchVelocity);
            }
            else if (shotblaster != null && shotblaster.enabled == true)
            {
                shotblaster.enabled = false;
                fireRate = shotblaster.fireRate;
                firePosition = shotblaster.firePosition;
                fireSound = shotblaster.fireSound;
                muzzle = shotblaster.muzzle;
                anim = shotblaster.anim;
                projectile = shotblaster.projectile;
                launchVelocity = 5000;
                launchVector = new Vector3(0, 0, launchVelocity);
            }
            else if (sniperBlaster != null && sniperBlaster.enabled == true)
            {
                sniperBlaster.enabled = false;
                fireRate = sniperBlaster.fireRate;
                firePosition = sniperBlaster.firePosition;
                fireSound = sniperBlaster.fireSound;
                muzzle = sniperBlaster.muzzle;
                anim = sniperBlaster.anim;
                projectile = sniperBlaster.projectile;
                launchVelocity = 15000;
                launchVector = new Vector3(0, 0, launchVelocity);
            }
            audioSource.clip = fireSound;

            StartCoroutine(EndPowerup());
        }

        private void OnDisable()
        {
            StopCoroutine(EndPowerup());

            switch (swapManager.GetCurrentWeaponIndex())
            {
                case 0:// Pistol
                    blaster.enabled = true;
                    grenade.enabled = false;
                    break;
                case 1:// Grenade
                    if (grenade.grenadeCount > 0) // if has grenades switch, else swap to next weapon
                    {
                        blaster.enabled = false;
                        grenade.enabled = true;
                    }
                    else
                        swapManager.SwapToNextWeapon();
                    break;
                case 2:// SMB
                    smb.enabled = true;
                    blaster.enabled = false;
                    grenade.enabled = false;

                    break;
                case 3:// AB
                    aB.enabled = true;
                    blaster.enabled = false;
                    grenade.enabled = false;

                    break;
                case 4:// Shotblaster
                    shotblaster.enabled = true;
                    blaster.enabled = false;
                    grenade.enabled = false;

                    break;
                case 5:// Sniper
                    sniperBlaster.enabled = true;
                    blaster.enabled = false;
                    grenade.enabled = false;

                    break;
                default:
                    blaster.enabled = true;
                    smb.enabled = false;
                    aB.enabled = false;
                    grenade.enabled = false;
                    break;
            }
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
            GameObject[] projs = { clone, clone2, clone3 };

            foreach (GameObject proj in projs)
            {
                if (projectile == shotblaster.projectile)
                {
                    foreach (Projectile p in proj.GetComponentsInChildren<Projectile>())
                    {
                        if (p.name.Contains("Blast")) continue;
                        float x = Random.Range(-6f, 6f);
                        float y = Random.Range(-6f, 6f);
                        p.transform.SetParent(null);
                        p.transform.localRotation *= Quaternion.Euler(new Vector3(x, y, 0));
                        p.GetComponent<Rigidbody>().AddForce(p.transform.forward * launchVelocity);


                    }
                    float cx = Random.Range(-6f, 6f);
                    float cy = Random.Range(-6f, 6f);
                    proj.transform.localRotation *= Quaternion.Euler(new Vector3(cx, cy, 0));
                    proj.GetComponent<Rigidbody>().AddForce(proj.transform.forward * launchVelocity);
                }else
                    proj.GetComponent<Rigidbody>().AddForce(proj.transform.forward * launchVelocity);
            }

            if (Player.Instance != null)
            {
                Player.Instance.totalProjectilesFired++;

                CheckForTriggerHappyAchievements();
            }
        }

        IEnumerator EndPowerup()
        {
            yield return new WaitForSeconds(25f);
            this.enabled = false;
        }

    }
}
