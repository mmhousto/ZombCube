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
            if (SceneLoader.GetCurrentScene().name == "GameScene" || SceneLoader.GetCurrentScene().name == "Display" || SceneLoader.GetCurrentScene().name == "MainMenu")
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
            CheckHitArmor(collision);

            CheckHitEnemy(collision);

            CheckHitPlayer(collision);

            CheckHitShield(collision);

            //CheckHitShielded(collision);
            
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

        protected void CheckHitEnemy(Collision collision)
        {

            if (collision.transform.name.Contains("Duped") && SceneLoader.GetCurrentScene().name == "MainMenu" && collision.gameObject.tag == "Enemy")
            {
                audioSource.Play();
                Destroy(collision.gameObject);
            }
            else if (SceneLoader.GetCurrentScene().name == "MainMenu" && collision.transform.name.Contains("Shielded"))
            {
                audioSource.Play();
                collision.gameObject.SetActive(false);
                return;
            }
            else if (collision.transform.name.Contains("Shielded") && collision.transform.TryGetComponent(out ShieldedCube shielded))
            {

                if (shielded != null && shielded.shield == null)
                {
                    HitEnemy(collision);

                    SpawnPowerup(collision.transform.position);
                }

                return;
            }
            else if (collision.transform.name.Contains("Shielded") && collision.transform.TryGetComponent(out NetworkShieldedCube networkShielded))
            {
                if (networkShielded != null && networkShielded.shield == null)
                {
                    HitEnemy(collision);

                    SpawnPowerup(collision.transform.position);
                }
                return;
            }
            else if (SceneLoader.GetCurrentScene().name == "MainMenu" && collision.gameObject.tag == "Enemy")
            {
                audioSource.Play();
                collision.gameObject.SetActive(false);
            }
            else if (collision.gameObject.tag == "Enemy")
            {
                HitEnemy(collision);

                SpawnPowerup(collision.transform.position);
            }
        }

        protected void CheckHitPlayer(Collision collision)
        {
            if (SceneLoader.GetCurrentScene().name == "MainMenu" && collision.gameObject.tag == "Player")
            {
                
                return;
            }
            if (collision.gameObject.tag == "Player" && this.photonView == null && GameManager.Instance?.numOfPlayers == 1)
            {
                ovAudioSource.clip = clips[0];
                ovAudioSource.Play();
            }
            else if (collision.gameObject.tag == "Player" && this.photonView == null && GameManager.Instance?.numOfPlayers > 1)
            {
                ovAudioSource.clip = clips[couchCoopManager.GetPlayerIndex(collision.transform.parent.gameObject)];
                ovAudioSource.Play();
            }
            else if (collision.gameObject.tag == "Player" && this.photonView != null && this.photonView.IsMine)
            {
                ovAudioSource.clip = clips[Random.Range(0, clips.Length)];
                ovAudioSource.Play();
            }
        }

        protected void CheckHitArmor(Collision collision)
        {
            if (collision.gameObject.CompareTag("Armor"))
            {
                audioSource.Play();
                if (SceneLoader.GetCurrentScene().name == "GameScene" || SceneLoader.GetCurrentScene().name == "Display")
                    Destroy(collision.gameObject);
                else if (SceneLoader.GetCurrentScene().name == "MainMenu")
                    collision.gameObject.SetActive(false);
            }
        }

        protected void CheckHitShield(Collision collision)
        {
            if (SceneLoader.GetCurrentScene().name == "MainMenu" && collision.gameObject.CompareTag("Shield"))
            {
                audioSource.Play();
                collision.gameObject.SetActive(false);
                return;
            }else if (collision.gameObject.CompareTag("Shield"))
            {
                HitEnemy(collision);
            }
        }

        protected void CheckHitShielded(Collision collision)
        {
            if (SceneLoader.GetCurrentScene().name == "MainMenu" && collision.transform.name.Contains("Shielded"))
            {
                audioSource.Play();
                collision.gameObject.SetActive(false);
                return;
            }
            else if (collision.transform.name.Contains("Shielded") && collision.transform.TryGetComponent(out ShieldedCube shielded))
            {

                if (shielded != null && shielded.shield == null)
                {
                    HitEnemy(collision);

                    SpawnPowerup(collision.transform.position);
                }


            }
            else if (collision.transform.CompareTag("Shielded") && collision.transform.TryGetComponent(out NetworkShieldedCube networkShielded))
            {
                if (networkShielded != null && networkShielded.shield == null)
                {
                    HitEnemy(collision);

                    SpawnPowerup(collision.transform.position);
                }
            }
        }

        private void HitEnemy(Collision collision)
        {
            audioSource.Play();
            
            if (SceneLoader.GetCurrentScene().name == "GameScene" || SceneLoader.GetCurrentScene().name == "Display")
            {

                if (collision.transform.name.Contains("Dupe") && !collision.transform.name.Contains("Duped"))
                {
                    collision.gameObject.GetComponent<DupeCube>().Dupe();
                }

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
                if (collision.transform.name.Contains("Dupe") && !collision.transform.name.Contains("Duped"))
                {
                    collision.gameObject.GetComponent<NetworkDupeCube>().Dupe();

                }
            }

            CheckForCubeDestroyerAchievements();

            enemiesHit++;

            if (enemiesHit >= 5 && Social.localUser.authenticated)
            {
                LeaderboardManager.UnlockRicochetKing();
            }
        }

    }

}
