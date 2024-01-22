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
        private NetworkShootProjectile blaster; //blaster
        private NetworkFullyAuto smb; //SMB
        private NetworkAB aB; //AB
        private NetworkShotblaster shotblaster; //Shotblaster
        private NetworkLaunchGrenade grenade; // Grenade
        private NetworkSwapManager swapManager;

        // Start is called before the first frame update
        void Start()
        {
            if (photonView.IsMine)
            {
                swapManager = GetComponent<NetworkSwapManager>();
                playerManager = GetComponent<NetworkPlayerManager>();
                audioSource = GetComponent<AudioSource>();
                blaster = GetComponent<NetworkShootProjectile>();
                smb = GetComponent<NetworkFullyAuto>();
                aB = GetComponent<NetworkAB>();
                shotblaster = GetComponent<NetworkShotblaster>();
                grenade = GetComponent<NetworkLaunchGrenade>();
            }
        }

        private void OnEnable()
        {
            if (photonView.IsMine)
            {
                if (grenade == null) grenade = GetComponent<NetworkLaunchGrenade>();
                if (blaster == null) blaster = GetComponent<NetworkShootProjectile>();
                if (smb == null) smb = GetComponent<NetworkFullyAuto>();
                if (aB == null) aB = GetComponent<NetworkAB>();
                if (shotblaster == null) shotblaster = GetComponent<NetworkShotblaster>();
                if (swapManager == null) swapManager = GetComponent<NetworkSwapManager>();

                if (grenade != null && grenade.enabled == true)
                {
                    swapManager.SwapToNextWeapon();
                }

                if (blaster != null && blaster.enabled == true)
                {
                    blaster.enabled = false;
                    fireRate = blaster.fireRate;
                    firePosition = blaster.firePosition;
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
                    muzzle = shotblaster.muzzle;
                    anim = shotblaster.anim;
                    projectile = shotblaster.projectile;
                    launchVelocity = 5000;
                    launchVector = new Vector3(0, 0, launchVelocity);
                }

                // Get the PlayerInput component
                PlayerInput playerInput = GetComponent<PlayerInput>();
                if (playerInput != null)
                {
                    // Find the fire action
                    fireAction = playerInput.actions.FindAction("Fire");
                    if (fireAction != null)
                    {
                        // Enable the fire action and attach the callback
                        fireAction.Enable();
                        fireAction.performed += OnFired;
                        fireAction.canceled += OnFired;
                    }
                    else
                    {
                        Debug.LogError("Fire action not found.");
                    }
                }
                else
                {
                    Debug.LogError("PlayerInput component not found.");
                }

                StartCoroutine(EndPowerup());
            }
        }

        private void OnDisable()
        {
            if (photonView.IsMine && fireAction != null)
            {
                StopCoroutine(EndPowerup());
                // Disable the fire action
                fireAction.Disable();
                fireAction.performed -= OnFired;
                fireAction.canceled -= OnFired;

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
                    default:
                        blaster.enabled = true;
                        smb.enabled = false;
                        aB.enabled = false;
                        grenade.enabled = false;
                        break;
                }
            }
        }

        private void Update()
        {
            CheckCanFire();

            CheckForFiring();
        }

        public override void LaunchProjectile()
        {
            if (photonView.IsMine && playerManager.isInputDisabled == false)
            {
                audioSource.Play();
                anim.SetTrigger("IsFiring");
                muzzle.Play();
                GameObject clone = PhotonNetwork.Instantiate(projectile.name, firePosition.position, Quaternion.AngleAxis(offset, Vector3.up) * firePosition.rotation);
                GameObject clone2 = PhotonNetwork.Instantiate(projectile.name, firePosition.position, Quaternion.AngleAxis(-offset, Vector3.up) * firePosition.rotation);
                GameObject clone3 = PhotonNetwork.Instantiate(projectile.name, firePosition.position, firePosition.rotation);

                GameObject[] projs = { clone, clone2, clone3 };

                foreach (GameObject proj in projs)
                {
                    if (projectile == shotblaster.projectile)
                    {
                        foreach (Projectile p in proj.GetComponentsInChildren<Projectile>())
                        {
                            p.GetComponent<Rigidbody>().AddRelativeForce(launchVector);
                        }
                    }
                    proj.GetComponent<Rigidbody>().AddRelativeForce(launchVector);
                }

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
        }
    }
}
