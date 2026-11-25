using System.Linq;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Editor-only checks that catch project breakage on every pull request:
// missing scripts after package or code changes, and broken scene references.
public class ProjectIntegrityTests
{
    static readonly string[] PrefabFolders = { "Assets/Prefabs", "Assets/Resources" };

    [Test]
    public void BuildScenesExist()
    {
        var missing = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (!File.Exists(scene.path))
                missing.Add(scene.path);
        }
        Assert.IsEmpty(missing, "Build scenes missing from disk:\n" + string.Join("\n", missing));
    }

    // SteamManager.Initialized creates a SteamManager and starts Steam if none exists. Gameplay
    // code reading it started Steam mid-game (e.g. at game over), the likely cause of editor crashes; only the
    // Steam sign-in may start Steam. Everything else uses SteamManager.IsReady.
    [Test]
    public void OnlySignInStartsSteam()
    {
        var offenders = new List<string>();
        foreach (var path in Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories))
        {
            var file = path.Replace('\\', '/');
            if (file.EndsWith("/CloudSaveLogin.cs") || file.Contains("/Steamworks.NET/")) continue;
            if (File.ReadAllText(file).Contains("SteamManager.Initialized"))
                offenders.Add(file);
        }
        Assert.IsEmpty(offenders, "Use SteamManager.IsReady instead of SteamManager.Initialized in:\n" + string.Join("\n", offenders));
    }

    // PlayerViewLayers puts each local player's gun on PlayerView1-4 so other players are drawn by the
    // Main Camera instead of the WeaponCam overlay (which showed them through walls).
    [Test]
    public void PlayerViewLayersExistAndCollideLikeBlaster()
    {
        int blaster = LayerMask.NameToLayer("Blaster");
        Assert.GreaterOrEqual(blaster, 0, "Blaster layer");
        for (int i = 1; i <= 4; i++)
        {
            int layer = LayerMask.NameToLayer("PlayerView" + i);
            Assert.GreaterOrEqual(layer, 0, "Missing layer PlayerView" + i);
            for (int other = 0; other < 32; other++)
            {
                if (other == layer || (other >= LayerMask.NameToLayer("PlayerView1") && other <= LayerMask.NameToLayer("PlayerView4"))) continue;
                Assert.AreEqual(Physics.GetIgnoreLayerCollision(blaster, other), Physics.GetIgnoreLayerCollision(layer, other),
                    $"PlayerView{i} should collide with layer {other} like Blaster does");
            }
        }
    }

    // The minimap camera doesn't render the Walls layer, so every Arena wall needs its own minimap icon.
    [Test]
    public void ArenaWallsHaveMinimapIcons()
    {
        var environment = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Maps/Environment.prefab");
        Assert.IsNotNull(environment, "Arena Environment prefab missing");
        int wallsLayer = LayerMask.NameToLayer("Walls");
        int minimapLayer = LayerMask.NameToLayer("MiniMap");
        var missing = new List<string>();
        int walls = 0;
        foreach (MeshRenderer r in environment.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (r.gameObject.layer != wallsLayer) continue;
            walls++;
            bool hasIcon = false;
            foreach (Transform child in r.transform)
                if (child.gameObject.layer == minimapLayer && child.name.StartsWith("MinimapIcon")) hasIcon = true;
            if (!hasIcon) missing.Add(r.name);
        }
        Assert.Greater(walls, 0, "No Walls-layer objects in the Arena");
        Assert.IsEmpty(missing, "Arena walls without a minimap icon:\n" + string.Join("\n", missing));
    }

    // Minimap icons on the level pieces must use a material (the icon prefab's default material is missing),
    // and it should be one of the unlit Minimap materials.
    [TestCase("Assets/Scenes/Levels/MovieTheatre/MovieTheatreScene.unity")]
    [TestCase("Assets/Scenes/Levels/MovieTheatre/NetworkMovieTheatreScene.unity")]
    [TestCase("Assets/Scenes/Levels/Spaceship/SpaceshipScene.unity")]
    [TestCase("Assets/Scenes/Levels/Spaceship/NetworkSpaceshipScene.unity")]
    public void MinimapIconsUseMinimapMaterials(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) Assert.Ignore(path + " not built yet");
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        int minimapLayer = LayerMask.NameToLayer("MiniMap");
        int wallsLayer = LayerMask.NameToLayer("Walls");
        var bad = new List<string>();
        int icons = 0;
        foreach (MeshRenderer r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (r.gameObject.layer != minimapLayer || !r.name.StartsWith("MinimapIcon")) continue;
            string root = r.transform.root.name;   // only the level geometry (pickups carry their own icons)
            if (!root.StartsWith("==== MOVIE THEATRE") && !root.StartsWith("==== SPACESHIP")) continue;
            icons++;
            Material m = r.sharedMaterial;
            if (m == null || !AssetDatabase.GetAssetPath(m).StartsWith("Assets/Materials/Minimap/"))
                bad.Add($"{r.transform.parent?.name}/{r.name}: {(m == null ? "missing" : AssetDatabase.GetAssetPath(m))}");

            // Wall icons sit at the wall's base: the minimap camera is only a few floors above the player.
            Transform wall = r.transform.parent;
            MeshRenderer wallRenderer = wall != null && wall.gameObject.layer == wallsLayer ? wall.GetComponent<MeshRenderer>() : null;
            if (wallRenderer != null && Mathf.Abs(r.bounds.center.y - wallRenderer.bounds.min.y) > 1f)
                bad.Add($"{wall.name}/{r.name}: icon at y {r.bounds.center.y:F2}, wall base at {wallRenderer.bounds.min.y:F2}");
        }
        Assert.Greater(icons, 0, "No minimap icons found");
        Assert.IsEmpty(bad, "Minimap icons with a wrong material or position:\n" + string.Join("\n", bad.Take(20)));
    }

    [Test]
    public void PrefabsHaveNoMissingScripts()
    {
        var problems = new List<string>();
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", PrefabFolders))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                problems.Add(path + " (could not load)");
                continue;
            }
            CollectMissingScripts(prefab.transform, path, problems);
        }
        Assert.IsEmpty(problems, "Prefabs with missing scripts:\n" + string.Join("\n", problems));
    }

    [Test]
    public void BuildScenesHaveNoMissingScripts()
    {
        var problems = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled || !File.Exists(scene.path))
                continue;

            var opened = EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
            foreach (var root in opened.GetRootGameObjects())
                CollectMissingScripts(root.transform, scene.path, problems);
        }
        Assert.IsEmpty(problems, "Scenes with missing scripts:\n" + string.Join("\n", problems));
    }

    static void CollectMissingScripts(Transform root, string asset, List<string> problems)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            foreach (var component in t.GetComponents<Component>())
            {
                if (component == null)
                    problems.Add(asset + " -> " + t.name);
            }
        }
    }
}
