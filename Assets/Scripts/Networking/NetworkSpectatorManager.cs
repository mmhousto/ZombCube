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
        public GameObject eliminatedCamera;

        private void Awake()
        {
            isAlive = true;
            playerCams.Add(eliminatedCamera.GetComponent<Camera>());
            currentCam = 0;
            prevCam = playerCams[0];
            eliminatedCamera.SetActive(false);
        }

        private void Start()
        {
            showNextPlayerText = GameObject.FindWithTag("SpectatorLabel");
            showNextPlayerText.SetActive(false);

        }

        private void Update()
        {
            if ((NetworkGameManager.Instance.IsGameOver() == true || isAlive) && showNextPlayerText.activeInHierarchy)
            {
                showNextPlayerText.SetActive(false);
            }

            if (NetworkGameManager.Instance.IsGameOver() == false && isAlive == false && showNextPlayerText.activeInHierarchy == false)
            {
                showNextPlayerText.SetActive(true);
            }

            if (Camera.main == null && isAlive == false)
            {
                /*if (playerCams[0] != null)
                {
                    playerCams[0].enabled = true;
                    
                }*/
                currentCam = 0;
                prevCam = playerCams[0];
                eliminatedCamera.SetActive(true);

            }

            if (isAlive == true && playerCams[currentCam] != null && playerCams[currentCam].isActiveAndEnabled)
            {
                Destroy(playerCams[currentCam].GetComponent<AudioListener>());
                playerCams[currentCam].enabled = false;
            }
        }

        private void OnDisable()
        {
            playerCams.Clear();
            currentCam = 0;
        }

        public static void ActivateSpectatorCamera(Camera playerCam)
        {
            isAlive = false;
            for (int i = 0; i < playerCams.Count; i++)
            {
                if (playerCam == playerCams[i])
                {
                    playerCams.RemoveAt(i);
                }

            }


        }

        public static void ShowNextPlayerCam()
        {
            Debug.Log("Current Cam: " + currentCam);
            int i = 0;
            foreach (Camera cam in playerCams)
            {
                Debug.Log(cam + " " + i);
                i++;
            }
            // Disables current cam
            if (playerCams[currentCam] != null)
            {
                Destroy(playerCams[currentCam].GetComponent<AudioListener>());
                playerCams[currentCam].enabled = false;
            }

            // Increases cam
            currentCam++;

            // Checks if cam is still in array bounds, if not sets to eliminated camera
            if (currentCam >= playerCams.Count) currentCam = 0;

            // Enables new camera if not null
            if (playerCams[currentCam] != null)
            {
                playerCams[currentCam].gameObject.AddComponent<AudioListener>();
                playerCams[currentCam].enabled = true;
            }

            else
                ShowNextPlayerCam();

            /*if(playerCams[currentCam] != null)
            {
                prevCam.enabled = false;
                prevCam = playerCams[currentCam];

                if (currentCam < playerCams.Count - 1)
                    currentCam++;
                else
                    currentCam = 0;
            }
            else
            {
                currentCam = 0;
                prevCam = playerCams[currentCam];
                playerCams[currentCam].enabled = true;
            }
            
            if (playerCams[currentCam] != null && currentCam <= playerCams.Count-1 && playerCams.Count > 0)
            {
                playerCams[currentCam].enabled = true;
            }
            else if(playerCams.Count > 0)
            {
                currentCam = 0;
                prevCam = playerCams[currentCam];
                playerCams[currentCam].enabled = true;
            }
            */
        }

    }
}
