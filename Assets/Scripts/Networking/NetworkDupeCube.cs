using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;

namespace Com.GCTC.ZombCube
{
    public class NetworkDupeCube : NetworkEnemy
    {
        public Vector3 offset;

        // Start is called before the first frame update
        void Start()
        {
            if (!this.photonView.IsMine)
            {
                ai = GetComponent<NavMeshAgent>();
                ai.enabled = false;
                return;
            }

            ai = GetComponent<NavMeshAgent>();
            isGameOver = NetworkGameManager.Instance.IsGameOver();
            players = GameObject.FindGameObjectsWithTag("Player");
            target = GetClosestPlayer(players);
            offset = Vector3.right + Vector3.up;
        }

        // Update is called once per frame
        void Update()
        {
            if (!this.photonView.IsMine) { return; }

            players = GameObject.FindGameObjectsWithTag("Player");

            isGameOver = NetworkGameManager.Instance.IsGameOver();

            if (isGameOver == false)
            {
                target = GetClosestPlayer(players);
                if (target == null) { return; }
                else
                {
                    ai.SetDestination(target.position);
                }

            }
            else
            {
                if (ai != null && ai.isStopped == false)
                    ai.isStopped = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && hasHit == false)
            {
                other.transform.root.GetComponent<NetworkPlayerManager>().DamagePlayerCall(20f);
                hasHit = true;

                photonView.RPC(nameof(DestroyEnemy), RpcTarget.MasterClient);
            }
        }

        public void CallDupe()
        {
            photonView.RPC(nameof(Dupe), RpcTarget.MasterClient);
        }

        [PunRPC]
        public void Dupe()
        {
            PhotonNetwork.InstantiateRoomObject("NetworkDupedCube",
                        transform.position + offset,
                        transform.rotation);
            PhotonNetwork.InstantiateRoomObject("NetworkDupedCube",
                    transform.position - offset,
                    transform.rotation);
            DestroyEnemyCall();
        }
    }
}
