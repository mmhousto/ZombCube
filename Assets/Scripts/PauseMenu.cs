using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    public class PauseMenu : MonoBehaviour
    {
        public void ChangeHorizontalSens(float sensitivty)
        {
            PreferencesManager.SetHorizontalSens(sensitivty);
        }

        public void ChangeVerticalSens(float sensitivity)
        {
            PreferencesManager.SetVerticalSens(sensitivity);
        }
    }
}
