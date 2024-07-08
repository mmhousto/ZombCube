using System.IO;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;

namespace Com.GCTC.ZombCube
{

    public static class SaveSystem
    {

        /// <summary>
        /// Saves the player's data to a file in binary by serialization.
        /// </summary>
        /// <param name="player"></param>
        public static void SavePlayer(Player player)
        {
            if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.PS)
                PSSaveData.singleton.StartAutoSave();
            else
            {
                BinaryFormatter formatter = new BinaryFormatter();
                string path = Application.persistentDataPath + "/playerData.hax";
                FileStream stream = new FileStream(path, FileMode.Create);

                SaveData data = new SaveData(player);

                formatter.Serialize(stream, data);
                stream.Close();
            }
        }

        /// <summary>
        /// Loads the player's data after deserializing it and returns it.
        /// </summary>
        /// <returns></returns>
        public static SaveData LoadPlayer()
        {
            string path = Application.persistentDataPath + "/playerData.hax";
            if (File.Exists(path))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream stream = new FileStream(path, FileMode.Open);

                SaveData data = formatter.Deserialize(stream) as SaveData;
                stream.Close();

                return data;
            }
            else
            {
                Debug.Log("Save file not found in " + path);
                SaveData data = new SaveData();
                return data;
            }
        }

        public static void DeletePlayer()
        {
            if (CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.PS)
                PSSaveData.singleton.DeleteSaveData();
            else
            {
                string path = Application.persistentDataPath + "/playerData.hax";
                File.Delete(path);
            }

#if UNITY_EDITOR
 UnityEditor.AssetDatabase.Refresh();
#endif
        }

    }

}
