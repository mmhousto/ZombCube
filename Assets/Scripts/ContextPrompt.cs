using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Com.GCTC.ZombCube
{
    public class ContextPrompt : MonoBehaviour
    {
        public Sprite pc, xbox;
        private Image image;
        // Start is called before the first frame update
        void Start()
        {
            image = GetComponent<Image>();

            CheckControllerType();
        }

        // Update is called once per frame
        void Update()
        {
            CheckControllerType();

        }

        void CheckControllerType()
        {
            if (Gamepad.current != null)
            {
                // Any connected gamepad uses the Xbox prompts.
                if (image.sprite != xbox)
                    image.sprite = xbox;
            }
            else
            {
                if (image.sprite != pc)
                    image.sprite = pc;

#if (UNITY_IOS || UNITY_ANDROID)
                if (image.enabled == true)
                    image.enabled = false;
#endif
            }

        }
    }
}
