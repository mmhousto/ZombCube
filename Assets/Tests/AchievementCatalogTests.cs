using System.Linq;
using Com.GCTC.ZombCube;
using NUnit.Framework;
using UnityEngine;

// The in-game achievements list must match the Steam / Game Center achievement IDs.
public class AchievementCatalogTests
{
    private static readonly string[] PlatformIds =
    {
        "stayin_alive_solo", "stayin_alive_party", "cube_destroyer_i", "cube_destroyer_ii", "cube_destroyer_iii",
        "ricochet_king", "trigger_happy_i", "trigger_happy_ii", "trigger_happy_iii", "n_gamer_1",
        "point_racker_i", "point_racker_ii", "point_racker_iii", "lets_play", "lets_play_together", "lets_party"
    };

    [Test]
    public void CatalogMatchesPlatformAchievements()
    {
        CollectionAssert.AreEquivalent(PlatformIds, AchievementCatalog.All.Select(a => a.Id).ToArray());
        foreach (AchievementDef a in AchievementCatalog.All)
        {
            Assert.IsFalse(string.IsNullOrEmpty(a.Name), a.Id);
            Assert.IsFalse(string.IsNullOrEmpty(a.Description), a.Id);
            if (a.HasProgress) Assert.Greater(a.Target, 0, a.Id);
        }
    }

    [Test]
    public void UnlocksAreSavedOnce()
    {
        string[] unlocked = AchievementCatalog.AddUnlock(null, "lets_play", out bool added);
        Assert.IsTrue(added);
        unlocked = AchievementCatalog.AddUnlock(unlocked, "lets_play", out added);
        Assert.IsFalse(added);
        Assert.AreEqual(1, unlocked.Length);
    }

    [Test]
    public void ProgressIsClamped()
    {
        AchievementDef cubes = AchievementCatalog.Get("cube_destroyer_i");
        Assert.AreEqual(0.5f, AchievementCatalog.Progress(cubes, 5000, false), 1e-4f);
        Assert.AreEqual(1f, AchievementCatalog.Progress(cubes, 50000, false));
        Assert.AreEqual(1f, AchievementCatalog.Progress(AchievementCatalog.Get("ricochet_king"), 0, true));
    }

    [Test]
    public void SaveDataKeepsAchievements()
    {
        System.Type type = System.Type.GetType("Com.GCTC.ZombCube.SaveData, Assembly-CSharp");
        Assert.IsNotNull(type);
        System.Reflection.FieldInfo field = type.GetField("achievements");
        Assert.IsNotNull(field, "SaveData.achievements missing");
        object data = System.Activator.CreateInstance(type);
        field.SetValue(data, new[] { "ricochet_king" });
        object loaded = JsonUtility.FromJson(JsonUtility.ToJson(data), type);
        CollectionAssert.AreEqual(new[] { "ricochet_king" }, (string[])field.GetValue(loaded));
    }
}
