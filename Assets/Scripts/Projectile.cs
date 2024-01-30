using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace Com.GCTC.ZombCube
{

    public class Projectile : MonoBehaviourPun
    {
        public static int pointsToAdd = 10;
        protected int enemiesHit = 0;
        public GameObject[] powerUpPrefabs;
        public float dropChance = 0.5f;
        protected AudioSource audioSource;
        protected AudioSource ovAudioSource;
        public AudioClip[] clips;
        protected CouchCoopManager couchCoopManager;

        private void Start()
        {
            enemiesHit = 0;
            Invoke(nameof(DestroyProjectile), 3f);
            audioSource = GetComponent<AudioSource>();
            ovAudioSource = GameObject.FindWithTag("OVAudio")?.GetComponent<AudioSource>();

            if(SceneLoader.GetCurrentScene().name == "GameScene" & GameManager.Instance?.numOfPlayers > 1)
            {
                couchCoopManager = GameObject.Find("CoopManager").GetComponent<CouchCoopManager>();
            }
        }

        protected void DestroyProjectile()
        {
            if (SceneLoader.GetCurrentScene().name == "GameScene" || SceneLoader.GetCurrentScene().name == "Display")
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
            if (collision.gameObject.CompareTag("Armor"))
            {
                audioSource.Play();
                if((SceneLoader.GetCurrentScene().name == "GameScene" || SceneLoader.GetCurrentScene().name == "Display"))
                    Destroy(collision.gameObject);
            }

            if (collision.gameObject.tag == "Enemy")
            {
                audioSource.Play();
                if (SceneLoader.GetCurrentScene().name == "GameScene" || SceneLoader.GetCurrentScene().name == "Display")
                {

                    Destroy(collision.gameObject);
                    PlayerManager.AddPoints(pointsToAdd);
                    if (Player.Instance != null)
                        Player.Instance.cubesEliminated++;
                }
                else if (this.photonView.IsMine)
                {
                    NetworkPlayerManager.AddPoints(pointsToAdd);
                    if (Player.Instance != null)
                        Player.Instance.cubesEliminated++;

                }

                CheckForCubeDestroyerAchievements();

                SpawnPowerup(collision.transform.position);
            }

            if (collision.gameObject.tag == "Player" && this.photonView == null && GameManager.Instance?.numOfPlayers == 1)
            {
                ovAudioSource.clip = clips[0];
                ovAudioSource.Play();
            }else if (collision.gameObject.tag == "Player" && this.photonView == null && GameManager.Instance?.numOfPlayers > 1)
            {
                ovAudioSource.clip = clips[couchCoopManager.GetPlayerIndex(collision.transform.parent.gameObject)];
                ovAudioSource.Play();
            }
            else if (collision.gameObject.tag == "Player" && this.photonView.IsMine)
            {
                ovAudioSource.clip = clips[NetworkGameManager.players.IndexOf(collision.gameObject)];
                ovAudioSource.Play();
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

        protected void SpawnPowerup(Vector3 pos)
        {
            int rand = Random.Range(0, powerUpPrefabs.Length);
            float randChance = Random.value;

            // Check if power-up should be spawned based on drop chance
            if (randChance <= dropChance && SceneLoader.GetCurrentScene().name == "GameScene")
            {
                // Instantiate the power-up prefab at the enemy's position
                Instantiate(powerUpPrefabs[rand], new Vector3(pos.x, 2, pos.z), Quaternion.identity);
            }else if (randChance <= dropChance && SceneLoader.GetCurrentScene().name == "NetworkGameScene" && this.photonView.IsMine)
            {
                PhotonNetwork.Instantiate(powerUpPrefabs[rand].name, new Vector3(pos.x, 2, pos.z), Quaternion.identity);
            }
        }

        protected void CheckForCubeDestroyerAchievements()
        {
            if (Social.localUser.authenticated && Player.Instance != null)
            {
                if (Player.Instance.cubesEliminated >= 10_000)
                {
                    LeaderboardManager.UnlockCubeDestroyerI();
                }

                if (Player.Instance.cubesEliminated >= 100_000)
                {
                    LeaderboardManager.UnlockCubeDestroyerII();
                }

                if (Player.Instance.cubesEliminated >= 1_000_000)
                {
                    LeaderboardManager.UnlockCubeDestroyerIII();
                }
            }
        }

    }

}
