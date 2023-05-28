using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    public class FlashingDestroy : MonoBehaviour
    {
        public float destroyTime = 30f;
        public float flashTime = 5f;
        public float flashInterval = 0.5f;
        public float maxIntensity = 1f;

        private bool isFlashing;
        private Renderer[] objectRenderers;
        private Color[] originalColors;
        private float[] originalIntensities;
        private float flashTimer;

        private void Start()
        {
            CollectRenderers(transform);

            originalColors = new Color[objectRenderers.Length];
            originalIntensities = new float[objectRenderers.Length];
            flashTimer = flashTime;

            for (int i = 0; i < objectRenderers.Length; i++)
            {
                Material material = objectRenderers[i].material;
                originalColors[i] = material.GetColor("_EmissionColor");
                originalIntensities[i] = material.GetFloat("_EmissionIntensity");

                // Enable emission if it's not already enabled
                if (!material.IsKeywordEnabled("_EMISSION"))
                {
                    material.EnableKeyword("_EMISSION");
                }
            }

            Invoke("DestroyObject", destroyTime);
        }

        private void Update()
        {
            if (flashTimer <= flashTime)
            {
                flashTimer += Time.deltaTime;

                if (flashTimer > flashTime - flashInterval)
                {
                    ToggleFlash();
                }
            }
        }

        private void ToggleFlash()
        {
            isFlashing = !isFlashing;

            for (int i = 0; i < objectRenderers.Length; i++)
            {
                Material material = objectRenderers[i].material;

                if (material.IsKeywordEnabled("_EMISSION"))
                {
                    if (isFlashing)
                    {
                        material.SetColor("_EmissionColor", Color.red);
                        material.SetFloat("_EmissionIntensity", maxIntensity);
                    }
                    else
                    {
                        material.SetColor("_EmissionColor", originalColors[i]);
                        material.SetFloat("_EmissionIntensity", originalIntensities[i]);
                    }
                }
            }
        }

        private void DestroyObject()
        {
            Destroy(gameObject);
        }

        private void CollectRenderers(Transform parent)
        {
            objectRenderers = parent.GetComponentsInChildren<Renderer>(true);
        }
    }
}
