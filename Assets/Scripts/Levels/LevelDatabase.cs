using System;
using System.Collections.Generic;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    [Serializable]
    public class LevelEntry
    {
        [Tooltip("Stable id for the map, e.g. \"arena\". Don't rename after release.")]
        public string id;
        public string displayName;
        [Tooltip("Scene name (not path) for Solo and Couch Co-op.")]
        public string soloScene;
        [Tooltip("Scene name (not path) for Party / online multiplayer.")]
        public string partyScene;
        [Tooltip("How many texture packs the map supports (MaterialForLevel). 1 = no choice.")]
        public int texturePackCount = 1;
        [Tooltip("Optional names for each texture pack, shown on the level cards.")]
        public string[] texturePackNames;
        [Header("Level select card")]
        public Sprite preview;
        public Color previewColor = Color.white;
    }

    /// <summary>
    /// Every playable map, in level-select order. Each map has a scene for Solo/Co-op
    /// and a scene for Party. GameScenes uses this list to recognise game scenes.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelDatabase", menuName = "ZombCube/Level Database")]
    public class LevelDatabase : ScriptableObject
    {
        public const string ResourcePath = "LevelDatabase";

        public List<LevelEntry> entries = new List<LevelEntry>();

        private static LevelDatabase instance;

        public static LevelDatabase Instance
        {
            get
            {
                if (instance == null)
                    instance = Resources.Load<LevelDatabase>(ResourcePath);
                return instance;
            }
        }

        public int Count => entries.Count;

        /// <summary>Entry for a map index; unknown indexes fall back to the first map.</summary>
        public LevelEntry Get(int index)
        {
            if (entries.Count == 0) return null;
            if (index < 0 || index >= entries.Count) index = 0;
            return entries[index];
        }

        public bool IsSoloScene(string sceneName)
        {
            foreach (LevelEntry entry in entries)
                if (entry.soloScene == sceneName) return true;
            return false;
        }

        public bool IsPartyScene(string sceneName)
        {
            foreach (LevelEntry entry in entries)
                if (entry.partyScene == sceneName) return true;
            return false;
        }
    }
}
