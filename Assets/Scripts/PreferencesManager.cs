using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

}
