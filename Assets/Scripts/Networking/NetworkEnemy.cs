using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;

namespace Com.GCTC.ZombCube
{

    public class NetworkEnemy : MonoBehaviourPun
    {
        protected GameObject[] players;

        protected Transform target;
        protected NavMeshAgent ai;

        protected bool hasHit = false;

        public bool isGameOver = false;
        public bool armorEnabled = false;

        public GameObject armor;

        // Start is called before the first frame update
        void Start()
        {
            if (!this.photonView.IsMine) {
                ai = GetComponent<NavMeshAgent>();
                ai.enabled = false;
                return; 
            }

            ai = GetComponent<NavMeshAgent>();
            isGameOver = NetworkGameManager.Instance.IsGameOver();
            players = GameObject.FindGameObjectsWithTag("Player");
            target = GetClosestPlayer(players);
        }
        
        /// <summary>
        /// This method does this.
        /// </summary>
        void Update()
        {
            if (!this.photonView.IsMine) { return; }

            players = GameObject.FindGameObjectsWithTag("Player");

            isGameOver = NetworkGameManager.Instance.IsGameOver();

            if (isGameOver == false)
            {
                target = GetClosestPlayer(players);
                if (target == null || ai.enabled == false) { return; }
                else
                {
                    ai.SetDestination(target.position);
                }

            }
            else
            {
                DestroyEnemyCall();
            }

        }

        public void EnableDisableArmor()
        {
            photonView.RPC(nameof(EnableArmor), RpcTarget.AllBuffered);
        }

        [PunRPC]
        public void EnableArmor()
        {
            armor.SetActive(true);
        }

        /*private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Projectile"))
            {
                //photonView.RPC(nameof(DestroyEnemy), RpcTarget.MasterClient);

            }
        }*/

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && hasHit == false)
            {
                other.transform.root.GetComponent<NetworkPlayerManager>().DamagePlayerCall(20f);

                hasHit = true;

                photonView.RPC(nameof(DestroyEnemy), RpcTarget.MasterClient);
            }
        }

        protected Transform GetClosestPlayer(GameObject[] players)
        {
            Transform tMin = null;
            float minDist = Mathf.Infinity;
            Vector3 currentPos = transform.position;
            foreach (GameObject player in players)
            {
                if (player == null || currentPos == null) { return null; }
                float dist = Vector3.Distance(player.transform.position, currentPos);
                if (dist < minDist)
                {
                    tMin = player.transform;
                    minDist = dist;
                }
            }
            return tMin;
        }

        public void DestroyEnemyCall()
        {
            photonView.RPC(nameof(DestroyEnemy), RpcTarget.MasterClient);
        }

        [PunRPC]
        public void DestroyEnemy()
        {
            if(this.gameObject != null)
            {
                PhotonNetwork.Destroy(this.gameObject);
            }
            
        }
    }

}
