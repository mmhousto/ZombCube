using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    public class NetworkSpectatorManager : MonoBehaviour
    {
        public static GameObject showNextPlayerText;
        public static List<Camera> playerCams = new List<Camera>();
        public static Camera prevCam;
        public static int currentCam;
        public static bool isAlive = true;

        private void Start()
        {
            showNextPlayerText = GameObject.FindWithTag("SpectatorLabel");
            showNextPlayerText.SetActive(false);

        }

        private void Update()
        {
            if(NetworkGameManager.Instance.IsGameOver() == true && showNextPlayerText.activeInHierarchy)
            {
                showNextPlayerText.SetActive(false);
            }

            if(Camera.main == null && playerCams.Count > 0)
            {
                playerCams[0].enabled = true;
                currentCam = 0;
                prevCam = playerCams[0];
                showNextPlayerText.SetActive(true);
            }
        }

        private void OnDisable()
        {
            playerCams.Clear();
        }

        public static void ActivateSpectatorCamera(Camera playerCam)
        {
            isAlive = false;
            for(int i = 0; i < playerCams.Count; i++)
            {
                if(playerCam == playerCams[i])
                {
                    playerCams.RemoveAt(i);
                }
            }

            if(playerCams.Count > 0)
            {
                playerCams[0].enabled = true;
                currentCam = 0;
                prevCam = playerCams[0];
                showNextPlayerText.SetActive(true);
            }
            
        }

        public static void ShowNextPlayerCam()
        {
            if(prevCam != null && playerCams[currentCam] != null)
            {
                prevCam.enabled = false;
                prevCam = playerCams[currentCam];
                currentCam++;
            }
            else
            {
                return;
            }
            
            if(currentCam <= playerCams.Count-1 && playerCams.Count > 0)
            {
                playerCams[currentCam].enabled = true;
            }
            else if(playerCams.Count > 0)
            {
                currentCam = 0;
                playerCams[currentCam].enabled = true;
            }
            
        }

    }
}
