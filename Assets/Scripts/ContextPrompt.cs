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
        private PlayerInput playerInput;
        private Image image;
        // Start is called before the first frame update
        void Start()
        {
            if (PlayerInput.all.Count > 0)
                playerInput = PlayerInput.all[0];
            image = GetComponent<Image>();

            if (playerInput != null)
            {
                switch (playerInput.currentControlScheme)
                {
                    case "KeyboardMouse":
                        if (image.sprite != pc)
                            image.sprite = pc;
                        break;
                    case "Gamepad":
                        if (image.sprite != xbox)
                            image.sprite = xbox;
                        break;
                    case "Touch":
                        if (image.sprite != null)
                            image.sprite = null;
                        break;
                    default:
                        if (image.sprite != pc)
                            image.sprite = pc;
                        break;
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (playerInput == null && PlayerInput.all.Count > 0)
                playerInput = PlayerInput.all[0];

            if(playerInput != null)
            {
                switch (playerInput.currentControlScheme)
                {
                    case "KeyboardMouse":
                        if (image.sprite != pc)
                            image.sprite = pc;
                        break;
                    case "Gamepad":
                        if (image.sprite != xbox)
                            image.sprite = xbox;
                        break;
                    case "Touch":
                        if (image.sprite != null)
                            image.sprite = null;
                        break;
                    default:
                        if (image.sprite != pc)
                            image.sprite = pc;
                        break;
                }
            }
            
        }
    }
}
