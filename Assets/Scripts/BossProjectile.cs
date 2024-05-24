using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    public class BossProjectile : MonoBehaviourPun
    {
        public LayerMask playerLayer;

        AudioSource audioSource;

        // Start is called before the first frame update
        void Start()
        {
            audioSource = GetComponent<AudioSource>();
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnCollisionEnter(Collision collision)
        {
            Explode();
            audioSource.Play();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(transform.position, 8f);
        }

        void Explode()
        {
            RaycastHit[] hits;
            hits = Physics.SphereCastAll(transform.position, 8f, Vector3.forward, 0f, playerLayer);
            List<GameObject> hitPlayers = new List<GameObject>();
            int i = 0;

            foreach (RaycastHit hit in hits)
            {
                Transform hitPlayer = hit.transform;
                if (hitPlayer.CompareTag("Player") && SceneLoader.GetCurrentScene().name == "GameScene" || SceneLoader.GetCurrentScene().name == "Display" || SceneLoader.GetCurrentScene().name == "MainMenu")
                {
                    if (hitPlayers.Contains(hitPlayer.gameObject) || hitPlayer.parent != null && hitPlayers.Contains(hitPlayer.parent.gameObject))
                        continue;

                    if (hitPlayer.TryGetComponent(out PlayerManager pM))
                    {
                        hitPlayers.Add(pM.gameObject);
                        pM.Damage(20);
                    }
                    else if (hitPlayer.parent.TryGetComponent(out PlayerManager ppM))
                    {
                        hitPlayers.Add(ppM.gameObject);
                        ppM.Damage(20);
                    }
                        
                }
                else if (hitPlayer.TryGetComponent(out NetworkPlayerManager playerManager) && this.photonView.IsMine && !hitPlayers.Contains(hitPlayer.gameObject))
                {
                    hitPlayers.Add(hitPlayer.gameObject);
                    playerManager.DamagePlayerCall(20f);
                }
            }

            hitPlayers.Clear();
            DestroyProjectile();
        }

        void DestroyProjectile()
        {
            if (SceneLoader.GetCurrentScene().name == "GameScene" || SceneLoader.GetCurrentScene().name == "Display" || SceneLoader.GetCurrentScene().name == "MainMenu")
            {
                Destroy(gameObject);
            }
            else if (this.photonView.IsMine)
            {
                PhotonNetwork.Destroy(this.gameObject);
            }
        }
    }
}
