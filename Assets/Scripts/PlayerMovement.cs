using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Com.MorganHouston.ZombCube
{

    public class PlayerMovement : MonoBehaviour
    {

        #region Variables

        private CharacterController controller;
        private Vector3 playerVelocity = Vector3.zero;
        [SerializeField]
        private bool groundedPlayer;
        private float vertical;
        private float horizontal;
        private float playerSpeed = 20.0f;
        private float jumpHeight = 1.7f;
        private float gravityValue = -20f;
        private float jumpTime = 1f;
        private float jumpTimer;
        private bool canJump = true;
        private bool hasJumped = false;

        public float PlayerSpeed { get { return playerSpeed; } set { playerSpeed = value; } }


        #endregion


        #region MonoBehavior Methods

        private void Start()
        {
            jumpTimer = 0.0f;
            controller = GetComponent<CharacterController>();
        }

        void Update()
        {
            MovePlayer();

            JumpPlayer();

            ApplyGravity();
        }

        /// <summary>
        /// Sets the groundedPlayer boolean to true, when the player is on the ground.
        /// </summary>
        /// <param name="other">What the player is colliding with.</param>
        private void OnTriggerStay(Collider other)
        {
            if (other.gameObject.layer == 7)
                groundedPlayer = true;
        }

        /// <summary>
        /// Sets the groundedPlayer boolean to false, when player is not on the ground.
        /// </summary>
        /// <param name="other">What the player has stopped colliding with.</param>
        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.layer == 7)
                groundedPlayer = false;
        }


        #endregion


        #region Private Methods

        /// <summary>
        /// Determines jump logic: when a player can jump, resets player veolicity when grounded, decreases jump timer, and applies velocity to make player jump.
        /// </summary>
        private void JumpPlayer()
        {
            // Resets player velocity when grounded.
            if (groundedPlayer && playerVelocity.y < 0)
            {
                playerVelocity.y = -2f;
            }

            // Decreases jump timer when player can not jump.
            if (!canJump)
            {
                jumpTimer -= Time.deltaTime;
            }

            // When timer is up (reaches 0), sets timer to 0 and canJump to true. Else timer is greater than 0, set canJump to false.
            if (jumpTimer <= 0)
            {
                jumpTimer = 0;
                canJump = true;
            }
            else
            {
                canJump = false;
            }

            // Changes the height position of the player when the player can jump, has jumped, and was also on the ground.
            if (hasJumped && groundedPlayer && canJump)
            {
                playerVelocity.y += Mathf.Sqrt(jumpHeight * -2.0f * gravityValue);
                jumpTimer = jumpTime;
                canJump = false;
                groundedPlayer = false;
            }
        }

        /// <summary>
        /// Applies gravity to the player
        /// </summary>
        private void ApplyGravity()
        {
            playerVelocity.y += gravityValue * Time.deltaTime;
            controller.Move(playerVelocity * Time.deltaTime);
        }

        /// <summary>
        /// Moves the player locally depending on which way he is facing.
        /// </summary>
        private void MovePlayer()
        {
            
            Vector3 move = transform.forward * vertical + transform.right * horizontal;
            controller.Move(move * Time.deltaTime * PlayerSpeed);
        }


        #endregion


        #region Public Methods


        /// <summary>
        /// Gets Input from user on Move action and assigns to float variables, horizontal and vertical respectfully.
        /// </summary>
        /// <param name="context"></param>
        public void Move(InputAction.CallbackContext context)
        {
            MoveInput(context.ReadValue<Vector2>());
                
        }

        /// <summary>
        /// Gets input when player performs Jump action and assigns value to hasJumped.
        /// </summary>
        /// <param name="context"></param>
        public void Jump(InputAction.CallbackContext context)
        {
            JumpInput(context.ReadValueAsButton());
        }

        public void MoveInput(Vector2 newMoveDirection)
        {
            horizontal = Mathf.Clamp(newMoveDirection.x, -1, 1);
            vertical = Mathf.Clamp(newMoveDirection.y, -1, 1);
        }

        public void JumpInput(bool newValue)
        {
            hasJumped = newValue;
        }

        #endregion


    }

}
