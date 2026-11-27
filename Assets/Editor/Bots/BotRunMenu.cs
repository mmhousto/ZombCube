using UnityEditor;
using UnityEditor.SceneManagement;

namespace Com.GCTC.ZombCube.EditorTools
{
    /// <summary>
    /// ZombCube > Bots: watch bots play any map in Solo, Co-op or Party (Photon offline) from wave 49.
    /// Enters play mode in an empty scene and hands the run to BotRunController. Nothing is saved to
    /// your profile or leaderboards. Results go to the Console ("[ZCBOT]") and persistentDataPath/zcbot-results.
    /// </summary>
    public static class BotRunMenu
    {
        private const string SpeedKey = "ZombCube.BotRunSpeed";
        private const string Root = "ZombCube/Bots/";

        [MenuItem(Root + "Solo/Arena")] private static void SoloArena() => Run("solo -zcbotMap arena");
        [MenuItem(Root + "Solo/Spaceship")] private static void SoloSpaceship() => Run("solo -zcbotMap spaceship");
        [MenuItem(Root + "Solo/Movie Theatre")] private static void SoloTheatre() => Run("solo -zcbotMap theatre");

        [MenuItem(Root + "Co-op (2 bots)/Arena")] private static void Coop2Arena() => Run("coop -zcbotPlayers 2 -zcbotMap arena");
        [MenuItem(Root + "Co-op (2 bots)/Spaceship")] private static void Coop2Spaceship() => Run("coop -zcbotPlayers 2 -zcbotMap spaceship");
        [MenuItem(Root + "Co-op (2 bots)/Movie Theatre")] private static void Coop2Theatre() => Run("coop -zcbotPlayers 2 -zcbotMap theatre");

        [MenuItem(Root + "Co-op (4 bots)/Arena")] private static void Coop4Arena() => Run("coop -zcbotPlayers 4 -zcbotMap arena");
        [MenuItem(Root + "Co-op (4 bots)/Spaceship")] private static void Coop4Spaceship() => Run("coop -zcbotPlayers 4 -zcbotMap spaceship");
        [MenuItem(Root + "Co-op (4 bots)/Movie Theatre")] private static void Coop4Theatre() => Run("coop -zcbotPlayers 4 -zcbotMap theatre");

        [MenuItem(Root + "Party (offline, 1 bot)/Arena")] private static void PartyArena() => Run("party -zcbotOffline -zcbotMap arena");
        [MenuItem(Root + "Party (offline, 1 bot)/Spaceship")] private static void PartySpaceship() => Run("party -zcbotOffline -zcbotMap spaceship");
        [MenuItem(Root + "Party (offline, 1 bot)/Movie Theatre")] private static void PartyTheatre() => Run("party -zcbotOffline -zcbotMap theatre");

        [MenuItem(Root + "Speed/x1", false, 100)] private static void Speed1() => EditorPrefs.SetFloat(SpeedKey, 1f);
        [MenuItem(Root + "Speed/x2", false, 101)] private static void Speed2() => EditorPrefs.SetFloat(SpeedKey, 2f);
        [MenuItem(Root + "Speed/x4", false, 102)] private static void Speed4() => EditorPrefs.SetFloat(SpeedKey, 4f);

        [MenuItem(Root + "Speed/x1", true)] private static bool Speed1Check() { Menu.SetChecked(Root + "Speed/x1", CurrentSpeed == 1f); return true; }
        [MenuItem(Root + "Speed/x2", true)] private static bool Speed2Check() { Menu.SetChecked(Root + "Speed/x2", CurrentSpeed == 2f); return true; }
        [MenuItem(Root + "Speed/x4", true)] private static bool Speed4Check() { Menu.SetChecked(Root + "Speed/x4", CurrentSpeed == 4f); return true; }

        private static float CurrentSpeed => EditorPrefs.GetFloat(SpeedKey, 1f);

        private static void Run(string arguments)
        {
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // Start from an empty scene so no menu or sign-in scene runs first.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetString(BotRunController.EditorPendingKey,
                $"{arguments} -zcbotSpeed {CurrentSpeed.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            EditorApplication.isPlaying = true;
        }
    }
}
