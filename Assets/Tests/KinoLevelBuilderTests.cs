using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Runs the Kino Der Cube builder into a temporary folder and checks the result.
// The builder lives in Assembly-CSharp-Editor, which test assemblies can't reference,
// so it is called through reflection.
public class KinoLevelBuilderTests
{
    private const string Folder = "Assets/__KinoBuilderTest";
    private const string BuilderType = "Com.GCTC.ZombCube.EditorTools.KinoLevelBuilder, Assembly-CSharp-Editor";

    private static string soloPath;
    private static string partyPath;

    [OneTimeSetUp]
    public void BuildOnce()
    {
        Type builder = Type.GetType(BuilderType);
        Assert.IsNotNull(builder, "KinoLevelBuilder not found");
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
