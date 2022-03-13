using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Com.MorganHouston.ZombCube
{

    public class AudioPlayerSettings : MonoBehaviour
    {
        private AudioMixer masterMixer;
        public Slider musicSlider, masterSlider;

        private void Start()
        {
            if (masterMixer == null)
                masterMixer = AudioManager.Instance.masterMixer;
            musicSlider.value = PreferencesManager.GetMusicVolume();
            masterSlider.value = PreferencesManager.GetMasterVolume();
        }

        private void OnEnable()
        {
            musicSlider.onValueChanged.AddListener(ChangeMusicVolume);
            masterSlider.onValueChanged.AddListener(ChangeSoundVolume);
            if(masterMixer == null)
                masterMixer = AudioManager.Instance.masterMixer;
            musicSlider.value = PreferencesManager.GetMusicVolume();
            masterSlider.value = PreferencesManager.GetMasterVolume();
        }

        private void OnDisable()
        {
            musicSlider.onValueChanged.RemoveListener(ChangeMusicVolume);
            masterSlider.onValueChanged.RemoveListener(ChangeSoundVolume);
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
