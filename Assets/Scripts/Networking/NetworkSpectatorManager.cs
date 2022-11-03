using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    public class NetworkSpectatorManager : MonoBehaviour
    {

        public static List<Camera> playerCams = new List<Camera>();
        public static Camera prevCam;
        public static int currentCam;
        public static bool isAlive = true;

        private void OnDisable()
        {
            playerCams.Clear();
        }

        public static void ActivateSpectatorCamera()
        {
            isAlive = false;
            playerCams[0].enabled = true;
            currentCam = 0;
            prevCam = playerCams[0];
        }

        public static void ShowNextPlayerCam()
        {
            prevCam.enabled = false;
            prevCam = playerCams[currentCam];
            currentCam++;

            if(currentCam <= playerCams.Count-1)
            {
                playerCams[currentCam].enabled = true;
            }
            else
            {
                currentCam = 0;
                playerCams[currentCam].enabled = true;
            }
            
        }

    }
}
