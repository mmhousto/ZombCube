using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Com.MorganHouston.Imprecision
{
    public class ShowSliderValue : MonoBehaviour
    {
        private float sliderValue;
        private Slider slider;
        public string sliderName;
        public TextMeshProUGUI sliderValueLabel;


        // Start is called before the first frame update
        void Start()
        {
            sliderValue = PlayerPrefs.GetFloat(sliderName, 20);
            slider = GetComponent<Slider>();
            slider.value = sliderValue;
            if(sliderValueLabel != null)
                sliderValueLabel.text = slider.value.ToString();
        }

        void Update()
        {
            if(sliderValueLabel != null && sliderValueLabel.text != slider.value.ToString())
            {
                sliderValueLabel.text = slider.value.ToString();
            }
                
        }
    }
}
