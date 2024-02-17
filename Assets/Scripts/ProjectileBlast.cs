using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    public class ProjectileBlast : Projectile
    {
        // Start is called before the first frame update
        void Start()
        {
            enemiesHit = 0;
            Invoke(nameof(DestroyProjectile), 3f);
            audioSource = GetComponent<AudioSource>();
            ovAudioSource = GameObject.FindWithTag("OVAudio")?.GetComponent<AudioSource>();

            if (SceneLoader.GetCurrentScene().name == "GameScene" & GameManager.Instance?.numOfPlayers > 1)
            {
                couchCoopManager = GameObject.Find("CoopManager").GetComponent<CouchCoopManager>();
            }
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Armor"))
            {
                audioSource.Play();
                if ((SceneLoader.GetCurrentScene().name == "GameScene" || SceneLoader.GetCurrentScene().name == "Display"))
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

                enemiesHit++;

                if (enemiesHit >= 5 && Social.localUser.authenticated)
                {
                    LeaderboardManager.UnlockRicochetKing();
                }

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
            else if (collision.gameObject.tag == "Player" && this.photonView.IsMine)
            {
                ovAudioSource.clip = clips[NetworkGameManager.players.IndexOf(collision.gameObject)];
                ovAudioSource.Play();
            }

            DestroyProjectile();
        }
    }
}
