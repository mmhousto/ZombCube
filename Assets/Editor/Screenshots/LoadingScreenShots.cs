using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Com.GCTC.ZombCube.EditorTools
{
    /// <summary>
    /// Loading screen screenshots from ScreenshotScene.
    ///
    /// 1. Stage: builds four shots next to the menu player spot: inert copies of the player and enemy
    ///    prefabs (renderers only, so nothing runs in play mode) and one camera per shot.
    /// 2. Capture: enters play mode, hides everything but the environment and the shots, and records
    ///    one 1920x1080 PNG per shot camera with the Recorder into Assets/Screenshots/loadingScreen/Shots.
    /// 3. Use as loading screens: imports the PNGs as sprites and shows a random one on the Loading and
    ///    LoadingScene loading screens (runs automatically after Capture).
    ///
    /// Move the cast or the cameras by hand after staging if you like; Capture records whatever is there.
    /// </summary>
    [InitializeOnLoad]
    public static class LoadingScreenShots
    {
        private const string ScenePath = "Assets/Scenes/ScreenshotScene.unity";
        private const string RootName = "==== LOADING SCREEN SHOTS ====";
        private const string ShotPrefix = "Shot ";
        private const string OutputFolder = "Assets/Screenshots/loadingScreen/Shots";
        private const int Width = 1920, Height = 1080;

        private const string CaptureKey = "ZombCube.LoadingShots.Capture";
        private const string ApplyKey = "ZombCube.LoadingShots.Apply";

        private static readonly string[] LoadingScenes = { "Assets/Scenes/Loading.unity", "Assets/Scenes/LoadingScene.unity" };
        private const string OldImageFolder = "Assets/Screenshots/loadingScreen/";

        // Roots left active while capturing (everything else, UI and managers included, is hidden).
        private static readonly string[] KeepRoots = { "EnvironmentMainMenu Variant", "Directional Light", RootName };

        // The menu player's spot in ScreenshotScene; the shots are built around it.
        private static readonly Vector3 Anchor = new Vector3(31.6f, 0f, -48.3f);

        private const string PlayerPrefab = "Assets/Prefabs/Player/Player.prefab";
        private const string ZombCube = "Assets/Prefabs/Enemies/ZombCube.prefab";
        private const string FastCube = "Assets/Prefabs/Enemies/FastCube.prefab";
        private const string DupeCube = "Assets/Prefabs/Enemies/DupeCube.prefab";
        private const string ShieldedCube = "Assets/Prefabs/Enemies/ShieldedCube.prefab";
        private const string BossCube = "Assets/Prefabs/Enemies/BossCube.prefab";
        private const string Armor = "Assets/Prefabs/Enemies/Armor.prefab";

        static LoadingScreenShots()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        #region Menu

        [MenuItem("ZombCube/Screenshots/1. Stage Loading Screen Shots")]
        private static void StageFromMenu()
        {
            if (!OpenScreenshotScene()) return;
            if (FindRoot() != null &&
                !EditorUtility.DisplayDialog("Restage shots?", "This replaces the staged shots, including any changes you made to them.", "Restage", "Cancel"))
                return;
            Stage();
            Selection.activeGameObject = FindRoot().transform.GetChild(0).GetComponentInChildren<Camera>(true).gameObject;
            Debug.Log("[Loading Shots] Staged. Select a shot camera to preview it, then run ZombCube > Screenshots > 2. Capture.");
        }

        [MenuItem("ZombCube/Screenshots/2. Capture Loading Screens (Recorder 1920x1080)")]
        private static void CaptureFromMenu()
        {
            if (!OpenScreenshotScene()) return;
            if (FindRoot() == null) Stage();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            SessionState.SetBool(CaptureKey, true);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("ZombCube/Screenshots/3. Use Shots as Loading Screens")]
        private static void ApplyFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ApplyToLoadingScreens();
        }

        private static bool OpenScreenshotScene()
        {
            if (SceneManager.GetActiveScene().path == ScenePath) return true;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            return true;
        }

        private static GameObject FindRoot() =>
            SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == RootName);

        #endregion

        #region Staging

        private class Shot
        {
            public string name;
            public Transform group;
            public System.Random rng;
        }

        /// <summary>Builds the shots in the open ScreenshotScene; saves the scene when <paramref name="save"/> is true.</summary>
        public static void Stage(bool save = true)
        {
            GameObject old = FindRoot();
            if (old != null) Object.DestroyImmediate(old);
            Physics.SyncTransforms();

            var root = new GameObject(RootName).transform;
            Vector3 forward = Vector3.left;                 // the menu camera looks this way across the arena
            Vector3 side = Vector3.Cross(Vector3.up, forward);

            // 1. Back to back: two players hold the line as the horde pours in.
            {
                Shot s = NewShot(root, "1 Back To Back", 101);
                Player(s, Anchor + side * 0.9f, forward, "gold", "neon_grid", -10f);
                Player(s, Anchor - side * 0.9f + forward * 0.4f, forward, "camo_urban", "stripes_hazard", 12f);
                Horde(s, Anchor, forward, 60f, 9f, 34f, 55);
                ShotCamera(s, Anchor - forward * 6.5f + side * 1.8f + Vector3.up * 1.3f, Anchor + forward * 16f + Vector3.up * 2.2f, 55f);
            }

            // 2. Titan: a lone player faces the boss cube towering over the arena.
            {
                Shot s = NewShot(root, "2 Titan", 202);
                Vector3 bossPos = Anchor + forward * 48f;
                Enemy(s, BossCube, bossPos, Anchor, false);
                Player(s, Anchor, forward, "red", "gold", 0f);
                Horde(s, bossPos, -forward, 120f, 14f, 30f, 18);
                ShotCamera(s, Anchor - forward * 5f - side * 2.2f + Vector3.up * 0.5f, bossPos + Vector3.up * 14f, 62f);
            }

            // 3. Surrounded: four players back to back in a closing ring of cubes, from above.
            {
                Shot s = NewShot(root, "3 Surrounded", 303);
                string[] skins = { "blue", "lime", "purple", "orange" };
                string[] guns = { "silver", "checker", "camo_neon", "polka" };
                for (int i = 0; i < 4; i++)
                {
                    Vector3 dir = Quaternion.Euler(0f, 45f + i * 90f, 0f) * forward;
                    Player(s, Anchor + dir * 1.4f, dir, skins[i], guns[i], 0f);
                }
                Horde(s, Anchor, forward, 360f, 6.5f, 24f, 80);
                ShotCamera(s, Anchor - forward * 13f + side * 7f + Vector3.up * 19f, Anchor + Vector3.up * 0.5f, 52f);
            }

            // 4. Charge: fast cubes rush the camera while a player lines up the shot.
            {
                Shot s = NewShot(root, "4 Charge", 404);
                Player(s, Anchor + forward * 1.2f + side * 2.4f, forward, "black", "stripes_tiger", -8f);
                Horde(s, Anchor + forward * 2f, forward, 26f, 5f, 38f, 30, FastCube, ZombCube);
                ShotCamera(s, Anchor - forward * 2.5f + Vector3.up * 0.45f, Anchor + forward * 18f + Vector3.up * 1.4f, 72f);
            }

            foreach (Transform shot in root) shot.gameObject.SetActive(true);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            if (save) EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        private static Shot NewShot(Transform root, string name, int seed)
        {
            var group = new GameObject(ShotPrefix + name).transform;
            group.SetParent(root, false);
            return new Shot { name = name, group = group, rng = new System.Random(seed) };
        }

        private static void ShotCamera(Shot s, Vector3 position, Vector3 lookAt, float fov)
        {
            var go = new GameObject("Camera");
            go.transform.SetParent(s.group, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.LookRotation(lookAt - position);
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1000f;
            cam.cullingMask = ~LayerMask.GetMask("UI", "MiniMap", "Invisible");
            cam.depth = -10;   // the menu camera stays the Game view camera; select a shot camera to preview it
            go.tag = "Untagged";
        }

        /// <summary>Enemies spread over an arc in front of <paramref name="center"/>, all facing it.</summary>
        private static void Horde(Shot s, Vector3 center, Vector3 facing, float arcDegrees, float minDistance, float maxDistance, int count, params string[] kinds)
        {
            if (kinds.Length == 0) kinds = new[] { ZombCube, ZombCube, ZombCube, FastCube, FastCube, DupeCube, ShieldedCube };
            float floor = FloorAt(center);
            int placed = 0, tries = 0;
            while (placed < count && tries++ < count * 20)
            {
                float t = (float)s.rng.NextDouble();
                float distance = Mathf.Lerp(minDistance, maxDistance, Mathf.Sqrt(t));
                float angle = ((float)s.rng.NextDouble() - 0.5f) * arcDegrees;
                Vector3 pos = center + Quaternion.Euler(0f, angle, 0f) * facing * distance;
                if (Mathf.Abs(FloorAt(pos) - floor) > 1.2f) continue;   // skip walls and ledges
                string kind = kinds[s.rng.Next(kinds.Length)];
                Enemy(s, kind, pos, center + facing * 0.01f, s.rng.NextDouble() < 0.25);
                placed++;
            }
        }

        private static void Enemy(Shot s, string prefab, Vector3 pos, Vector3 target, bool armored)
        {
            GameObject enemy = Statue(prefab, s.group);
            if (enemy == null) return;
            Vector3 look = target - pos; look.y = 0f;
            float jitter = s.rng != null ? ((float)s.rng.NextDouble() - 0.5f) * 30f : 0f;
            enemy.transform.rotation = Quaternion.LookRotation(look.sqrMagnitude > 0.01f ? look : Vector3.forward) * Quaternion.Euler(0f, jitter, 0f);
            enemy.transform.position = pos;

            if (armored)
            {
                GameObject armor = Statue(Armor, enemy.transform);
                if (armor != null)
                {
                    armor.transform.localPosition = Vector3.zero;
                    armor.transform.localRotation = Quaternion.identity;
                }
            }
            SitOnFloor(enemy);
        }

        private static void Player(Shot s, Vector3 pos, Vector3 facing, string skinId, string blasterId, float yawOffset)
        {
            GameObject player = Statue(PlayerPrefab, s.group);
            if (player == null) return;
            player.name = "Player " + skinId;
            player.transform.position = pos;
            player.transform.rotation = Quaternion.LookRotation(facing) * Quaternion.Euler(0f, yawOffset, 0f);

            Material skin = CosmeticMaterial(skinId), blaster = CosmeticMaterial(blasterId);
            foreach (MeshRenderer r in player.GetComponentsInChildren<MeshRenderer>(true))
            {
                Material m = r.CompareTag("Blaster") ? blaster : (r.transform.parent == player.transform ? skin : null);
                if (m != null) r.sharedMaterial = m;
            }
            SitOnFloor(player);
        }

        private static Material CosmeticMaterial(string id)
        {
            int index = CosmeticCatalog.IndexOf(id);
            return index >= 0 ? CosmeticDatabase.MaterialAt(index) : null;
        }

        /// <summary>A copy of a prefab with only its renderers left, so nothing runs or collides.</summary>
        private static GameObject Statue(string prefabPath, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) { Debug.LogWarning("[Loading Shots] Missing prefab " + prefabPath); return null; }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            // Drop UI and minimap children, then every component that isn't part of the look.
            foreach (Canvas canvas in go.GetComponentsInChildren<Canvas>(true))
                if (canvas != null) Object.DestroyImmediate(canvas.gameObject);
            for (int pass = 0; pass < 6; pass++)
            {
                bool removedAny = false;
                foreach (Component c in go.GetComponentsInChildren<Component>(true))
                {
                    if (c == null || IsLook(c)) continue;
                    if (!CanRemove(c)) continue;
                    Object.DestroyImmediate(c);
                    removedAny = true;
                }
                if (!removedAny) break;
            }
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                if (t.gameObject.layer == LayerMask.NameToLayer("MiniMap")) t.gameObject.SetActive(false);
            return go;
        }

        // What a statue keeps: meshes and sprites (the enemies' faces are SpriteRenderers).
        private static bool IsLook(Component c) =>
            c is Transform || c is MeshFilter || c is MeshRenderer || c is SkinnedMeshRenderer || c is SpriteRenderer;

        // A component can go once nothing on its GameObject still requires it.
        private static bool CanRemove(Component c)
        {
            foreach (Component other in c.GetComponents<Component>())
            {
                if (other == null || other == c) continue;
                foreach (RequireComponent req in other.GetType().GetCustomAttributes(typeof(RequireComponent), true))
                    if ((req.m_Type0 != null && req.m_Type0.IsInstanceOfType(c)) ||
                        (req.m_Type1 != null && req.m_Type1.IsInstanceOfType(c)) ||
                        (req.m_Type2 != null && req.m_Type2.IsInstanceOfType(c)))
                        return false;
            }
            return true;
        }

        private static void SitOnFloor(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
            go.transform.position += Vector3.up * (FloorAt(go.transform.position) - b.min.y);
        }

        private static float FloorAt(Vector3 pos)
        {
            var origin = new Vector3(pos.x, 80f, pos.z);
            return Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 200f, ~0, QueryTriggerInteraction.Ignore) ? hit.point.y : 0f;
        }

        #endregion

        #region Capture (play mode)

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(CaptureKey, false))
            {
                SessionState.SetBool(CaptureKey, false);
                new CaptureRun().Start();
            }
            else if (change == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(ApplyKey, false))
            {
                SessionState.SetBool(ApplyKey, false);
                ApplyToLoadingScreens();
            }
        }

        private class CaptureRun
        {
            private readonly List<Transform> shots = new List<Transform>();
            private int index = -1;
            private int waitUntilFrame;
            private RecorderController recorder;
            private string outputAbsolute;

            public void Start()
            {
                Scene scene = SceneManager.GetActiveScene();
                foreach (GameObject root in scene.GetRootGameObjects())
                    if (!KeepRoots.Contains(root.name)) root.SetActive(false);

                GameObject shotRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
                if (shotRoot == null) { Debug.LogError("[Loading Shots] No staged shots."); EditorApplication.isPlaying = false; return; }
                foreach (Transform shot in shotRoot.transform)
                    if (shot.name.StartsWith(ShotPrefix) && shot.GetComponentInChildren<Camera>(true) != null)
                    {
                        shots.Add(shot);
                        shot.gameObject.SetActive(false);
                    }

                outputAbsolute = Path.GetFullPath(OutputFolder);
                Directory.CreateDirectory(outputAbsolute);
                EditorApplication.update += Tick;
                Next();
            }

            private void Next()
            {
                if (index >= 0) shots[index].gameObject.SetActive(false);
                index++;
                if (index >= shots.Count) { Finish(); return; }

                Transform shot = shots[index];
                shot.gameObject.SetActive(true);
                foreach (Camera other in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) other.enabled = false;
                Camera cam = shot.GetComponentInChildren<Camera>(true);
                cam.gameObject.tag = "MainCamera";
                cam.enabled = true;
                waitUntilFrame = Time.frameCount + 10;   // let lighting and post-processing settle
                recorder = null;
            }

            private void Tick()
            {
                if (!EditorApplication.isPlaying) { EditorApplication.update -= Tick; return; }
                if (index < 0 || index >= shots.Count) return;

                if (recorder == null)
                {
                    if (Time.frameCount < waitUntilFrame) return;
                    recorder = StartRecording(shots[index].name.Substring(ShotPrefix.Length).Replace(' ', '_'));
                    return;
                }
                if (!recorder.IsRecording()) Next();
            }

            private RecorderController StartRecording(string fileName)
            {
                var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
                var image = ScriptableObject.CreateInstance<ImageRecorderSettings>();
                image.name = "Loading Screen " + fileName;
                image.Enabled = true;
                image.OutputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.PNG;
                image.CaptureAlpha = false;
                image.imageInputSettings = new CameraInputSettings
                {
                    Source = ImageSource.MainCamera,
                    OutputWidth = Width,
                    OutputHeight = Height,
                    CaptureUI = false,
                };
                image.OutputFile = Path.Combine(outputAbsolute, "LoadingScreen_" + fileName);
                controllerSettings.AddRecorderSettings(image);
                controllerSettings.SetRecordModeToSingleFrame(0);

                var controller = new RecorderController(controllerSettings);
                controller.PrepareRecording();
                controller.StartRecording();
                Debug.Log($"[Loading Shots] Recording {fileName} at {Width}x{Height}");
                return controller;
            }

            private void Finish()
            {
                EditorApplication.update -= Tick;
                Debug.Log($"[Loading Shots] Captured {shots.Count} screenshots into {OutputFolder}.");
                SessionState.SetBool(ApplyKey, true);
                EditorApplication.isPlaying = false;
            }
        }

        #endregion

        #region Loading screens

        private static void ApplyToLoadingScreens()
        {
            AssetDatabase.Refresh();
            var sprites = new List<Sprite>();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { OutputFolder }).OrderBy(g => AssetDatabase.GUIDToAssetPath(g)))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single || importer.mipmapEnabled)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.mipmapEnabled = false;
                    importer.maxTextureSize = 2048;
                    importer.SaveAndReimport();
                }
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null) sprites.Add(sprite);
            }
            if (sprites.Count == 0) { Debug.LogWarning($"[Loading Shots] No screenshots in {OutputFolder}; run Capture first."); return; }

            string returnTo = SceneManager.GetActiveScene().path;
            foreach (string scenePath in LoadingScenes)
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                int updated = 0;
                foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (Image image in root.GetComponentsInChildren<Image>(true))
                    {
                        bool oldLoadingImage = image.sprite != null && AssetDatabase.GetAssetPath(image.sprite).StartsWith(OldImageFolder);
                        if (!oldLoadingImage && image.GetComponent<RandomLoadingImage>() == null) continue;
                        Undo.RecordObject(image, "Loading screen image");
                        image.sprite = sprites[0];
                        image.preserveAspect = false;
                        RandomLoadingImage random = image.GetComponent<RandomLoadingImage>();
                        if (random == null) random = image.gameObject.AddComponent<RandomLoadingImage>();
                        random.images = sprites.ToArray();
                        EditorUtility.SetDirty(image);
                        EditorUtility.SetDirty(random);
                        updated++;
                    }
                if (updated == 0) Debug.LogWarning($"[Loading Shots] {scenePath}: no loading screen image found.");
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            if (!string.IsNullOrEmpty(returnTo)) EditorSceneManager.OpenScene(returnTo, OpenSceneMode.Single);
            Debug.Log($"[Loading Shots] Loading screens now show a random one of {sprites.Count} screenshots.");
        }

        #endregion
    }
}
