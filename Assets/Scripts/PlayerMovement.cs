using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Com.MorganHouston.ZombCube
{

    public class PlayerMovement : MonoBehaviour
    {
        private CharacterController controller;
        private Rigidbody rb;
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


        private void Start()
        {
            jumpTimer = 0.0f;
            controller = GetComponent<CharacterController>();
            rb = GetComponent<Rigidbody>();
        }

        void Update()
        {
            MovePlayer();

            JumpPlayer();

            ApplyGravity();
        }

        private void JumpPlayer()
        {
            if (groundedPlayer && playerVelocity.y < 0)
            {
                playerVelocity.y = 0f;
            }

            if (!canJump)
            {
                jumpTimer -= Time.deltaTime;
            }

            if (jumpTimer <= 0)
            {
                jumpTimer = 0;
                canJump = true;
            }
            else
            {
                canJump = false;
            }

            // Changes the height position of the player..
            if (hasJumped && groundedPlayer && canJump)
            {
                playerVelocity.y += Mathf.Sqrt(jumpHeight * -2.0f * gravityValue);
                jumpTimer = jumpTime;
                canJump = false;
                groundedPlayer = false;
            }
        }

        private void ApplyGravity()
        {
            playerVelocity.y += gravityValue * Time.deltaTime;
            controller.Move(playerVelocity * Time.deltaTime);
        }

        private void MovePlayer()
        {
            Vector3 move = transform.forward * vertical + transform.right * horizontal;
            controller.Move(move * Time.deltaTime * PlayerSpeed);
        }

        private void OnTriggerStay(Collider other)
        {
            if(other.gameObject.layer == 7)
                groundedPlayer = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.layer == 7)
                groundedPlayer = false;
        }

        /// <summary>
        /// Gets Input from user and assigns to float variables, horizontal and vertical respectfully.
        /// </summary>
        /// <param name="context"></param>
        public void Move(InputAction.CallbackContext context)
        {
            horizontal = context.ReadValue<Vector2>().x;
            vertical = context.ReadValue<Vector2>().y;
                
        }

        public void Jump(InputAction.CallbackContext context)
        {
            hasJumped = context.ReadValueAsButton();
        }

    }

}
