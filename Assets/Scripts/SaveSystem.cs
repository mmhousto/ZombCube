using System.IO;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;

namespace Com.GCTC.ZombCube
{

    public static class SaveSystem
    {
        // Current local save format (JSON). Older builds wrote a binary file (.hax);
        // it is read once, converted to JSON, and kept as a backup.
        private const string JsonFileName = "/playerData.json";
        private const string LegacyFileName = "/playerData.hax";

        private static string JsonPath => Application.persistentDataPath + JsonFileName;
        private static string LegacyPath => Application.persistentDataPath + LegacyFileName;

        /// <summary>
        /// Saves the player's data to a JSON file.
        /// </summary>
        /// <param name="player"></param>
        /// <summary>Automated tests set this so they never overwrite the player's save file.</summary>
        public static bool WritesDisabled;

        public static void SavePlayer(Player player)
        {
            if (WritesDisabled) return;
            SaveData data = new SaveData(player);
            File.WriteAllText(JsonPath, JsonUtility.ToJson(data));
        }

        /// <summary>
        /// Loads the player's data and returns it in the current format.
        /// </summary>
        /// <returns></returns>
        public static SaveData LoadPlayer()
        {
            if (File.Exists(JsonPath))
            {
                SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(JsonPath));
                if (data != null)
                {
                    data.Normalize();
                    return data;
                }
            }

            if (File.Exists(LegacyPath))
            {
                SaveData legacy = ReadLegacyBinary(LegacyPath);
                if (legacy != null)
                {
                    legacy.Normalize();
                    File.WriteAllText(JsonPath, JsonUtility.ToJson(legacy));
                    return legacy;
                }
            }

            Debug.Log("Save file not found in " + Application.persistentDataPath);
            SaveData fresh = new SaveData();
            fresh.Normalize();
            return fresh;
        }

        private static SaveData ReadLegacyBinary(string path)
        {
            try
            {
                using (FileStream stream = new FileStream(path, FileMode.Open))
                {
                    return new BinaryFormatter().Deserialize(stream) as SaveData;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Could not read legacy save: " + e.Message);
                return null;
            }
        }

        public static void DeletePlayer()
        {
            if (File.Exists(JsonPath))
                File.Delete(JsonPath);
            if (File.Exists(LegacyPath))
                File.Delete(LegacyPath);

#if UNITY_EDITOR
 UnityEditor.AssetDatabase.Refresh();
#endif
        }

    }

}
