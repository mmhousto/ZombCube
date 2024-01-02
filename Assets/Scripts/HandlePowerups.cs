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
        public FullyAuto fullyAutoPowerup;
        public LaunchGrenade launchGrenade;
        public NetworkPlayerMovement networkPlayerMovement;
        public NetworkLaunchGrenade networkLaunchGrenade;
        public NetworkTripleShot networkTriple;
        public NetworkFullyAuto networkFullyAuto;

        private void Start()
        {
            if (SceneLoader.GetCurrentScene().name == "GameScene")
            {
                playerMovement = GetComponent<PlayerMovement>();
                tripleShotPowerup = GetComponent<TripleShot>();
                fullyAutoPowerup = GetComponent<FullyAuto>();
                launchGrenade = GetComponent<LaunchGrenade>();
            }
            else if (SceneLoader.GetCurrentScene().name == "NetworkGameScene")
            {
                networkPlayerMovement = GetComponent<NetworkPlayerMovement>();
                networkLaunchGrenade = GetComponent<NetworkLaunchGrenade>();
                networkTriple = GetComponent<NetworkTripleShot>();
                networkFullyAuto = GetComponent<NetworkFullyAuto>();
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

                if (other.CompareTag("FullyAuto"))
                {
                    fullyAutoPowerup.enabled = true;
                    Destroy(other.gameObject);
                }

                if (other.CompareTag("SpeedBoost"))
                {
                    playerMovement.ActivateSpeedBoost();
                    Destroy(other.gameObject);
                }

                if (other.CompareTag("x2"))
                {
                    int currentPoints = Projectile.pointsToAdd;
                    int pointsAdded = currentPoints * 2 > 120 ? 120 - currentPoints : (currentPoints * 2) - currentPoints;
                    Projectile.pointsToAdd = currentPoints * 2 > 120 ? 120 : currentPoints * 2;
                    StartCoroutine(EndMultiplierPowerup(pointsAdded));
                    Destroy(other.gameObject);
                }

                if (other.CompareTag("x3"))
                {
                    int currentPoints = Projectile.pointsToAdd;
                    int pointsAdded = currentPoints * 3 > 120 ? 120 - currentPoints : (currentPoints * 3) - currentPoints;
                    Projectile.pointsToAdd = currentPoints * 3 > 120 ? 120 : currentPoints * 3;
                    StartCoroutine(EndMultiplierPowerup(pointsAdded));
                    Destroy(other.gameObject);
                }

                if (other.CompareTag("MaxNades") && GameManager.mode == 0)
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
            else
            {
                if (other.CompareTag("TripleShot"))
                {
                    if (photonView.IsMine)
                        networkTriple.enabled = true;
                    other.GetComponent<PhotonView>().RequestOwnership();
                    PhotonNetwork.Destroy(other.gameObject);
                }

                if (other.CompareTag("FullyAuto"))
                {
                    if(photonView.IsMine)
                        networkFullyAuto.enabled = true;
                    other.GetComponent<PhotonView>().RequestOwnership();
                    PhotonNetwork.Destroy(other.gameObject);
                }

                if (other.CompareTag("SpeedBoost"))
                {
                    if (photonView.IsMine)
                        networkPlayerMovement.ActivateSpeedBoost();
                    other.GetComponent<PhotonView>().RequestOwnership();
                    PhotonNetwork.Destroy(other.gameObject);
                }

                if (other.CompareTag("x2"))
                {
                    int currentPoints = Projectile.pointsToAdd;
                    int pointsAdded = currentPoints * 2 > 120 ? 120 - currentPoints : (currentPoints * 2) - currentPoints;
                    Projectile.pointsToAdd = currentPoints * 2 > 120 ? 120 : currentPoints * 2;
                    StartCoroutine(EndMultiplierPowerup(pointsAdded));
                    other.GetComponent<PhotonView>().RequestOwnership();
                    PhotonNetwork.Destroy(other.gameObject);
                }

                if (other.CompareTag("x3"))
                {
                    int currentPoints = Projectile.pointsToAdd;
                    int pointsAdded = currentPoints * 3 > 120 ? 120 - currentPoints : (currentPoints * 3) - currentPoints;
                    Projectile.pointsToAdd = currentPoints * 3 > 120 ? 120 : currentPoints * 3;
                    StartCoroutine(EndMultiplierPowerup(pointsAdded));
                    other.GetComponent<PhotonView>().RequestOwnership();
                    PhotonNetwork.Destroy(other.gameObject);
                }

                if (other.CompareTag("MaxNades"))
                {
                    networkLaunchGrenade.grenadeCount = 4;
                    other.GetComponent<PhotonView>().RequestOwnership();
                    PhotonNetwork.Destroy(other.gameObject);
                }
            }
        }

        IEnumerator EndMultiplierPowerup(int pointsToMinus)
        {
            yield return new WaitForSeconds(25f);
            Projectile.pointsToAdd -= pointsToMinus;
            if(Projectile.pointsToAdd < 10) Projectile.pointsToAdd = 10;
        }
    }
}
