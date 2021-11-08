using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Com.MorganHouston.ZombCube
{

    public class PointsCollected : MonoBehaviour
    {

        private TextMeshProUGUI pointsCollected;

        // Start is called before the first frame update
        void Start()
        {
            pointsCollected = GetComponent<TextMeshProUGUI>();
            pointsCollected.text = "Score: " + PlayerManager.currentPoints.ToString();
        }

        // Update is called once per frame
        void Update()
        {
            pointsCollected.text = "Score: " + PlayerManager.currentPoints.ToString();
        }
    }

}
