using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

namespace Com.MorganHouston.ZombCube
{

    public class NetworkPlayerMovement : MonoBehaviourPun
    {


        #region Private Fields 

        private CharacterController controller;
        private Vector3 playerVelocity = Vector3.zero;
        private bool groundedPlayer;
        private float vertical;
        private float horizontal;
        private float playerSpeed = 20.0f;
        private float jumpHeight = 4f;
        private float gravityValue = -20f;
        private bool hasJumped = false;


        #endregion


        #region MonoBehaviour Methods


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


        #endregion


        #region Private Methods


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
                    groundedPlayer = false;
                }

                playerVelocity.y += gravityValue * Time.deltaTime;
                controller.Move(playerVelocity * Time.deltaTime);
            }
        }


        #endregion


        #region Public Dynamic Methods for Input


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
                Debug.Log(hasJumped);
            
        }


        #endregion

    }

}
