using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace Com.GCTC.ZombCube
{
    public class ProjectileSniper : Projectile
    {
        private int objectsHit = 0;

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
            RaycastHit hit;


            // Cast a sphere wrapping projectile 3 meters forward
            // to see if it is about to hit anything.
            if (Physics.SphereCast(transform.position, .1f, transform.forward, out hit, 5))
            {
                CheckHitArmor(hit);

                CheckHitEnemy(hit);

                CheckHitPlayer(hit);

                CheckHitShield(hit);

                CheckHitShielded(hit);

                if (objectsHit > 5) { DestroyProjectile(); }
            }
        }

        private void CheckHitEnemy(RaycastHit hit)
        {
            if (hit.transform.CompareTag("Enemy"))
            {
                HitEnemy(hit);
                
                SpawnPowerup(hit.transform.position);

            }
        }

        private void CheckHitPlayer(RaycastHit hit)
        {
            if (hit.transform.CompareTag("Player") && this.photonView == null && GameManager.Instance?.numOfPlayers == 1)
            {
                ovAudioSource.clip = clips[0];
                ovAudioSource.Play();
            }
            else if (hit.transform.CompareTag("Player") && this.photonView == null && GameManager.Instance?.numOfPlayers > 1)
            {
                ovAudioSource.clip = clips[couchCoopManager.GetPlayerIndex(hit.transform.parent.gameObject)];
                ovAudioSource.Play();
            }
            else if (hit.transform.CompareTag("Player") && this.photonView.IsMine)
            {
                ovAudioSource.clip = clips[NetworkGameManager.players.IndexOf(hit.transform.gameObject)];
                ovAudioSource.Play();
            }
        }

        private void CheckHitArmor(RaycastHit hit)
        {
            if (hit.transform.CompareTag("Armor"))
            {
                audioSource.Play();
                if ((SceneLoader.GetCurrentScene().name == "GameScene" || SceneLoader.GetCurrentScene().name == "Display"))
                    Destroy(hit.transform.gameObject);
                objectsHit++;
            }
        }

        protected void CheckHitShield(RaycastHit hit)
        {
            if (hit.transform.CompareTag("Shield"))
            {
                HitEnemy(hit);
            }
        }

        protected void CheckHitShielded(RaycastHit hit)
        {
            if (hit.transform.CompareTag("Shielded") && hit.transform.GetComponent<ShieldedCube>().shield == null)
            {
                HitEnemy(hit);

                SpawnPowerup(hit.transform.position);
            }
        }

        void HitEnemy(RaycastHit hit)
        {
            audioSource.Play();

            CheckForCubeDestroyerAchievements();

            objectsHit++;

            if (SceneLoader.GetCurrentScene().name == "GameScene" || SceneLoader.GetCurrentScene().name == "Display")
            {

                Destroy(hit.transform.gameObject);
                PlayerManager.AddPoints(pointsToAdd);
                if (Player.Instance != null)
                    Player.Instance.cubesEliminated++;
            }
            else if (this.photonView.IsMine)
            {
                NetworkPlayerManager.AddPoints(pointsToAdd);
                if (Player.Instance != null)
                    Player.Instance.cubesEliminated++;
                hit.transform.GetComponent<NetworkEnemy>().DestroyEnemyCall();
            }
        }
    }
}
