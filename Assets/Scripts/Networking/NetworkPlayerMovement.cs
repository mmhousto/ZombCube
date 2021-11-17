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

        private void Awake()
        {
            if (!this.photonView.IsMine && this != null)
            {
                Destroy(this);
                Destroy(GetComponent<PlayerInput>());
            }
        }

        private void Start()
        {
            if (this.photonView.IsMine == false && PhotonNetwork.IsConnected == true)
            {
                return;
            }

            if(this.photonView.IsMine)
                controller = GetComponent<CharacterController>();

        }

        void Update()
        {
            MovePlayer();

            
            
        }

        private void MovePlayer()
        {
            if (this.photonView.IsMine)
            {
                if (groundedPlayer && playerVelocity.y < 0)
                {
                    playerVelocity.y = -2f;
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
            if (!this.photonView.IsMine)
            {
                return;
            }
            if (other.CompareTag("Ground"))
                groundedPlayer = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!this.photonView.IsMine)
            {
                return;
            }
            if (other.CompareTag("Ground"))
                groundedPlayer = false;
        }

        /// <summary>
        /// Gets Input from user.
        /// </summary>
        /// <param name="context"></param>
        public void Move(InputAction.CallbackContext context)
        {
            if (this.photonView.IsMine)
            {
                horizontal = context.ReadValue<Vector2>().x;
                vertical = context.ReadValue<Vector2>().y;

                Debug.Log("Horizontal: " + horizontal);
                Debug.Log("Vertical: " + vertical);
            }
            

        }

        public void Jump(InputAction.CallbackContext context)
        {
            if (this.photonView.IsMine)
            {
                hasJumped = context.ReadValueAsButton();
            }
            
        }

    }

}
