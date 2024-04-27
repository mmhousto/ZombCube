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
        private NetworkFullyAuto smBN;
        private NetworkAB aBN;
        private NetworkShotblaster shotblasterN;
        private NetworkSniper sniperBlasterN;
        private NetworkTripleShot playerFireTripleN;
        private NetworkLaunchGrenade playerGrenadeN;
        private NetworkPlayerManager playerManagerN;

        public GameManager gameManager;
        public PlayerMovement playerMovement;
        public SwapManager swapManager;
        public MouseLook playerLook;
        public ShootProjectile playerFire;
        public FullyAuto smB;
        public AssaultBlaster aB;
        public Shotblaster shotblaster;
        public SniperBlaster sniperBlaster;
        public TripleShot playerFireTriple;
        public LaunchGrenade playerGrenade;

        private GameObject currentPlayer;

        private void Start()
        {
#if (UNITY_IOS || UNITY_ANDROID)
            if (SceneLoader.GetCurrentScene().name == "GameScene")
            {
                GetPlayer(gameManager.GetPlayer(), false);
            }
#endif
        }

        public void GetPlayer(GameObject player)
        {
            currentPlayer = player;
            playerMovementN = currentPlayer.GetComponent<NetworkPlayerMovement>();
            swapManagerN = currentPlayer.GetComponent<NetworkSwapManager>();
            playerGrenadeN = currentPlayer.GetComponent<NetworkLaunchGrenade>();
            playerFireN = currentPlayer.GetComponent<NetworkShootProjectile>();
            smBN = currentPlayer.GetComponent<NetworkFullyAuto>();
            aBN = currentPlayer.GetComponent<NetworkAB>();
            shotblasterN = currentPlayer.GetComponent<NetworkShotblaster>();
            sniperBlasterN = currentPlayer.GetComponent<NetworkSniper>();
            playerFireTripleN = currentPlayer.GetComponent<NetworkTripleShot>();
            playerLookN = currentPlayer.GetComponentInChildren<NetworkMouseLook>();
            playerManagerN = currentPlayer.GetComponent<NetworkPlayerManager>();

            if (playerFireTripleN != null)
                playerFireTripleN.enabled = false;

            if (smBN != null)
                smBN.enabled = false;

            if (aBN != null)
                aBN.enabled = false;

            if (shotblasterN != null)
                shotblasterN.enabled = false;

            if(sniperBlasterN != null)
                sniperBlasterN.enabled = false;
        }

        public void GetPlayer(GameObject player, bool online)
        {
            currentPlayer = player;
            playerMovement = currentPlayer.GetComponent<PlayerMovement>();
            swapManager = currentPlayer.GetComponent<SwapManager>();
            playerGrenade = currentPlayer.GetComponent<LaunchGrenade>();
            playerFire = currentPlayer.GetComponent<ShootProjectile>();
            smB = currentPlayer.GetComponent<FullyAuto>();
            aB = currentPlayer.GetComponent<AssaultBlaster>();
            shotblaster =currentPlayer.GetComponent<Shotblaster>();
            sniperBlaster = currentPlayer.GetComponent<SniperBlaster>();
            playerFireTriple = currentPlayer.GetComponent<TripleShot>();
            playerLook = currentPlayer.GetComponentInChildren<MouseLook>();

            if(playerFireTriple != null)
                playerFireTriple.enabled = false;

            if (smB != null)
                smB.enabled = false;

            if (aB != null)
                aB.enabled = false;

            if(shotblaster != null)
                shotblaster.enabled = false;

            if(sniperBlaster != null)
                sniperBlaster.enabled = false;
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

                if (smBN != null)
                    smBN.FireInput(virtualFireState);

                if (aBN != null)
                    aBN.FireInput(virtualFireState);

                if (shotblasterN != null)
                    shotblasterN.FireInput(virtualFireState);

                if(sniperBlasterN != null)
                    sniperBlasterN.FireInput(virtualFireState);

                if (playerFireTripleN != null)
                    playerFireTripleN.FireInput(virtualFireState);

                if (playerGrenadeN != null)
                    playerGrenadeN.FireInput(virtualFireState);
            }
            else
            {
                if (playerFire != null)
                    playerFire.FireInput(virtualFireState);

                if (smB != null)
                    smB.FireInput(virtualFireState);

                if (aB != null)
                    aB.FireInput(virtualFireState);

                if (shotblaster != null)
                    shotblaster.FireInput(virtualFireState);

                if(sniperBlaster != null)
                    sniperBlaster.FireInput(virtualFireState);

                if (playerFireTriple != null)
                    playerFireTriple.FireInput(virtualFireState);

                if(playerGrenade != null)
                    playerGrenade.FireInput(virtualFireState);
            }
        }

    }
}

