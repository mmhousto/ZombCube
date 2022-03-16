using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace Com.MorganHouston.ZombCube
{

    public class Projectile : MonoBehaviourPun
    {

        private void Start()
        {
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
            if (SceneLoader.GetCurrentScene().name == "GameScene")
            {
                if (collision.gameObject.tag == "Enemy")
                {
                    Destroy(collision.gameObject);
                    PlayerManager.AddPoints(10);
                }
            }
            else
            {
                if (this.photonView.IsMine)
                {
                    if (collision.gameObject.tag == "Enemy")
                    {
                        NetworkPlayerManager.AddPoints(10);
                    }
                }
            }
                    
            
        }

    }

}
