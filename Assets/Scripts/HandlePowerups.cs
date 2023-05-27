using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    public class HandlePowerups : MonoBehaviour
    {

        public TripleShot tripleShotPowerup;
        public FullyAuto fullyAutoPowerup;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("TripleShot"))
            {
                tripleShotPowerup.enabled = true;
                Destroy(other.gameObject);
            }
        }
    }
}
