using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

namespace Com.GCTC.ZombCube.EditorTools
{
    /// <summary>What a level builder produced.</summary>
    public class LevelBuildResult
    {
        public string soloScenePath;
        public string partyScenePath;
        public readonly List<string> warnings = new List<string>();
    }

    /// <summary>
    /// Everything the shared pipeline needs to know about one level. Positions are real-world metres
    /// (x across, z forward, y = floor height); <see cref="LevelGeometry.S"/> scales them to the player.
    /// </summary>
    public class LevelSpec
    {
        public string id, displayName, soloScene, partyScene, folder, rootName;
        public Vector3[] playerSpawns, enemySpawns, healthPackSpots, weaponSpots;

        /// <summary>Where the boss cube may spawn (big open rooms); empty means it uses the enemy spawns.</summary>
        public Vector3[] bossSpawns = new Vector3[0];
        public Color previewColor = Color.white;

        /// <summary>Floor heights (metres) above the ground; wall pieces whose top is one of these are walkable sills.</summary>
        public float[] floorLevels = new float[0];

        /// <summary>Props (by name) whose tops enemies should not walk on.</summary>
        public string[] notWalkablePrefixes = new string[0];
        public string[] notWalkableSuffixes = new string[0];

        /// <summary>Builds the level geometry under the given root.</summary>
        public System.Action<LevelGeometry, Transform> buildGeometry;
    }

    /// <summary>ProBuilder geometry helpers. All arguments are real-world metres.</summary>
    public class LevelGeometry
    {
        /// <summary>Real-world metres to world units: the 3.4-unit player stands in for a 1.8 m person.</summary>
        public const float S = 3.4f / 1.8f;

        public float wallTop = 10f, wallThickness = 0.4f, slabThickness = 0.3f, doorHeight = 2.6f, railHeight = 1.1f;
        public const float Riser = 0.18f, Tread = 0.3f;

        public enum Kind { Floor, Wall, Prop }

        public struct Opening
        {
            public float from, to, bottom, top;
            public Opening(float from, float to, float bottom, float top) { this.from = from; this.to = to; this.bottom = bottom; this.top = top; }
        }

        private const string MinimapIconPrefab = "Assets/Prefabs/UI/MinimapIconSqaure.prefab";
        private readonly string materialFolder;
        private readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        public readonly List<string> warnings = new List<string>();

        public LevelGeometry(string materialFolder) { this.materialFolder = materialFolder; }

        public Material M(string name)
        {
            if (materials.TryGetValue(name, out Material m)) return m;
            m = AssetDatabase.LoadAssetAtPath<Material>($"{materialFolder}/{name}.mat");
            if (m == null) warnings.Add($"Missing material {materialFolder}/{name}.mat");
            return materials[name] = m;
        }

        public Opening Door(float from, float to, float floor = 0f) => new Opening(from, to, floor, floor + doorHeight);

        public static Transform Group(Transform parent, string name)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        public static float StairRun(float rise) => Mathf.Ceil(rise / Riser - 0.001f) * Tread;

        public GameObject Box(Transform parent, string name, float x, float z, float width, float depth, float baseY, float height, Material material, Kind kind, bool minimap = true)
        {
            var size = new Vector3(width, height, depth) * S;
            ProBuilderMesh mesh = ShapeGenerator.GenerateCube(PivotLocation.Center, size);
            GameObject go = mesh.gameObject;
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, baseY + height / 2f, z) * S;

            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;
            go.AddComponent<BoxCollider>();

            // The player's ground check is a trigger on an object with no Rigidbody, so level pieces need one.
            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            if (kind == Kind.Wall)
            {
                go.layer = LayerMask.NameToLayer("Walls");
            }
            else
            {
                go.tag = "Ground";
                go.layer = LayerMask.NameToLayer("Ground");
            }

            if (minimap && kind != Kind.Prop) AddMinimapIcon(go, size, kind == Kind.Wall);
            return go;
        }

        /// <summary>A floor slab whose top is at <paramref name="level"/>.</summary>
        public void Slab(Transform parent, string name, float x, float z, float width, float depth, float level, Material material)
            => Box(parent, name, x, z, width, depth, level - slabThickness, slabThickness, material, Kind.Floor);

        /// <summary>A railing or parapet on a floor at <paramref name="level"/>, along X (or Z) centred on (x, z).</summary>
        public void Rail(Transform parent, string name, float x, float z, float length, bool alongX, float level, Material material, float thickness = 0.3f)
            => Box(parent, name, x, z, alongX ? length : thickness, alongX ? thickness : length, level, railHeight, material, Kind.Wall);

