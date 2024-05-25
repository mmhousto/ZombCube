using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Com.GCTC.ZombCube
{
    public class BossCube : EnemyAI
    {

        public GameObject projectile;
        public Slider healthBar;
        [SerializeField]
        private float health = 1000;
        public delegate void BossDead();
        public static event BossDead bossDead;


        // Start is called before the first frame update
        void Start()
        {
            ai = GetComponent<NavMeshAgent>();
            target = GameObject.FindWithTag("Player").transform;
            health = 1000;
            healthBar.maxValue = 1000;
            healthBar.value = 1000;
            InvokeRepeating(nameof(AttackPlayer), 2f, 3f);
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
                    transform.LookAt(target.position);
                }

            }
            else
            {
                if (ai != null && ai.isStopped == false)
                    ai.isStopped = true;
            }
        }

        void AttackPlayer()
        {
            if (isGameOver == true) return;

            Vector3 direction = target.position - transform.position;
            GameObject clone = Instantiate(projectile, transform.position, Quaternion.identity);
            clone.GetComponent<Rigidbody>().AddForce(direction * 50, ForceMode.Acceleration);
        }

        public void TakeDamage()
        {
            health--;
            healthBar.value--;

            if(health <= 0 && bossDead != null)
            {
                bossDead();
                PlayerManager.AddPoints(5000);

                if (Player.Instance != null)
                    Player.Instance.cubesEliminated++;

                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            
        }


    }
}
