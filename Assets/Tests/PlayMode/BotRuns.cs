using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Bot runs: every run starts at the SignIn screen and goes through the menus like a player (anonymous sign-in
/// on a throwaway account, random name, random owned blaster and skin, the map, then Solo / the co-op lobby /
/// the Party lobby and room). Then bots play each map from wave 1; the boss spawns on wave 5. Reaching the boss passes (beating it
/// is optional). They take a long time, so they only run in the
/// "Bot runs" GitHub workflow or on demand from the Test Runner (category BotRun), never in the PR checks.
///
/// Party here is Photon offline mode with one bot (one Unity editor can only be one Photon client).
/// Hardcore Party needs two players to start, so the offline test checks that Start refuses one player and
/// then starts anyway; the "Bots - Party Hardcore 2" scenario plays it for real with two.
/// Real multi-player Party runs use the "Bots - Party 2/4" Multiplayer Play Mode scenarios.
///
/// The bot code is Editor-only (Assets/Editor/Bots, Assembly-CSharp-Editor), which test assemblies can't
/// reference, so it's called by reflection.
/// </summary>
[Category("BotRun")]
public class BotRuns
{
    private const string Controller = "Com.GCTC.ZombCube.EditorTools.Bots.BotRun, Assembly-CSharp-Editor";
    
    // Speed and time limit for CI; override with the ZCBOT_SPEED environment variable when running locally.
    private static string Speed => Environment.GetEnvironmentVariable("ZCBOT_SPEED") ?? "2";
    private const float TimeoutMinutes = 40f;

    [UnityTest, Timeout(int.MaxValue)] public IEnumerator Solo_Arena() => Run("solo -zcbotMap arena");
    [UnityTest, Timeout(int.MaxValue)] public IEnumerator Solo_Spaceship() => Run("solo -zcbotMap spaceship");
    [UnityTest, Timeout(int.MaxValue)] public IEnumerator Solo_Theatre() => Run("solo -zcbotMap theatre");

    [UnityTest, Timeout(int.MaxValue)] public IEnumerator Coop_Arena() => Run("coop -zcbotMap arena -zcbotPlayers 2");
    [UnityTest, Timeout(int.MaxValue)] public IEnumerator Coop_Spaceship() => Run("coop -zcbotMap spaceship -zcbotPlayers 2");
    [UnityTest, Timeout(int.MaxValue)] public IEnumerator Coop_Theatre() => Run("coop -zcbotMap theatre -zcbotPlayers 2");

    [UnityTest, Timeout(int.MaxValue)] public IEnumerator CoopChill_Arena() => Run("coop -zcbotChill -zcbotMap arena -zcbotPlayers 2");
    [UnityTest, Timeout(int.MaxValue)] public IEnumerator CoopChill_Spaceship() => Run("coop -zcbotChill -zcbotMap spaceship -zcbotPlayers 2");
    [UnityTest, Timeout(int.MaxValue)] public IEnumerator CoopChill_Theatre() => Run("coop -zcbotChill -zcbotMap theatre -zcbotPlayers 2");

    [UnityTest, Timeout(int.MaxValue)] public IEnumerator PartyOffline_Arena() => Run("party -zcbotOffline -zcbotMap arena");
    [UnityTest, Timeout(int.MaxValue)] public IEnumerator PartyOffline_Spaceship() => Run("party -zcbotOffline -zcbotMap spaceship");
    [UnityTest, Timeout(int.MaxValue)] public IEnumerator PartyOffline_Theatre() => Run("party -zcbotOffline -zcbotMap theatre");

    [UnityTest, Timeout(int.MaxValue)] public IEnumerator PartyHardcore_Arena() => Run("party -zcbotOffline -zcbotHardcore -zcbotMap arena");
    [UnityTest, Timeout(int.MaxValue)] public IEnumerator PartyHardcore_Spaceship() => Run("party -zcbotOffline -zcbotHardcore -zcbotMap spaceship");
    [UnityTest, Timeout(int.MaxValue)] public IEnumerator PartyHardcore_Theatre() => Run("party -zcbotOffline -zcbotHardcore -zcbotMap theatre");

    private static IEnumerator Run(string arguments)
    {
        // The run goes through sign-in and the menus, which can log errors that don't matter here (no Steam in
        // the Editor, ads, analytics...); the bot result decides the test.
        LogAssert.ignoreFailingMessages = true;
        Type controller = Type.GetType(Controller);
        Assert.IsNotNull(controller, "BotRunController not found");

        string resultDir = System.IO.Path.Combine(Application.dataPath, "..", "bot-results");
        System.IO.Directory.CreateDirectory(resultDir);
        string variant = arguments.Contains("-zcbotChill") ? "chill-" : arguments.Contains("-zcbotHardcore") ? "hardcore-" : "";
        string name = arguments.Split(' ')[0] + "-" + variant + arguments.Substring(arguments.IndexOf("-zcbotMap ", StringComparison.Ordinal) + 10).Split(' ')[0];
        string args = $"{arguments} -zcbotSpeed {Speed} -zcbotTimeout {TimeoutMinutes} -zcbotResult \"{System.IO.Path.Combine(resultDir, name + ".json")}\"";
        // Paths with spaces would split the argument string; keep the result path simple.
        args = args.Replace("\"", "");

        bool started = (bool)controller.GetMethod("Start", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { args });
        Assert.IsTrue(started, "Bot run didn't start");

        PropertyInfo finished = controller.GetProperty("Finished", BindingFlags.Public | BindingFlags.Static);
        float limit = Time.realtimeSinceStartup + TimeoutMinutes * 60f + 120f;
        while (!(bool)finished.GetValue(null))
        {
            if (Time.realtimeSinceStartup > limit) Assert.Fail("Bot run never finished");
            yield return null;
        }

        bool passed = (bool)controller.GetProperty("Passed", BindingFlags.Public | BindingFlags.Static).GetValue(null);
        string result = (string)controller.GetProperty("ResultJson", BindingFlags.Public | BindingFlags.Static).GetValue(null);

        // Stop the run (removes the virtual gamepads, re-enables saving) so the next test starts clean.
        object current = controller.GetProperty("Current", BindingFlags.Public | BindingFlags.Static).GetValue(null);
        // Wait (briefly) for the throwaway anonymous account to be deleted.
        PropertyInfo cleaning = controller.GetProperty("CleaningUp", BindingFlags.Public | BindingFlags.Static);
        float cleanUntil = Time.realtimeSinceStartup + 15f;
        while ((bool)cleaning.GetValue(null) && Time.realtimeSinceStartup < cleanUntil) yield return null;

        current?.GetType().GetMethod("Stop").Invoke(current, null);
        Time.timeScale = 1f;

        Debug.Log("[ZCBOT] " + name + ": " + result);
        Assert.IsTrue(passed, name + " failed: " + result);
    }
}
