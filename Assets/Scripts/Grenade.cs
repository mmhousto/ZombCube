using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    public class Grenade : Projectile
    {

        public GameObject grenadePS;
        public float timeTicked;

        // Start is called before the first frame update
        void Start()
        {
            float tickTime = 4 - timeTicked;
            enemiesHit = 0;
            Invoke(nameof(GrenadeGoBoom), tickTime);
            audioSource = GetComponent<AudioSource>();
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
            grenadePS.SetActive(true);
            grenadePS.transform.SetParent(null);
            DestroyProjectile();
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
