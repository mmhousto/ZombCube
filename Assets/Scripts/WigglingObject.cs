using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    public class WigglingObject : MonoBehaviour
    {
        public float wiggleSpeed = 1f;   // Speed of the wiggle
        public float wiggleHeight = 0.5f; // Height of the wiggle

        private Vector3 initialPosition;
        private float time;

        private void Start()
        {
            initialPosition = transform.position;
        }

        private void Update()
        {
            time += Time.deltaTime * wiggleSpeed;
            float yOffset = Mathf.Sin(time) * wiggleHeight;
            transform.position = initialPosition + new Vector3(0f, yOffset, 0f);
        }
    }
}
