using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace Com.GCTC.ZombCube
{

    public class Projectile : MonoBehaviourPun
    {

        int enemiesHit = 0;
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
                    Player.Instance.cubesEliminated++;
                }
                else if (this.photonView.IsMine)
                {
                    NetworkPlayerManager.AddPoints(10);
                    Player.Instance.cubesEliminated++;

                }
                CheckForCubeDestroyerAchievements();
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

        private void CheckForCubeDestroyerAchievements()
        {
            if (Social.localUser.authenticated)
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
