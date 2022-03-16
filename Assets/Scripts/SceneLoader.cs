using UnityEngine;
using UnityEngine.SceneManagement;

namespace Com.MorganHouston.ZombCube
{

    public static class SceneLoader
    {

        public static int levelToLoad = 0;

        public static void ToMainMenu()
        {
#if UNITY_WSA
            ResetLightingData();
#endif
            levelToLoad = 1;
            SceneManager.LoadScene(7);
        }

        public static void ToLobby()
        {
#if UNITY_WSA
            ResetLightingData();
#endif
            levelToLoad = 3;
            SceneManager.LoadScene("LobbyScene");
        }

        public static void ToLoading()
        {
#if UNITY_WSA
            ResetLightingData();
#endif
            levelToLoad = 2;
            SceneManager.LoadScene("LoadingScene");
        }

        public static void PlayGame()
        {
#if UNITY_WSA
            ResetLightingData();
#endif
            levelToLoad = 5;
            SceneManager.LoadScene(7);
        }

        public static void LoadThisScene(int sceneToLoad)
        {
#if UNITY_WSA
            ResetLightingData();
#endif
            levelToLoad = sceneToLoad;
            SceneManager.LoadScene(7);
        }

        public static Scene GetCurrentScene()
        {
            return SceneManager.GetActiveScene();
        }

        private static void ResetLightingData()
        {
            LightmapSettings.lightmaps = new LightmapData[0];
            Resources.UnloadUnusedAssets();
        }

    }

}
