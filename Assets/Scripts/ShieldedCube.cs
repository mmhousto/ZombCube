using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Com.GCTC.ZombCube
{
    public class ShieldedCube : EnemyAI
    {
        public GameObject shield;

        // Start is called before the first frame update
        void Start()
        {
            ai = GetComponent<NavMeshAgent>();
            target = GameObject.FindWithTag("Player").transform;
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
                Destroy(gameObject);
                if (other.name == "Capsule")
                    other.transform.parent.GetComponent<PlayerManager>().Damage(20);
                else
                    other.GetComponent<PlayerManager>().Damage(20);
            }
        }
    }
}
