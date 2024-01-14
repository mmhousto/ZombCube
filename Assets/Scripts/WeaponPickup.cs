using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    public class WeaponPickup : MonoBehaviour
    {
        SpriteRenderer weaponImage;
        public bool isUsable;
        public string contextPrompt = "Hold \t <br>for Submachine Blaster (SMB) [Cost: 2500]";

        // Start is called before the first frame update
        void Start()
        {
            weaponImage = GetComponent<SpriteRenderer>();
            isUsable = true;
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void StartResetWeapon()
        {
            StartCoroutine(ResetWeapon());
        }

        IEnumerator ResetWeapon()
        {
            isUsable = false;
            weaponImage.enabled = false;
            yield return new WaitForSeconds(120);
            weaponImage.enabled = true;
            isUsable = true;
        }
    }
}
