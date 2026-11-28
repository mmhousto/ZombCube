using System.Linq;
using Com.GCTC.ZombCube;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// Every map has a Solo, Co-op and Party highest-wave board, and wave records only go up.
public class WaveLeaderboardsTests
{
    [Test]
    public void EveryLevelHasABoardForEveryMode()
    {
        var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Resources/LevelDatabase.asset");
        Assert.IsNotNull(asset);
        SerializedProperty entries = new SerializedObject(asset).FindProperty("entries");
        for (int i = 0; i < entries.arraySize; i++)
        {
            string id = entries.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue;
            foreach (WaveMode mode in (WaveMode[])System.Enum.GetValues(typeof(WaveMode)))
            {
                WaveBoard board = WaveLeaderboards.For(id, mode);
                Assert.IsNotNull(board, $"No {mode} leaderboard for level '{id}'");
                Assert.IsFalse(string.IsNullOrEmpty(board.AppleId), $"No Apple ID for {id} {mode}");
                Assert.IsFalse(string.IsNullOrEmpty(board.SteamName), $"No Steam name for {id} {mode}");
            }
        }
    }

    [Test]
    public void BoardIdsAreUnique()
    {
        Assert.AreEqual(WaveLeaderboards.All.Count, WaveLeaderboards.All.Select(b => b.AppleId).Distinct().Count());
        Assert.AreEqual(WaveLeaderboards.All.Count, WaveLeaderboards.All.Select(b => b.SteamName).Distinct().Count());
    }

    [Test]
    public void ChillCoopAndHardcorePartyBoards()
    {
        Assert.AreEqual("highest_coop_theatre_chill", WaveLeaderboards.For("theatre", WaveMode.CoopChill).AppleId);
        Assert.AreEqual("Highest COOP Wave - Theatre: Chill", WaveLeaderboards.For("theatre", WaveMode.CoopChill).SteamName);
        Assert.AreEqual("highest_party_arena_hard", WaveLeaderboards.For("arena", WaveMode.PartyHardcore).AppleId);
        Assert.AreEqual("Highest Party Wave - Arena: HM", WaveLeaderboards.For("arena", WaveMode.PartyHardcore).SteamName);
        // Each mode keeps its own record.
        WaveRecord[] records = WaveLeaderboards.Record(null, "arena", WaveMode.PartyHardcore, 30, out _);
        Assert.AreEqual(0, WaveLeaderboards.Best(records, "arena", WaveMode.Party));
        Assert.AreEqual(30, WaveLeaderboards.Best(records, "arena", WaveMode.PartyHardcore));
    }

    [Test]
    public void ArenaKeepsTheOriginalBoards()
    {
        Assert.AreEqual("highest_wave_on_starting_level", WaveLeaderboards.For("arena", WaveMode.Solo).AppleId);
        Assert.AreEqual("Highest Solo Wave", WaveLeaderboards.For("arena", WaveMode.Solo).SteamName);
        Assert.AreEqual("highest_party_wave_on_starting_level", WaveLeaderboards.For("arena", WaveMode.Party).AppleId);
        Assert.AreEqual("Highest Party Wave", WaveLeaderboards.For("arena", WaveMode.Party).SteamName);
    }

    [Test]
    public void RecordsOnlyGoUpAndAreSeparatePerMapAndMode()
    {
        WaveRecord[] records = null;
        records = WaveLeaderboards.Record(records, "theatre", WaveMode.Coop, 12, out bool improved);
        Assert.IsTrue(improved);
        records = WaveLeaderboards.Record(records, "theatre", WaveMode.Coop, 8, out improved);
        Assert.IsFalse(improved);
        records = WaveLeaderboards.Record(records, "theatre", WaveMode.Solo, 5, out improved);
        Assert.IsTrue(improved);
        records = WaveLeaderboards.Record(records, "spaceship", WaveMode.Coop, 20, out improved);

        Assert.AreEqual(12, WaveLeaderboards.Best(records, "theatre", WaveMode.Coop));
        Assert.AreEqual(5, WaveLeaderboards.Best(records, "theatre", WaveMode.Solo));
        Assert.AreEqual(0, WaveLeaderboards.Best(records, "theatre", WaveMode.Party));
        Assert.AreEqual(20, WaveLeaderboards.Best(records, "spaceship", WaveMode.Coop));
        Assert.AreEqual(3, records.Length);
    }

    // SaveData lives in Assembly-CSharp, which test assemblies can't reference, so it's reached by reflection.
    [Test]
    public void SaveDataRoundTripsWaveRecords()
    {
        System.Type type = System.Type.GetType("Com.GCTC.ZombCube.SaveData, Assembly-CSharp");
        Assert.IsNotNull(type, "SaveData not found");
        System.Reflection.FieldInfo field = type.GetField("waveRecords");
        Assert.IsNotNull(field, "SaveData.waveRecords missing");

        object data = System.Activator.CreateInstance(type);
        field.SetValue(data, WaveLeaderboards.Record((WaveRecord[])field.GetValue(data), "spaceship", WaveMode.Party, 9, out _));
        object loaded = JsonUtility.FromJson(JsonUtility.ToJson(data), type);
        Assert.AreEqual(9, WaveLeaderboards.Best((WaveRecord[])field.GetValue(loaded), "spaceship", WaveMode.Party));

        // Saves from before wave records get an empty list once normalized.
        object old = JsonUtility.FromJson("{\"highestWave\":7}", type);
        type.GetMethod("Normalize").Invoke(old, null);
        Assert.IsNotNull(field.GetValue(old));
    }
}
