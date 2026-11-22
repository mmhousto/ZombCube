using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Com.GCTC.ZombCube;

// Keeps Resources/CosmeticDatabase.asset in step with CosmeticCatalog.ColorIds.
public class CosmeticDatabaseTests
{
    private const string AssetPath = "Assets/Resources/CosmeticDatabase.asset";

    private static SerializedProperty Entries()
    {
        var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetPath);
        Assert.IsNotNull(asset, "Missing " + AssetPath);
        var serialized = new SerializedObject(asset);
        SerializedProperty entries = serialized.FindProperty("entries");
        Assert.IsNotNull(entries, "CosmeticDatabase has no 'entries' list");
        return entries;
    }

    [Test]
    public void EntriesMatchCatalogOrder()
    {
        SerializedProperty entries = Entries();
        Assert.AreEqual(CosmeticCatalog.ColorIds.Length, entries.arraySize, "Entry count must match the catalog");
        for (int i = 0; i < entries.arraySize; i++)
        {
            string id = entries.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue;
            Assert.AreEqual(CosmeticCatalog.ColorIds[i], id, "Entry " + i);
        }
    }

    [Test]
    public void EveryEntryHasAMaterial()
    {
        SerializedProperty entries = Entries();
        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            Assert.IsNotNull(entry.FindPropertyRelative("material").objectReferenceValue,
                "No material for " + entry.FindPropertyRelative("id").stringValue);
        }
    }

    [Test]
    public void PricesAreNotNegative()
    {
        SerializedProperty entries = Entries();
        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            Assert.GreaterOrEqual(entry.FindPropertyRelative("blasterPrice").intValue, 0);
            Assert.GreaterOrEqual(entry.FindPropertyRelative("skinPrice").intValue, 0);
        }
    }

    [Test]
    public void PatternEntriesHavePreviewSprites()
    {
        SerializedProperty entries = Entries();
        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            string id = entry.FindPropertyRelative("id").stringValue;
            bool isPattern = id.StartsWith("stripes_") || id.StartsWith("camo_") || id == "checker" || id == "polka" || id == "neon_grid";
            if (isPattern)
                Assert.IsNotNull(entry.FindPropertyRelative("previewSprite").objectReferenceValue, "No preview sprite for " + id);
        }
    }

    [Test]
    public void DefaultCosmeticIsFree()
    {
        SerializedProperty first = Entries().GetArrayElementAtIndex(0);
        Assert.AreEqual(0, first.FindPropertyRelative("blasterPrice").intValue);
        Assert.AreEqual(0, first.FindPropertyRelative("skinPrice").intValue);
    }
}
