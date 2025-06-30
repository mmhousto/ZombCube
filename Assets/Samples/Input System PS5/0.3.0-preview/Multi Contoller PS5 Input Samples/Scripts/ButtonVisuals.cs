using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEngine.InputSystem.PS5.ControllerSample
{
    [Serializable]
    public class ButtonVisuals
    {
        public Color inputOn = Color.white;
        public Color inputOff = Color.grey;
        public Gradient triggerGradient;
    }
}
