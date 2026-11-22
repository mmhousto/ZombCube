using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

namespace Com.GCTC.ZombCube.EditorTools
{
    /// <summary>
    /// Builds "Kino Der Cube", a test map inspired by Kino der Toten: a lobby spawn room,
    /// a theatre hall with rows of seats and a stage with a screen, and two side alleys
    /// that loop from the lobby to the stage.
    ///
    /// Each scene is copied from GameScene (Solo / Co-op) or NetworkGameScene (Party), so
    /// managers, HUD, cameras and network setup stay wired. The Arena environment is then
    /// replaced with ProBuilder geometry, spawns and pickups are moved, and the NavMesh is
    /// rebaked. Edit the layout below and run the menu item again to rebuild.
    /// </summary>
    public static class KinoLevelBuilder
    {
        public const string LevelId = "kino";
        public const string DisplayName = "Kino Der Cube";
        public const string SoloSceneName = "KinoScene";
        public const string PartySceneName = "NetworkKinoScene";
        public const string DefaultFolder = "Assets/Scenes/Levels/Kino";

        private const string SoloTemplate = "Assets/Scenes/GameScene.unity";
        private const string PartyTemplate = "Assets/Scenes/NetworkGameScene.unity";
        private const string EnvironmentPrefab = "Assets/Prefabs/Maps/Environment.prefab";
        private const string MinimapIconPrefab = "Assets/Prefabs/UI/MinimapIconSqaure.prefab";
        private const string MaterialFolder = "Assets/Materials/Levels/Kino";
        private const string LevelDatabasePath = "Assets/Resources/LevelDatabase.asset";

        private static readonly string[] HealthPackPrefabs =
        {
            "Assets/Prefabs/HealthPack.prefab",
            "Assets/Resources/NetworkHealthPack.prefab",
        };

        // SMB, AB, Shotblaster, Sniper (the pistol is the starting weapon).
        private static readonly string[] WeaponPickupPrefabs =
        {
            "Assets/Prefabs/WeaponPickup.prefab",
            "Assets/Prefabs/WeaponPickup AB.prefab",
            "Assets/Prefabs/WeaponPickup Shotblaster.prefab",
            "Assets/Prefabs/WeaponPickup Sniper Blaster.prefab",
        };

        private static readonly string[] SoloPlayerSpawnNames = { "SP1", "SP2", "SP3", "SP4" };

        // Layout units are multiplied by Scale on X/Z. Heights are in metres.
        private const float Scale = 1.5f;
        private const float WallHeight = 8f;
        private const float DoorHeight = 4.5f;
        private const float WallThickness = 1f;
        private const float StageHeight = 1.5f;

        #region Layout (layout units: x across, z from lobby to stage)

        // Player spawns in the lobby, facing the theatre.
        private static readonly Vector3[] PlayerSpawns =
        {
            new Vector3(-5, 0, -25), new Vector3(5, 0, -25),
            new Vector3(-5, 0, -20), new Vector3(5, 0, -20),
        };

        // Enemy spawns along the outer walls of every area (y = floor height).
        private static readonly Vector3[] EnemySpawns =
        {
            new Vector3(-14, 0, -28), new Vector3(14, 0, -28),                         // lobby
            new Vector3(-22, 0, -28), new Vector3(-22, 0, 4), new Vector3(-22, 0, 20),  // west alley
            new Vector3(22, 0, -28), new Vector3(22, 0, 4), new Vector3(22, 0, 20),     // east alley
            new Vector3(-14, 0, -12), new Vector3(14, 0, -12),                         // theatre, back rows
            new Vector3(-11, StageHeight, 20), new Vector3(11, StageHeight, 20),       // stage
        };

        private static readonly Vector3[] HealthPackSpots =
        {
            new Vector3(-12, 0, -17),            // lobby
            new Vector3(-20, 0, -8),             // west alley
            new Vector3(20, 0, -8),              // east alley
            new Vector3(0, 0, 4),                // centre aisle
            new Vector3(0, StageHeight, 19),     // stage
        };

