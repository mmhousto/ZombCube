using Photon.Pun;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    /// <summary>Picks this renderer's material from the selected texture pack.</summary>
    public class MaterialForLevel : MonoBehaviour
    {
        MeshRenderer renderer;
        public Material[] materialsForLevel;

        void Awake()
        {
            renderer = GetComponent<MeshRenderer>();
            if (renderer == null || materialsForLevel == null || materialsForLevel.Length == 0)
                return;

            int pack = LevelSelect.levelSelected;

            // In party games everyone uses the host's texture pack.
            if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(RoomLevelKeys.TexturePack, out object roomPack) && roomPack is int hostPack)
                pack = hostPack;

            if (pack < 0 || pack >= materialsForLevel.Length)
                pack = 0;

            renderer.material = materialsForLevel[pack];
        }
    }
}
