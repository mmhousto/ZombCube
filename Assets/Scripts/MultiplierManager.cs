using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Com.GCTC.ZombCube
{
    public class MultiplierManager : MonoBehaviour
    {

        private TextMeshProUGUI multiplierLabel;
        private int multiplier;
        // Start is called before the first frame update
        void Start()
        {
            multiplierLabel = GetComponent<TextMeshProUGUI>();
            multiplier = Projectile.pointsToAdd / 10;

            if (multiplier == 1) multiplierLabel.enabled = false;
        }

        // Update is called once per frame
        void Update()
        {
            multiplier = Projectile.pointsToAdd / 10;
            if(multiplier > 1)
            {
                multiplierLabel.enabled = true;
                multiplierLabel.text = $"x{multiplier}";
            }
            else
            {
                multiplierLabel.enabled = false;
            }
        }
    }
}
