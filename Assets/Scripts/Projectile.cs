using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace Com.MorganHouston.ZombCube
{

    public class Projectile : MonoBehaviourPun
    {

        private void OnCollisionEnter(Collision collision)
        {
            if (SceneLoader.GetCurrentScene().name == "GameScene")
            {
                if (collision.gameObject.tag == "Enemy")
                {
                    Destroy(collision.gameObject);
                    PlayerManager.AddPoints(10);
                }
                Destroy(gameObject);
            }
            else
            {
                if (this.photonView.IsMine)
                {
                    if (collision.gameObject.tag == "Enemy")
                    {
                        NetworkPlayerManager.AddPoints(10);

                    }
                    PhotonNetwork.Destroy(this.gameObject);
                }
            }
                    
            
        }

    }

}
