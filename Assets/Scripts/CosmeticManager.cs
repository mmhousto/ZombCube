using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    public class CosmeticManager : MonoBehaviour
    {
        private GameObject[] blaster;
        public MeshRenderer playerSkin;

        // Start is called before the first frame update
        void Start()
        {
            blaster = GameObject.FindGameObjectsWithTag("Blaster");

            if(Player.Instance != null)
            {
                playerSkin.material = MaterialSelector.Instance.materials[Player.Instance.currentSkin];

                foreach (GameObject item in blaster)
                {
                    item.GetComponent<MeshRenderer>().material = MaterialSelector.Instance.materials[Player.Instance.currentBlaster];
                }
            }
            
        }

        // Update is called once per frame
        void Update()
        {
            if (Player.Instance != null && playerSkin.material != MaterialSelector.Instance.materials[Player.Instance.currentSkin] && blaster[0].GetComponent<MeshRenderer>().material != MaterialSelector.Instance.materials[Player.Instance.currentBlaster])
            {
                playerSkin.material = MaterialSelector.Instance.materials[Player.Instance.currentSkin];

                foreach (GameObject item in blaster)
                {
                    item.GetComponent<MeshRenderer>().material = MaterialSelector.Instance.materials[Player.Instance.currentBlaster];
                }
            }
        }
    }
}
