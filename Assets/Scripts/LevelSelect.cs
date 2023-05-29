using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Com.GCTC.ZombCube
{
    public class LevelSelect : MonoBehaviour
    {
        public GameObject soloButton;
        public Toggle[] levelToggles;
        public static int levelSelected = 0;

        private void Start()
        {
            levelSelected = 0;
            levelToggles[levelSelected].interactable = false;
        }

        public void SelectLevel(int level)
        {
            levelToggles[levelSelected].isOn = false;
            levelToggles[levelSelected].interactable = true;
            levelSelected = level;
            levelToggles[levelSelected].interactable = false;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(soloButton);
        }


    }
}
