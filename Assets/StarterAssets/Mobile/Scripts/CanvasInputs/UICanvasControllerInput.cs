using UnityEngine;

namespace StarterAssets
{
    public class UICanvasControllerInput : MonoBehaviour
    {

        [Header("Output")]
        public Com.MorganHouston.ZombCube.PlayerMovement playerMovement;
        public Com.MorganHouston.ZombCube.MouseLook playerLook;
        public Com.MorganHouston.ZombCube.ShootProjectile playerFire;

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

        public void VirtualFireInput(bool virtualSprintState)
        {
            playerFire.FireInput(virtualSprintState);
        }
        
    }

}
