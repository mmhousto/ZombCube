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
    /// <summary>
    /// Builds "Movie Theatre", an abandoned picture palace built from ProBuilder boxes:
    ///
    ///  - Grand lobby (start): twin staircases up to a wraparound balcony, ticket booth,
    ///    concession counter, columns and a chandelier.
    ///  - Upper circle: tiered seats over the back of the auditorium with a projection booth,
    ///    reached from the lobby balcony.
    ///  - Auditorium: rows of seats, a full-width stage with a screen and curtains, and steps up.
    ///  - Side corridors: loop from the lobby past the auditorium to backstage.
    ///  - Backstage (stage level) with a back staircase up to the dressing-room gallery.
    ///
    /// The layout is written in real-world metres for a 1.8 m person and scaled to the player
    /// (<see cref="S"/>), so doors, stairs and rooms are true to life for ZombCube's 3.4-unit player.
    /// Stairs use 18 cm risers and 30 cm treads, which the player and enemy NavMesh agents can climb.
    ///
    /// Each scene is copied from GameScene (Solo / Co-op) or NetworkGameScene (Party), so managers,
    /// HUD, cameras and network setup stay wired. The Arena environment is replaced, spawns and
    /// pickups are moved, and the NavMesh is rebaked. Edit the layout and run the menu item to rebuild.
    /// </summary>
    public static class MovieTheatreLevelBuilder
    {
        public const string LevelId = "theatre";
        public const string DisplayName = "Movie Theatre";
        public const string SoloSceneName = "MovieTheatreScene";
        public const string PartySceneName = "NetworkMovieTheatreScene";
        public const string DefaultFolder = "Assets/Scenes/Levels/MovieTheatre";
        public const string LevelRootName = "==== MOVIE THEATRE ====";

        private const string SoloTemplate = "Assets/Scenes/GameScene.unity";
        private const string PartyTemplate = "Assets/Scenes/NetworkGameScene.unity";
        private const string EnvironmentPrefab = "Assets/Prefabs/Maps/Environment.prefab";
        private const string MinimapIconPrefab = "Assets/Prefabs/UI/MinimapIconSqaure.prefab";
        private const string MaterialFolder = "Assets/Materials/Levels/MovieTheatre";
        private const string LevelDatabasePath = "Assets/Resources/LevelDatabase.asset";

        // The first test map; removed from the LevelDatabase and Build Settings when this one is built.
        private const string LegacyLevelId = "kino";
        private const string LegacySceneFolder = "Assets/Scenes/Levels/Kino";

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

        /// <summary>
        /// Real-world metres to world units: the player is 3.4 units tall (CharacterController),
        /// standing in for a 1.8 m person.
        /// </summary>
        public const float S = 3.4f / 1.8f;

        // Real-world metres.
        public const float WallTop = 10f;
        public const float WallThickness = 0.4f;
        public const float BalconyLevel = 4f;      // lobby balcony and upper circle floor
        public const float StageLevel = 1.2f;      // stage and backstage floor
        public const float GalleryLevel = 5.2f;    // dressing-room gallery floor
        public const float SlabThickness = 0.3f;
        public const float DoorHeight = 2.6f;
        public const float RailHeight = 1.1f;
        public const float Riser = 0.18f;
        public const float Tread = 0.3f;

        #region Layout (real-world metres: x across, z from the entrance (south) to the stage (north), y up)

        // Player spawns in the lobby under the chandelier, facing the auditorium doors.
        private static readonly Vector3[] PlayerSpawns =
        {
            new Vector3(-3, 0, -20), new Vector3(3, 0, -20),
            new Vector3(-3, 0, -16), new Vector3(3, 0, -16),
        };

        // Enemy spawns at boarded doors and windows around the edge of every area (y = floor height).
        private static readonly Vector3[] EnemySpawns =
        {
            new Vector3(-6, 0, -29), new Vector3(6, 0, -29),                                     // lobby entrance
            new Vector3(-14.5f, BalconyLevel, -29), new Vector3(14.5f, BalconyLevel, -29),       // lobby balcony
            new Vector3(-18.5f, 0, -28.5f), new Vector3(-18.5f, 0, 4),                           // west corridor
            new Vector3(18.5f, 0, -28.5f), new Vector3(18.5f, 0, 4),                             // east corridor
            new Vector3(-14.5f, BalconyLevel, -5f), new Vector3(14.5f, BalconyLevel, -5f),         // upper circle
            new Vector3(-19.5f, StageLevel, 24.5f), new Vector3(3, StageLevel, 25),              // backstage
            new Vector3(-19.5f, GalleryLevel, 25), new Vector3(19.5f, GalleryLevel, 21.5f),      // gallery
        };

        private static readonly Vector3[] HealthPackSpots =
        {
            new Vector3(12, 0, -22),                   // lobby, under the east balcony
            new Vector3(-18.5f, 0, -12),               // west corridor
            new Vector3(18.5f, 0, 10),                 // east corridor
            new Vector3(0, BalconyLevel, -5),          // upper circle
            new Vector3(0, StageLevel, 23),            // backstage
        };

        // Same order as WeaponPickupPrefabs: SMB, AB, Shotblaster, Sniper.
        private static readonly Vector3[] WeaponSpots =
        {
            new Vector3(10, BalconyLevel, -28),        // SMB on the lobby balcony
            new Vector3(-10, BalconyLevel, -5),        // AB in the upper circle
            new Vector3(-10, StageLevel, 23),          // Shotblaster backstage
            new Vector3(-17.5f, GalleryLevel, 21),     // Sniper in the dressing-room gallery
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
            public Material carpet, wall, floor, seat, stage, screen, curtain, trim, marble, wood, plaster, debris;
        }

        /// <summary>An opening in a wall: from/to along the wall, bottom/top in metres.</summary>
        private struct Opening
        {
            public float from, to, bottom, top;
            public Opening(float from, float to, float bottom, float top) { this.from = from; this.to = to; this.bottom = bottom; this.top = top; }
        }

        private static Opening Door(float from, float to, float floor = 0f) => new Opening(from, to, floor, floor + DoorHeight);

        [MenuItem("ZombCube/Levels/Build Movie Theatre")]
        private static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            string solo = $"{DefaultFolder}/{SoloSceneName}.unity";
            string party = $"{DefaultFolder}/{PartySceneName}.unity";
            if ((File.Exists(solo) || File.Exists(party)) &&
                !EditorUtility.DisplayDialog("Rebuild Movie Theatre?",
                    $"This replaces {SoloSceneName} and {PartySceneName}, including any manual changes in them.",
                    "Rebuild", "Cancel"))
                return;

            Result result = Build(DefaultFolder, true);
            foreach (string warning in result.warnings)
                Debug.LogWarning("[Movie Theatre] " + warning);

            EditorSceneManager.OpenScene(result.soloScenePath, OpenSceneMode.Single);
            Debug.Log($"[Movie Theatre] Built {result.soloScenePath} and {result.partyScenePath}. They're in Build Settings and the LevelDatabase.");
        }

        /// <summary>
        /// Re-bakes the NavMesh of both Movie Theatre scenes with the theatre's enemy settings, keeping any
        /// manual edits in the scenes. Use this instead of the NavMeshSurface "Bake" button: that bakes with
        /// the project's Boss agent settings (13 units of headroom, 12-unit step), which cuts the doorways and
        /// the space under the balconies out of the enemy NavMesh.
        /// </summary>
        [MenuItem("ZombCube/Levels/Bake Movie Theatre NavMesh (both scenes)")]
        private static void BakeNavMeshFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var result = new Result();
            foreach (string sceneName in new[] { SoloSceneName, PartySceneName })
            {
                string path = $"{DefaultFolder}/{sceneName}.unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                {
                    result.warnings.Add($"{path} not found; run Build Movie Theatre first.");
                    continue;
                }
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                BakeNavMesh(DefaultFolder, sceneName, result);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();

            foreach (string warning in result.warnings)
                Debug.LogWarning("[Movie Theatre] " + warning);
            EditorSceneManager.OpenScene($"{DefaultFolder}/{SoloSceneName}.unity", OpenSceneMode.Single);
            Debug.Log("[Movie Theatre] Baked the NavMesh for both scenes with the theatre's enemy settings.");
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
                RemoveLegacyLevel();
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
            var root = new GameObject(LevelRootName);
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
            BuildShell(Group(root, "Shell"), m);
            BuildLobby(Group(root, "Lobby"), m);
            BuildUpperCircle(Group(root, "Upper Circle"), m);
            BuildAuditorium(Group(root, "Auditorium"), m);
            BuildCorridors(Group(root, "Side Corridors"), m);
            BuildBackstage(Group(root, "Backstage"), m);
        }

        // Footprint: x -21..21, z -30..26. Lobby x -16..16 z -30..-12, auditorium x -16..16 z -12..20,
        // corridors x ±16..±21 z -30..20, backstage x -21..21 z 20..26.
        private static void BuildShell(Transform t, Materials m)
        {
            Box(t, "Ground", 0, -2, 42, 56, -0.5f, 0.5f, m.floor, Kind.Floor);

            WallX(t, "Outer South", -30, -21, 21, 0, m.wall);
            WallX(t, "Outer North", 26, -21, 21, 0, m.wall);
            WallZ(t, "Outer West", -21, -30, 26, 0, m.wall);
            WallZ(t, "Outer East", 21, -30, 26, 0, m.wall);

            // Lobby / auditorium side walls, with doors to the corridors.
            WallZ(t, "West Inner", -16, -30, 20, 0, m.plaster, Door(-16, -13), Door(-2, 1), Door(10, 13));
            WallZ(t, "East Inner", 16, -30, 20, 0, m.plaster, Door(-16, -13), Door(-2, 1), Door(10, 13));

            // Lobby to auditorium: main doors below, upper circle doors from the balcony.
            WallX(t, "Lobby North", -12, -16, 16, 0, m.plaster,
                Door(-3, 3), Door(-15.5f, -12.5f, BalconyLevel), Door(12.5f, 15.5f, BalconyLevel));

            // Stage back wall, with openings at stage level to backstage.
            WallX(t, "Stage Back", 20, -16, 16, 0, m.wall, Door(-15, -12, StageLevel), Door(12, 15, StageLevel));
        }

        private static void BuildLobby(Transform t, Materials m)
        {
            Box(t, "Marble Floor", 0, -21, 32, 18, 0f, 0.02f, m.marble, Kind.Floor);
            Box(t, "Entrance Rug", 0, -27, 6, 5, 0.02f, 0.02f, m.carpet, Kind.Floor);

            // Wraparound balcony (south, west and east) at BalconyLevel.
            Slab(t, "Balcony South", 0, -28, 32, 4, BalconyLevel, m.carpet);
            Slab(t, "Balcony West", -14, -19, 4, 14, BalconyLevel, m.carpet);
            Slab(t, "Balcony East", 14, -19, 4, 14, BalconyLevel, m.carpet);

            // Railings, leaving gaps where the staircases arrive.
            Rail(t, "Balcony Rail South 1", -10.8f, -26, 2.4f, true, BalconyLevel, m.trim);
            Rail(t, "Balcony Rail South 2", 0, -26, 12.8f, true, BalconyLevel, m.trim);
            Rail(t, "Balcony Rail South 3", 10.8f, -26, 2.4f, true, BalconyLevel, m.trim);
            Rail(t, "Balcony Rail West", -12, -19, 14, false, BalconyLevel, m.trim);
            Rail(t, "Balcony Rail East", 12, -19, 14, false, BalconyLevel, m.trim);

            // Twin grand staircases rising south to the balcony.
            Stairs(t, "Grand Stairs West", -8, -26f + StairRun(BalconyLevel), 3f, new Vector2(0, -1), 0f, BalconyLevel, m.carpet);
            Stairs(t, "Grand Stairs East", 8, -26f + StairRun(BalconyLevel), 3f, new Vector2(0, -1), 0f, BalconyLevel, m.carpet);
            Box(t, "Stair Rail West Outer", -9.6f, -26f + StairRun(BalconyLevel) / 2f, 0.15f, StairRun(BalconyLevel), 0f, BalconyLevel + RailHeight, m.trim, Kind.Wall);
            Box(t, "Stair Rail West Inner", -6.4f, -26f + StairRun(BalconyLevel) / 2f, 0.15f, StairRun(BalconyLevel), 0f, BalconyLevel + RailHeight, m.trim, Kind.Wall);
            Box(t, "Stair Rail East Inner", 6.4f, -26f + StairRun(BalconyLevel) / 2f, 0.15f, StairRun(BalconyLevel), 0f, BalconyLevel + RailHeight, m.trim, Kind.Wall);
            Box(t, "Stair Rail East Outer", 9.6f, -26f + StairRun(BalconyLevel) / 2f, 0.15f, StairRun(BalconyLevel), 0f, BalconyLevel + RailHeight, m.trim, Kind.Wall);

            // Columns holding up the balcony.
            foreach (Vector2 c in new[] { new Vector2(-12, -26), new Vector2(-12, -19), new Vector2(-12, -12.6f),
                                          new Vector2(12, -26), new Vector2(12, -19), new Vector2(12, -12.6f),
                                          new Vector2(-3, -26), new Vector2(3, -26) })
                Column(t, "Column", c.x, c.y, 0f, BalconyLevel - SlabThickness, m.marble, m.trim);

            // Entrance doors (boarded), ticket booth, concession counter, debris.
            Box(t, "Entrance Doors", 0, -29.75f, 5, 0.1f, 0f, 3.2f, m.wood, Kind.Prop);
            Box(t, "Entrance Doors Frame", 0, -29.7f, 5.6f, 0.15f, 3.2f, 0.3f, m.trim, Kind.Prop);
            Box(t, "Ticket Booth", 0, -28.2f, 3, 1.4f, 0f, 1.2f, m.wood, Kind.Prop);
            Box(t, "Ticket Booth Top", 0, -28.2f, 3.2f, 1.6f, 1.2f, 0.08f, m.trim, Kind.Prop);
            Box(t, "Ticket Booth Glass Frame", 0, -27.5f, 3, 0.08f, 1.28f, 1.2f, m.trim, Kind.Wall);
            Box(t, "Concession Counter", -14.4f, -21.5f, 1.2f, 6, 0f, 1.1f, m.wood, Kind.Prop);
            Box(t, "Concession Counter Top", -14.4f, -21.5f, 1.4f, 6.2f, 1.1f, 0.06f, m.marble, Kind.Prop);
            Box(t, "Concession Back Shelf", -15.6f, -21.5f, 0.6f, 6, 0f, 2.2f, m.wood, Kind.Wall);
            Box(t, "Poster Case West", -15.75f, -27, 0.1f, 2, 1f, 2.5f, m.trim, Kind.Prop);
            Box(t, "Poster Case East", 15.75f, -27, 0.1f, 2, 1f, 2.5f, m.trim, Kind.Prop);
            Box(t, "Debris West 1", -5, -29, 2, 1.2f, 0f, 0.8f, m.debris, Kind.Prop);
            Box(t, "Debris West 2", -4.5f, -28.6f, 1.2f, 1, 0.8f, 0.6f, m.wood, Kind.Prop);
            Box(t, "Debris East 1", 5, -28.8f, 1.6f, 1.4f, 0f, 0.7f, m.debris, Kind.Prop);

            // Chandelier hanging over the middle of the lobby.
            Box(t, "Chandelier", 0, -19, 2.6f, 2.6f, 7f, 0.5f, m.trim, Kind.Prop);
            Box(t, "Chandelier Tier", 0, -19, 1.6f, 1.6f, 6.5f, 0.5f, m.trim, Kind.Prop);
            Box(t, "Chandelier Chain", 0, -19, 0.1f, 0.1f, 7.5f, WallTop - 7.5f, m.trim, Kind.Prop);
        }

        private static void BuildUpperCircle(Transform t, Materials m)
        {
            // Circle over the back of the auditorium, reached through the upper lobby doors.
            Slab(t, "Circle Floor", 0, -8, 32, 8, BalconyLevel, m.carpet);
            Rail(t, "Circle Rail", 0, -4, 32, true, BalconyLevel, m.trim);
            Column(t, "Circle Column", -8, -4.4f, 0f, BalconyLevel - SlabThickness, m.marble, m.trim);
            Column(t, "Circle Column", 8, -4.4f, 0f, BalconyLevel - SlabThickness, m.marble, m.trim);

            // Projection booth against the lobby wall.
            WallZ(t, "Booth West", -3, -12, -10, BalconyLevel, m.wall, top: BalconyLevel + 3f);
            WallZ(t, "Booth East", 3, -12, -10, BalconyLevel, m.wall, top: BalconyLevel + 3f);
            WallX(t, "Booth Front", -10, -3, 3, BalconyLevel, m.wall, new[] { Door(-0.6f, 0.6f, BalconyLevel), new Opening(1.2f, 2.6f, BalconyLevel + 1.2f, BalconyLevel + 2f) }, BalconyLevel + 3f);
            Box(t, "Booth Roof", 0, -11, 6.4f, 2.4f, BalconyLevel + 3f, 0.2f, m.wall, Kind.Wall);
            Box(t, "Projector", 1.9f, -10.8f, 0.8f, 1.2f, BalconyLevel, 1.3f, m.debris, Kind.Prop);
            Box(t, "Projector Lens", 1.9f, -10.1f, 0.3f, 0.3f, BalconyLevel + 1.4f, 0.3f, m.trim, Kind.Prop);

            // Tiered seats either side of the centre aisle.
            float[] rows = { -9.2f, -7.4f };
            for (int r = 0; r < rows.Length; r++)
            {
                // One stair riser per tier, so the tiers can be stepped onto from the walkways.
                float tier = BalconyLevel + Riser * (rows.Length - r);
                Box(t, $"Circle Tier {r + 1} West", -9.5f, rows[r], 10, 1.8f, BalconyLevel, tier - BalconyLevel, m.carpet, Kind.Floor);
                Box(t, $"Circle Tier {r + 1} East", 9.5f, rows[r], 10, 1.8f, BalconyLevel, tier - BalconyLevel, m.carpet, Kind.Floor);
                SeatRow(t, $"Circle Seats {r + 1} West", -9.5f, rows[r], 9, tier, m);
                SeatRow(t, $"Circle Seats {r + 1} East", 9.5f, rows[r], 9, tier, m);
            }
        }

        private static void BuildAuditorium(Transform t, Materials m)
        {
            Box(t, "Centre Aisle Carpet", 0, 0, 3, 22, 0f, 0.02f, m.carpet, Kind.Floor);
            Box(t, "Cross Aisle Carpet", 0, 10.4f, 32, 2, 0f, 0.02f, m.carpet, Kind.Floor);

            // Rows of seats either side of the centre aisle, with side aisles along the walls.
            for (int r = 0; r < 6; r++)
            {
                float z = -1f + r * 2f;
                SeatRow(t, $"Seats Row {r + 1} West", -7.75f, z, 11.5f, 0f, m);
                SeatRow(t, $"Seats Row {r + 1} East", 7.75f, z, 11.5f, 0f, m);
            }

            // Full-width stage with steps up from the cross aisle.
            Box(t, "Stage", 0, 16.5f, 32, 7, 0f, StageLevel, m.stage, Kind.Floor);
            Box(t, "Stage Lip", 0, 13.05f, 32, 0.1f, StageLevel - 0.2f, 0.25f, m.trim, Kind.Prop);
            Stairs(t, "Stage Steps West", -9.5f, 13f - StairRun(StageLevel), 3f, new Vector2(0, 1), 0f, StageLevel, m.stage);
            Stairs(t, "Stage Steps East", 9.5f, 13f - StairRun(StageLevel), 3f, new Vector2(0, 1), 0f, StageLevel, m.stage);

            // Proscenium, screen and curtains.
            Box(t, "Proscenium West", -15, 13.4f, 2, 0.8f, StageLevel, WallTop - StageLevel, m.plaster, Kind.Wall);
            Box(t, "Proscenium East", 15, 13.4f, 2, 0.8f, StageLevel, WallTop - StageLevel, m.plaster, Kind.Wall);
            Box(t, "Proscenium Arch", 0, 13.4f, 28, 0.8f, 8.5f, WallTop - 8.5f, m.plaster, Kind.Wall);
            Box(t, "Proscenium Trim", 0, 12.95f, 28, 0.1f, 8.3f, 0.2f, m.trim, Kind.Prop);
            Box(t, "Screen", 0, 19.7f, 18, 0.2f, StageLevel + 1.8f, 5f, m.screen, Kind.Prop);
            Box(t, "Screen Frame", 0, 19.75f, 18.6f, 0.2f, StageLevel + 1.5f, 5.6f, m.wood, Kind.Prop);
            Box(t, "Curtain West", -12.5f, 14.4f, 3, 0.6f, StageLevel, 7.3f, m.curtain, Kind.Wall);
            Box(t, "Curtain East", 12.5f, 14.4f, 3, 0.6f, StageLevel, 7.3f, m.curtain, Kind.Wall);
            Box(t, "Curtain Valance", 0, 14.1f, 28, 0.4f, 7.5f, 1f, m.curtain, Kind.Prop);
            Box(t, "Stage Crate", 6, 17.5f, 1.4f, 1.4f, StageLevel, 1.2f, m.wood, Kind.Prop);
            Box(t, "Stage Piano", -6, 16.5f, 2.2f, 1.4f, StageLevel, 1.1f, m.debris, Kind.Prop);

            // Wall sconces.
            for (int i = 0; i < 4; i++)
            {
                float z = -2.5f + i * 4.5f;
                Box(t, "Sconce West", -15.7f, z, 0.2f, 0.6f, 3.2f, 0.5f, m.trim, Kind.Prop);
                Box(t, "Sconce East", 15.7f, z, 0.2f, 0.6f, 3.2f, 0.5f, m.trim, Kind.Prop);
            }
        }

        private static void BuildCorridors(Transform t, Materials m)
        {
            foreach (float side in new[] { -1f, 1f })
            {
                string name = side < 0 ? "West" : "East";
                float x = side * 18.5f;
                Box(t, $"Corridor Runner {name}", x, -5, 2.4f, 48, 0f, 0.02f, m.carpet, Kind.Floor);

                // Steps up to backstage at the north end.
                Stairs(t, $"Backstage Steps {name}", x, 20f - StairRun(StageLevel), 4f, new Vector2(0, 1), 0f, StageLevel, m.wood);

                // Clutter for cover, boarded windows and fire doors.
                Box(t, $"Crates {name} 1", side * 20, -22, 1.4f, 1.4f, 0f, 1.4f, m.wood, Kind.Prop);
                Box(t, $"Crates {name} 2", side * 17.2f, -6, 1.2f, 1.2f, 0f, 1.2f, m.wood, Kind.Prop);
                Box(t, $"Crates {name} 3", side * 20.1f, 6, 1.2f, 2.2f, 0f, 0.9f, m.debris, Kind.Prop);
                Box(t, $"Boarded Window {name} 1", side * 20.75f, -28.5f, 0.1f, 2.4f, 1f, 1.6f, m.wood, Kind.Prop);
                Box(t, $"Boarded Window {name} 2", side * 20.75f, 4, 0.1f, 2.4f, 1f, 1.6f, m.wood, Kind.Prop);
                Box(t, $"Fire Door {name}", side * 20.75f, -10, 0.1f, 1.6f, 0f, 2.4f, m.debris, Kind.Prop);
            }
        }

        private static void BuildBackstage(Transform t, Materials m)
        {
            // Backstage floor at stage level, joined to the stage through the openings in the back wall.
            Box(t, "Backstage Floor", 0, 23, 42, 6, 0f, StageLevel, m.stage, Kind.Floor);

            // Back staircase up to the dressing-room gallery, along the north wall rising east.
            float galleryRise = GalleryLevel - StageLevel;
            float stairStart = 15f - StairRun(galleryRise);
            Stairs(t, "Back Stairs", stairStart, 24.6f, 2.6f, new Vector2(1, 0), StageLevel, galleryRise, m.wood);
            Box(t, "Back Stairs Rail", stairStart + StairRun(galleryRise) / 2f, 23.25f, StairRun(galleryRise), 0.15f, StageLevel, galleryRise + RailHeight, m.trim, Kind.Wall);

            // Gallery floor, open above the staircase.
            Slab(t, "Gallery Floor South", 0, 21.5f, 42, 3, GalleryLevel, m.floor);
            Slab(t, "Gallery Floor North West", -7, 24.5f, 28, 3, GalleryLevel, m.floor);
            Slab(t, "Gallery Floor North East", 18f, 24.5f, 6, 3, GalleryLevel, m.floor);
            Rail(t, "Gallery Rail Stairwell", stairStart - 0.5f + (15f - stairStart + 0.5f) / 2f, 23.1f, 15f - stairStart + 0.5f, true, GalleryLevel, m.trim);
            Rail(t, "Gallery Rail Stairwell End", 7f, 24.5f, 3, false, GalleryLevel, m.trim);

            // Dressing rooms along the north wall of the gallery.
            WallX(t, "Dressing Rooms Front", 22.6f, -21, 7, GalleryLevel, m.plaster,
                Door(-18.5f, -17f, GalleryLevel), Door(-11.5f, -10f, GalleryLevel), Door(-4.5f, -3f, GalleryLevel), Door(2.5f, 4f, GalleryLevel));
            WallZ(t, "Dressing Room Wall 1", -14, 22.6f, 26, GalleryLevel, m.plaster);
            WallZ(t, "Dressing Room Wall 2", -7, 22.6f, 26, GalleryLevel, m.plaster);
            WallZ(t, "Dressing Room Wall 3", 0, 22.6f, 26, GalleryLevel, m.plaster);
            foreach (float x in new[] { -17.5f, -10.5f, -3.5f, 3.5f })
            {
                Box(t, "Vanity", x, 25.5f, 3, 0.7f, GalleryLevel, 0.9f, m.wood, Kind.Prop);
                Box(t, "Vanity Mirror", x, 25.75f, 2.4f, 0.1f, GalleryLevel + 1.1f, 1.1f, m.screen, Kind.Prop);
                Box(t, "Wardrobe", x + 2.4f, 24.2f, 0.8f, 1.6f, GalleryLevel, 2.2f, m.wood, Kind.Prop);
            }

            // Backstage clutter.
            Box(t, "Prop Crates 1", -6, 24.5f, 1.6f, 1.6f, StageLevel, 1.6f, m.wood, Kind.Prop);
            Box(t, "Prop Crates 2", -4.5f, 25, 1.2f, 1.2f, StageLevel, 1f, m.wood, Kind.Prop);
            Box(t, "Rope Rack", 3, 25.7f, 3, 0.4f, StageLevel, 3f, m.trim, Kind.Wall);
            Box(t, "Scenery Flat", -12, 25.6f, 4, 0.2f, StageLevel, 3.4f, m.plaster, Kind.Wall);
        }

        private static Transform Group(Transform parent, string name)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        /// <summary>Horizontal run of a staircase climbing <paramref name="rise"/> metres.</summary>
        private static float StairRun(float rise) => Mathf.Ceil(rise / Riser - 0.001f) * Tread;

        /// <summary>
        /// A solid staircase: steps of <see cref="Riser"/> x <see cref="Tread"/>, starting at (x, z) on
        /// the bottom edge and climbing <paramref name="rise"/> metres along <paramref name="direction"/>.
        /// </summary>
        private static void Stairs(Transform parent, string name, float x, float z, float width, Vector2 direction, float baseY, float rise, Material material)
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
                float w = alongZ ? width : Tread;
                float d = alongZ ? Tread : width;
                Box(group, $"Step {i}", cx, cz, w, d, baseY, riser * i, material, Kind.Floor, minimap: i == steps);
            }
        }

        /// <summary>A floor slab whose top is at <paramref name="level"/>.</summary>
        private static void Slab(Transform parent, string name, float x, float z, float width, float depth, float level, Material material)
        {
            Box(parent, name, x, z, width, depth, level - SlabThickness, SlabThickness, material, Kind.Floor);
        }

        /// <summary>A railing on a floor at <paramref name="level"/>, along X (or Z) centred on (x, z).</summary>
        private static void Rail(Transform parent, string name, float x, float z, float length, bool alongX, float level, Material material)
        {
            Box(parent, name, x, z, alongX ? length : 0.15f, alongX ? 0.15f : length, level, RailHeight, material, Kind.Wall);
            Box(parent, name + " Cap", x, z, alongX ? length : 0.3f, alongX ? 0.3f : length, level + RailHeight, 0.06f, material, Kind.Prop);
        }

        private static void Column(Transform parent, string name, float x, float z, float baseY, float top, Material shaft, Material trim)
        {
            Box(parent, name + " Base", x, z, 0.9f, 0.9f, baseY, 0.3f, trim, Kind.Wall);
            Box(parent, name, x, z, 0.6f, 0.6f, baseY + 0.3f, top - baseY - 0.6f, shaft, Kind.Wall);
            Box(parent, name + " Capital", x, z, 0.9f, 0.9f, top - 0.3f, 0.3f, trim, Kind.Wall);
        }

        /// <summary>A row of cinema seats facing the stage (+z) on a floor at <paramref name="level"/>.</summary>
        private static void SeatRow(Transform parent, string name, float x, float z, float length, float level, Materials m)
        {
            Box(parent, name + " Seat", x, z, length, 0.55f, level, 0.45f, m.seat, Kind.Prop);
            Box(parent, name + " Back", x, z - 0.35f, length, 0.15f, level, 1f, m.seat, Kind.Prop);
            Box(parent, name + " Arm Ends", x, z - 0.1f, length + 0.2f, 0.6f, level + 0.45f, 0.08f, m.wood, Kind.Prop);
        }

        /// <summary>A wall along X at constant z from x0 to x1, with openings (from/to are x).</summary>
        private static void WallX(Transform parent, string name, float z, float x0, float x1, float baseY, Material material, params Opening[] openings)
            => Wall(parent, name, true, z, x0, x1, baseY, WallTop, material, openings);

        private static void WallX(Transform parent, string name, float z, float x0, float x1, float baseY, Material material, Opening[] openings, float top)
            => Wall(parent, name, true, z, x0, x1, baseY, top, material, openings);

        /// <summary>A wall along Z at constant x from z0 to z1, with openings (from/to are z).</summary>
        private static void WallZ(Transform parent, string name, float x, float z0, float z1, float baseY, Material material, params Opening[] openings)
            => Wall(parent, name, false, x, z0, z1, baseY, WallTop, material, openings);

        private static void WallZ(Transform parent, string name, float x, float z0, float z1, float baseY, Material material, float top)
            => Wall(parent, name, false, x, z0, z1, baseY, top, material, new Opening[0]);

        private static void Wall(Transform parent, string name, bool alongX, float fixedCoord, float a0, float a1, float baseY, float top, Material material, Opening[] openings)
        {
            Transform group = Group(parent, name);
            int part = 0;

            void Piece(float from, float to, float bottom, float height)
            {
                if (to - from < 0.01f || height < 0.01f) return;
                float mid = (from + to) / 2f;
                if (alongX) Box(group, $"{name} {++part}", mid, fixedCoord, to - from, WallThickness, bottom, height, material, Kind.Wall);
                else Box(group, $"{name} {++part}", fixedCoord, mid, WallThickness, to - from, bottom, height, material, Kind.Wall);
            }

            float cursor = a0;
            foreach (Opening o in openings.OrderBy(o => o.from))
            {
                Piece(cursor, o.from, baseY, top - baseY);          // solid section before the opening
                Piece(o.from, o.to, baseY, o.bottom - baseY);       // below the opening (sill)
                Piece(o.from, o.to, o.top, top - o.top);            // above the opening (lintel)
                cursor = o.to;
            }
            Piece(cursor, a1, baseY, top - baseY);
        }

        /// <summary>A ProBuilder box. All arguments are real-world metres.</summary>
        private static GameObject Box(Transform parent, string name, float x, float z, float width, float depth, float baseY, float height, Material material, Kind kind, bool minimap = true)
        {
            var size = new Vector3(width, height, depth) * S;
            ProBuilderMesh mesh = ShapeGenerator.GenerateCube(PivotLocation.Center, size);
            GameObject go = mesh.gameObject;
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, baseY + height / 2f, z) * S;
            Finish(go, material, kind);
            if (minimap && kind != Kind.Prop)
                AddMinimapIcon(go, size, kind == Kind.Wall);
            return go;
        }

        private static void Finish(GameObject go, Material material, Kind kind)
        {
            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;

            go.AddComponent<BoxCollider>();

            // The player's ground check is a trigger (PlayerMovement.OnTriggerStay) on an object with
            // no Rigidbody, so the level piece needs one for the trigger to fire. The Arena's floors and
            // walls use a kinematic Rigidbody for the same reason.
            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

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

        #endregion

        #region Helpers

        private static Vector3 World(Vector3 metres) => metres * S;

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

        /// <summary>
        /// The enemy prefabs use the "Boss" agent type, which is 13 units tall with a 12-unit step:
        /// fine on the open Arena, but at true scale it would rule out every doorway and let enemies
        /// hop straight up onto balconies. The enemy surface is baked with the regular enemies' size
        /// instead (same agent type id, so their agents use it); project settings are not changed.
        /// </summary>
        public const float EnemyClearance = 3.4f;   // world units: the player's height (enemy cubes are 3)
        public const float EnemyStep = 0.75f;       // world units: same as the player's step offset

        private static void BakeNavMesh(string folder, string sceneName, Result result)
        {
            NavMeshSurface[] surfaces = Object.FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (surfaces.Length == 0)
            {
                result.warnings.Add($"{sceneName}: no NavMeshSurface found; enemies won't be able to move.");
                return;
            }

            MarkNotWalkable(SceneManager.GetActiveScene());

            string dataFolder = $"{folder}/{sceneName}";
            EnsureFolder(dataFolder);
            var baked = new HashSet<string>();
            for (int i = 0; i < surfaces.Length; i++)
            {
                NavMeshSurface surface = surfaces[i];
                NavMeshBuildSettings settings = NavMesh.GetSettingsByID(surface.agentTypeID);
                if (settings.agentHeight > EnemyClearance || settings.agentClimb > EnemyStep)
                    BuildForTheatre(surface, settings);
                else
                    surface.BuildNavMesh();

                if (surface.navMeshData == null)
                {
                    result.warnings.Add($"{sceneName}: NavMesh bake produced no data for {surface.name}.");
                    continue;
                }

                string assetPath = $"{dataFolder}/NavMesh-{surface.name}-{i}.asset";
                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.CreateAsset(surface.navMeshData, assetPath);
                baked.Add(assetPath);
                EditorUtility.SetDirty(surface);
                PrefabUtility.RecordPrefabInstancePropertyModifications(surface);
            }

            // Drop NavMesh data the surfaces no longer use (e.g. left by the NavMeshSurface Bake button).
            foreach (string guid in AssetDatabase.FindAssets("t:NavMeshData", new[] { dataFolder }))
            {
                string stale = AssetDatabase.GUIDToAssetPath(guid);
                if (!baked.Contains(stale)) AssetDatabase.DeleteAsset(stale);
            }
        }

        // Props whose tops enemies should never use (their NavMesh islands only add links).
        private static readonly string[] NotWalkableSuffixes = { " Cap", " Top" };

        // Cinema seats stay walkable: enemies climb over the rows.
        private static readonly string[] WalkableSeatSuffixes = { " Seat", " Back", " Arm Ends" };
        private static readonly string[] NotWalkablePrefixes =
        {
            "Concession", "Crates", "Prop Crates", "Stage Crate", "Stage Piano", "Chandelier", "Debris", "Poster Case",
            "Sconce", "Vanity", "Wardrobe", "Ticket Booth", "Projector", "Entrance Doors", "Boarded Window", "Fire Door", "Screen",
        };

        /// <summary>
        /// Gives walls, rails, columns and props (not the seats) in the level a NavMeshModifier with the Not Walkable area, so the
        /// enemy NavMesh has no islands on top of them (and no links up to them). Wall pieces whose top is a floor
        /// level are the sills enemies walk across under raised doorways, so they stay walkable.
        /// </summary>
        public static int MarkNotWalkable(Scene scene)
        {
            GameObject root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == LevelRootName);
            if (root == null) return 0;

            int notWalkable = NavMesh.GetAreaFromName("Not Walkable");
            int walls = LayerMask.NameToLayer("Walls");
            float[] floorTops = { StageLevel * S, BalconyLevel * S, GalleryLevel * S };
            int marked = 0;

            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                GameObject go = collider.gameObject;
                if (WalkableSeatSuffixes.Any(x => go.name.EndsWith(x)))
                {
                    // Undo an earlier bake that marked the seats.
                    NavMeshModifier old = go.GetComponent<NavMeshModifier>();
                    if (old != null) Object.DestroyImmediate(old, true);
                    continue;
                }

                bool wall = go.layer == walls;
                bool prop = !wall && (NotWalkableSuffixes.Any(x => go.name.EndsWith(x)) || NotWalkablePrefixes.Any(x => go.name.StartsWith(x)));
                if (!wall && !prop) continue;
                if (wall && floorTops.Any(top => Mathf.Abs(collider.bounds.max.y - top) < 0.05f)) continue;   // doorway sill

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

        /// <summary>Same bake as NavMeshSurface.BuildNavMesh, with the agent's headroom and step capped.</summary>
        private static void BuildForTheatre(NavMeshSurface surface, NavMeshBuildSettings settings)
        {
            settings.agentHeight = Mathf.Min(settings.agentHeight, EnemyClearance);
            settings.agentClimb = Mathf.Min(settings.agentClimb, EnemyStep);
            settings.minRegionArea = surface.minRegionArea;
            settings.buildHeightMesh = surface.buildHeightMesh;
            if (surface.overrideVoxelSize) { settings.overrideVoxelSize = true; settings.voxelSize = surface.voxelSize; }
            if (surface.overrideTileSize) { settings.overrideTileSize = true; settings.tileSize = surface.tileSize; }

            var sources = new List<NavMeshBuildSource>();
            var worldBounds = new Bounds(Vector3.zero, new Vector3(2000f, 1000f, 2000f));
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
            NavMeshBuilder.CollectSources(worldBounds, surface.layerMask, surface.useGeometry, surface.defaultArea, markups, sources);
            if (surface.ignoreNavMeshAgent)
                sources.RemoveAll(s => s.component != null && s.component.gameObject.GetComponent<NavMeshAgent>() != null);
            if (surface.ignoreNavMeshObstacle)
                sources.RemoveAll(s => s.component != null && s.component.gameObject.GetComponent<NavMeshObstacle>() != null);

            Transform t = surface.transform;
            var localBounds = new Bounds(Vector3.zero, worldBounds.size);
            NavMeshData data = NavMeshBuilder.BuildNavMeshData(settings, sources, localBounds, t.position, t.rotation);
            if (data == null) return;
            data.name = surface.gameObject.name;

            surface.RemoveData();
            surface.navMeshData = data;
            if (surface.isActiveAndEnabled)
                surface.AddData();
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
                carpet = Load("Theatre_Carpet"),
                wall = Load("Theatre_Wall"),
                floor = Load("Theatre_Floor"),
                seat = Load("Theatre_Seat"),
                stage = Load("Theatre_Stage"),
                screen = Load("Theatre_Screen"),
                curtain = Load("Theatre_Curtain"),
                trim = Load("Theatre_Trim"),
                marble = Load("Theatre_Marble"),
                wood = Load("Theatre_Wood"),
                plaster = Load("Theatre_Plaster"),
                debris = Load("Theatre_Debris"),
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

        /// <summary>Drops the first test map (Kino) from Build Settings and the LevelDatabase if it is still listed.</summary>
        private static void RemoveLegacyLevel()
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.RemoveAll(s => s.path.StartsWith(LegacySceneFolder + "/")) > 0)
                EditorBuildSettings.scenes = scenes.ToArray();

            var db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(LevelDatabasePath);
            if (db != null && db.entries.RemoveAll(e => e.id == LegacyLevelId) > 0)
                EditorUtility.SetDirty(db);
        }

        private static void RegisterLevel(Result result)
        {
            var db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(LevelDatabasePath);
            if (db == null)
            {
                result.warnings.Add($"LevelDatabase asset not found; add {DisplayName} to it by hand.");
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
