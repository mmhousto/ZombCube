using UnityEngine;
using Photon.Pun;

namespace StarterAssets
{
    public class UICanvasControllerInput : MonoBehaviourPun
    {

        [Header("Output")]
        public Com.MorganHouston.ZombCube.PlayerMovement playerMovement;
        public Com.MorganHouston.ZombCube.MouseLook playerLook;
        public Com.MorganHouston.ZombCube.ShootProjectile playerFire;

        private GameObject[] players;
        private GameObject currentPlayer;
        private int myID = PhotonNetwork.LocalPlayer.ActorNumber;

        private void Awake()
        {
            if (!photonView.IsMine && Com.MorganHouston.ZombCube.SceneLoader.GetCurrentScene().name == "NetworkGameScene")
            {
                Destroy(this.gameObject);
            }
        }

        private void Start()
        {
            if(Com.MorganHouston.ZombCube.SceneLoader.GetCurrentScene().name == "NetworkGameScene")
            {
                players = GameObject.FindGameObjectsWithTag("Player");
                foreach(GameObject player in players)
                {
                    int playerID = player.GetComponent<PhotonView>().Owner.ActorNumber;
                    if(playerID == myID)
                    {
                        currentPlayer = player;
                    }
                }
                playerMovement = currentPlayer.GetComponent<Com.MorganHouston.ZombCube.PlayerMovement>();
                playerFire = currentPlayer.GetComponent<Com.MorganHouston.ZombCube.ShootProjectile>();
                playerLook = currentPlayer.GetComponentInChildren<Com.MorganHouston.ZombCube.MouseLook>();
            }
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

}
