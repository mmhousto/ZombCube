using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;

namespace Com.GCTC.ZombCube
{
    public class NetworkShieldedCube : NetworkEnemy
    {
        public GameObject shield;

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

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Projectile") && shield == null)
            {
                photonView.RPC(nameof(DestroyEnemy), RpcTarget.MasterClient);

            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && hasHit == false)
            {
                other.transform.root.GetComponent<NetworkPlayerManager>().DamagePlayerCall(25f);
                hasHit = true;

                photonView.RPC(nameof(DestroyEnemy), RpcTarget.MasterClient);
            }
        }

    }
}
