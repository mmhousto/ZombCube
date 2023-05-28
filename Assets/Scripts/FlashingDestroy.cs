using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    using UnityEngine;

    using UnityEngine;

    public class FlashingDestroy : MonoBehaviour
    {
        private float destroyTime = 30f;
        private float flashStartTime = 25f;
        private bool isFlashing;
        private float flashInterval = 0.25f;
        private float flashTimer;
        private Light childLight;

        private void Start()
        {
            flashTimer = flashStartTime;
            childLight = GetComponentInChildren<Light>();

            Invoke("DestroyObject", destroyTime);
            ToggleFlash();
        }

        private void Update()
        {
            flashTimer -= Time.deltaTime;

            if (flashTimer <= 0f)
            {
                ToggleFlash();
                flashTimer = flashInterval;
            }
        }

        private void ToggleFlash()
        {
            isFlashing = !isFlashing;
            SetFlashingRecursive(transform, isFlashing);

            if (childLight != null)
            {
                childLight.enabled = isFlashing;
            }
        }

        private void SetFlashingRecursive(Transform parent, bool flashing)
        {
            MeshRenderer[] meshRenderers = parent.GetComponentsInChildren<MeshRenderer>();

            foreach (MeshRenderer renderer in meshRenderers)
            {
                renderer.enabled = flashing;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                SetFlashingRecursive(child, flashing);
            }
        }

        private void DestroyObject()
        {
            Destroy(gameObject);
        }
    }



}
