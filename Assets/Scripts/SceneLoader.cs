using UnityEngine;
using UnityEngine.SceneManagement;

namespace Com.GCTC.ZombCube
{

    public static class SceneLoader
    {

        public static int levelToLoad = 0;

        /// <summary>Scene name for the Loading scene to load when levelToLoad is negative.</summary>
        public static string sceneToLoad;

        public static void ToMainMenu()
        {
#if UNITY_WSA
            ResetLightingData();
#endif
            Time.timeScale = 1;
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
            // The selected map's Solo / Co-op scene (GameScene for the original map).
            levelToLoad = -1;
            sceneToLoad = GameScenes.SelectedSoloScene();
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
