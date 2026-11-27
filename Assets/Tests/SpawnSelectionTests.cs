using NUnit.Framework;
using Com.GCTC.ZombCube;

public class SpawnSelectionTests
{
    [Test]
    public void NeverPicksASpawnNextToAPlayerWhenAnotherIsSafe()
    {
        float[] distances = { 2f, 40f, 5f, 30f };
        var rng = new System.Random(1);
        for (int i = 0; i < 200; i++)
        {
            int pick = SpawnSelection.Choose(distances, 18f, rng);
            Assert.That(pick == 1 || pick == 3, "Picked spawn " + pick + " at distance " + distances[pick]);
        }
    }

    [Test]
    public void UsesEverySafeSpawn()
    {
        float[] distances = { 20f, 25f, 30f };
        var rng = new System.Random(2);
        var seen = new bool[3];
        for (int i = 0; i < 300; i++) seen[SpawnSelection.Choose(distances, 18f, rng)] = true;
        Assert.That(seen, Is.All.True);
    }

    [Test]
    public void FallsBackToTheFarthestSpawn()
    {
        Assert.AreEqual(2, SpawnSelection.Choose(new[] { 3f, 5f, 9f, 1f }, 18f, new System.Random(3)));
    }

    [Test]
    public void HandlesNoSpawnsAndNoPlayers()
    {
        Assert.AreEqual(-1, SpawnSelection.Choose(new float[0], 18f, new System.Random(4)));
        float far = float.MaxValue;
        int pick = SpawnSelection.Choose(new[] { far, far }, 18f, new System.Random(5));
        Assert.That(pick == 0 || pick == 1);
    }
}
