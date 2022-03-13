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

        private NetworkPlayerManager playerManager;
        private CharacterController controller;
        private Vector3 playerVelocity = Vector3.zero;
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

            if (this.photonView.IsMine)
            {
                jumpTimer = 0.0f;
                controller = GetComponent<CharacterController>();
                playerManager = GetComponent<NetworkPlayerManager>();
            }
                

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
            if (other.gameObject.layer == 7)
                groundedPlayer = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!this.photonView.IsMine)
            {
                return;
            }
            if (other.gameObject.layer == 7)
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

                if (playerManager.isInputDisabled == false)
                {
                    Vector3 move = transform.forward * vertical + transform.right * horizontal;
                    controller.Move(move * Time.deltaTime * playerSpeed);
                }
                

                // Changes the height position of the player..
                if (hasJumped && groundedPlayer && canJump && playerManager.isInputDisabled == false)
                {
                    playerVelocity.y += Mathf.Sqrt(jumpHeight * -2.0f * gravityValue);
                    jumpTimer = jumpTime;
                    canJump = false;
                    groundedPlayer = false;
                    hasJumped = false;
                }

                playerVelocity.y += gravityValue * Time.deltaTime;
                controller.Move(playerVelocity * Time.deltaTime);
            }
        }


        #endregion


        #region Public Dynamic Methods for Input

        /// <summary>
        /// Gets Input from user on Move action and assigns to float variables, horizontal and vertical respectfully.
        /// </summary>
        /// <param name="context"></param>
        public void OnPlayerMove(InputValue value)
        {
            MoveInput(value.Get<Vector2>().x, value.Get<Vector2>().y);

        }

        /// <summary>
        /// Gets input when player performs Jump action and assigns value to hasJumped.
        /// </summary>
        /// <param name="context"></param>
        public void OnJump(InputValue value)
        {
            JumpInput(value.isPressed);
        }

        public void MoveInput(float newXDir, float newYDir)
        {
            horizontal = Mathf.Clamp(newXDir, -1, 1);
            vertical = Mathf.Clamp(newYDir, -1, 1);
        }

        public void JumpInput(bool newValue)
        {
            hasJumped = newValue;
        }


        #endregion

    }

}
