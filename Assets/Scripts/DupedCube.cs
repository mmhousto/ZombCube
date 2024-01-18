using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Com.GCTC.ZombCube
{
    public class DupedCube : EnemyAI
    {
        // Start is called before the first frame update
        void Start()
        {
            ai = GetComponent<NavMeshAgent>();
            target = GameObject.FindWithTag("Player").transform;
        }

        // Update is called once per frame
        void Update()
        {
            ai.SetDestination(target.position);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Destroy(gameObject);
                other.transform.root.GetComponent<PlayerManager>().Damage(5);
            }
        }
    }
}
