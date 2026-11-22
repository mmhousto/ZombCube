using UnityEngine;

namespace Com.GCTC.ZombCube
{
    public class CosmeticManager : MonoBehaviour
    {
        private GameObject[] blaster;
        public MeshRenderer playerSkin;

        private int appliedSkin = -1;
        private int appliedBlaster = -1;

        void Start()
        {
            blaster = GameObject.FindGameObjectsWithTag("Blaster");
            Apply();
        }

        void Update()
        {
            Apply();
        }

        // Only reassign when the equipped cosmetic changes. Reading Renderer.material
        // every frame (as before) created a new material instance each time.
        private void Apply()
        {
            if (Player.Instance == null)
                return;

            int skin = Player.Instance.currentSkin;
            int blasterIndex = Player.Instance.currentBlaster;
            if (skin == appliedSkin && blasterIndex == appliedBlaster)
                return;

            if (playerSkin != null)
                playerSkin.sharedMaterial = CosmeticDatabase.MaterialAt(skin);

            Material blasterMaterial = CosmeticDatabase.MaterialAt(blasterIndex);
            foreach (GameObject item in blaster)
            {
                MeshRenderer renderer = item != null ? item.GetComponent<MeshRenderer>() : null;
                if (renderer != null)
                    renderer.sharedMaterial = blasterMaterial;
            }

            appliedSkin = skin;
            appliedBlaster = blasterIndex;
        }
    }
}
