using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Com.MorganHouston.ZombCube
{

    public static class SaveSystem
    {

        /// <summary>
        /// Saves the player's data to a file in binary by serialization.
        /// </summary>
        /// <param name="player"></param>
        public static void SavePlayer(Player player)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            string path = Application.persistentDataPath + "/playerData.hax";
            FileStream stream = new FileStream(path, FileMode.Create);

            SaveData data = new SaveData(player);

            formatter.Serialize(stream, data);
            stream.Close();
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

    }

}
