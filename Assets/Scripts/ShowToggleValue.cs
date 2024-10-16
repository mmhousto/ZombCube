using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Com.MorganHouston.Imprecision
{
    public class ShowToggleValue : MonoBehaviour
    {

        private bool toggleValue;
        private Toggle toggle;
        public string toggleName;

        // Start is called before the first frame update
        void Start()
        {
            toggleValue = Convert.ToBoolean(PlayerPrefs.GetInt(toggleName, 0));
            toggle = GetComponent<Toggle>();
            toggle.isOn = toggleValue;
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
