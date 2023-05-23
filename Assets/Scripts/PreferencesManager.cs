using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.GCTC.ZombCube
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

        public static void SetSFXVolume(float soundLevel)
        {
            PlayerPrefs.SetFloat("SFXVolume", soundLevel);
        }

        public static float GetSFXVolume()
        {
            return PlayerPrefs.GetFloat("SFXVolume", 1);
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

        public static void SetSwipeToLook(bool input)
        {
            PlayerPrefs.SetInt("SwipeToLook", input ? 1 : 0);
        }

        public static int GetSwipeToLook()
        {
            return PlayerPrefs.GetInt("SwipeToLook", 0);
        }

        public static void SetShadows(bool input)
        {
            PlayerPrefs.SetInt("Shadows", input ? 1 : 0);
        }

        public static int GetShadows()
        {
            return PlayerPrefs.GetInt("Shadows", 0);
        }

    }

}
