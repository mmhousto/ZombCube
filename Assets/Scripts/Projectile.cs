using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace Com.GCTC.ZombCube
{

    public class Projectile : MonoBehaviourPun
    {

        int enemiesHit = 0;

        private void Start()
        {
            enemiesHit = 0;
            Invoke(nameof(DestroyProjectile), 3f);
        }

        private void DestroyProjectile()
        {
            if (SceneLoader.GetCurrentScene().name == "GameScene")
            {
                Destroy(gameObject);
            }
            else
            {
                PhotonNetwork.Destroy(this.gameObject);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.tag == "Enemy")
            {

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

                if(enemiesHit == 5 && CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Google)
                {
                    LeaderboardManager.UnlockRicochetKing();
                }
            }
        }

        private void CheckForCubeDestroyerAchievements()
        {
            if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Google)
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
