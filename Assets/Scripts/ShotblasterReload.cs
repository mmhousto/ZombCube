using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    public class ShotblasterReload : MonoBehaviour
    {
        private Animator anim;
        int ammoToReload = 5;
        private float rewindDuration = 0.15f; // Set the desired rewind duration in seconds


        // Start is called before the first frame update
        void Start()
        {
            anim = GetComponent<Animator>();
        }

        public void SetAmmo(int ammoInClip) { ammoToReload = 5 - ammoInClip; }

        // Called by the Animation Event
        public void RewindAnimationClip()
        {
            // Get the current normalized time
            float currentTime = anim.GetCurrentAnimatorStateInfo(0).normalizedTime;

            // Start the coroutine to smoothly rewind
            StartCoroutine(SmoothRewind(currentTime));
        }

        public void Reload()
        {
            if (ammoToReload > 1)
            {
                ammoToReload--;

                RewindAnimationClip();
                Debug.Log("Shotblaster Reload");
            }
            else
            {
                ammoToReload--;
                Debug.Log("Shotblaster Reload 1");
            }

        }

        private IEnumerator SmoothRewind(float startNormalizedTime)
        {
            float startTime = Time.time;
            float endTime = startTime + rewindDuration;

            while (Time.time < endTime)
            {
                // Calculate the normalized time based on the elapsed time and rewind duration
                float elapsedRatio = (Time.time - startTime) / rewindDuration;
                float normalizedTime = Mathf.Lerp(startNormalizedTime, startNormalizedTime - rewindDuration, elapsedRatio);

                // Set the animator's time to the calculated time
                anim.Play(anim.GetCurrentAnimatorStateInfo(0).fullPathHash, 0, normalizedTime);

                yield return null;
            }


        }
    }
}
