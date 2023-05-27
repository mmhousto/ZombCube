using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace Com.GCTC.ZombCube
{

    public class Projectile : MonoBehaviourPun
    {

        int enemiesHit = 0;
        public GameObject powerUpPrefab;
        public float dropChance = 0.5f;
        private AudioSource audioSource;

        private void Start()
        {
            enemiesHit = 0;
            Invoke(nameof(DestroyProjectile), 3f);
            audioSource = GetComponent<AudioSource>();
        }

        private void DestroyProjectile()
        {
            if (SceneLoader.GetCurrentScene().name == "GameScene")
            {
                Destroy(gameObject);
            }
            else if (this.photonView.IsMine)
            {
                PhotonNetwork.Destroy(this.gameObject);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.tag == "Enemy")
            {
                audioSource.Play();
                if (SceneLoader.GetCurrentScene().name == "GameScene")
                {

                    Destroy(collision.gameObject);
                    PlayerManager.AddPoints(10);
                    if(Player.Instance != null)
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

        }

        private void OnCollisionExit(Collision collision)
        {
            if (collision.gameObject.CompareTag("Enemy"))
            {
                enemiesHit++;

                if (enemiesHit == 5 && Social.localUser.authenticated)
                {
                    LeaderboardManager.UnlockRicochetKing();
                }
            }
        }

        private void SpawnPowerup(Vector3 pos)
        {
            // Check if power-up should be spawned based on drop chance
            if (Random.value <= dropChance && SceneLoader.GetCurrentScene().name == "GameScene")
            {
                // Instantiate the power-up prefab at the enemy's position
                Instantiate(powerUpPrefab, new Vector3(pos.x, 2, pos.z), Quaternion.identity);
            }else if (Random.value <= dropChance && SceneLoader.GetCurrentScene().name == "NetworkGameScene" && this.photonView.IsMine)
            {
                PhotonNetwork.Instantiate("TripleShotPowerup", new Vector3(pos.x, 2, pos.z), Quaternion.identity);
            }
        }

        private void CheckForCubeDestroyerAchievements()
        {
            if (Social.localUser.authenticated && Player.Instance != null)
            {
                if (Player.Instance.cubesEliminated == 10_000)
                {
                    LeaderboardManager.UnlockCubeDestroyerI();
                }

                if (Player.Instance.cubesEliminated == 100_000)
                {
                    LeaderboardManager.UnlockCubeDestroyerII();
                }

                if (Player.Instance.cubesEliminated == 1_000_000)
                {
                    LeaderboardManager.UnlockCubeDestroyerIII();
                }
            }
        }

    }

}
