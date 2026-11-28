using System;
using System.Reflection;
using NUnit.Framework;

// Bot run settings: argument parsing (BotSettings is Editor-only code in
// Assembly-CSharp-Editor, reached by reflection). The bot runs themselves are PlayMode tests (BotRuns) in their own workflow.
public class BotSettingsTests
{
    private static readonly Type Settings = Type.GetType("Com.GCTC.ZombCube.EditorTools.Bots.BotSettings, Assembly-CSharp-Editor");

    private static object Get(string field) => Settings.GetField(field, BindingFlags.Public | BindingFlags.Static).GetValue(null);
    private static object Call(string method, params object[] args) =>
        Settings.GetMethod(method, BindingFlags.Public | BindingFlags.Static, null, Array.ConvertAll(args, a => a.GetType()), null).Invoke(null, args);

    [TearDown]
    public void TearDown() => Settings?.GetMethod("Reset").Invoke(null, null);

    [Test]
    public void ParsesCoopRun()
    {
        Assert.IsNotNull(Settings);
        Assert.IsTrue((bool)Call("TryParse", (object)new[] { "Game.exe", "-zcbot", "coop", "-zcbotMap", "Theatre", "-zcbotPlayers", "3", "-zcbotSpeed", "2.5", "-zcbotQuit" }));
        Assert.IsTrue((bool)Get("Active"));
        Assert.AreEqual("Coop", Get("Mode").ToString());
        Assert.AreEqual("theatre", Get("MapId"));
        Assert.AreEqual(3, Get("Players"));
        Assert.AreEqual(2.5f, (float)Get("Speed"), 1e-4f);
        Assert.IsTrue((bool)Get("QuitWhenDone"));
    }

    [Test]
    public void IgnoredWithoutFlag()
    {
        Assert.IsFalse((bool)Call("TryParse", (object)new[] { "Game.exe", "-logFile", "x.log" }));
        Assert.IsFalse((bool)Get("Active"));
    }

    [Test]
    public void CoopNeedsTwoToFourBots()
    {
        Call("TryParse", (object)new[] { "-zcbot", "coop", "-zcbotPlayers", "9" });
        Assert.AreEqual(4, Get("Players"));
        Call("TryParse", (object)new[] { "-zcbot", "coop", "-zcbotPlayers", "1" });
        Assert.AreEqual(2, Get("Players"));
    }

    [Test]
    public void ParsesChillAndHardcoreModes()
    {
        Call("TryParse", (object)new[] { "-zcbot", "coop", "-zcbotChill" });
        Assert.IsTrue((bool)Get("Chill"));
        Assert.IsFalse((bool)Get("Hardcore"));

        Call("TryParse", (object)new[] { "-zcbot", "party", "-zcbotHardcore", "-zcbotOffline" });
        Assert.IsTrue((bool)Get("Hardcore"));
        Assert.IsFalse((bool)Get("Chill"));

        // Each flag only applies to its own mode.
        Call("TryParse", (object)new[] { "-zcbot", "solo", "-zcbotChill", "-zcbotHardcore" });
        Assert.IsFalse((bool)Get("Chill"));
        Assert.IsFalse((bool)Get("Hardcore"));
    }
}
