using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

namespace Com.MorganHouston.ZombCube
{

    public class NetworkMouseLook : MonoBehaviourPun
    {

        [SerializeField] private Vector2 mouseSensitivity;
        [SerializeField] private float pitch, yaw, maxVerticalAngle;
        private float yInput, xInput;
        private float xRotation = 0f;

        private Camera cam;

        public Transform playerBody;

        // Start is called before the first frame update
        void Start()
        {
            
            if (!photonView.IsMine)
            {
                cam = GetComponent<Camera>();
                cam.gameObject.SetActive(false);
                return;
            }
            Cursor.lockState = CursorLockMode.Locked;
        }

        // Update is called once per frame
        void Update()
        {
            LookAround();
            
        }

        public void LookAround()
        {
            if (photonView.IsMine)
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
            pitch = context.ReadValue<Vector2>().y;
            yaw = context.ReadValue<Vector2>().x;
        }

        private float ClampVerticalAngle(float angle)
        {
            return Mathf.Clamp(angle, -maxVerticalAngle, maxVerticalAngle);
        }

    }

}
