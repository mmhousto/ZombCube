using System;
using System.Collections.Generic;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    public enum CosmeticCurrency
    {
        Points = 0,
        Coins = 1,
    }

    [Serializable]
    public class CosmeticEntry
    {
        [Tooltip("Must match CosmeticCatalog.ColorIds at the same position.")]
        public string id;
        [Tooltip("Shown in the shop; \" Blaster\" or \" Skin\" is added after it.")]
        public string displayName;
        public Material material;
        public CosmeticCurrency currency;
        public int blasterPrice;
        public int skinPrice;
        [Header("Shop preview")]
        public Color previewColor = Color.white;
        public Sprite previewSprite;
        public Material previewMaterial;
    }

    /// <summary>
    /// Every skin and blaster cosmetic, in catalog order. The index of an entry is
    /// the index stored in Player.currentSkin / currentBlaster and sent to other
    /// players. Add new cosmetics at the end, matching CosmeticCatalog.ColorIds.
    /// </summary>
    [CreateAssetMenu(fileName = "CosmeticDatabase", menuName = "ZombCube/Cosmetic Database")]
    public class CosmeticDatabase : ScriptableObject
    {
        public const string ResourcePath = "CosmeticDatabase";

        public List<CosmeticEntry> entries = new List<CosmeticEntry>();

        private static CosmeticDatabase instance;

        public static CosmeticDatabase Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<CosmeticDatabase>(ResourcePath);
                    if (instance == null)
                        Debug.LogError("CosmeticDatabase asset not found in a Resources folder.");
                }
                return instance;
            }
        }

        public int Count => entries.Count;

        /// <summary>
        /// Entry for a cosmetic index. Unknown indexes (for example from another
        /// player running a newer build) fall back to the default cosmetic.
        /// </summary>
        public CosmeticEntry Get(int index)
        {
            if (entries.Count == 0)
                return null;
            if (index < 0 || index >= entries.Count)
                index = 0;
            return entries[index];
        }

        public static Material MaterialAt(int index)
        {
            CosmeticDatabase db = Instance;
            CosmeticEntry entry = db != null ? db.Get(index) : null;
            return entry != null ? entry.material : null;
        }

        public Material[] AllMaterials()
        {
            var materials = new Material[entries.Count];
            for (int i = 0; i < entries.Count; i++)
                materials[i] = entries[i].material;
            return materials;
        }
    }
}
