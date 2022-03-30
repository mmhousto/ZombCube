using UnityEngine;
using Com.MorganHouston.ZombCube;

namespace StarterAssets
{
    public class UICanvasControllerInput : MonoBehaviour
    {

        [Header("Output")]
        private NetworkPlayerMovement playerMovementN;
        private NetworkMouseLook playerLookN;
        private NetworkShootProjectile playerFireN;
        private NetworkPlayerManager playerManagerN;
        public GameManager gameManager;
        public PlayerMovement playerMovement;
        public MouseLook playerLook;
        public ShootProjectile playerFire;

        private GameObject currentPlayer;

        public void GetPlayer(GameObject player)
        {
            currentPlayer = player;
            playerMovementN = currentPlayer.GetComponent<NetworkPlayerMovement>();
            playerFireN = currentPlayer.GetComponent<NetworkShootProjectile>();
            playerLookN = currentPlayer.GetComponentInChildren<NetworkMouseLook>();
            playerManagerN = currentPlayer.GetComponent<NetworkPlayerManager>();
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

        public void VirtualPauseInput(bool virtualPauseState)
        {
            if (SceneLoader.GetCurrentScene().name == "NetworkGameScene")
            {
                playerManagerN.PauseInput(virtualPauseState);
            }
            else
            {
                gameManager.PauseInput(virtualPauseState);
            }

        }

        public void VirtualFireInput(bool virtualFireState)
        {
            if (SceneLoader.GetCurrentScene().name == "NetworkGameScene")
            {
                playerFireN.FireInput(virtualFireState);
            }
            else
            {
                playerFire.FireInput(virtualFireState);
            }
        }

    }
}