        // Same order as WeaponPickupPrefabs: SMB, AB, Shotblaster, Sniper.
        private static readonly Vector3[] WeaponSpots =
        {
            new Vector3(12, 0, -17),             // SMB in the lobby
            new Vector3(-20, 0, 14),             // AB in the west alley
            new Vector3(20, 0, 14),              // Shotblaster in the east alley
            new Vector3(6, StageHeight, 19),     // Sniper on the stage
        };

        #endregion

        public class Result
        {
            public string soloScenePath;
            public string partyScenePath;
            public readonly List<string> warnings = new List<string>();
        }

        private enum Kind { Floor, Wall, Prop }

        private class Materials
        {
            public Material carpet, wall, floor, seat, stage, screen, curtain, trim;
        }

        [MenuItem("ZombCube/Levels/Build Kino Der Cube (test level)")]
        private static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            string solo = $"{DefaultFolder}/{SoloSceneName}.unity";
            string party = $"{DefaultFolder}/{PartySceneName}.unity";
            if ((File.Exists(solo) || File.Exists(party)) &&
                !EditorUtility.DisplayDialog("Rebuild Kino Der Cube?",
                    "This replaces KinoScene and NetworkKinoScene, including any manual changes in them.",
                    "Rebuild", "Cancel"))
                return;

            Result result = Build(DefaultFolder, true);
            foreach (string warning in result.warnings)
                Debug.LogWarning("[Kino] " + warning);

            EditorSceneManager.OpenScene(result.soloScenePath, OpenSceneMode.Single);
            Debug.Log($"[Kino] Built {result.soloScenePath} and {result.partyScenePath}. They're in Build Settings and the LevelDatabase.");
        }

        /// <summary>
        /// Builds both scenes into <paramref name="folder"/>. When <paramref name="register"/>
        /// is true they are added to Build Settings and the LevelDatabase.
        /// </summary>
        public static Result Build(string folder, bool register)
        {
            var result = new Result();
            EnsureFolder(folder);
            Materials materials = LoadMaterials(result);

            result.soloScenePath = BuildVariant(SoloTemplate, folder, SoloSceneName, false, materials, result);
            result.partyScenePath = BuildVariant(PartyTemplate, folder, PartySceneName, true, materials, result);

            if (register)
            {
                AddToBuildSettings(result.soloScenePath);
                AddToBuildSettings(result.partyScenePath);
                RegisterLevel(result);
            }

            AssetDatabase.SaveAssets();
            return result;
        }

