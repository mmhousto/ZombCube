using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    public class AuraLight : MonoBehaviour
    {
        public Light pointLight;
        public float minIntensity = 1f;
        public float maxIntensity = 3f;
        public float speed = 1f;

        private float targetIntensity;
        private float currentIntensity;

        private void Start()
        {
            // Set initial target intensity
            targetIntensity = maxIntensity;
        }

        private void Update()
        {
            // Update the current intensity towards the target intensity
            currentIntensity = Mathf.MoveTowards(currentIntensity, targetIntensity, speed * Time.deltaTime);

            // Update the light's intensity
            pointLight.intensity = currentIntensity;

            // Check if the current intensity has reached the target intensity
            if (currentIntensity == targetIntensity)
            {
                // Switch the target intensity between min and max values
                if (targetIntensity == maxIntensity)
                    targetIntensity = minIntensity;
                else
                    targetIntensity = maxIntensity;
            }
        }
    }
}
