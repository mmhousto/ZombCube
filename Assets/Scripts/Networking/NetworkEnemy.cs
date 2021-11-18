using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;

namespace Com.MorganHouston.ZombCube
{

    public class NetworkEnemy : MonoBehaviourPun
    {
        private GameObject[] players;

        private Transform target;
        private NavMeshAgent ai;

        public bool isGameOver = false;

        // Start is called before the first frame update
        void Start()
        {
            if (!this.photonView.IsMine) { return; }

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
                ai.isStopped = true;
            }

        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.tag == "Player")
            {
                collision.gameObject.GetComponent<NetworkPlayerManager>().Damage(20);

                photonView.RPC(nameof(DestroyEnemy), RpcTarget.MasterClient);
            }

            if (collision.gameObject.tag == "Projectile")
            {
                photonView.RPC(nameof(DestroyEnemy), RpcTarget.MasterClient);

            }
        }

        Transform GetClosestPlayer(GameObject[] players)
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


        [PunRPC]
        public void DestroyEnemy()
        {
            PhotonNetwork.Destroy(this.gameObject);
        }
    }

}
