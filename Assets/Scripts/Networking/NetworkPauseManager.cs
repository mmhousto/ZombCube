using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Com.GCTC.ZombCube
{
    public class NetworkPauseManager : MonoBehaviour
    {
        public void PauseInput()
        {
            Cursor.lockState = CursorLockMode.None;

            NetworkGameManager.Instance.PauseGame();
        }

        public void OnGamePause(InputValue value)
        {
            Cursor.lockState = CursorLockMode.None;

            NetworkGameManager.Instance.PauseGame();
        }
    }
}
