using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    public class Grenade : Projectile
    {

        public GameObject grenadePS;
        public float timeTicked;
        public float blastRadius = 5f;

        // Start is called before the first frame update
        void Start()
        {
            float tickTime = 4 - timeTicked;
            enemiesHit = 0;
            Invoke(nameof(GrenadeGoBoom), tickTime);
            //audioSource = GetComponent<AudioSource>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            /*if (collision.gameObject.tag == "Enemy")
            {
                audioSource.Play();
                if (SceneLoader.GetCurrentScene().name == "GameScene" || SceneLoader.GetCurrentScene().name == "Display")
                {

                    Destroy(collision.gameObject);
                    PlayerManager.AddPoints(10);
                    if (Player.Instance != null)
                        Player.Instance.cubesEliminated++;
                }
                else if (this.photonView.IsMine)
                {
                    NetworkPlayerManager.AddPoints(10);
                    if (Player.Instance != null)
                        Player.Instance.cubesEliminated++;

                }

                CheckForCubeDestroyerAchievements();

                SpawnPowerup(collision.transform.position);
            }

            if (collision.gameObject.tag == "Player" && this.photonView == null)
            {
                ovAudioSource.clip = clips[Random.Range(0, clips.Length)];
                ovAudioSource.Play();
            }
            else if (collision.gameObject.tag == "Player" && this.photonView.IsMine)
            {
                ovAudioSource.clip = clips[Random.Range(0, clips.Length)];
                ovAudioSource.Play();
            }*/
        }

        private void OnCollisionExit(Collision collision)
        {
            /*if (collision.gameObject.CompareTag("Enemy"))
            {
                enemiesHit++;

                if (enemiesHit == 5 && Social.localUser.authenticated)
                {
                    LeaderboardManager.UnlockRicochetKing();
                }
            }*/
        }

        private void GrenadeGoBoom()
        {
            //audioSource.Play();
            Explode();
            grenadePS.SetActive(true);
            grenadePS.transform.SetParent(null);
            DestroyProjectile();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.position, blastRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position, blastRadius/2);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position, (blastRadius / 2)/2);
        }

        void Explode()
        {
            Collider[] collidersFar = Physics.OverlapSphere(transform.position, blastRadius);
            Collider[] collidersMed = Physics.OverlapSphere(transform.position, blastRadius/2);
            Collider[] collidersDead = Physics.OverlapSphere(transform.position, (blastRadius/2)/2);

            foreach (Collider collider in collidersFar)
            {
                if (SceneLoader.GetCurrentScene().name == "GameScene" || SceneLoader.GetCurrentScene().name == "Display")
                {
                    if (collider.gameObject.tag == "Enemy")
                    {
                        if (collider.TryGetComponent(out DupeCube dupeCube))
                        {
                            dupeCube.Dupe();
                        }

                        Destroy(collider.gameObject);
                        
                        PlayerManager.AddPoints(pointsToAdd);
                        if (Player.Instance != null)
                            Player.Instance.cubesEliminated++;

                        CheckForCubeDestroyerAchievements();

                        SpawnPowerup(collider.transform.position);
                    }
                    if (collider.gameObject.tag == "Player")
                    {
                        if (collider.name == "Capsule")
                            collider.transform.parent.GetComponent<PlayerManager>().Damage(15);
                           else
                            collider.GetComponent<PlayerManager>().Damage(15);
                           
                    }
                }
                else if (this.photonView != null & this.photonView.IsMine)
                {
                    if(collider.gameObject.tag == "Enemy")
                    {
                        collider.GetComponent<PhotonView>().RequestOwnership();
                        if (collider.GetComponent<PhotonView>().IsMine)
                        {
                            PhotonNetwork.Destroy(collider.gameObject);
                        }
                        NetworkPlayerManager.AddPoints(pointsToAdd);
                        if (Player.Instance != null)
                            Player.Instance.cubesEliminated++;

                        CheckForCubeDestroyerAchievements();

                        SpawnPowerup(collider.transform.position);
                    }

                    if (collider.gameObject.tag == "Player" && collider.transform.root.GetComponent<PhotonView>().IsMine)
                    {
                        collider.transform.root.GetComponent<NetworkPlayerManager>().Damage(15);
                    }
                }
            }

            // Medium Explosion Zone
            foreach (Collider collider in collidersMed)
            {
                if (collider.gameObject.tag == "Player" && this.photonView == null)
                {
                    if (collider.name == "Capsule")
                        collider.transform.parent.GetComponent<PlayerManager>().Damage(35);
                    else
                        collider.GetComponent<PlayerManager>().Damage(35);
                }
                else if (collider.gameObject.tag == "Player" && this.photonView.IsMine)
                {
                    collider.transform.root.GetComponent<NetworkPlayerManager>().Damage(35);
                }
            }

            // Dead Explosion Zone
            foreach (Collider collider in collidersDead)
            {
                if (collider.gameObject.tag == "Player" && this.photonView == null)
                {
                    if (collider.name == "Capsule")
                        collider.transform.parent.GetComponent<PlayerManager>().Damage(50);
                    else
                        collider.GetComponent<PlayerManager>().Damage(50);
                }
                else if (collider.gameObject.tag == "Player" && this.photonView.IsMine)
                {
                    collider.transform.root.GetComponent<NetworkPlayerManager>().Damage(50);
                }
            }
        }
    }
}
