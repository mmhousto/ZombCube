using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    public class SettingsManager : MonoBehaviour
    {

        public GameObject[] audioSettings;
        public GameObject[] gameSettings;
        public GameObject[] visualSettings;
        public GameObject swipeToLook;


        public void SetAudio()
        {
            foreach (GameObject setting in audioSettings)
            {
                setting.SetActive(true);
            }

            foreach (GameObject setting in gameSettings)
            {
                setting.SetActive(false);
            }

            foreach (GameObject setting in visualSettings)
            {
                setting.SetActive(false);
            }
        }

        public void SetGame()
        {
            foreach (GameObject setting in audioSettings)
            {
                setting.SetActive(false);
            }

            foreach (GameObject setting in gameSettings)
            {
                setting.SetActive(true);
            }

            foreach (GameObject setting in visualSettings)
            {
                setting.SetActive(false);
            }

#if (UNITY_IOS || UNITY_ANDROID)
            swipeToLook.SetActive(true);
#else
            swipeToLook.SetActive(false);
#endif
        }

        public void SetVisual()
        {
            foreach (GameObject setting in audioSettings)
            {
                setting.SetActive(false);
            }

            foreach (GameObject setting in gameSettings)
            {
                setting.SetActive(false);
            }

            foreach (GameObject setting in visualSettings)
            {
                setting.SetActive(true);
            }
        }
    }
}
