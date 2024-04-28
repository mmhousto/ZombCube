using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Com.GCTC.ZombCube
{
    public class DupeCube : EnemyAI
    {

        public GameObject dupe;
        public Vector3 offset;

        // Start is called before the first frame update
        void Start()
        {
            ai = GetComponent<NavMeshAgent>();
            target = GameObject.FindWithTag("Player").transform;
            offset = Vector3.right + Vector3.up;
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
                Dupe();
                Destroy(gameObject);
                if (other.name == "Capsule")
                    other.transform.parent.GetComponent<PlayerManager>().Damage(20);
                else
                    other.GetComponent<PlayerManager>().Damage(20);
            }
        }

        public void Dupe()
        {
            Instantiate(dupe, transform.position + offset, transform.rotation);
            Instantiate(dupe, transform.position - offset, transform.rotation);
        }
    }
}
