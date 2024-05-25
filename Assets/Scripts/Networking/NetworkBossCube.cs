using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace Com.GCTC.ZombCube
{
    public class NetworkBossCube : NetworkEnemy
    {
        public GameObject projectile;
        public Slider healthBar;
        [SerializeField]
        private float health = 1000;
        public delegate void BossDead();
        public static event BossDead bossDead;

        public const byte BossDeadEventCode = 1;

        // Start is called before the first frame update
        void Start()
        {
            ai = GetComponent<NavMeshAgent>();
            target = GameObject.FindWithTag("Player").transform;
            health = 10;
            healthBar.maxValue = 1000;
            healthBar.value = 10;
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
            if (isGameOver == true || photonView.IsMine == false) return;

            Vector3 direction = target.position - transform.position;
            GameObject clone = PhotonNetwork.Instantiate(projectile.name, transform.position, Quaternion.identity);
            clone.GetComponent<Rigidbody>().AddForce(direction * 50, ForceMode.Acceleration);
        }

        public void TakeDamage()
        {
            photonView.RPC(nameof(TakeDamageRPC), RpcTarget.AllBuffered);
        }

        [PunRPC]
        public void TakeDamageRPC()
        {
            health--;
            healthBar.value--;

            if (health <= 0 && bossDead != null)
            {
                bossDead();
                //RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All }; // You would have to set the Receivers to All in order to receive this event on the local client as well
                //PhotonNetwork.RaiseEvent(BossDeadEventCode, null, raiseEventOptions, SendOptions.SendReliable);

                NetworkPlayerManager.AddPoints(5000);

                if (Player.Instance != null)
                    Player.Instance.cubesEliminated++;

                PhotonNetwork.Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {

        }
    }
}
