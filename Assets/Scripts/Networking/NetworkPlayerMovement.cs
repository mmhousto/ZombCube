using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

namespace Com.MorganHouston.ZombCube
{

    public class NetworkPlayerMovement : MonoBehaviourPun
    {
        private CharacterController controller;
        private Rigidbody rb;
        private Vector3 playerVelocity = Vector3.zero;
        private bool groundedPlayer;
        private float vertical;
        private float horizontal;
        private float rotationSpeed = 100f;
        private float playerSpeed = 20.0f;
        private float jumpHeight = .6f;
        private float gravityValue = -20f;
        private bool hasJumped = false;

        private void Start()
        {
            if (photonView.IsMine == false && PhotonNetwork.IsConnected == true)
            {
                return;
            }

            controller = GetComponent<CharacterController>();

        }

        void Update()
        {
            MovePlayer();

            
            
        }

        private void MovePlayer()
        {
            if (photonView.IsMine)
            {
                if (groundedPlayer && playerVelocity.y < 0)
                {
                    playerVelocity.y = 0f;
                }

                Vector3 move = transform.forward * vertical + transform.right * horizontal;
                controller.Move(move * Time.deltaTime * playerSpeed);

                //transform.Rotate(Vector3.up * horizontal * rotationSpeed * Time.deltaTime);

                // Changes the height position of the player..
                if (hasJumped && groundedPlayer)
                {
                    playerVelocity.y += jumpHeight;
                }

                playerVelocity.y += gravityValue * Time.deltaTime;
                controller.Move(playerVelocity * Time.deltaTime);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (!photonView.IsMine)
            {
                return;
            }
            if (other.tag == "Ground")
                groundedPlayer = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!photonView.IsMine)
            {
                return;
            }
            if (other.tag == "Ground")
                groundedPlayer = false;
        }

        /// <summary>
        /// Gets Input from user.
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
