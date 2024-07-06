using System;
using UnityEngine;

namespace UnityEngine.InputSystem.PS5.ControllerSample
{
    [Serializable]
    public struct ButtonImages
    {
        [Header("D-Pad")]
        public SpriteRenderer dPadUp;
        public SpriteRenderer dPadLeft;
        public SpriteRenderer dPadDown;
        public SpriteRenderer dPadRight;

        [Header("Buttons")]
        public SpriteRenderer triangle;
        public SpriteRenderer circle;
        public SpriteRenderer square;
        public SpriteRenderer cross;

        [Header("Sticks")]
        public SpriteRenderer leftStick;
        public SpriteRenderer rightStick;

        [Header("Triggers")]
        public SpriteRenderer l1;
        public SpriteRenderer l2;
        public SpriteRenderer r1;
        public SpriteRenderer r2;

        [Header("Extra Buttons")]
        public SpriteRenderer share;
        public SpriteRenderer options;
        public SpriteRenderer playstation;

        [Header("Touchpad")]
        public SpriteRenderer touchpad;
        public SpriteRenderer touch0;
        public SpriteRenderer touch1;
    }

    [Serializable]
    public struct LightbarVisuals
    {
        public void SetColor(Color color)
        {
            image.color = color;
            light.color = color;
        }

        [SerializeField] SpriteRenderer image;
        [SerializeField] Light light;
    }
}