        private static string BuildVariant(string template, string folder, string sceneName, bool party, Materials materials, Result result)
        {
            string path = $"{folder}/{sceneName}.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
                AssetDatabase.DeleteAsset(path);
            if (!AssetDatabase.CopyAsset(template, path))
                throw new IOException($"Could not copy {template} to {path}");

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Physics.SyncTransforms();

            // Collect what we move, and how high each one sits above the Arena floor,
            // before the Arena is removed.
            List<GameObject> healthPacks = FindPrefabInstances(scene, HealthPackPrefabs);
            var weapons = new List<GameObject>();
            foreach (string prefab in WeaponPickupPrefabs)
            {
                GameObject weapon = FindPrefabInstances(scene, new[] { prefab }).FirstOrDefault();
                if (weapon == null) result.warnings.Add($"{sceneName}: no instance of {Path.GetFileName(prefab)} to place.");
                weapons.Add(weapon);
            }
            GameObject[] soloSpawns = party ? new GameObject[0] : SoloPlayerSpawnNames.Select(n => FindInScene(scene, n)).ToArray();

            var hover = new Dictionary<GameObject, float>();
            foreach (GameObject go in healthPacks.Concat(weapons).Concat(soloSpawns))
                if (go != null && !hover.ContainsKey(go))
                    hover[go] = HeightAboveFloor(go, 1f);

            // Remove the Arena and any enemy spawn points left in the scene.
            foreach (GameObject environment in FindPrefabInstances(scene, new[] { EnvironmentPrefab }))
                Object.DestroyImmediate(environment);
            foreach (GameObject leftover in GameObject.FindGameObjectsWithTag("SpawnPoint"))
            {
                if (PrefabUtility.IsPartOfPrefabInstance(leftover) && !PrefabUtility.IsOutermostPrefabInstanceRoot(leftover))
                    result.warnings.Add($"{sceneName}: left spawn point {leftover.name} (inside a prefab).");
                else
                    Object.DestroyImmediate(leftover);
            }

            // New environment.
            var root = new GameObject("==== KINO DER CUBE ====");
            BuildGeometry(root.transform, materials);

            var spawnRoot = new GameObject("SpawnPoints").transform;
            spawnRoot.SetParent(root.transform, false);
            for (int i = 0; i < EnemySpawns.Length; i++)
            {
                var spawn = new GameObject("SpawnPoint " + (i + 1)) { tag = "SpawnPoint" };
                spawn.transform.SetParent(spawnRoot, false);
                spawn.transform.position = World(EnemySpawns[i]);
            }

            // Player spawns.
            if (party)
            {
                var group = new GameObject("PlayerSpawns").transform;
                group.SetParent(root.transform, false);
                var spawns = new Transform[PlayerSpawns.Length];
                for (int i = 0; i < PlayerSpawns.Length; i++)
                {
                    spawns[i] = new GameObject("PlayerSpawn " + (i + 1)).transform;
                    spawns[i].SetParent(group, false);
                    spawns[i].position = World(PlayerSpawns[i]) + Vector3.up;
                }

                GameSetup setup = Object.FindFirstObjectByType<GameSetup>();
                if (setup != null)
                {
                    setup.spawnLocations = spawns;
                    EditorUtility.SetDirty(setup);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(setup);
                }
                else
                {
                    result.warnings.Add($"{sceneName}: no GameSetup found for player spawns.");
                }
            }
            else
            {
                for (int i = 0; i < soloSpawns.Length; i++)
                {
                    if (soloSpawns[i] == null) { result.warnings.Add($"{sceneName}: missing {SoloPlayerSpawnNames[i]}."); continue; }
                    Place(soloSpawns[i], World(PlayerSpawns[i]), Mathf.Clamp(hover[soloSpawns[i]], 0.2f, 3f));
                    soloSpawns[i].transform.rotation = Quaternion.identity;
                }
            }

            // Pickups.
            for (int i = 0; i < HealthPackSpots.Length; i++)
            {
                if (i >= healthPacks.Count) { result.warnings.Add($"{sceneName}: only {healthPacks.Count} health packs to place."); break; }
                Place(healthPacks[i], World(HealthPackSpots[i]), hover[healthPacks[i]]);
            }
            for (int i = healthPacks.Count - 1; i >= HealthPackSpots.Length; i--)
                Object.DestroyImmediate(healthPacks[i]);

            for (int i = 0; i < weapons.Count && i < WeaponSpots.Length; i++)
                if (weapons[i] != null)
                    Place(weapons[i], World(WeaponSpots[i]), hover[weapons[i]]);

            BakeNavMesh(folder, sceneName, result);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return path;
        }

        #region Geometry

