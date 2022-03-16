using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

namespace Com.MorganHouston.ZombCube
{

    public class MouseLook : MonoBehaviourPun
    {

        [SerializeField] private Vector2 mouseSensitivity;
        [SerializeField] private float pitch, yaw, maxVerticalAngle;
        private float yInput, xInput;
        private float xRotation = 0f;

        public Transform playerBody;

        // Start is called before the first frame update
        void Start()
        {
            mouseSensitivity.x = PreferencesManager.GetHorizontalSens();
            mouseSensitivity.y = PreferencesManager.GetVerticalSens();
        }

        // Update is called once per frame
        void Update()
        {
            UpdateMouseSensitivity();

            yInput = pitch * mouseSensitivity.y * Time.deltaTime;
            xInput = yaw * mouseSensitivity.x * Time.deltaTime;

            xRotation -= yInput;
            xRotation = ClampVerticalAngle(xRotation);

            transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            playerBody.Rotate(Vector3.up * xInput);
        }

        public void Look(InputAction.CallbackContext context)
        {
            LookInput(context.ReadValue<Vector2>());
        }

        public void LookInput(Vector2 newLookDirection)
        {
            pitch = newLookDirection.y;
            yaw = newLookDirection.x;
        }

        private float ClampVerticalAngle(float angle)
        {
            return Mathf.Clamp(angle, -maxVerticalAngle, maxVerticalAngle);
        }

        private void UpdateMouseSensitivity()
        {
            if (mouseSensitivity.x != PreferencesManager.GetHorizontalSens())
                mouseSensitivity.x = PreferencesManager.GetHorizontalSens();
            if (mouseSensitivity.y != PreferencesManager.GetVerticalSens())
                mouseSensitivity.y = PreferencesManager.GetVerticalSens();
        }

    }

}
