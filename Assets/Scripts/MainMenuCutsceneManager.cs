using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Com.GCTC.ZombCube
{
    public class MainMenuCutsceneManager : MonoBehaviour
    {
        public GameObject[] objects;
        public Vector3[] objectSpawnPoints;
        public Quaternion[] objectSpawnRotations;
        private PlayableDirector director;

        private void Start()
        {
            director = GetComponent<PlayableDirector>();
            int i = 0;
            foreach (GameObject obj in objects)
            {
                objectSpawnPoints[i] = obj.transform.position;
                objectSpawnRotations[i] = obj.transform.rotation;
                i++;
            }
        }

        public void ResetObjects()
        {
            if (objects == null) return;
            int i = 0;
            foreach (GameObject obj in objects)
            {
                if(obj != null)
                {
                    obj.transform.position = objectSpawnPoints[i];
                    obj.transform.rotation = objectSpawnRotations[i];
                    obj.SetActive(true);
                }
                
                i++;
            }
        }

        public void CallResetCutscne()
        {
            StartCoroutine(ResetCutscene());
        }

        IEnumerator ResetCutscene()
        {
            yield return new WaitForSeconds(10f);
            director.time = 0;
            director.Play();
        }
    }
}