        private static void BuildGeometry(Transform root, Materials m)
        {
            Transform floors = Group(root, "Floors");
            Transform walls = Group(root, "Walls");
            Transform theatre = Group(root, "Theatre");
            Transform props = Group(root, "Props");

            // Floor across the whole map (top at y = 0).
            Box(floors, "Floor", 0, -4, 48, 52, -1f, 1f, m.floor, Kind.Floor);
            Box(floors, "Lobby Carpet", 0, -22, 30, 14, 0f, 0.02f, m.carpet, Kind.Floor);
            Box(floors, "Aisle Carpet", 0, 0.5f, 6, 27, 0f, 0.02f, m.carpet, Kind.Floor);

            // Outer walls.
            WallAlongX(walls, "Outer South", -30, -24, 24, m.wall);
            WallAlongX(walls, "Outer North", 22, -24, 24, m.wall);
            WallAlongZ(walls, "Outer West", -24, -30, 22, m.wall);
            WallAlongZ(walls, "Outer East", 24, -30, 22, m.wall);

            // Lobby to theatre (main doors), lobby to alleys, theatre to alleys.
            WallAlongX(walls, "Lobby North", -14, -16, 16, m.wall, new Vector2(-3, 3));
            WallAlongZ(walls, "Lobby West", -16, -30, -14, m.wall, new Vector2(-24, -20));
            WallAlongZ(walls, "Lobby East", 16, -30, -14, m.wall, new Vector2(-24, -20));
            WallAlongZ(walls, "Theatre West", -16, -14, 22, m.wall, new Vector2(-6, -2), new Vector2(8, 12));
            WallAlongZ(walls, "Theatre East", 16, -14, 22, m.wall, new Vector2(-6, -2), new Vector2(8, 12));

            // Stage, ramps, screen and curtains.
            Box(theatre, "Stage", 0, 19, 28, 6, 0f, StageHeight, m.stage, Kind.Floor);
            Box(theatre, "Stage Trim", 0, 16.1f, 28, 0.2f, StageHeight - 0.15f, 0.3f, m.trim, Kind.Prop);
            Ramp(theatre, "Stage Ramp West", -10, 13, 16, 3, m.stage);
            Ramp(theatre, "Stage Ramp East", 10, 13, 16, 3, m.stage);
            Box(theatre, "Screen", 0, 21.3f, 22, 0.3f, StageHeight + 2f, 5.5f, m.screen, Kind.Wall);
            Box(theatre, "Curtain West", -13, 19, 2, 6, StageHeight, WallHeight - StageHeight, m.curtain, Kind.Wall);
            Box(theatre, "Curtain East", 13, 19, 2, 6, StageHeight, WallHeight - StageHeight, m.curtain, Kind.Wall);
            Box(theatre, "Projector Booth", 0, -14.5f, 8, 2, DoorHeight + 0.5f, 2.5f, m.wall, Kind.Wall);

            // Rows of seats either side of the centre aisle.
            float[] rows = { -9, -5, -1, 3, 7, 11 };
            foreach (float z in rows)
            {
                Box(theatre, $"Seats West z{z}", -8.5f, z, 9, 1.2f, 0f, 0.9f, m.seat, Kind.Prop);
                Box(theatre, $"Seats East z{z}", 8.5f, z, 9, 1.2f, 0f, 0.9f, m.seat, Kind.Prop);
            }

            // Lobby counters and alley crates for cover.
            Box(props, "Ticket Booth", -8, -24, 4, 2, 0f, 1.2f, m.trim, Kind.Prop);
            Box(props, "Concession Stand", 8, -24, 4, 2, 0f, 1.2f, m.trim, Kind.Prop);
            Box(props, "Crate West 1", -21, -14, 2, 2, 0f, 1.5f, m.stage, Kind.Prop);
            Box(props, "Crate West 2", -19, 6, 2, 2, 0f, 1.5f, m.stage, Kind.Prop);
            Box(props, "Crate East 1", 21, -14, 2, 2, 0f, 1.5f, m.stage, Kind.Prop);
            Box(props, "Crate East 2", 19, 6, 2, 2, 0f, 1.5f, m.stage, Kind.Prop);
        }

