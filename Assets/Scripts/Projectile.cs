using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.MorganHouston.ZombCube
{

    public class Projectile : MonoBehaviour
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
            if (collision.gameObject.tag == "Enemy")
            {
                Destroy(collision.gameObject);
                PlayerManager.AddPoints(10);
            }
            Destroy(gameObject);
        }
    }

}
