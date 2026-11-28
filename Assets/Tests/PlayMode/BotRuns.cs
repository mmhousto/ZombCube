using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Bot runs: bots play each map from wave 49 with 10,000 points and 15 seconds to buy a wall gun, and must
/// defeat the boss (wave 50) and clear the next wave. They take a long time, so they only run in the
/// "Bot runs" GitHub workflow or on demand from the Test Runner (category BotRun), never in the PR checks.
///
/// Party here is Photon offline mode with one bot (one Unity editor can only be one Photon client).
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

    [UnityTest, Timeout(int.MaxValue)] public IEnumerator PartyOffline_Arena() => Run("party -zcbotOffline -zcbotMap arena");
    [UnityTest, Timeout(int.MaxValue)] public IEnumerator PartyOffline_Spaceship() => Run("party -zcbotOffline -zcbotMap spaceship");
    [UnityTest, Timeout(int.MaxValue)] public IEnumerator PartyOffline_Theatre() => Run("party -zcbotOffline -zcbotMap theatre");

    private static IEnumerator Run(string arguments)
    {
        Type controller = Type.GetType(Controller);
        Assert.IsNotNull(controller, "BotRunController not found");

        string resultDir = System.IO.Path.Combine(Application.dataPath, "..", "bot-results");
        System.IO.Directory.CreateDirectory(resultDir);
        string name = arguments.Split(' ')[0] + "-" + arguments.Substring(arguments.IndexOf("-zcbotMap ", StringComparison.Ordinal) + 10).Split(' ')[0];
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
        current?.GetType().GetMethod("Stop").Invoke(current, null);
        Time.timeScale = 1f;

        Debug.Log("[ZCBOT] " + name + ": " + result);
        Assert.IsTrue(passed, name + " failed: " + result);
    }
}
