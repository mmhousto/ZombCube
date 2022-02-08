using UnityEngine;
using Photon.Pun;
using Com.MorganHouston.ZombCube;


    public class UICanvasControllerInput : MonoBehaviourPun
    {

        [Header("Output")]
        public NetworkPlayerMovement playerMovement;
        public NetworkMouseLook playerLook;
        public NetworkShootProjectile playerFire;

        private GameObject currentPlayer;

        private void Awake()
        {
            if (!photonView.IsMine && SceneLoader.GetCurrentScene().name == "NetworkGameScene")
            {
                Destroy(this.gameObject);
            }
        }

        public void GetPlayer(GameObject player)
        {
            currentPlayer = player;
            Debug.Log(currentPlayer);
            playerMovement = currentPlayer.GetComponent<NetworkPlayerMovement>();
            playerFire = currentPlayer.GetComponent<NetworkShootProjectile>();
            playerLook = currentPlayer.GetComponentInChildren<NetworkMouseLook>();
            Debug.Log(playerMovement);
            Debug.Log(playerFire);
            Debug.Log(playerLook);
    }

        public void VirtualMoveInput(Vector2 virtualMoveDirection)
        {
            playerMovement.MoveInput(virtualMoveDirection);
        }

        public void VirtualLookInput(Vector2 virtualLookDirection)
        {
            playerLook.LookInput(virtualLookDirection);
        }

        public void VirtualJumpInput(bool virtualJumpState)
        {
            playerMovement.JumpInput(virtualJumpState);
        }

        public void VirtualFireInput(bool virtualFireState)
        {
            playerFire.FireInput(virtualFireState);
        }
        
    }


