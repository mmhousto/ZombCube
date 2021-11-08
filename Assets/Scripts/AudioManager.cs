using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Com.MorganHouston.ZombCube
{

    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;

        public static AudioManager Instance { get { return _instance; } }

        public AudioMixer masterMixer;
        public Slider musicSlider, masterSlider;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                _instance = this;
            }
            DontDestroyOnLoad(this.gameObject);
        }

        private void Start()
        {
            masterMixer.SetFloat("MasterVol", PreferencesManager.GetMasterVolume());
            masterMixer.SetFloat("MusicVol", PreferencesManager.GetMusicVolume());

            if (musicSlider)
                musicSlider.value = PreferencesManager.GetMusicVolume();

            if (masterSlider)
                masterSlider.value = PreferencesManager.GetMasterVolume();
        }

        public void ChangeSoundVolume(float soundLevel)
        {
            masterMixer.SetFloat("MasterVol", soundLevel);
            PreferencesManager.SetMasterVolume(soundLevel);
        }

        public void ChangeMusicVolume(float soundLevel)
        {
            masterMixer.SetFloat("MusicVol", soundLevel);
            PreferencesManager.SetMusicVolume(soundLevel);
        }
    }
}
