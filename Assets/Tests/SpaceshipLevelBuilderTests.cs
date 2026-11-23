using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// Runs the Spaceship builder into a temporary folder and checks the result (through reflection,
// because the builder lives in Assembly-CSharp-Editor).
public class SpaceshipLevelBuilderTests
{
    private const string Folder = "Assets/__SpaceshipBuilderTest";
    private const string BuilderType = "Com.GCTC.ZombCube.EditorTools.SpaceshipLevelBuilder, Assembly-CSharp-Editor";

    private static string soloPath, partyPath;

    [OneTimeSetUp]
    public void BuildOnce()
    {
        Type builder = Type.GetType(BuilderType);
        Assert.IsNotNull(builder, "SpaceshipLevelBuilder not found");
        object result = builder.GetMethod("Build", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { Folder, false });
        soloPath = (string)result.GetType().GetField("soloScenePath").GetValue(result);
        partyPath = (string)result.GetType().GetField("partyScenePath").GetValue(result);
    }

    [OneTimeTearDown]
    public void CleanUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        AssetDatabase.DeleteAsset(Folder);
    }

    private static T Static<T>(string name)
    {
        Type type = Type.GetType(BuilderType);
        object value = (object)type.GetField(name, BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        Assert.IsNotNull(value, "Builder field " + name);
        return (T)Convert.ChangeType(value, typeof(T));
    }

    private static Vector3[] Points(string name) =>
        (Vector3[])Type.GetType(BuilderType).GetField(name, BindingFlags.Public | BindingFlags.Static).GetValue(null);

    private static int CountPrefabInstances(string prefabPath) =>
        UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(t => PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject) && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) == prefabPath);

    private static void Open(string path)
    {
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(path), "Scene not built: " + path);
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    }

    [Test]
    public void EnemySpawnsAreAwayFromPlayerSpawns()
    {
        float min = Static<float>("MinSpawnDistance");
        foreach (Vector3 enemy in Points("EnemySpawns"))
            foreach (Vector3 player in Points("PlayerSpawns"))
                Assert.GreaterOrEqual(Vector3.Distance(enemy, player), min, $"Enemy spawn {enemy} is too close to player spawn {player}");
    }

    private static void AssertCommon()
    {
        Assert.AreEqual(0, CountPrefabInstances("Assets/Prefabs/Maps/Environment.prefab"), "Arena environment still in the scene");
        Assert.GreaterOrEqual(GameObject.FindGameObjectsWithTag("SpawnPoint").Length, 12, "Enemy spawn points");
        foreach (string weapon in new[] { "WeaponPickup", "WeaponPickup AB", "WeaponPickup Shotblaster", "WeaponPickup Sniper Blaster" })
            Assert.AreEqual(1, CountPrefabInstances($"Assets/Prefabs/{weapon}.prefab"), weapon);

        NavMeshSurface[] surfaces = UnityEngine.Object.FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.Greater(surfaces.Length, 0, "No NavMeshSurface");
        foreach (NavMeshSurface surface in surfaces)
            Assert.IsNotNull(surface.navMeshData, "NavMesh not baked for " + surface.name);

        foreach (string stairs in new[] { "Hangar Stairs Port", "Hangar Stairs Starboard", "Reactor Stairs", "Command Deck Steps" })
            Assert.IsNotNull(GameObject.Find(stairs), stairs);

        // Floors on every level, and a kinematic Rigidbody on level pieces for the ground check.
        float s = Static<float>("S");
        foreach (string level in new[] { "CatwalkLevel", "DeckLevel" })
        {
            float top = Static<float>(level) * s;
            Assert.IsTrue(GameObject.FindGameObjectsWithTag("Ground").Select(g => g.GetComponent<Collider>()).Where(c => c != null)
                .Any(c => Mathf.Abs(c.bounds.max.y - top) < 0.05f), "No floor at " + level);
        }
        foreach (GameObject ground in GameObject.FindGameObjectsWithTag("Ground"))
        {
            if (ground.GetComponent<UnityEngine.ProBuilder.ProBuilderMesh>() == null) continue;
            Rigidbody body = ground.GetComponent<Rigidbody>();
            Assert.IsTrue(body != null && body.isKinematic, ground.name + " needs a kinematic Rigidbody");
        }

        // Walls are Not Walkable for the enemies.
        int notWalkable = NavMesh.GetAreaFromName("Not Walkable");
        int wallsLayer = LayerMask.NameToLayer("Walls");
        var walls = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c => c.gameObject.layer == wallsLayer).ToList();
        Assert.Greater(walls.Count, 20, "Wall pieces");
        Assert.Greater(walls.Count(c => c.GetComponent<NavMeshModifier>() is NavMeshModifier m && m.overrideArea && m.area == notWalkable), walls.Count * 3 / 4, "Walls Not Walkable");

        // It's an interior: every area has a ceiling and its own lights.
        Assert.GreaterOrEqual(UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t => t.name.EndsWith(" Ceiling")), 9, "Ceilings");
        Assert.GreaterOrEqual(UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Count(l => l.type == LightType.Point), 15, "Point lights");

        // Every enemy spawn is reachable from the hangar on the enemies' NavMesh.
        NavMeshAgent agent = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/ZombCube.prefab").GetComponentInChildren<NavMeshAgent>(true);
        var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = NavMesh.AllAreas };
        Assert.IsTrue(NavMesh.SamplePosition(new Vector3(0f, 0f, -14f) * s, out NavMeshHit start, 2f, filter), "Hangar is not on the enemy NavMesh");
        foreach (GameObject spawn in GameObject.FindGameObjectsWithTag("SpawnPoint"))
        {
            Assert.IsTrue(NavMesh.SamplePosition(spawn.transform.position, out NavMeshHit end, 2f, filter), spawn.name + " is not on the enemy NavMesh");
            var path = new NavMeshPath();
            NavMesh.CalculatePath(start.position, end.position, filter, path);
            Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status, spawn.name + " can't be reached from the hangar");
        }
    }

    [Test]
    public void SoloSceneIsBuilt()
    {
        Open(soloPath);
        AssertCommon();
        foreach (string spawn in new[] { "SP1", "SP2", "SP3", "SP4" })
            Assert.IsNotNull(GameObject.Find(spawn), spawn);
        Assert.GreaterOrEqual(CountPrefabInstances("Assets/Prefabs/HealthPack.prefab"), 4, "Health packs");
    }

    [Test]
    public void PartySceneIsBuilt()
    {
        Open(partyPath);
        AssertCommon();
        Assert.GreaterOrEqual(CountPrefabInstances("Assets/Resources/NetworkHealthPack.prefab"), 4, "Network health packs");
        MonoBehaviour setup = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).FirstOrDefault(b => b != null && b.GetType().Name == "GameSetup");
        Assert.IsNotNull(setup, "GameSetup");
        Assert.AreEqual(4, new SerializedObject(setup).FindProperty("spawnLocations").arraySize, "Party player spawns");
    }
}
