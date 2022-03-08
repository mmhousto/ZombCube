using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.MorganHouston.ZombCube
{

    public static class PreferencesManager
    {

        public static void SetMasterVolume(float soundLevel)
        {
            PlayerPrefs.SetFloat("MasterVolume", soundLevel);
        }

        public static void SetMusicVolume(float soundLevel)
        {
            PlayerPrefs.SetFloat("MusicVolume", soundLevel);
        }

        public static float GetMasterVolume()
        {
            return PlayerPrefs.GetFloat("MasterVolume", 1);
        }

        public static float GetMusicVolume()
        {
            return PlayerPrefs.GetFloat("MusicVolume", 1);
        }

        public static void SetHorizontalSens(float sensitivity)
        {
            PlayerPrefs.SetFloat("HorizontalSens", sensitivity);
        }

        public static void SetVerticalSens(float sensitivity)
        {
            PlayerPrefs.SetFloat("VerticalSens", sensitivity);
        }

        public static float GetHorizontalSens()
        {
            return PlayerPrefs.GetFloat("HorizontalSens", 20);
        }

        public static float GetVerticalSens()
        {
            return PlayerPrefs.GetFloat("VerticalSens", 20);
        }

    }

}
