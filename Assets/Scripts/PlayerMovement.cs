using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool groundedPlayer;
    private float vertical;
    private float horizontal;
    private float rotationSpeed = 100f;
    private float playerSpeed = 20.0f;
    private float jumpHeight = 1.0f;
    private float gravityValue = -9.81f;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        vertical = Input.GetAxis("Vertical");
        horizontal = Input.GetAxis("Horizontal");


        if (groundedPlayer && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }

        Vector3 move = transform.forward * vertical;
        controller.Move(move * Time.deltaTime * playerSpeed);

        transform.Rotate(Vector3.up * horizontal * rotationSpeed * Time.deltaTime);

        // Changes the height position of the player..
        if (Input.GetButtonDown("Jump") && groundedPlayer)
        {
            playerVelocity.y += Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
        }

        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.tag == "Ground")
            groundedPlayer = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Ground")
            groundedPlayer = false;
    }

}
