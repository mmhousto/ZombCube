using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// Runs the Movie Theatre builder into a temporary folder and checks the result.
// The builder lives in Assembly-CSharp-Editor, which test assemblies can't reference,
// so it is called through reflection.
public class MovieTheatreLevelBuilderTests
{
    private const string Folder = "Assets/__MovieTheatreBuilderTest";
    private const string BuilderType = "Com.GCTC.ZombCube.EditorTools.MovieTheatreLevelBuilder, Assembly-CSharp-Editor";

    private static string soloPath;
    private static string partyPath;

    [OneTimeSetUp]
    public void BuildOnce()
    {
        Type builder = Type.GetType(BuilderType);
        Assert.IsNotNull(builder, "MovieTheatreLevelBuilder not found");
        MethodInfo build = builder.GetMethod("Build", BindingFlags.Public | BindingFlags.Static);
        object result = build.Invoke(null, new object[] { Folder, false });
        soloPath = (string)result.GetType().GetField("soloScenePath").GetValue(result);
        partyPath = (string)result.GetType().GetField("partyScenePath").GetValue(result);
    }

    [OneTimeTearDown]
    public void CleanUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        AssetDatabase.DeleteAsset(Folder);
    }

    private static void Open(string path)
    {
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(path), "Scene not built: " + path);
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    }

    private static int CountPrefabInstances(string prefabPath)
    {
        return UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(t => PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)
                        && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) == prefabPath);
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
        {
            Assert.IsNotNull(surface.navMeshData, "NavMesh not baked for " + surface.name);
            StringAssert.StartsWith(Folder, AssetDatabase.GetAssetPath(surface.navMeshData), "NavMesh data should be saved with the level");
        }

        Assert.IsNotNull(GameObject.Find("Stage"), "Stage geometry");
        Assert.Greater(GameObject.FindGameObjectsWithTag("Ground").Length, 10, "Walkable geometry tagged Ground");
        // Without a Rigidbody the player's ground trigger never fires and jumping is impossible.
        foreach (GameObject ground in GameObject.FindGameObjectsWithTag("Ground"))
        {
            if (ground.GetComponent<UnityEngine.ProBuilder.ProBuilderMesh>() == null) continue;
            Rigidbody body = ground.GetComponent<Rigidbody>();
            Assert.IsNotNull(body, ground.name + " needs a Rigidbody for the ground check");
            Assert.IsTrue(body.isKinematic, ground.name + " Rigidbody should be kinematic");
        }

        // Starting lobby with twin staircases up to the balcony.
        Assert.IsNotNull(GameObject.Find("Grand Stairs West"), "West grand staircase");
        Assert.IsNotNull(GameObject.Find("Grand Stairs East"), "East grand staircase");
        Assert.IsNotNull(GameObject.Find("Back Stairs"), "Backstage staircase");

        // Walkable floors on every level: ground, stage, balcony / circle and gallery.
        float scale = Constant("S");
        foreach (string level in new[] { "StageLevel", "BalconyLevel", "GalleryLevel" })
        {
            float top = Constant(level) * scale;
            bool found = GameObject.FindGameObjectsWithTag("Ground")
                .Select(g => g.GetComponent<Collider>()).Where(c => c != null)
                .Any(c => Mathf.Abs(c.bounds.max.y - top) < 0.05f);
            Assert.IsTrue(found, "No floor at " + level);
        }

        // Every enemy spawn must be reachable from the lobby on the NavMesh (stairs walkable by enemies).
        Vector3 lobby = new Vector3(0f, 0f, -18f) * scale;
        Assert.IsTrue(NavMesh.SamplePosition(lobby, out NavMeshHit start, 2f, NavMesh.AllAreas), "Lobby is not on the NavMesh");
        foreach (GameObject spawn in GameObject.FindGameObjectsWithTag("SpawnPoint"))
        {
            Assert.IsTrue(NavMesh.SamplePosition(spawn.transform.position, out NavMeshHit end, 2f, NavMesh.AllAreas), spawn.name + " is not on the NavMesh");
            var path = new NavMeshPath();
            NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path);
            Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status, spawn.name + " can't be reached from the lobby");
        }
    }

    private static float Constant(string name)
    {
        FieldInfo field = Type.GetType(BuilderType).GetField(name, BindingFlags.Public | BindingFlags.Static);
        Assert.IsNotNull(field, "Builder constant " + name);
        return Convert.ToSingle(field.GetValue(null));
    }

    [Test]
    public void StairsAreClimbable()
    {
        // CharacterController.stepOffset on the player prefabs and the NavMesh agent climb are both 0.75.
        float riserWorld = Constant("Riser") * Constant("S");
        Assert.Less(riserWorld, 0.75f, "Stair risers must be below the player's step offset");
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

        MonoBehaviour setup = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .FirstOrDefault(b => b != null && b.GetType().Name == "GameSetup");
        Assert.IsNotNull(setup, "GameSetup");
        SerializedProperty spawns = new SerializedObject(setup).FindProperty("spawnLocations");
        Assert.AreEqual(4, spawns.arraySize, "Party player spawns");
        for (int i = 0; i < spawns.arraySize; i++)
            Assert.IsNotNull(spawns.GetArrayElementAtIndex(i).objectReferenceValue, "Player spawn " + i);
    }
}
