using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace Com.MorganHouston.ZombCube
{

    public class Projectile : MonoBehaviourPun
    {
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnCollisionEnter(Collision collision)
        {
            if(SceneLoader.GetCurrentScene().name == "GameScene")
            {
                if (collision.gameObject.tag == "Enemy")
                {
                    Destroy(collision.gameObject);
                    PlayerManager.AddPoints(10);
                }
                Destroy(gameObject);
            }else
            {
                if (collision.gameObject.tag == "Enemy")
                {
                    if(PhotonNetwork.IsMasterClient)
                        PhotonNetwork.Destroy(collision.gameObject);
                    PlayerManager.AddPoints(10);
                }
                if (photonView.IsMine)
                    PhotonNetwork.Destroy(gameObject);
            }
            
        }
    }

}
