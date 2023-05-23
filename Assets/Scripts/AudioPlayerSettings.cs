using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Com.GCTC.ZombCube
{

    public class AudioPlayerSettings : MonoBehaviour
    {
        private AudioMixer masterMixer;
        public Slider musicSlider, masterSlider, sfxSlider;
        public Toggle swipeToggle, shadowsToggle;

        private void Start()
        {
            musicSlider.value = PreferencesManager.GetMusicVolume();
            masterSlider.value = PreferencesManager.GetMasterVolume();
            sfxSlider.value = PreferencesManager.GetSFXVolume();
            swipeToggle.isOn = Convert.ToBoolean(PreferencesManager.GetSwipeToLook());
            
        }

        private void OnEnable()
        {
            musicSlider.onValueChanged.AddListener(ChangeMusicVolume);
            masterSlider.onValueChanged.AddListener(ChangeSoundVolume);
            sfxSlider.onValueChanged.AddListener(ChangeSFXVolume);
            musicSlider.value = PreferencesManager.GetMusicVolume();
            masterSlider.value = PreferencesManager.GetMasterVolume();
            sfxSlider.value = PreferencesManager.GetSFXVolume();
            swipeToggle.isOn = Convert.ToBoolean(PreferencesManager.GetSwipeToLook());
        }

        private void OnDisable()
        {
            musicSlider.onValueChanged.RemoveListener(ChangeMusicVolume);
            masterSlider.onValueChanged.RemoveListener(ChangeSoundVolume);
            sfxSlider.onValueChanged.RemoveListener(ChangeSFXVolume);
        }

        public void ChangeSoundVolume(float soundLevel)
        {
            AudioManager.Instance.masterMixer.SetFloat("MasterVol", soundLevel);
            PreferencesManager.SetMasterVolume(soundLevel);
        }

        public void ChangeMusicVolume(float soundLevel)
        {
            AudioManager.Instance.masterMixer.SetFloat("MusicVol", soundLevel);
            PreferencesManager.SetMusicVolume(soundLevel);
        }

        public void ChangeSFXVolume(float soundLevel)
        {
            AudioManager.Instance.masterMixer.SetFloat("SFXVol", soundLevel);
            PreferencesManager.SetSFXVolume(soundLevel);
        }

        public void ChangeSwipeToggle(bool input)
        {
            PreferencesManager.SetSwipeToLook(input);
        }

        public void ChangeShadowToggle(bool input)
        {
            PreferencesManager.SetShadows(input);
        }


    }
}
