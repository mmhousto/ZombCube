using NUnit.Framework;
using Com.GCTC.ZombCube;

// Checks the cosmetic ID catalog and the conversion from old index-based saves.
public class CosmeticMigrationTests
{
    [Test]
    public void ColorIdsAreUnique()
    {
        var seen = new System.Collections.Generic.HashSet<string>();
        foreach (string id in CosmeticCatalog.ColorIds)
            Assert.IsTrue(seen.Add(id), "Duplicate cosmetic id: " + id);
    }

    [Test]
    public void ColorIdsFitInOwnershipArrays()
    {
        Assert.LessOrEqual(CosmeticCatalog.ColorIds.Length, CosmeticCatalog.OwnershipSize);
    }

    [Test]
    public void LegacyFlagsConvertToIds()
    {
        int[] flags = { 1, 0, 1, 0, 0, 0, 0, 0, 0 };
        string[] ids = SaveMigration.IdsFromFlags(flags);
        CollectionAssert.AreEquivalent(new[] { "default", "blue" }, ids);
    }

    [Test]
    public void DefaultIsAlwaysOwned()
    {
        string[] ids = SaveMigration.IdsFromFlags(new int[9]);
        CollectionAssert.Contains(ids, "default");
    }

    [Test]
    public void IdsConvertBackToFlags()
    {
        int[] flags = SaveMigration.FlagsFromIds(new[] { "green", "ou" });
        Assert.AreEqual(9, flags.Length);
        Assert.AreEqual(1, flags[0]);
        Assert.AreEqual(1, flags[1]);
        Assert.AreEqual(1, flags[7]);
        Assert.AreEqual(0, flags[2]);
    }

    [Test]
    public void UnknownIdsAreIgnored()
    {
        int[] flags = SaveMigration.FlagsFromIds(new[] { "not-a-colour" });
        Assert.AreEqual(1, flags[0]);
        Assert.AreEqual(0, flags[1]);
    }

    [Test]
    public void EquippedIndexRoundTrips()
    {
        for (int i = 0; i < CosmeticCatalog.ColorIds.Length; i++)
        {
            string id = SaveMigration.EquippedIdFromIndex(i);
            Assert.AreEqual(i, SaveMigration.EquippedIndexFromId(id));
        }
    }

    [Test]
    public void UnknownEquippedIdFallsBackToDefault()
    {
        Assert.AreEqual(0, SaveMigration.EquippedIndexFromId("missing"));
        Assert.AreEqual("default", SaveMigration.EquippedIdFromIndex(99));
    }
}
