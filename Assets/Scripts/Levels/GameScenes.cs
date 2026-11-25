using UnityEngine.SceneManagement;

namespace Com.GCTC.ZombCube
{
    /// <summary>
    /// Tells which kind of game scene is loaded. Use these instead of comparing scene
    /// names, so new levels from the LevelDatabase behave like GameScene / NetworkGameScene.
    /// </summary>
    public static class GameScenes
    {
        public const string DefaultSoloScene = "GameScene";
        public const string DefaultPartyScene = "NetworkGameScene";

        /// <summary>Solo or Couch Co-op level scene.</summary>
        public static bool IsSolo(string sceneName)
        {
            if (sceneName == DefaultSoloScene) return true;
            LevelDatabase db = LevelDatabase.Instance;
            return db != null && db.IsSoloScene(sceneName);
        }

        /// <summary>Party / online multiplayer level scene.</summary>
        public static bool IsParty(string sceneName)
        {
            if (sceneName == DefaultPartyScene) return true;
            LevelDatabase db = LevelDatabase.Instance;
            return db != null && db.IsPartyScene(sceneName);
        }

        /// <summary>LevelDatabase ID of a game scene; the Arena scenes (and anything unknown) are "arena".</summary>
        public static string LevelIdOf(string sceneName)
        {
            LevelDatabase db = LevelDatabase.Instance;
            LevelEntry entry = db != null ? db.ForScene(sceneName) : null;
            return entry != null && !string.IsNullOrEmpty(entry.id) ? entry.id : WaveLeaderboards.Arena;
        }

        /// <summary>LevelDatabase ID of the level being played.</summary>
        public static string ActiveLevelId => LevelIdOf(SceneManager.GetActiveScene().name);

        public static bool IsSoloActive => IsSolo(SceneManager.GetActiveScene().name);
        public static bool IsPartyActive => IsParty(SceneManager.GetActiveScene().name);

        /// <summary>Solo scene for the map picked on the level select screen.</summary>
        public static string SelectedSoloScene()
        {
            LevelEntry level = LevelDatabase.Instance != null ? LevelDatabase.Instance.Get(LevelSelect.mapSelected) : null;
            return level != null && !string.IsNullOrEmpty(level.soloScene) ? level.soloScene : DefaultSoloScene;
        }

        /// <summary>Party scene for the map picked on the level select screen.</summary>
        public static string SelectedPartyScene()
        {
            LevelEntry level = LevelDatabase.Instance != null ? LevelDatabase.Instance.Get(LevelSelect.mapSelected) : null;
            return level != null && !string.IsNullOrEmpty(level.partyScene) ? level.partyScene : DefaultPartyScene;
        }
    }
}
