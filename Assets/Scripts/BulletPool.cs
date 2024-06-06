using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Photon.Pun;

namespace Com.GCTC.ZombCube
{
    public class BulletPool : MonoBehaviourPun
    {
        public static BulletPool instance;

        public Projectile bulletPrefab;
        public ObjectPool<Projectile> bulletPool;

        public ProjectileBlast bulletBlastPrefab;
        public ObjectPool<ProjectileBlast> bulletBlastPool;

        private List<Projectile> bulletList = new List<Projectile>();

        private int spawnAmount = 20;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                instance = this;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            bulletPool = new ObjectPool<Projectile>(CreateBullet, OnGet, OnRelease, OnEnd, false, spawnAmount, 40);
            bulletBlastPool = new ObjectPool<ProjectileBlast>(CreateBulletBlast, OnGet, OnRelease, OnEnd, true, spawnAmount, 40);
        }

        private Projectile CreateBullet()
        {
            Projectile projectile = null;
            if (SceneLoader.GetCurrentScene().name == "GameScene" || SceneLoader.GetCurrentScene().name == "MainMenu" || SceneLoader.GetCurrentScene().name == "Display")
                projectile = Instantiate(bulletPrefab);
            else if (SceneLoader.GetCurrentScene().name == "NetworkGameScene")
                projectile = PhotonNetwork.Instantiate("NetworkProjectile", transform.position, transform.rotation).GetComponent<Projectile>();

            bulletList.Add(projectile);
            return projectile;
        }

        private ProjectileBlast CreateBulletBlast()
        {
            ProjectileBlast projectile = null;
            if (SceneLoader.GetCurrentScene().name == "GameScene" || SceneLoader.GetCurrentScene().name == "MainMenu" || SceneLoader.GetCurrentScene().name == "Display")
                projectile = Instantiate(bulletBlastPrefab);
            else if (SceneLoader.GetCurrentScene().name == "NetworkGameScene")
                projectile = PhotonNetwork.Instantiate("NetworkProjectileBlast", transform.position, transform.rotation).GetComponent<ProjectileBlast>();
            return projectile;
        }

        private void OnGet(Projectile bullet)
        {
            if (SceneLoader.GetCurrentScene().name == "GameScene" || SceneLoader.GetCurrentScene().name == "MainMenu" || SceneLoader.GetCurrentScene().name == "Display")
            {
                bullet.gameObject.SetActive(true);
            }
            else if (SceneLoader.GetCurrentScene().name == "NetworkGameScene")
                photonView.RPC(nameof(OnGetRPC), RpcTarget.AllBuffered, bulletList.IndexOf(bullet));
        }

        private void OnRelease(Projectile bullet)
        {
            if (SceneLoader.GetCurrentScene().name == "GameScene" || SceneLoader.GetCurrentScene().name == "MainMenu" || SceneLoader.GetCurrentScene().name == "Display")
            {
                bullet.GetComponent<Rigidbody>().velocity = Vector3.zero;
                bullet.gameObject.SetActive(false);
            }
            else if (SceneLoader.GetCurrentScene().name == "NetworkGameScene")
                photonView.RPC(nameof(OnReleaseRPC), RpcTarget.AllBuffered, bulletList.IndexOf(bullet));
        }

        private void OnEnd(Projectile bullet)
        {
            if (SceneLoader.GetCurrentScene().name == "GameScene" || SceneLoader.GetCurrentScene().name == "MainMenu" || SceneLoader.GetCurrentScene().name == "Display")
                Destroy(bullet);
            else if (SceneLoader.GetCurrentScene().name == "NetworkGameScene")
                photonView.RPC(nameof(OnEndRPC), RpcTarget.AllBuffered, bulletList.IndexOf(bullet));
        }

        [PunRPC]
        public void OnGetRPC(int bullet)
        {
            Projectile b = bulletList[bullet];
            b.GetComponent<Rigidbody>().velocity = Vector3.zero;
            b.gameObject.SetActive(true);
        }

        [PunRPC]
        public void OnReleaseRPC(int bullet)
        {
            Projectile b = bulletList[bullet];
            b.GetComponent<Rigidbody>().velocity = Vector3.zero;
            b.gameObject.SetActive(false);
        }

        [PunRPC]
        public void OnEndRPC(int bullet)
        {
            Projectile b = bulletList[bullet];
            PhotonNetwork.Destroy(b.gameObject);
        }
    }
}
