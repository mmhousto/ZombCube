using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    public class HealthPack : MonoBehaviour
    {
        SpriteRenderer healthPack;
        public bool isUsable;
        public string contextPrompt = "Hold \t <br>for HP [Cost: 500]";

        // Start is called before the first frame update
        void Start()
        {
            contextPrompt = "Hold \t <br>for HP [Cost: 500]";
            healthPack = GetComponent<SpriteRenderer>();
            isUsable = true;
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public void StartResetHealthPack()
        {
            StartCoroutine(ResetHealthPack());
        }

        IEnumerator ResetHealthPack()
        {
            isUsable = false;
            healthPack.enabled = false;
            yield return new WaitForSeconds(120);
            healthPack.enabled = true;
            isUsable = true;
        }
    }
}
