using UnityEngine;
using Com.GCTC.ZombCube;

namespace StarterAssets
{
    public class UICanvasControllerInput : MonoBehaviour
    {

        [Header("Output")]
        private NetworkPlayerMovement playerMovementN;
        private NetworkMouseLook playerLookN;
        private NetworkShootProjectile playerFireN;
        private NetworkTripleShot playerFireTripleN;
        private NetworkPlayerManager playerManagerN;

        public GameManager gameManager;
        public PlayerMovement playerMovement;
        public MouseLook playerLook;
        public ShootProjectile playerFire;
        public TripleShot playerFireTriple;

        private GameObject currentPlayer;

        public void GetPlayer(GameObject player)
        {
            currentPlayer = player;
            playerMovementN = currentPlayer.GetComponent<NetworkPlayerMovement>();
            playerFireN = currentPlayer.GetComponent<NetworkShootProjectile>();
            playerFireTripleN = currentPlayer.GetComponent<NetworkTripleShot>();
            playerLookN = currentPlayer.GetComponentInChildren<NetworkMouseLook>();
            playerManagerN = currentPlayer.GetComponent<NetworkPlayerManager>();

            playerFireTripleN.enabled = false;
        }

        public void VirtualMoveInput(Vector2 virtualMoveDirection)
        {
            if(SceneLoader.GetCurrentScene().name == "NetworkGameScene")
            {
                playerMovementN.MoveInput(virtualMoveDirection.x, virtualMoveDirection.y);
            }
            else
            {
                playerMovement.MoveInput(virtualMoveDirection);
            }
            
        }

        public void VirtualLookInput(Vector2 virtualLookDirection)
        {
            if (SceneLoader.GetCurrentScene().name == "NetworkGameScene")
            {
                playerLookN.LookInput(virtualLookDirection);
            }
            else
            {
                playerLook.LookInput(virtualLookDirection);
            }
        }

        public void VirtualJumpInput(bool virtualJumpState)
        {
            if (SceneLoader.GetCurrentScene().name == "NetworkGameScene")
            {
                playerMovementN.JumpInput(virtualJumpState);
            }
            else
            {
                playerMovement.JumpInput(virtualJumpState);
            }
            
        }

        public void VirtualPauseInput()
        {
            if (SceneLoader.GetCurrentScene().name == "NetworkGameScene")
            {
                playerManagerN.PauseInput();
            }
            else
            {
                gameManager.PauseInput();
            }

        }

        public void VirtualFireInput(bool virtualFireState)
        {
            if (SceneLoader.GetCurrentScene().name == "NetworkGameScene")
            {
                if (playerFireN.gameObject.activeInHierarchy == true)
                    playerFireN.FireInput(virtualFireState);
                else if (playerFireTripleN.gameObject.activeInHierarchy == true)
                    playerFireTripleN.FireInput(virtualFireState);
            }
            else
            {
                if (playerFire.gameObject.activeInHierarchy == true)
                    playerFire.FireInput(virtualFireState);
                else if (playerFireTriple.gameObject.activeInHierarchy == true)
                    playerFireTriple.FireInput(virtualFireState);
            }
        }

    }
}

