using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.ProBuilder.MeshOperations;

namespace Com.GCTC.ZombCube
{
    public class ProjectileSniper : Projectile
    {
        private int objectsHit = 0;

        // Start is called before the first frame update
        void Awake()
        {
            enemiesHit = 0;
            Invoke(nameof(DestroyProjectile), 3f);
            audioSource = GetComponent<AudioSource>();
            ovAudioSource = GameObject.FindWithTag("OVAudio")?.GetComponent<AudioSource>();

            if (SceneLoader.GetCurrentScene().name == "GameScene" & GameManager.Instance?.numOfPlayers > 1)
            {
                couchCoopManager = GameObject.Find("CoopManager").GetComponent<CouchCoopManager>();
            }

            DetectAllCollision(5);
        }

        private void OnEnable()
        {
            DetectAllCollision(5);
        }

        void Update()
        {
            DetectCollision(6);
        }

        private void DetectAllCollision(int max)
        {
            RaycastHit[] hits = Physics.SphereCastAll(transform.position - (transform.forward), .05f, transform.forward, max);

            if (hits.Length > 0)
            {
                foreach (RaycastHit hit in hits)
                {
                    CheckHitArmor(hit);

                    CheckHitEnemy(hit);

                    CheckHitPlayer(hit);

                    CheckHitShield(hit);

                    //CheckHitShielded(hit);

                }
            }
            if (objectsHit > 5) { DestroyProjectile(); }
        }

        private void DetectCollision(int max)
        {
            RaycastHit hit;

            if (Physics.SphereCast(transform.position, .05f, transform.forward, out hit, max))
            {
                CheckHitArmor(hit);

                CheckHitEnemy(hit);

                CheckHitPlayer(hit);

                CheckHitShield(hit);

                //CheckHitShielded(hit);

                if (objectsHit > 5) { DestroyProjectile(); }
            }
        }

        private void CheckHitEnemy(RaycastHit hit)
        {
            if (hit.transform.name.Contains("Shielded") && hit.transform.TryGetComponent(out ShieldedCube shielded))
            {

                if (shielded != null && shielded.shield == null)
                {
                    HitEnemy(hit);

                    SpawnPowerup(hit.transform.position);
                }


            }
            else if (hit.transform.name.Contains("Shielded") && hit.transform.TryGetComponent(out NetworkShieldedCube networkShielded))
            {
                if (networkShielded != null && networkShielded.shield == null)
                {
                    HitEnemy(hit);

                    SpawnPowerup(hit.transform.position);
                }
            }
            else if (hit.transform.CompareTag("Enemy"))
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
                else if (this.photonView.IsMine)
                {
                    hit.transform.GetComponent<NetworkArmor>().CallDestroyEnemy();
                }
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
            if(hit.transform.name.Contains("Shielded") && hit.transform.TryGetComponent(out ShieldedCube shielded))
            {

                if (shielded != null && shielded.shield == null)
                {
                    HitEnemy(hit);

                    SpawnPowerup(hit.transform.position);
                }

                
            }
            else if (hit.transform.name.Contains("Shielded") && hit.transform.TryGetComponent<NetworkShieldedCube>(out NetworkShieldedCube networkShielded))
            {
                if (networkShielded != null && networkShielded.shield == null)
                {
                    HitEnemy(hit);

                    SpawnPowerup(hit.transform.position);
                }
            }
        }

        void HitEnemy(RaycastHit hit)
        {
            audioSource.Play();

            CheckForCubeDestroyerAchievements();

            objectsHit++;

            if (SceneLoader.GetCurrentScene().name == "GameScene" || SceneLoader.GetCurrentScene().name == "Display")
            {

                if (hit.transform.TryGetComponent<DupeCube>(out DupeCube dupeCube))
                {
                    dupeCube.Dupe();
                }

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

                if (hit.transform.TryGetComponent<NetworkDupeCube>(out NetworkDupeCube dupeCube))
                {
                    dupeCube.Dupe();
                    dupeCube.CallDestroyEnemy();
                }
                else if (hit.transform.TryGetComponent<NetworkEnemy>(out NetworkEnemy enemy))
                {
                    enemy.DestroyEnemyCall();
                }
                else
                {
                    PhotonNetwork.Destroy(hit.transform.gameObject);
                }
            }
        }
    }
}
