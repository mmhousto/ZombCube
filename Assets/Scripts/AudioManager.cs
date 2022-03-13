using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
            if (GameObject.FindWithTag("MusicSlider"))
                musicSlider = GameObject.FindWithTag("MusicSlider").GetComponent<Slider>();
            if (GameObject.FindWithTag("MasterSlider"))
                masterSlider = GameObject.FindWithTag("MasterSlider").GetComponent<Slider>();

            masterMixer.SetFloat("MasterVol", PreferencesManager.GetMasterVolume());
            masterMixer.SetFloat("MusicVol", PreferencesManager.GetMusicVolume());


        }

        private void Update()
        {
            if (GameObject.FindWithTag("MusicSlider") && musicSlider == null)
            {
                musicSlider = GameObject.FindWithTag("MusicSlider").GetComponent<Slider>();
                musicSlider.value = PreferencesManager.GetMusicVolume();
            }
                
            if (GameObject.FindWithTag("MasterSlider") && masterSlider == null)
            {
                masterSlider = GameObject.FindWithTag("MasterSlider").GetComponent<Slider>();
                masterSlider.value = PreferencesManager.GetMasterVolume();
            }
            
        }

    }
}
