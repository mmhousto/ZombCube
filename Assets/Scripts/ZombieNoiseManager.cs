using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Com.GCTC.ZombCube
{
    public class ZombieNoiseManager : MonoBehaviour
    {

        public AudioClip[] clips;
        private AudioSource audioSource;

        // Start is called before the first frame update
        void Start()
        {
            audioSource = GetComponent<AudioSource>();
            Invoke(nameof(PlayRandomClip), Random.Range(3.0f, 30.0f));
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        
        void PlayRandomClip()
        {
            if(audioSource.isActiveAndEnabled)
            {
                audioSource.clip = clips[Random.Range(0, clips.Length - 1)];
                audioSource.Play();
            }
            Invoke(nameof(PlayRandomClip), Random.Range(3.5f, 20.5f));
        }
    }
}
