using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{

    [SerializeField]private Vector2 mouseSensitivity;
    [SerializeField]private float pitch; // y-axis
    [SerializeField]private float yaw; // x-axis
    [SerializeField]private float maxVerticalAngle;

    public Transform playerBody;

    private float xRotation = 0f;
    private Vector2 velocity;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    private void Update()
    {
        pitch *= mouseSensitivity.y * Time.deltaTime;
        yaw *= mouseSensitivity.x * Time.deltaTime;

        xRotation -= pitch;
        xRotation = ClampVerticalAngle(xRotation);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * yaw);
        

    }

    public void Look(InputAction.CallbackContext context)
    {
        pitch = context.ReadValue<Vector2>().y;
        yaw = context.ReadValue<Vector2>().x;
    }

    // Clamps angle between twgo angles.
    private float ClampVerticalAngle(float angle)
    {
        return Mathf.Clamp(angle, -maxVerticalAngle, maxVerticalAngle);
    }

}
