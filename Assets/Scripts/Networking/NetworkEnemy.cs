using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;

namespace Com.MorganHouston.ZombCube
{

    public class NetworkEnemy : MonoBehaviour
    {
        private GameObject[] players;

        private Transform target;
        private NavMeshAgent ai;

        public static bool isGameOver = false;

        // Start is called before the first frame update
        void Start()
        {
            ai = GetComponent<NavMeshAgent>();
            isGameOver = NetworkGameManager.Instance.IsGameOver();
            players = GameObject.FindGameObjectsWithTag("Player");
            target = GetClosestPlayer(players);
        }

        // Update is called once per frame
        void Update()
        {
            isGameOver = NetworkGameManager.Instance.IsGameOver();

            if (isGameOver == false)
            {
                target = GetClosestPlayer(players);
                if (target)
                    ai.SetDestination(target.position);
            }
                
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.tag == "Player")
            {
                PhotonNetwork.Destroy(gameObject);
                collision.gameObject.GetComponent<NetworkPlayerManager>().Damage(20);
            }
        }

        Transform GetClosestPlayer(GameObject[] players)
        {
            Transform tMin = null;
            float minDist = Mathf.Infinity;
            Vector3 currentPos = transform.position;
            foreach (GameObject player in players)
            {
                float dist = Vector3.Distance(player.transform.position, currentPos);
                if (dist < minDist)
                {
                    tMin = player.transform;
                    minDist = dist;
                }
            }
            return tMin;
        }
    }

}
