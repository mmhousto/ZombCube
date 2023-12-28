using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace Com.GCTC.ZombCube
{
    public class NetworkArmor : MonoBehaviourPun
    {
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Projectile"))
            {
                photonView.RPC(nameof(DestroyEnemy), RpcTarget.MasterClient);

            }
        }

        [PunRPC]
        public void DestroyEnemy()
        {
            PhotonNetwork.Destroy(this.gameObject);
        }
    }
}
