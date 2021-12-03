using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Com.MorganHouston.ZombCube
{

    public class EnemyAI : MonoBehaviour
    {


        private Transform target;
        private NavMeshAgent ai;

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
                other.gameObject.GetComponent<PlayerManager>().Damage(20);
            }
        }
    }

}
