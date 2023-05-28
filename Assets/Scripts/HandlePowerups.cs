using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    public class HandlePowerups : MonoBehaviourPun
    {

        public TripleShot tripleShotPowerup;
        public FullyAuto fullyAutoPowerup;
        public NetworkTripleShot networkTriple;
        public NetworkFullyAuto networkFullyAuto;

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
            }
            else
            {
                if (other.CompareTag("TripleShot") && photonView.IsMine)
                {
                    networkTriple.enabled = true;
                    PhotonNetwork.Destroy(other.gameObject);
                }

                if (other.CompareTag("FullyAuto") && photonView.IsMine)
                {
                    networkFullyAuto.enabled = true;
                    PhotonNetwork.Destroy(other.gameObject);
                }
            }
        }
    }
}
