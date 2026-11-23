using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Stages the loading screen shots in ScreenshotScene (without saving) and checks the result.
// Capturing needs a GPU and play mode, so CI only checks the staging.
public class LoadingScreenShotsTests
{
    private const string ToolType = "Com.GCTC.ZombCube.EditorTools.LoadingScreenShots, Assembly-CSharp-Editor";

    [OneTimeTearDown]
    public void CleanUp() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

    [Test]
    public void StagesFourInertShotsWithCameras()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/ScreenshotScene.unity", OpenSceneMode.Single);
        Type tool = Type.GetType(ToolType);
        Assert.IsNotNull(tool, "LoadingScreenShots not found");
        tool.GetMethod("Stage", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { false });

        GameObject root = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == "==== LOADING SCREEN SHOTS ====");
        Assert.IsNotNull(root, "Shot root");
        Assert.AreEqual(4, root.transform.childCount, "Shots");
        foreach (Transform shot in root.transform)
        {
            Assert.IsNotNull(shot.GetComponentInChildren<Camera>(true), shot.name + " camera");
            Assert.GreaterOrEqual(shot.GetComponentsInChildren<MeshRenderer>(true).Length, 5, shot.name + " cast");
            Assert.IsTrue(shot.Cast<Transform>().Any(t => t.name.StartsWith("Player")), shot.name + " has no player");

            // The cast must be renderers only, so nothing runs, moves or collides in play mode.
            foreach (Component c in shot.GetComponentsInChildren<Component>(true))
                Assert.IsTrue(c is Transform || c is MeshFilter || c is MeshRenderer || c is SkinnedMeshRenderer || c is SpriteRenderer || c is Camera,
                    $"{shot.name}/{c.name} still has {c.GetType().Name}");
        }
        Assert.Greater(root.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("ZombCube") || t.name.StartsWith("FastCube")), 50, "Enemies");

        // Enemies keep their face (a SpriteRenderer child), visible to the shot cameras.
        int faces = root.GetComponentsInChildren<SpriteRenderer>(false).Count(r => r.gameObject.layer != LayerMask.NameToLayer("MiniMap"));
        Assert.Greater(faces, 50, "Enemy faces");
    }
}
