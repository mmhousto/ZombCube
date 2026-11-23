using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// Keeps Resources/LevelDatabase.asset loadable: every scene it names must be in Build Settings.
public class LevelDatabaseTests
{
    private const string AssetPath = "Assets/Resources/LevelDatabase.asset";

    private static SerializedProperty Entries()
    {
        var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetPath);
        Assert.IsNotNull(asset, "Missing " + AssetPath);
        SerializedProperty entries = new SerializedObject(asset).FindProperty("entries");
        Assert.IsNotNull(entries);
        return entries;
    }

    [Test]
    public void FirstMapIsTheArena()
    {
        SerializedProperty entries = Entries();
        Assert.Greater(entries.arraySize, 0);
        SerializedProperty first = entries.GetArrayElementAtIndex(0);
        Assert.AreEqual("GameScene", first.FindPropertyRelative("soloScene").stringValue);
        Assert.AreEqual("NetworkGameScene", first.FindPropertyRelative("partyScene").stringValue);
    }

    [Test]
    public void IdsAreUnique()
    {
        SerializedProperty entries = Entries();
        var ids = Enumerable.Range(0, entries.arraySize)
            .Select(i => entries.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue).ToList();
        Assert.AreEqual(ids.Count, ids.Distinct().Count(), "Duplicate level id");
    }

    [Test]
    public void EveryLevelSceneIsInBuildSettings()
    {
        var buildScenes = EditorBuildSettings.scenes.Where(s => s.enabled)
            .Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path)).ToList();
        SerializedProperty entries = Entries();
        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            foreach (string field in new[] { "soloScene", "partyScene" })
            {
                string scene = entry.FindPropertyRelative(field).stringValue;
                CollectionAssert.Contains(buildScenes, scene,
                    $"{entry.FindPropertyRelative("id").stringValue}.{field} '{scene}' is not an enabled Build Settings scene");
            }
        }
    }
}
