using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemoteConfig : MonoBehaviour
{
    private static RemoteConfig _instance;

    public static RemoteConfig Instance { get { return _instance; } }

    public int cubesToSpawn = 5;

    private void Awake()
    {
        if(_instance != null  && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
        DontDestroyOnLoad(this.gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        cubesToSpawn = RemoteSettings.GetInt("CubesToSpawn", 5);
        RemoteSettings.Completed += RemoteSettingsUpdated;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void RemoteSettingsUpdated(bool wasUpdatedFromServer, bool settingsChanged, int serverResponse)
    {
        int newCubesToSpawn = RemoteSettings.GetInt("CubesToSpawn", 5);
        if (settingsChanged)
        {
            cubesToSpawn = newCubesToSpawn;
            Debug.Log("Updated spawning of enemies to increment by " + cubesToSpawn);
        }
        else
        {
            Debug.Log("Remote Settings were not updated: " + cubesToSpawn);
        }
    }
}