        /// <summary>A solid staircase from (x, z) on its bottom edge, climbing <paramref name="rise"/> along <paramref name="direction"/>.</summary>
        public void Stairs(Transform parent, string name, float x, float z, float width, Vector2 direction, float baseY, float rise, Material material)
        {
            Transform group = Group(parent, name);
            int steps = Mathf.CeilToInt(rise / Riser - 0.001f);
            float riser = rise / steps;
            bool alongZ = Mathf.Abs(direction.y) > Mathf.Abs(direction.x);
            for (int i = 1; i <= steps; i++)
            {
                float along = (i - 0.5f) * Tread;
                float cx = alongZ ? x : x + Mathf.Sign(direction.x) * along;
                float cz = alongZ ? z + Mathf.Sign(direction.y) * along : z;
                Box(group, $"Step {i}", cx, cz, alongZ ? width : Tread, alongZ ? Tread : width, baseY, riser * i, material, Kind.Floor, minimap: i == steps);
            }
        }

        public void WallX(Transform parent, string name, float z, float x0, float x1, float baseY, Material material, params Opening[] openings)
            => Wall(parent, name, true, z, x0, x1, baseY, wallTop, material, openings);

        public void WallZ(Transform parent, string name, float x, float z0, float z1, float baseY, Material material, params Opening[] openings)
            => Wall(parent, name, false, x, z0, z1, baseY, wallTop, material, openings);

        public void Wall(Transform parent, string name, bool alongX, float fixedCoord, float a0, float a1, float baseY, float top, Material material, Opening[] openings)
        {
            Transform group = Group(parent, name);
            int part = 0;
            void Piece(float from, float to, float bottom, float height)
            {
                if (to - from < 0.01f || height < 0.01f) return;
                float mid = (from + to) / 2f;
                if (alongX) Box(group, $"{name} {++part}", mid, fixedCoord, to - from, wallThickness, bottom, height, material, Kind.Wall);
                else Box(group, $"{name} {++part}", fixedCoord, mid, wallThickness, to - from, bottom, height, material, Kind.Wall);
            }

            float cursor = a0;
            foreach (Opening o in openings.OrderBy(o => o.from))
            {
                Piece(cursor, o.from, baseY, top - baseY);
                Piece(o.from, o.to, baseY, o.bottom - baseY);
                Piece(o.from, o.to, o.top, top - o.top);
                cursor = o.to;
            }
            Piece(cursor, a1, baseY, top - baseY);
        }

