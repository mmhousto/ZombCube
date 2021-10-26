using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using MLAPI;
using MLAPI.Messaging;

public class EnemyAI : NetworkBehaviour
{

    private GameObject[] targets;
    private NavMeshAgent ai;

    private Transform currentTarget;

    private float currentTargetDistance = 1000f;

    // Start is called before the first frame update
    void Start()
    {
        ai = GetComponent<NavMeshAgent>();
        if (!GameObject.FindWithTag("Player"))
        {
            targets = null;
        }else
        {
            targets = GameObject.FindGameObjectsWithTag("Player");
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameObject.FindWithTag("Player"))
        {
            targets = null;
        }
        else
        {
            targets = GameObject.FindGameObjectsWithTag("Player");
        }

        SetClosestPlayer();

        if (currentTarget)
        {
            ai.SetDestination(currentTarget.position);
            currentTargetDistance = Vector3.Distance(currentTarget.position, transform.position);
        }
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Player" && SceneLoader.GetCurrentScene() == "GameScene")
        {
            Destroy(gameObject);
            PlayerManager.Damage(20);
        } else if (collision.gameObject.tag == "Player" && SceneLoader.GetCurrentScene() == "NetworkGameScene")
        {
            DeleteEnemyServerRpc();
            PlayerManager.Damage(20);
        }
    }

    private void SetClosestPlayer()
    {
        if(targets == null) { return; }

        foreach(GameObject target in targets)
        {
            float distance = Vector3.Distance(target.transform.position, transform.position);
            if (distance < currentTargetDistance)
            {
                currentTarget = target.transform;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DeleteEnemyServerRpc()
    {
        NetworkObject.Despawn(this.gameObject);
    }
}
