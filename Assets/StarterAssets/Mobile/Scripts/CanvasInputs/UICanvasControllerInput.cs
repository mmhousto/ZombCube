using UnityEngine;
using Com.GCTC.ZombCube;

namespace StarterAssets
{
    public class UICanvasControllerInput : MonoBehaviour
    {

        [Header("Output")]
        private NetworkPlayerMovement playerMovementN;
        private NetworkSwapManager swapManagerN;
        private NetworkMouseLook playerLookN;
        private NetworkShootProjectile playerFireN;
        private NetworkTripleShot playerFireTripleN;
        private NetworkLaunchGrenade playerGrenadeN;
        private NetworkPlayerManager playerManagerN;

        public GameManager gameManager;
        public PlayerMovement playerMovement;
        public SwapManager swapManager;
        public MouseLook playerLook;
        public ShootProjectile playerFire;
        public TripleShot playerFireTriple;
        public LaunchGrenade playerGrenade;

        private GameObject currentPlayer;

        private void Start()
        {
            GetPlayer(gameManager.GetPlayer(), false);
        }

        public void GetPlayer(GameObject player)
        {
            currentPlayer = player;
            playerMovementN = currentPlayer.GetComponent<NetworkPlayerMovement>();
            swapManagerN = currentPlayer.GetComponent<NetworkSwapManager>();
            playerGrenadeN = currentPlayer.GetComponent<NetworkLaunchGrenade>();
            playerFireN = currentPlayer.GetComponent<NetworkShootProjectile>();
            playerFireTripleN = currentPlayer.GetComponent<NetworkTripleShot>();
            playerLookN = currentPlayer.GetComponentInChildren<NetworkMouseLook>();
            playerManagerN = currentPlayer.GetComponent<NetworkPlayerManager>();

            playerFireTripleN.enabled = false;
        }

        public void GetPlayer(GameObject player, bool online)
        {
            currentPlayer = player;
            playerMovement = currentPlayer.GetComponent<PlayerMovement>();
            swapManager = currentPlayer.GetComponent<SwapManager>();
            playerGrenade = currentPlayer.GetComponent<LaunchGrenade>();
            playerFire = currentPlayer.GetComponent<ShootProjectile>();
            playerFireTriple = currentPlayer.GetComponent<TripleShot>();
            playerLook = currentPlayer.GetComponentInChildren<MouseLook>();

            playerFireTriple.enabled = false;
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

        public void VirtualSwitchInput(bool virtualJumpState)
        {
            if (SceneLoader.GetCurrentScene().name == "NetworkGameScene")
            {
                swapManagerN.SwapInput(virtualJumpState);
            }
            else
            {
                swapManager.SwapInput(virtualJumpState);
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
                if (playerFireN != null)
                    playerFireN.FireInput(virtualFireState);

                if (playerFireTripleN != null)
                    playerFireTripleN.FireInput(virtualFireState);

                if (playerGrenadeN != null)
                    playerGrenadeN.FireInput(virtualFireState);
            }
            else
            {
                if (playerFire != null)
                    playerFire.FireInput(virtualFireState);

                if (playerFireTriple != null)
                    playerFireTriple.FireInput(virtualFireState);

                if(playerGrenade != null)
                    playerGrenade.FireInput(virtualFireState);
            }
        }

    }
}