        private static void AddMinimapIcon(GameObject piece, Vector3 size, bool atBase)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MinimapIconPrefab);
            if (prefab == null) return;
            var icon = (GameObject)PrefabUtility.InstantiatePrefab(prefab, piece.transform);
            // Wall icons sit just above the wall's base so the minimap camera (above the player) sees them
            // and catwalks/balconies above can still cover them; floor icons sit on the top face.
            icon.transform.localPosition = new Vector3(0f, atBase ? -size.y / 2f + 0.25f : size.y / 2f + 0.05f, 0f);
            icon.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            icon.transform.localScale = new Vector3(size.x, size.z, 1f);

            // Unlit minimap material in the piece's colour (the icon prefab's own material is missing).
            MeshRenderer pieceRenderer = piece.GetComponent<MeshRenderer>();
            Material minimap = MinimapColors.For(pieceRenderer != null ? pieceRenderer.sharedMaterial : null);
            if (minimap != null)
                foreach (MeshRenderer r in icon.GetComponentsInChildren<MeshRenderer>(true)) r.sharedMaterial = minimap;
            foreach (Collider collider in icon.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(collider);
        }
    }

    /// <summary>
    /// Shared level pipeline: copies GameScene / NetworkGameScene, replaces the Arena with the level,
    /// places spawns and pickups, bakes the enemy NavMesh at the enemies' real size, and registers the
    /// level in Build Settings and the LevelDatabase.
    /// </summary>
    public static class LevelPipeline
    {
        private const string SoloTemplate = "Assets/Scenes/GameScene.unity";
        private const string PartyTemplate = "Assets/Scenes/NetworkGameScene.unity";
        private const string EnvironmentPrefab = "Assets/Prefabs/Maps/Environment.prefab";
        private const string LevelDatabasePath = "Assets/Resources/LevelDatabase.asset";

        private static readonly string[] HealthPackPrefabs = { "Assets/Prefabs/HealthPack.prefab", "Assets/Resources/NetworkHealthPack.prefab" };
        private static readonly string[] WeaponPickupPrefabs =
        {
            "Assets/Prefabs/WeaponPickup.prefab",
            "Assets/Prefabs/WeaponPickup AB.prefab",
            "Assets/Prefabs/WeaponPickup Shotblaster.prefab",
            "Assets/Prefabs/WeaponPickup Sniper Blaster.prefab",
        };
        private static readonly string[] SoloPlayerSpawnNames = { "SP1", "SP2", "SP3", "SP4" };

        /// <summary>
        /// The enemy prefabs use the "Boss" agent type (13 units tall, 12-unit step). The enemy surface is
        /// baked with the regular enemies' size instead (same agent type id); project settings are unchanged.
        /// </summary>
        public const float EnemyClearance = 3.4f;
        public const float EnemyStep = 0.75f;

        private static Vector3 World(Vector3 metres) => metres * LevelGeometry.S;

        public static LevelBuildResult Build(LevelSpec spec, LevelGeometry geometry, string folder, bool register)
        {
            var result = new LevelBuildResult();
            EnsureFolder(folder);
            result.soloScenePath = BuildVariant(spec, geometry, SoloTemplate, folder, spec.soloScene, false, result);
            result.partyScenePath = BuildVariant(spec, geometry, PartyTemplate, folder, spec.partyScene, true, result);
            result.warnings.AddRange(geometry.warnings.Distinct());

            if (register)
            {
                AddToBuildSettings(result.soloScenePath);
                AddToBuildSettings(result.partyScenePath);
                RegisterLevel(spec, result);
            }
            AssetDatabase.SaveAssets();
            return result;
        }

        /// <summary>Rebakes both scenes of a level, keeping manual edits.</summary>
        public static void BakeBothScenes(LevelSpec spec)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var result = new LevelBuildResult();
            foreach (string sceneName in new[] { spec.soloScene, spec.partyScene })
            {
                string path = $"{spec.folder}/{sceneName}.unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) { result.warnings.Add($"{path} not found; build the level first."); continue; }
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                BakeNavMesh(spec, spec.folder, sceneName, result);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            foreach (string warning in result.warnings) Debug.LogWarning($"[{spec.displayName}] {warning}");
            EditorSceneManager.OpenScene($"{spec.folder}/{spec.soloScene}.unity", OpenSceneMode.Single);
            Debug.Log($"[{spec.displayName}] Baked the NavMesh for both scenes with the enemy settings.");
        }

        private static string BuildVariant(LevelSpec spec, LevelGeometry geometry, string template, string folder, string sceneName, bool party, LevelBuildResult result)
        {
            string path = $"{folder}/{sceneName}.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null) AssetDatabase.DeleteAsset(path);
            if (!AssetDatabase.CopyAsset(template, path)) throw new IOException($"Could not copy {template} to {path}");

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Physics.SyncTransforms();

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
                if (go != null && !hover.ContainsKey(go)) hover[go] = HeightAboveFloor(go, 1f);

            foreach (GameObject environment in FindPrefabInstances(scene, new[] { EnvironmentPrefab }))
                Object.DestroyImmediate(environment);
            foreach (GameObject leftover in GameObject.FindGameObjectsWithTag("SpawnPoint"))
            {
                if (PrefabUtility.IsPartOfPrefabInstance(leftover) && !PrefabUtility.IsOutermostPrefabInstanceRoot(leftover))
                    result.warnings.Add($"{sceneName}: left spawn point {leftover.name} (inside a prefab).");
                else Object.DestroyImmediate(leftover);
            }

            var root = new GameObject(spec.rootName);
            spec.buildGeometry(geometry, root.transform);

            var spawnRoot = new GameObject("SpawnPoints").transform;
            spawnRoot.SetParent(root.transform, false);
            for (int i = 0; i < spec.enemySpawns.Length; i++)
            {
                var spawn = new GameObject("SpawnPoint " + (i + 1)) { tag = "SpawnPoint" };
                spawn.transform.SetParent(spawnRoot, false);
                spawn.transform.position = World(spec.enemySpawns[i]);
            }
            for (int i = 0; i < spec.bossSpawns.Length; i++)
            {
                var spawn = new GameObject("BossSpawnPoint " + (i + 1));
                spawn.transform.SetParent(spawnRoot, false);
                spawn.transform.position = World(spec.bossSpawns[i]);
            }

            if (party)
            {
                var group = new GameObject("PlayerSpawns").transform;
                group.SetParent(root.transform, false);
                var spawns = new Transform[spec.playerSpawns.Length];
                for (int i = 0; i < spawns.Length; i++)
                {
                    spawns[i] = new GameObject("PlayerSpawn " + (i + 1)).transform;
                    spawns[i].SetParent(group, false);
                    spawns[i].position = World(spec.playerSpawns[i]) + Vector3.up;
                }
                GameSetup setup = Object.FindFirstObjectByType<GameSetup>();
                if (setup != null)
                {
                    setup.spawnLocations = spawns;
                    EditorUtility.SetDirty(setup);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(setup);
                }
                else result.warnings.Add($"{sceneName}: no GameSetup found for player spawns.");
            }
            else
            {
                for (int i = 0; i < soloSpawns.Length; i++)
                {
                    if (soloSpawns[i] == null) { result.warnings.Add($"{sceneName}: missing {SoloPlayerSpawnNames[i]}."); continue; }
                    Place(soloSpawns[i], World(spec.playerSpawns[i]), Mathf.Clamp(hover[soloSpawns[i]], 0.2f, 3f));
                    soloSpawns[i].transform.rotation = Quaternion.identity;
                }
            }

            for (int i = 0; i < spec.healthPackSpots.Length; i++)
            {
                if (i >= healthPacks.Count) { result.warnings.Add($"{sceneName}: only {healthPacks.Count} health packs to place."); break; }
                Place(healthPacks[i], World(spec.healthPackSpots[i]), hover[healthPacks[i]]);
            }
            for (int i = healthPacks.Count - 1; i >= spec.healthPackSpots.Length; i--) Object.DestroyImmediate(healthPacks[i]);
            for (int i = 0; i < weapons.Count && i < spec.weaponSpots.Length; i++)
                if (weapons[i] != null) Place(weapons[i], World(spec.weaponSpots[i]), hover[weapons[i]]);

            BakeNavMesh(spec, folder, sceneName, result);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return path;
        }

        #region NavMesh

        public static void BakeNavMesh(LevelSpec spec, string folder, string sceneName, LevelBuildResult result)
        {
            NavMeshSurface[] surfaces = Object.FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (surfaces.Length == 0) { result.warnings.Add($"{sceneName}: no NavMeshSurface found; enemies won't be able to move."); return; }

            MarkNotWalkable(spec, SceneManager.GetActiveScene());

            string dataFolder = $"{folder}/{sceneName}";
            EnsureFolder(dataFolder);
            var baked = new HashSet<string>();
            for (int i = 0; i < surfaces.Length; i++)
            {
                NavMeshSurface surface = surfaces[i];
                NavMeshBuildSettings settings = NavMesh.GetSettingsByID(surface.agentTypeID);
                if (settings.agentHeight > EnemyClearance || settings.agentClimb > EnemyStep) BuildForEnemies(surface, settings);
                else surface.BuildNavMesh();

                if (surface.navMeshData == null) { result.warnings.Add($"{sceneName}: NavMesh bake produced no data for {surface.name}."); continue; }

                string assetPath = $"{dataFolder}/NavMesh-{surface.name}-{i}.asset";
                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.CreateAsset(surface.navMeshData, assetPath);
                baked.Add(assetPath);
                EditorUtility.SetDirty(surface);
                PrefabUtility.RecordPrefabInstancePropertyModifications(surface);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:NavMeshData", new[] { dataFolder }))
            {
                string stale = AssetDatabase.GUIDToAssetPath(guid);
                if (!baked.Contains(stale)) AssetDatabase.DeleteAsset(stale);
            }
        }

        /// <summary>
        /// Walls (except sills, whose top is a floor level) and the named props get a Not Walkable
        /// NavMeshModifier, so the enemy NavMesh has no islands or links on top of them.
        /// </summary>
        public static int MarkNotWalkable(LevelSpec spec, Scene scene)
        {
            GameObject root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == spec.rootName);
            if (root == null) return 0;

            int notWalkable = NavMesh.GetAreaFromName("Not Walkable");
            int walls = LayerMask.NameToLayer("Walls");
            float[] floorTops = spec.floorLevels.Select(l => l * LevelGeometry.S).ToArray();
            int marked = 0;
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                GameObject go = collider.gameObject;
                bool wall = go.layer == walls;
                bool prop = !wall && (spec.notWalkableSuffixes.Any(x => go.name.EndsWith(x)) || spec.notWalkablePrefixes.Any(x => go.name.StartsWith(x)));
                if (!wall && !prop) continue;
                if (wall && floorTops.Any(top => Mathf.Abs(collider.bounds.max.y - top) < 0.05f)) continue;

                NavMeshModifier modifier = go.GetComponent<NavMeshModifier>();
                if (modifier == null) modifier = go.AddComponent<NavMeshModifier>();
                modifier.overrideArea = true;
                modifier.area = notWalkable;
                modifier.applyToChildren = true;
                EditorUtility.SetDirty(modifier);
                marked++;
            }
            return marked;
        }

        /// <summary>Same bake as NavMeshSurface.BuildNavMesh, with the agent's headroom and step capped and modifiers applied.</summary>
        private static void BuildForEnemies(NavMeshSurface surface, NavMeshBuildSettings settings)
        {
            settings.agentHeight = Mathf.Min(settings.agentHeight, EnemyClearance);
            settings.agentClimb = Mathf.Min(settings.agentClimb, EnemyStep);
            settings.minRegionArea = surface.minRegionArea;
            settings.buildHeightMesh = surface.buildHeightMesh;
            if (surface.overrideVoxelSize) { settings.overrideVoxelSize = true; settings.voxelSize = surface.voxelSize; }
            if (surface.overrideTileSize) { settings.overrideTileSize = true; settings.tileSize = surface.tileSize; }

            var markups = new List<NavMeshBuildMarkup>();
            foreach (NavMeshModifier modifier in Object.FindObjectsByType<NavMeshModifier>(FindObjectsSortMode.None))
            {
                if (!modifier.isActiveAndEnabled || !modifier.AffectsAgentType(surface.agentTypeID)) continue;
                markups.Add(new NavMeshBuildMarkup
                {
                    root = modifier.transform,
                    overrideArea = modifier.overrideArea,
                    area = modifier.area,
                    ignoreFromBuild = modifier.ignoreFromBuild,
                    applyToChildren = modifier.applyToChildren,
                });
            }

            var sources = new List<NavMeshBuildSource>();
            var worldBounds = new Bounds(Vector3.zero, new Vector3(2000f, 1000f, 2000f));
            NavMeshBuilder.CollectSources(worldBounds, surface.layerMask, surface.useGeometry, surface.defaultArea, markups, sources);
            if (surface.ignoreNavMeshAgent)
                sources.RemoveAll(s => s.component != null && s.component.gameObject.GetComponent<NavMeshAgent>() != null);
            if (surface.ignoreNavMeshObstacle)
                sources.RemoveAll(s => s.component != null && s.component.gameObject.GetComponent<NavMeshObstacle>() != null);

            Transform t = surface.transform;
            NavMeshData data = NavMeshBuilder.BuildNavMeshData(settings, sources, new Bounds(Vector3.zero, worldBounds.size), t.position, t.rotation);
            if (data == null) return;
            data.name = surface.gameObject.name;
            surface.RemoveData();
            surface.navMeshData = data;
            if (surface.isActiveAndEnabled) surface.AddData();
        }

        #endregion

        #region Helpers

        private static void Place(GameObject go, Vector3 floorPoint, float heightAboveFloor)
        {
            Undo.RecordObject(go.transform, "Place");
            go.transform.position = floorPoint + Vector3.up * heightAboveFloor;
        }

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
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    GameObject go = t.gameObject;
                    if (!PrefabUtility.IsAnyPrefabInstanceRoot(go)) continue;
                    if (prefabPaths.Contains(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go))) found.Add(go);
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

        public static void EnsureFolder(string folder)
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

        private static void RegisterLevel(LevelSpec spec, LevelBuildResult result)
        {
            var db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(LevelDatabasePath);
            if (db == null) { result.warnings.Add($"LevelDatabase asset not found; add {spec.displayName} to it by hand."); return; }
            LevelEntry entry = db.entries.FirstOrDefault(e => e.id == spec.id);
            if (entry == null)
            {
                entry = new LevelEntry { id = spec.id };
                db.entries.Add(entry);
            }
            entry.displayName = spec.displayName;
            entry.soloScene = spec.soloScene;
            entry.partyScene = spec.partyScene;
            entry.texturePackCount = 1;
            entry.previewColor = spec.previewColor;
            EditorUtility.SetDirty(db);
        }

        #endregion
    }
}
