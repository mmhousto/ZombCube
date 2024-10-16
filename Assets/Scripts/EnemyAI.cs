using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Com.GCTC.ZombCube
{

    public class EnemyAI : MonoBehaviour
    {

        protected GameObject[] players;
        protected Transform target;
        protected NavMeshAgent ai;
        public bool isGameOver = false;

        // Start is called before the first frame update
        void Start()
        {
            ai = GetComponent<NavMeshAgent>();
            isGameOver = (GameManager.Instance != null) ? GameManager.Instance.isGameOver : false;
            players = GameObject.FindGameObjectsWithTag("Player");
            target = GetClosestPlayer(players);
        }

        // Update is called once per frame
        void Update()
        {
            players = GameObject.FindGameObjectsWithTag("Player");

            isGameOver = (GameManager.Instance != null) ? GameManager.Instance.isGameOver : false;

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
            if (other.CompareTag("Player"))
            {
                if(SceneLoader.GetCurrentScene().name == "MainMenu")
                {
                    gameObject.SetActive(false);
                    return;
                }

                Destroy(gameObject);
                if (other.name == "Capsule")
                    other.transform.parent.GetComponent<PlayerManager>().Damage(20);
                else
                    other.GetComponent<PlayerManager>().Damage(20);

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
    }

}
