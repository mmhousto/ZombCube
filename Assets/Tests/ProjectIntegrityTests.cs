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
