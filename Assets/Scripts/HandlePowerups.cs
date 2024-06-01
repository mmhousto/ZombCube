using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    public class HandlePowerups : MonoBehaviourPun
    {
        public PlayerMovement playerMovement;
        public TripleShot tripleShotPowerup;
        public FullyAuto smB; // SMB
        public AssaultBlaster assaultBlaster; // AB
        public Shotblaster shotBlaster; // Shotblaster
        public LaunchGrenade launchGrenade;
        public ShootProjectile shoot;
        public NetworkShootProjectile networkShoot;
        public NetworkPlayerMovement networkPlayerMovement;
        public NetworkLaunchGrenade networkLaunchGrenade;
        public NetworkTripleShot networkTriple;
        public NetworkFullyAuto networkFullyAuto; // SMB
        public NetworkAB networkAB; // AB
        public NetworkShotblaster networkShotblaster; //Shotblaster

        private void Start()
        {
            if (SceneLoader.GetCurrentScene().name == "GameScene")
            {
                shoot = GetComponent<ShootProjectile>();
                playerMovement = GetComponent<PlayerMovement>();
                tripleShotPowerup = GetComponent<TripleShot>();
                smB = GetComponent<FullyAuto>();
                assaultBlaster = GetComponent<AssaultBlaster>();
                shotBlaster = GetComponent<Shotblaster>();
                launchGrenade = GetComponent<LaunchGrenade>();
            }
            else if (SceneLoader.GetCurrentScene().name == "NetworkGameScene")
            {
                networkShoot = GetComponent<NetworkShootProjectile>();
                networkPlayerMovement = GetComponent<NetworkPlayerMovement>();
                networkLaunchGrenade = GetComponent<NetworkLaunchGrenade>();
                networkTriple = GetComponent<NetworkTripleShot>();
                networkFullyAuto = GetComponent<NetworkFullyAuto>();
                networkAB = GetComponent<NetworkAB>();
                networkShotblaster = GetComponent<NetworkShotblaster>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (SceneLoader.GetCurrentScene().name == "GameScene")
            {
                if (other.CompareTag("TripleShot"))
                {
                    tripleShotPowerup.enabled = true;
                    Destroy(other.gameObject);
                }
                else if(other.CompareTag("FullyAuto"))
                {
                    shoot.fireRate /= 2;
                    tripleShotPowerup.fireRate /= 2;
                    smB.fireRate /= 2;
                    assaultBlaster.fireRate /= 2;
                    shotBlaster.fireRate /= 2;
                    StartCoroutine(EndFullAutoPowerup());
                    Destroy(other.gameObject);
                }
                else if(other.CompareTag("SpeedBoost"))
                {
                    playerMovement.ActivateSpeedBoost();
                    Destroy(other.gameObject);
                }
                else if(other.CompareTag("x2"))
                {
                    int currentPoints = Projectile.pointsToAdd;
                    int pointsAdded = currentPoints * 2 > 120 ? 120 - currentPoints : (currentPoints * 2) - currentPoints;
                    Projectile.pointsToAdd = currentPoints * 2 > 120 ? 120 : currentPoints * 2;
                    StartCoroutine(EndMultiplierPowerup(pointsAdded));
                    Destroy(other.gameObject);
                }
                else if(other.CompareTag("x3"))
                {
                    int currentPoints = Projectile.pointsToAdd;
                    int pointsAdded = currentPoints * 3 > 120 ? 120 - currentPoints : (currentPoints * 3) - currentPoints;
                    Projectile.pointsToAdd = currentPoints * 3 > 120 ? 120 : currentPoints * 3;
                    StartCoroutine(EndMultiplierPowerup(pointsAdded));
                    Destroy(other.gameObject);
                }
                else if(other.CompareTag("MaxNades") && GameManager.mode == 0)
                {
                    launchGrenade.grenadeCount = 4;
                    Destroy(other.gameObject);
                }
                else if (other.CompareTag("MaxNades") && GameManager.mode == 1)
                {
                    CouchCoopManager.Instance.SetMaxNades();
                    Destroy(other.gameObject);
                }
            }
            else if (SceneLoader.GetCurrentScene().name == "NetworkGameScene" && photonView.IsMine)
            {
                if (other.CompareTag("TripleShot"))
                {
                    networkTriple.enabled = true;
                    other.GetComponent<PhotonView>().RequestOwnership();
                    if (other.GetComponent<PhotonView>().IsMine)
                    {
                        PhotonNetwork.Destroy(other.gameObject);
                    }
                }
                else if (other.CompareTag("FullyAuto"))
                {
                    networkShoot.fireRate /= 2;
                    networkTriple.fireRate /= 2;
                    networkFullyAuto.fireRate /= 2;
                    networkAB.fireRate /= 2;
                    networkShotblaster.fireRate /= 2;
                    StartCoroutine(EndFullAutoPowerup());
                    other.GetComponent<PhotonView>().RequestOwnership();
                    if (other.GetComponent<PhotonView>().IsMine)
                    {
                        PhotonNetwork.Destroy(other.gameObject);
                    }
                }
                else if (other.CompareTag("SpeedBoost"))
                {
                    networkPlayerMovement.ActivateSpeedBoost();
                    other.GetComponent<PhotonView>().RequestOwnership();
                    if (other.GetComponent<PhotonView>().IsMine)
                    {
                        PhotonNetwork.Destroy(other.gameObject);
                    }
                }
                else if (other.CompareTag("x2"))
                {
                    int currentPoints = Projectile.pointsToAdd;
                    int pointsAdded = currentPoints * 2 > 120 ? 120 - currentPoints : (currentPoints * 2) - currentPoints;
                    Projectile.pointsToAdd = currentPoints * 2 > 120 ? 120 : currentPoints * 2;
                    StartCoroutine(EndMultiplierPowerup(pointsAdded));
                    other.GetComponent<PhotonView>().RequestOwnership();
                    if (other.GetComponent<PhotonView>().IsMine)
                    {
                        PhotonNetwork.Destroy(other.gameObject);
                    }
                }
                else if (other.CompareTag("x3"))
                {
                    int currentPoints = Projectile.pointsToAdd;
                    int pointsAdded = currentPoints * 3 > 120 ? 120 - currentPoints : (currentPoints * 3) - currentPoints;
                    Projectile.pointsToAdd = currentPoints * 3 > 120 ? 120 : currentPoints * 3;
                    StartCoroutine(EndMultiplierPowerup(pointsAdded));
                    other.GetComponent<PhotonView>().RequestOwnership();
                    if (other.GetComponent<PhotonView>().IsMine)
                    {
                        PhotonNetwork.Destroy(other.gameObject);
                    }
                }
                else if (other.CompareTag("MaxNades"))
                {
                    networkLaunchGrenade.grenadeCount = 4;
                    other.GetComponent<PhotonView>().RequestOwnership();
                    if(other.GetComponent<PhotonView>().IsMine)
                    {
                        PhotonNetwork.Destroy(other.gameObject);
                    }
                    
                }
            }
        }

        IEnumerator EndMultiplierPowerup(int pointsToMinus)
        {
            yield return new WaitForSeconds(25f);
            Projectile.pointsToAdd -= pointsToMinus;
            if(Projectile.pointsToAdd < 10) Projectile.pointsToAdd = 10;
        }

        IEnumerator EndFullAutoPowerup()
        {
            yield return new WaitForSeconds(25f);
            if (SceneLoader.GetCurrentScene().name == "GameScene")
            {
                tripleShotPowerup.fireRate *= 2;
                shoot.fireRate *= 2;
                smB.fireRate *= 2;
                assaultBlaster.fireRate *= 2;
                shotBlaster.fireRate *= 2;
            }
            else if (SceneLoader.GetCurrentScene().name == "NetworkGameScene")
            {
                networkTriple.fireRate *= 2;
                networkShoot.fireRate *= 2;
                networkFullyAuto.fireRate *= 2;
                networkAB.fireRate *= 2;
                networkShotblaster.fireRate *= 2;
            }
        }
    }
}
