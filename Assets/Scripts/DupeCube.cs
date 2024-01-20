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
            ai.SetDestination(target.position);
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

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Bullet"))
            {
                Dupe();
            }
        }

        public void Dupe()
        {
            Instantiate(dupe, transform.position + offset, transform.rotation);
            Instantiate(dupe, transform.position - offset, transform.rotation);
        }
    }
}
