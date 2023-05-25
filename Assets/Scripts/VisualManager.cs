using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    public class VisualManager : MonoBehaviour
    {

        public Light sun;

        // Start is called before the first frame update
        void Start()
        {
            SetShadowsOnOff();
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void SetShadowsOnOff()
        {
            if (PlayerPrefs.GetInt("Shadows", 1) == 1)
            {
                sun.shadows = LightShadows.Soft;
            }
            else
            {
                sun.shadows = LightShadows.None;
            }
        }
    }
}
