using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using MLAPI;
using MLAPI.Messaging;

public class ShootProjectile : NetworkBehaviour
{
    public Transform firePosition;
    private bool isFiring;
    private bool canFire = true;
    private float fireTime = 0f;
    private float fireRate = 1.5f;
    private float launchVelocity = 5000f;

    public GameObject projectile;

    [SerializeField] public GameObject networkProjectile;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (SceneLoader.GetCurrentScene() == "GameScene" || IsLocalPlayer)
        {
            fireTime -= Time.deltaTime;

            if (fireTime <= 0)
            {
                canFire = true;
                fireTime = 0;
            }
            else
            {
                canFire = false;
            }

            if (isFiring & canFire && SceneLoader.GetCurrentScene() == "GameScene")
            {
                SpawnProjectile();
                fireTime = fireRate;
            }
            else if (isFiring & canFire && SceneLoader.GetCurrentScene() == "NetworkGameScene")
            {
                SpawnProjectileServerRpc();
                fireTime = fireRate;
            }
        }
    }

    public void Fire(InputAction.CallbackContext context)
    {
        if (SceneLoader.GetCurrentScene() == "GameScene" || IsLocalPlayer)
        {
            isFiring = context.ReadValueAsButton();
        }
    }

    private void SpawnProjectile()
    {
        GameObject clone = Instantiate(projectile, firePosition.position, firePosition.rotation);
        clone.GetComponent<Rigidbody>().AddRelativeForce(new Vector3(0, 0, launchVelocity));
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnProjectileServerRpc()
    {
        GameObject clone = Instantiate(networkProjectile, firePosition.position, firePosition.rotation);
        clone.GetComponent<NetworkObject>().Spawn();
        clone.GetComponent<Rigidbody>().AddRelativeForce(new Vector3(0, 0, launchVelocity));
    }
}
