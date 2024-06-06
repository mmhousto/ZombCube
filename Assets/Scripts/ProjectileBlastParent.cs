using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    public class ProjectileBlastParent : ProjectileBlast
    {
        // Start is called before the first frame update
        void Start()
        {
            pool = BulletPool.instance;

            audioSource = GetComponent<AudioSource>();
            ovAudioSource = GameObject.FindWithTag("OVAudio")?.GetComponent<AudioSource>();

            if (SceneLoader.GetCurrentScene().name == "GameScene" & GameManager.Instance?.numOfPlayers > 1)
            {
                couchCoopManager = GameObject.Find("CoopManager").GetComponent<CouchCoopManager>();
            }
        }

        private void OnEnable()
        {
            enemiesHit = 0;
            Invoke(nameof(DestroyProjectile), 3f);
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnCollisionEnter(Collision collision)
        {
            CheckHitArmor(collision);

            CheckHitEnemy(collision);

            CheckHitPlayer(collision);

            CheckHitShield(collision);

            //CheckHitShielded(collision);

            DestroyProjectile();
        }

        protected override void DestroyProjectile()
        {
            pool.bulletBlastPool.Release(this);
        }
    }
}
