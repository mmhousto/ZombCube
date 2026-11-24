using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Com.GCTC.ZombCube.EditorTools
{
    /// <summary>
    /// Picks the unlit Minimap material (Assets/Materials/Minimap) whose colour matches a level piece, so its
    /// minimap icon shows the piece's colour like the Arena's icons do. Known level materials are mapped by
    /// name; anything else gets the nearest minimap colour to its base colour.
    /// </summary>
    public static class MinimapColors
    {
        public const string Folder = "Assets/Materials/Minimap";

        private static readonly Dictionary<string, string> ByMaterial = new Dictionary<string, string>
        {
            { "Theatre_Carpet", "MinimapRed" }, { "Theatre_Curtain", "MinimapRed" }, { "Theatre_Seat", "MinimapRed" },
            { "Theatre_Debris", "MinimapGray" }, { "Theatre_Floor", "MinimapOrange" }, { "Theatre_Stage", "MinimapOrange" },
            { "Theatre_Wood", "MinimapOrange" }, { "Theatre_Marble", "DefaultMinimap" }, { "Theatre_Plaster", "DefaultMinimap" },
            { "Theatre_Screen", "DefaultBlaster" }, { "Theatre_Trim", "MinimapYellow" }, { "Theatre_Wall", "MinimapGray" },
            { "Ship_Floor", "MinimapBlack" }, { "Ship_Wall", "MinimapGray" }, { "Ship_Bulkhead", "MinimapGray" },
            { "Ship_Panel", "MinimapGray" }, { "Ship_Console", "MinimapBlueBlaster" }, { "Ship_Grate", "DefaultMinimap" },
            { "Ship_Light", "DefaultBlaster" }, { "Ship_Core", "MinimapOrange" }, { "Ship_Window", "MinimapBlack" },
            { "Ship_TrimBlue", "MinimapBlueBlaster" }, { "Ship_TrimOrange", "MinimapOrange" }, { "Ship_TrimRed", "MinimapRed" },
            { "Ship_TrimYellow", "MinimapYellow" },
        };

        public static Material For(Material levelMaterial)
        {
            if (levelMaterial == null) return Load("DefaultMinimap");
            string name = levelMaterial.name;
            foreach (var pair in ByMaterial)
                if (name == pair.Key || name.StartsWith(pair.Key + " ")) return Load(pair.Value);

            // Nearest minimap colour to the material's base colour (luminance-weighted).
            Color c = levelMaterial.HasProperty("_BaseColor") ? levelMaterial.GetColor("_BaseColor") : levelMaterial.color;
            Material best = null;
            float bestDistance = float.MaxValue;
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { Folder }))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (m == null) continue;
                Color mc = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.color;
                float d = 0.3f * Sq(c.r - mc.r) + 0.59f * Sq(c.g - mc.g) + 0.11f * Sq(c.b - mc.b);
                if (d < bestDistance) { bestDistance = d; best = m; }
            }
            return best;
        }

        private static float Sq(float x) => x * x;

        private static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/{name}.mat");
    }
}
