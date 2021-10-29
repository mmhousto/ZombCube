using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using MLAPI;

public class MouseLook : NetworkBehaviour
{

    [SerializeField]private Vector2 mouseSensitivity;
    [SerializeField]private float pitch, yaw, maxVerticalAngle;
    private float yInput, xInput;
    private float xRotation = 0f;

    public Transform playerBody;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        if (SceneLoader.GetCurrentScene() == "GameScene" || IsLocalPlayer)
        {
            yInput = pitch * mouseSensitivity.y * Time.deltaTime;
            xInput = yaw * mouseSensitivity.x * Time.deltaTime;

            xRotation -= yInput;
            xRotation = ClampVerticalAngle(xRotation);

            transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            playerBody.Rotate(Vector3.up * xInput);
        }
    }

    public void Look(InputAction.CallbackContext context)
    {

        if (SceneLoader.GetCurrentScene() == "GameScene" || IsLocalPlayer)
        {
            pitch = context.ReadValue<Vector2>().y;
            yaw = context.ReadValue<Vector2>().x;
        }
    }

    private float ClampVerticalAngle(float angle)
    {
        return Mathf.Clamp(angle, -maxVerticalAngle, maxVerticalAngle);
    }

}
