using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    public class MaterialForLevel : MonoBehaviour
    {
        MeshRenderer renderer;
        public Material[] materialsForLevel; 

        // Start is called before the first frame update
        void Awake()
        {
            renderer = GetComponent<MeshRenderer>();
            renderer.material = materialsForLevel[LevelSelect.levelSelected];
        }
    }
}
