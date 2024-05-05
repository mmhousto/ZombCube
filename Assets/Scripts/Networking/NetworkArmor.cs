using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace Com.GCTC.ZombCube
{
    public class NetworkArmor : MonoBehaviourPun
    {
       /* bool destroying;

        private void Update()
        {
            if (transform.root.tag != "Enemy" && destroying == false)
            {
                destroying = true;
                CallDestroyEnemy();
            }
        }*/ 

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Projectile"))
            {
                CallDestroyEnemy();

            }
        }

        public void CallDestroyEnemy()
        {
            photonView.RPC(nameof(DestroyEnemy), RpcTarget.AllBuffered);
        }

        [PunRPC]
        public void DestroyEnemy()
        {
            //PhotonNetwork.Destroy(gameObject);
            gameObject.SetActive(false);
        }
    }
}
