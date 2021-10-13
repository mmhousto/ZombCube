using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;

    public static AudioManager Instance { get { return _instance; } }

    public AudioMixer masterMixer;
    public Slider musicSlider, masterSlider;

    private void Awake()
    {
        if(_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        } else
        {
            _instance = this;
        }
        DontDestroyOnLoad(this.gameObject);
    }

    private void Start()
    {
        masterMixer.SetFloat("MasterVol", DataManager.Instance.GetMasterVolume());
        masterMixer.SetFloat("MusicVol", DataManager.Instance.GetMusicVolume());

        if(musicSlider)
            musicSlider.value = DataManager.Instance.GetMusicVolume();

        if(masterSlider)
            masterSlider.value = DataManager.Instance.GetMasterVolume();
    }

    public void ChangeSoundVolume(float soundLevel)
    {
        masterMixer.SetFloat("MasterVol", soundLevel);
        PlayerPrefs.SetFloat("MasterVolume", soundLevel);
    }

    public void ChangeMusicVolume(float soundLevel)
    {
        masterMixer.SetFloat("MusicVol", soundLevel);
        PlayerPrefs.SetFloat("MusicVolume", soundLevel);
    }
}