        private static Transform Group(Transform parent, string name)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        /// <summary>A wall along X at constant z, with optional doorways (x ranges).</summary>
        private static void WallAlongX(Transform parent, string name, float z, float x0, float x1, Material material, params Vector2[] doors)
        {
            float cursor = x0;
            int part = 0;
            foreach (Vector2 door in doors.OrderBy(d => d.x))
            {
                if (door.x > cursor)
                    Box(parent, $"{name} {++part}", (cursor + door.x) / 2f, z, door.x - cursor, WallThickness, 0f, WallHeight, material, Kind.Wall);
                Box(parent, $"{name} Lintel {part}", (door.x + door.y) / 2f, z, door.y - door.x, WallThickness, DoorHeight, WallHeight - DoorHeight, material, Kind.Wall);
                cursor = door.y;
            }
            if (x1 > cursor)
                Box(parent, $"{name} {++part}", (cursor + x1) / 2f, z, x1 - cursor, WallThickness, 0f, WallHeight, material, Kind.Wall);
        }

        /// <summary>A wall along Z at constant x, with optional doorways (z ranges).</summary>
        private static void WallAlongZ(Transform parent, string name, float x, float z0, float z1, Material material, params Vector2[] doors)
        {
            float cursor = z0;
            int part = 0;
            foreach (Vector2 door in doors.OrderBy(d => d.x))
            {
                if (door.x > cursor)
                    Box(parent, $"{name} {++part}", x, (cursor + door.x) / 2f, WallThickness, door.x - cursor, 0f, WallHeight, material, Kind.Wall);
                Box(parent, $"{name} Lintel {part}", x, (door.x + door.y) / 2f, WallThickness, door.y - door.x, DoorHeight, WallHeight - DoorHeight, material, Kind.Wall);
                cursor = door.y;
            }
            if (z1 > cursor)
                Box(parent, $"{name} {++part}", x, (cursor + z1) / 2f, WallThickness, z1 - cursor, 0f, WallHeight, material, Kind.Wall);
        }

