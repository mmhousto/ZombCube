using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

namespace Com.MorganHouston.ZombCube
{

    public class NetworkMouseLook : MonoBehaviourPun
    {
        private NetworkPlayerManager playerManager;
        [SerializeField] private Vector2 mouseSensitivity;
        [SerializeField] private float pitch, yaw, maxVerticalAngle;
        private float yInput, xInput;
        private float xRotation = 0f;

        private Camera cam;

        public GameObject minimapCamera;

        public Transform playerBody;

        // Start is called before the first frame update
        void Start()
        {
            
            if (!this.photonView.IsMine)
            {
                cam = GetComponent<Camera>();
                Destroy(minimapCamera);
                cam.enabled = false;
                return;
            }
            playerManager = GetComponentInParent<NetworkPlayerManager>();

            mouseSensitivity.x = PreferencesManager.GetHorizontalSens();
            mouseSensitivity.y = PreferencesManager.GetVerticalSens();
        }

        // Update is called once per frame
        void Update()
        {
            UpdateMouseSensitivity();
            LookAround();
            
        }

        public void LookAround()
        {
            if (this.photonView.IsMine && playerManager.isInputDisabled == false)
            {
                yInput = pitch * mouseSensitivity.y * Time.deltaTime;
                xInput = yaw * mouseSensitivity.x * Time.deltaTime;

                xRotation -= yInput;
                xRotation = ClampVerticalAngle(xRotation);

                transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
                playerBody.Rotate(Vector3.up * xInput);
            }
        }

        public void OnLook(InputValue value)
        {
            LookInput(value.Get<Vector2>());
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
            if (photonView.IsMine)
            {
                if (mouseSensitivity.x != PreferencesManager.GetHorizontalSens())
                    mouseSensitivity.x = PreferencesManager.GetHorizontalSens();
                if (mouseSensitivity.y != PreferencesManager.GetVerticalSens())
                    mouseSensitivity.y = PreferencesManager.GetVerticalSens();
            }
            
        }

    }

}