        /// <summary>A ProBuilder box. x/z/width/depth are layout units; baseY/height are metres.</summary>
        private static GameObject Box(Transform parent, string name, float x, float z, float width, float depth, float baseY, float height, Material material, Kind kind)
        {
            var size = new Vector3(width * Scale, height, depth * Scale);
            ProBuilderMesh mesh = ShapeGenerator.GenerateCube(PivotLocation.Center, size);
            GameObject go = mesh.gameObject;
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x * Scale, baseY + height / 2f, z * Scale);
            Finish(go, material, kind);
            if (kind != Kind.Prop)
                AddMinimapIcon(go, size);
            return go;
        }

        /// <summary>A ramp from the floor up to the stage, rising towards +z.</summary>
        private static void Ramp(Transform parent, string name, float x, float z0, float z1, float width, Material material)
        {
            float run = (z1 - z0) * Scale;
            float length = Mathf.Sqrt(run * run + StageHeight * StageHeight);
            float angle = Mathf.Atan2(StageHeight, run) * Mathf.Rad2Deg;
            const float thickness = 0.3f;

            ProBuilderMesh mesh = ShapeGenerator.GenerateCube(PivotLocation.Center, new Vector3(width * Scale, thickness, length));
            GameObject go = mesh.gameObject;
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.rotation = Quaternion.Euler(-angle, 0f, 0f);
            Vector3 middle = new Vector3(x * Scale, StageHeight / 2f, (z0 + z1) / 2f * Scale);
            go.transform.position = middle - go.transform.up * (thickness / 2f);
            Finish(go, material, Kind.Floor);
        }

        private static void Finish(GameObject go, Material material, Kind kind)
        {
            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;

            go.AddComponent<BoxCollider>();

            // Floors and walkable props use the Ground tag/layer like the Arena; walls use Walls.
            if (kind == Kind.Wall)
            {
                go.layer = LayerMask.NameToLayer("Walls");
            }
            else
            {
                go.tag = "Ground";
                go.layer = LayerMask.NameToLayer("Ground");
            }
        }

        private static void AddMinimapIcon(GameObject piece, Vector3 size)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MinimapIconPrefab);
            if (prefab == null) return;

            var icon = (GameObject)PrefabUtility.InstantiatePrefab(prefab, piece.transform);
            icon.transform.localPosition = new Vector3(0f, size.y / 2f + 0.05f, 0f);
            icon.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            icon.transform.localScale = new Vector3(size.x, size.z, 1f);
            foreach (Collider collider in icon.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(collider);
        }

        #endregion

        #region Helpers

        private static Vector3 World(Vector3 layout)
        {
            return new Vector3(layout.x * Scale, layout.y, layout.z * Scale);
        }

        private static void Place(GameObject go, Vector3 floorPoint, float heightAboveFloor)
        {
            Undo.RecordObject(go.transform, "Place");
            go.transform.position = floorPoint + Vector3.up * heightAboveFloor;
        }

        // Distance from the object down to the first collider that isn't part of it.
        private static float HeightAboveFloor(GameObject go, float fallback)
        {
            Vector3 origin = go.transform.position + Vector3.up * 0.05f;
            float best = float.MaxValue;
            foreach (RaycastHit hit in Physics.RaycastAll(origin, Vector3.down, 100f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(go.transform)) continue;
                best = Mathf.Min(best, hit.distance);
            }
            return best == float.MaxValue ? fallback : Mathf.Clamp(best - 0.05f, 0f, 3f);
        }

        private static List<GameObject> FindPrefabInstances(Scene scene, string[] prefabPaths)
        {
            var found = new List<GameObject>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    GameObject go = t.gameObject;
                    if (!PrefabUtility.IsAnyPrefabInstanceRoot(go)) continue;
                    string source = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                    if (prefabPaths.Contains(source)) found.Add(go);
                }
            }
            return found;
        }

        private static GameObject FindInScene(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name) return t.gameObject;
            return null;
        }

        private static void BakeNavMesh(string folder, string sceneName, Result result)
        {
            NavMeshSurface[] surfaces = Object.FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (surfaces.Length == 0)
            {
                result.warnings.Add($"{sceneName}: no NavMeshSurface found; enemies won't be able to move.");
                return;
            }

            string dataFolder = $"{folder}/{sceneName}";
            EnsureFolder(dataFolder);
            for (int i = 0; i < surfaces.Length; i++)
            {
                NavMeshSurface surface = surfaces[i];
                surface.BuildNavMesh();
                if (surface.navMeshData == null)
                {
                    result.warnings.Add($"{sceneName}: NavMesh bake produced no data for {surface.name}.");
                    continue;
                }

                string assetPath = $"{dataFolder}/NavMesh-{surface.name}-{i}.asset";
                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.CreateAsset(surface.navMeshData, assetPath);
                EditorUtility.SetDirty(surface);
                PrefabUtility.RecordPrefabInstancePropertyModifications(surface);
            }
        }

        private static Materials LoadMaterials(Result result)
        {
            Material Load(string name)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/{name}.mat");
                if (material == null) result.warnings.Add($"Missing material {name}.");
                return material;
            }

            return new Materials
            {
                carpet = Load("Kino_Carpet"),
                wall = Load("Kino_Wall"),
                floor = Load("Kino_Floor"),
                seat = Load("Kino_Seat"),
                stage = Load("Kino_Stage"),
                screen = Load("Kino_Screen"),
                curtain = Load("Kino_Curtain"),
                trim = Load("Kino_Trim"),
            };
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        private static void AddToBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == scenePath)) return;
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void RegisterLevel(Result result)
        {
            var db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(LevelDatabasePath);
            if (db == null)
            {
                result.warnings.Add("LevelDatabase asset not found; add Kino Der Cube to it by hand.");
                return;
            }

            LevelEntry entry = db.entries.FirstOrDefault(e => e.id == LevelId);
            if (entry == null)
            {
                entry = new LevelEntry { id = LevelId };
                db.entries.Add(entry);
            }
            entry.displayName = DisplayName;
            entry.soloScene = SoloSceneName;
            entry.partyScene = PartySceneName;
            entry.texturePackCount = 1;
            entry.previewColor = new Color(0.45f, 0.05f, 0.08f, 1f);
            EditorUtility.SetDirty(db);
        }

        #endregion
    }
}
