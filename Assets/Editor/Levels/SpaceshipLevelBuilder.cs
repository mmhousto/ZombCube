using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Kind = Com.GCTC.ZombCube.EditorTools.LevelGeometry.Kind;

namespace Com.GCTC.ZombCube.EditorTools
{
    /// <summary>
    /// Builds "Spaceship", the interior of a derelict starship, from ProBuilder boxes and the Sci-Fi
    /// Texture Pack materials (real-world metres, scaled to the player):
    ///
    ///  - Hangar bay (start): docked shuttle, cargo crates, an aft catwalk up two staircases.
    ///  - Main corridor: the ship's spine from the hangar to the bridge, with rooms either side.
    ///  - Crew quarters and armory (port), med bay (starboard).
    ///  - Reactor room (starboard): a glowing core with a catwalk up a staircase.
    ///  - Service corridors down both sides, looping round the rooms.
    ///  - Bridge (bow): consoles, a raised command deck and windows out to space.
    ///
    /// Every area has a ceiling and its own lights. Enemy spawns are never in the hangar where the
    /// players start, and SpawnPointPicker also skips any spawn point near a player at runtime.
    /// </summary>
    public static class SpaceshipLevelBuilder
    {
        public const string LevelId = "spaceship";
        public const string DisplayName = "Spaceship";
        public const string SoloSceneName = "SpaceshipScene";
        public const string PartySceneName = "NetworkSpaceshipScene";
        public const string DefaultFolder = "Assets/Scenes/Levels/Spaceship";
        public const string LevelRootName = "==== SPACESHIP ====";
        public const string MaterialFolder = "Assets/Materials/Levels/Spaceship";

        // Real-world metres.
        public const float S = LevelGeometry.S;
        public const float CatwalkLevel = 4.5f;   // hangar and reactor catwalks
        public const float DeckLevel = 1.2f;      // bridge command deck
        public const float Riser = LevelGeometry.Riser;
        public const float HullTop = 10f;

        /// <summary>Enemy spawns are at least this far (metres) from every player spawn.</summary>
        public const float MinSpawnDistance = 15f;

        #region Layout (metres: x port (-) to starboard (+), z aft (hangar) to bow (bridge), y = floor height)

        public static readonly Vector3[] PlayerSpawns =
        {
            new Vector3(-10, 0, -16), new Vector3(10, 0, -16),
            new Vector3(-6, 0, -30), new Vector3(6, 0, -30),
        };

        // Rooms beyond the hangar: never in the hangar where the players start.
        public static readonly Vector3[] EnemySpawns =
        {
            new Vector3(-18, 0, 24), new Vector3(-18, 0, 4),                         // port service corridor
            new Vector3(18, 0, 0),                                                   // starboard service corridor
            new Vector3(-12, 0, 2), new Vector3(12, 0, 2), new Vector3(14, 0, 4),    // crew quarters, med bay
            new Vector3(-10, 0, 20), new Vector3(-14, 0, 12),                        // armory
            new Vector3(17, 0, 13), new Vector3(16, CatwalkLevel, 26),               // reactor floor and catwalk
            new Vector3(0, 0, 24),                                                   // corridor, by the bridge
            new Vector3(-16, 0, 38), new Vector3(16, 0, 38), new Vector3(-4, DeckLevel, 38),  // bridge
        };

        // The boss cube (13 m) spawns in the two biggest rooms; the roof lifts off while it is alive.
        public static readonly Vector3[] BossSpawns =
        {
            new Vector3(12, 0, -20),                 // hangar
            new Vector3(-10, 0, 32),                 // bridge
        };

        public static readonly Vector3[] HealthPackSpots =
        {
            new Vector3(0, CatwalkLevel, -34),       // hangar catwalk
            new Vector3(-15, 0, 4.5f),               // crew quarters
            new Vector3(14.5f, 0, 4.5f),             // med bay
            new Vector3(18, CatwalkLevel, 25),       // reactor catwalk
            new Vector3(-18, 0, 30),                 // bridge
        };

        // Same order as the weapon pickups: SMB, AB, Shotblaster, Sniper.
        public static readonly Vector3[] WeaponSpots =
        {
            new Vector3(12, 0, -34),                 // SMB in the hangar
            new Vector3(-15, 0, 22),                 // AB in the armory
            new Vector3(18.5f, 0, -10),              // Shotblaster in the starboard service corridor
            new Vector3(4, DeckLevel, 38.5f),        // Sniper on the command deck
        };

        #endregion

        public static LevelSpec Spec(string folder = DefaultFolder) => new LevelSpec
        {
            id = LevelId,
            displayName = DisplayName,
            soloScene = SoloSceneName,
            partyScene = PartySceneName,
            folder = folder,
            rootName = LevelRootName,
            playerSpawns = PlayerSpawns,
            enemySpawns = EnemySpawns,
            healthPackSpots = HealthPackSpots,
            weaponSpots = WeaponSpots,
            bossSpawns = BossSpawns,
            previewColor = new Color(0.1f, 0.45f, 0.7f, 1f),
            floorLevels = new[] { DeckLevel, CatwalkLevel },
            notWalkablePrefixes = new[] { "Shuttle", "Crate", "Console", "Bunk", "Locker", "Bed", "Pod", "Rack", "Reactor Core", "Reactor Base", "Reactor Ring", "Chair", "Pipe", "Light", "Window", "Table", "Hangar Door" },
            buildGeometry = BuildGeometry,
        };

        [MenuItem("ZombCube/Levels/Build Spaceship")]
        private static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if ((File.Exists($"{DefaultFolder}/{SoloSceneName}.unity") || File.Exists($"{DefaultFolder}/{PartySceneName}.unity")) &&
                !EditorUtility.DisplayDialog("Rebuild Spaceship?",
                    $"This replaces {SoloSceneName} and {PartySceneName}, including any manual changes in them.", "Rebuild", "Cancel"))
                return;

            LevelBuildResult result = Build(DefaultFolder, true);
            foreach (string warning in result.warnings) Debug.LogWarning("[Spaceship] " + warning);
            EditorSceneManager.OpenScene(result.soloScenePath, OpenSceneMode.Single);
            Debug.Log($"[Spaceship] Built {result.soloScenePath} and {result.partyScenePath}. They're in Build Settings and the LevelDatabase.");
        }

        [MenuItem("ZombCube/Levels/Bake Spaceship NavMesh (both scenes)")]
        private static void BakeFromMenu() => LevelPipeline.BakeBothScenes(Spec());

        /// <summary>Builds both scenes into <paramref name="folder"/>; registers them when <paramref name="register"/> is true.</summary>
        public static LevelBuildResult Build(string folder, bool register)
            => LevelPipeline.Build(Spec(folder), new LevelGeometry(MaterialFolder) { wallTop = HullTop, wallThickness = 0.5f, doorHeight = 3f }, folder, register);

        #region Geometry

        private static void BuildGeometry(LevelGeometry g, Transform root)
        {
            Transform hull = LevelGeometry.Group(root, "Hull");
            g.Box(hull, "Deck", 0, 3, 40, 78, -0.5f, 0.5f, g.M("Ship_Floor"), Kind.Floor);

            BuildBulkheads(LevelGeometry.Group(root, "Bulkheads"), g);
            BuildHangar(LevelGeometry.Group(root, "Hangar Bay"), g);
            BuildCorridor(LevelGeometry.Group(root, "Main Corridor"), g);
            BuildCrewQuarters(LevelGeometry.Group(root, "Crew Quarters"), g);
            BuildMedBay(LevelGeometry.Group(root, "Med Bay"), g);
            BuildArmory(LevelGeometry.Group(root, "Armory"), g);
            BuildReactor(LevelGeometry.Group(root, "Reactor Room"), g);
            BuildServiceCorridors(LevelGeometry.Group(root, "Service Corridors"), g);
            BuildBridge(LevelGeometry.Group(root, "Bridge"), g);
        }

        /// <summary>Hull, the walls between areas with their doors, and the solid machinery blocks.</summary>
        private static void BuildBulkheads(Transform t, LevelGeometry g)
        {
            Material wall = g.M("Ship_Wall"), bulkhead = g.M("Ship_Bulkhead");

            // Outer hull. The bow has a long window band onto space.
            g.WallX(t, "Hull Aft", -36, -20, 20, 0, bulkhead);
            g.WallX(t, "Hull Bow", 42, -20, 20, 0, bulkhead, new LevelGeometry.Opening(-14, 14, 1.2f, 4.5f));
            g.WallZ(t, "Hull Port", -20, -36, 42, 0, wall);
            g.WallZ(t, "Hull Starboard", 20, -36, 42, 0, wall);

            // Hangar to the corridors.
            g.WallX(t, "Hangar Fore Wall", -12, -20, 20, 0, bulkhead, g.Door(-19.5f, -16.5f), g.Door(-2, 2), g.Door(16.5f, 19.5f));

            // Main corridor walls with the room doors.
            g.WallZ(t, "Corridor Port Wall", -3, -12, 28, 0, wall, g.Door(-1.5f, 1.5f), g.Door(15, 18));
            g.WallZ(t, "Corridor Starboard Wall", 3, -12, 28, 0, wall, g.Door(-1.5f, 1.5f), g.Door(17, 20.5f));

            // Service corridor walls.
            g.WallZ(t, "Port Service Wall", -16, -12, 28, 0, wall, g.Door(1, 3.5f), g.Door(12, 14.5f));
            g.WallZ(t, "Starboard Service Wall", 16, -12, 10, 0, wall, g.Door(-6, -3.5f));

            // Bridge bulkhead.
            g.WallX(t, "Bridge Aft Wall", 28, -20, 20, 0, bulkhead, g.Door(-19.5f, -16.5f), g.Door(-2, 2), g.Door(10, 13));

            // Machinery between the rooms.
            Machinery(t, g, "Machinery Port Aft", -9.5f, -10, 13, 4);
            Machinery(t, g, "Machinery Starboard Aft", 9.5f, -10, 13, 4);
            Machinery(t, g, "Machinery Port Mid", -9.5f, 8, 13, 4);
            Machinery(t, g, "Machinery Starboard Mid", 9.5f, 8, 13, 4);
            Machinery(t, g, "Machinery Port Fore", -9.5f, 26, 13, 4);

            // Hazard stripes in the doorways.
            foreach (Vector3 d in new[] { new Vector3(-18, 0, -12), new Vector3(0, 0, -12), new Vector3(18, 0, -12), new Vector3(-18, 0, 28), new Vector3(0, 0, 28), new Vector3(11.5f, 0, 28) })
                g.Box(t, "Door Stripe", d.x, d.z, 3, 0.8f, 0f, 0.02f, g.M("Ship_TrimYellow"), Kind.Floor, minimap: false);
        }

        private static void Machinery(Transform t, LevelGeometry g, string name, float x, float z, float width, float depth)
        {
            g.Box(t, name, x, z, width, depth, 0f, HullTop, g.M("Ship_Panel"), Kind.Wall);
        }

        private static void Ceiling(Transform t, LevelGeometry g, string name, float x, float z, float width, float depth, float height)
        {
            g.Box(t, name + " Ceiling", x, z, width, depth, height, 0.3f, g.M("Ship_Panel"), Kind.Wall, minimap: false);
        }

        /// <summary>A ceiling light: a glowing panel and a point light just under it.</summary>
        private static void CeilingLight(Transform t, LevelGeometry g, string name, float x, float z, float ceiling, Color color, float range = 9f, float intensity = 6f)
        {
            g.Box(t, "Light " + name, x, z, 1.6f, 0.4f, ceiling - 0.08f, 0.08f, g.M("Ship_Light"), Kind.Prop, minimap: false);
            var go = new GameObject("Point Light " + name);
            go.transform.SetParent(t, false);
            go.transform.position = new Vector3(x, ceiling - 0.4f, z) * S;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range * S;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
        }

        private static readonly Color CoolWhite = new Color(0.8f, 0.92f, 1f);

        private static void BuildHangar(Transform t, LevelGeometry g)
        {
            Ceiling(t, g, "Hangar", 0, -24, 40, 24, HullTop);

            // Docked shuttle.
            Material hullPlate = g.M("Ship_Panel"), trim = g.M("Ship_TrimOrange");
            g.Box(t, "Shuttle Hull", 0, -24, 6, 10, 0.8f, 3f, hullPlate, Kind.Prop);
            g.Box(t, "Shuttle Cockpit", 0, -18.2f, 4, 1.8f, 1.4f, 2f, g.M("Ship_Window"), Kind.Prop);
            g.Box(t, "Shuttle Wing Port", -5, -24.5f, 4, 3, 1.6f, 0.4f, hullPlate, Kind.Prop);
            g.Box(t, "Shuttle Wing Starboard", 5, -24.5f, 4, 3, 1.6f, 0.4f, hullPlate, Kind.Prop);
            g.Box(t, "Shuttle Stripe", 0, -24, 6.05f, 1, 2.4f, 0.4f, trim, Kind.Prop);
            g.Box(t, "Shuttle Engine", 0, -29.4f, 3, 0.8f, 1.2f, 2f, g.M("Ship_Core"), Kind.Prop);
            foreach (Vector2 leg in new[] { new Vector2(-2, -21), new Vector2(2, -21), new Vector2(-2, -27), new Vector2(2, -27) })
                g.Box(t, "Shuttle Leg", leg.x, leg.y, 0.4f, 0.4f, 0f, 0.8f, g.M("Ship_Grate"), Kind.Prop);

            // Cargo.
            CrateStack(t, g, "Crate Hangar Port", -16, -20);
            CrateStack(t, g, "Crate Hangar Starboard", 15, -22);
            CrateStack(t, g, "Crate Hangar Aft", -15, -29);

            // Sealed hangar door on the aft hull.
            g.Box(t, "Hangar Door", 0, -35.6f, 14, 0.3f, 0f, 3.9f, g.M("Ship_Bulkhead"), Kind.Prop);
            g.Box(t, "Hangar Door Stripe", 0, -35.4f, 14, 0.1f, 1.6f, 0.6f, g.M("Ship_TrimYellow"), Kind.Prop);

            // Aft catwalk up two staircases.
            float run = LevelGeometry.StairRun(CatwalkLevel);
            g.Slab(t, "Hangar Catwalk", 0, -34, 40, 4, CatwalkLevel, g.M("Ship_Grate"));
            g.Rail(t, "Hangar Catwalk Rail Port", -16, -32, 8, true, CatwalkLevel, g.M("Ship_TrimBlue"), 0.15f);
            g.Rail(t, "Hangar Catwalk Rail Middle", 0, -32, 18, true, CatwalkLevel, g.M("Ship_TrimBlue"), 0.15f);
            g.Rail(t, "Hangar Catwalk Rail Starboard", 16, -32, 8, true, CatwalkLevel, g.M("Ship_TrimBlue"), 0.15f);
            g.Stairs(t, "Hangar Stairs Port", -10.5f, -32f + run, 3, new Vector2(0, -1), 0f, CatwalkLevel, g.M("Ship_Grate"));
            g.Stairs(t, "Hangar Stairs Starboard", 10.5f, -32f + run, 3, new Vector2(0, -1), 0f, CatwalkLevel, g.M("Ship_Grate"));
            foreach (float x in new[] { -12.1f, -8.9f, 8.9f, 12.1f })
                g.Box(t, "Hangar Stairs Rail", x, -32f + run / 2f, 0.15f, run, 0f, CatwalkLevel + g.railHeight, g.M("Ship_TrimBlue"), Kind.Wall);

            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 2; j++)
                    CeilingLight(t, g, $"Hangar {i}{j}", -12 + i * 12, -30 + j * 12, HullTop, CoolWhite, 14f, 10f);
        }

        private static void BuildCorridor(Transform t, LevelGeometry g)
        {
            Ceiling(t, g, "Corridor", 0, 8, 6, 40, 4.5f);
            g.Box(t, "Corridor Runner", 0, 8, 2, 40, 0f, 0.02f, g.M("Ship_Grate"), Kind.Floor, minimap: false);
            for (int i = 0; i < 4; i++) CeilingLight(t, g, "Corridor " + i, 0, -8 + i * 11, 4.5f, CoolWhite);
            g.Box(t, "Pipe Corridor Port", -2.6f, 8, 0.3f, 40, 3.6f, 0.3f, g.M("Ship_TrimRed"), Kind.Prop);
            g.Box(t, "Pipe Corridor Starboard", 2.6f, 8, 0.3f, 40, 3.8f, 0.3f, g.M("Ship_TrimBlue"), Kind.Prop);
        }

        private static void BuildCrewQuarters(Transform t, LevelGeometry g)
        {
            Ceiling(t, g, "Crew Quarters", -9.5f, -1, 13, 14, 4f);
            for (int i = 0; i < 3; i++)
            {
                float z = -6.5f + i * 3.3f;
                g.Box(t, "Bunk Lower " + i, -11, z, 4, 1.2f, 0f, 0.6f, g.M("Ship_Panel"), Kind.Prop);
                g.Box(t, "Bunk Upper " + i, -11, z, 4, 1.2f, 1.6f, 0.3f, g.M("Ship_Panel"), Kind.Prop);
                g.Box(t, "Bunk Frame " + i, -13.05f, z, 0.1f, 1.2f, 0f, 1.9f, g.M("Ship_Grate"), Kind.Prop);
            }
            for (int i = 0; i < 4; i++)
                g.Box(t, "Locker " + i, -15.5f, -6 + i * 1.1f, 0.8f, 1f, 0f, 2.2f, g.M("Ship_TrimBlue"), Kind.Prop);
            g.Box(t, "Table Crew", -11, 3.5f, 2.4f, 1.2f, 0f, 0.9f, g.M("Ship_Panel"), Kind.Prop);
            CeilingLight(t, g, "Crew Quarters", -9.5f, -1, 4f, new Color(1f, 0.9f, 0.75f));
        }

        private static void BuildMedBay(Transform t, LevelGeometry g)
        {
            Ceiling(t, g, "Med Bay", 9.5f, -1, 13, 14, 4f);
            for (int i = 0; i < 3; i++)
                g.Box(t, "Bed " + i, 6.5f + i * 3.2f, -6.2f, 1.2f, 2.4f, 0f, 0.8f, g.M("Ship_Panel"), Kind.Prop);
            g.Box(t, "Pod Med 1", 14.5f, 0, 1.4f, 1.4f, 0f, 2.4f, g.M("Ship_Window"), Kind.Prop);
            g.Box(t, "Pod Med 2", 12.5f, 0, 1.4f, 1.4f, 0f, 2.4f, g.M("Ship_Window"), Kind.Prop);
            g.Box(t, "Console Med", 6, 4.8f, 2.4f, 0.8f, 0f, 1.1f, g.M("Ship_Console"), Kind.Prop);
            CeilingLight(t, g, "Med Bay", 9.5f, -1, 4f, new Color(0.75f, 1f, 0.9f));
        }

        private static void BuildArmory(Transform t, LevelGeometry g)
        {
            Ceiling(t, g, "Armory", -9.5f, 17, 13, 14, 4f);
            for (int i = 0; i < 3; i++)
                g.Box(t, "Rack " + i, -9 + i * 3, 23.2f, 2.4f, 0.6f, 0f, 2.2f, g.M("Ship_Bulkhead"), Kind.Prop);
            CrateStack(t, g, "Crate Armory", -14, 17);
            g.Box(t, "Table Armory", -8, 15, 3, 1.2f, 0f, 0.9f, g.M("Ship_TrimOrange"), Kind.Prop);
            CeilingLight(t, g, "Armory", -9.5f, 17, 4f, new Color(1f, 0.6f, 0.45f));
        }

        private static void BuildReactor(Transform t, LevelGeometry g)
        {
            Ceiling(t, g, "Reactor", 11.5f, 19, 17, 18, HullTop);

            // The core: a glowing column on a ring base.
            g.Box(t, "Reactor Base", 11.5f, 19, 6, 6, 0f, 0.8f, g.M("Ship_TrimOrange"), Kind.Prop);
            g.Box(t, "Reactor Core", 11.5f, 19, 3.2f, 3.2f, 0.8f, HullTop - 0.8f, g.M("Ship_Core"), Kind.Prop);
            foreach (float y in new[] { 2.5f, 5.5f, 8.5f })
                g.Box(t, "Reactor Ring", 11.5f, 19, 4, 4, y, 0.4f, g.M("Ship_Bulkhead"), Kind.Prop);

            // Catwalk along the bow wall and the starboard hull, up a staircase.
            float run = LevelGeometry.StairRun(CatwalkLevel);
            g.Slab(t, "Reactor Catwalk Fore", 11.5f, 26, 17, 4, CatwalkLevel, g.M("Ship_Grate"));
            g.Slab(t, "Reactor Catwalk Starboard", 18, 17, 4, 14, CatwalkLevel, g.M("Ship_Grate"));
            g.Stairs(t, "Reactor Stairs", 6.5f, 24f - run, 3, new Vector2(0, 1), 0f, CatwalkLevel, g.M("Ship_Grate"));
            g.Box(t, "Reactor Stairs Rail", 8.1f, 24f - run / 2f, 0.15f, run, 0f, CatwalkLevel + g.railHeight, g.M("Ship_TrimBlue"), Kind.Wall);
            g.Rail(t, "Reactor Catwalk Rail Fore", 12.5f, 24.05f, 9, true, CatwalkLevel, g.M("Ship_TrimBlue"), 0.15f);
            g.Rail(t, "Reactor Catwalk Rail Starboard", 15.95f, 17, 14, false, CatwalkLevel, g.M("Ship_TrimBlue"), 0.15f);
            g.Rail(t, "Reactor Catwalk Rail Fore West", 4.15f, 24.05f, 1.7f, true, CatwalkLevel, g.M("Ship_TrimBlue"), 0.15f);
            g.Rail(t, "Reactor Catwalk Rail Aft", 18, 10.1f, 4, true, CatwalkLevel, g.M("Ship_TrimBlue"), 0.15f);

            CeilingLight(t, g, "Reactor Port", 6, 13, HullTop, new Color(1f, 0.6f, 0.3f), 14f, 8f);
            CeilingLight(t, g, "Reactor Fore", 11.5f, 26, HullTop, CoolWhite, 12f, 8f);
            var glow = new GameObject("Point Light Reactor Core");
            glow.transform.SetParent(t, false);
            glow.transform.position = new Vector3(11.5f, 3f, 19f) * S;
            var light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.55f, 0.15f);
            light.range = 12f * S;
            light.intensity = 10f;
            light.shadows = LightShadows.None;
        }

        private static void BuildServiceCorridors(Transform t, LevelGeometry g)
        {
            Ceiling(t, g, "Port Service", -18, 8, 4, 40, 3.5f);
            Ceiling(t, g, "Starboard Service", 18, -1, 4, 22, 3.5f);
            for (int i = 0; i < 4; i++) CeilingLight(t, g, "Port Service " + i, -18, -8 + i * 11, 3.5f, new Color(1f, 0.45f, 0.35f), 8f, 4f);
            for (int i = 0; i < 2; i++) CeilingLight(t, g, "Starboard Service " + i, 18, -7 + i * 12, 3.5f, new Color(1f, 0.45f, 0.35f), 8f, 4f);
            g.Box(t, "Pipe Port Service", -19.6f, 8, 0.4f, 40, 2.6f, 0.4f, g.M("Ship_TrimOrange"), Kind.Prop);
            g.Box(t, "Pipe Starboard Service", 19.6f, -1, 0.4f, 22, 2.6f, 0.4f, g.M("Ship_TrimOrange"), Kind.Prop);
            g.Box(t, "Crate Port Service", -19.1f, 18, 1.1f, 1.1f, 0f, 1.1f, g.M("Ship_TrimOrange"), Kind.Prop);
        }

        private static void BuildBridge(Transform t, LevelGeometry g)
        {
            Ceiling(t, g, "Bridge", 0, 35, 40, 14, 6f);

            // Window band onto space and the consoles under it.
            g.Box(t, "Window Bow", 0, 41.85f, 28, 0.1f, 1.2f, 3.3f, g.M("Ship_Window"), Kind.Wall);
            for (int i = 0; i < 5; i++)
                g.Box(t, "Console Bow " + i, -10 + i * 5, 40.6f, 3.6f, 1, 0f, 1.1f, g.M("Ship_Console"), Kind.Prop);
            g.Box(t, "Console Port", -17, 34, 1, 4, 0f, 1.1f, g.M("Ship_Console"), Kind.Prop);
            g.Box(t, "Console Starboard", 17, 34, 1, 4, 0f, 1.1f, g.M("Ship_Console"), Kind.Prop);

            // Raised command deck with steps at the front.
            float run = LevelGeometry.StairRun(DeckLevel);
            g.Box(t, "Command Deck", 0, 36.5f, 12, 5, 0f, DeckLevel, g.M("Ship_Floor"), Kind.Floor);
            g.Box(t, "Command Deck Edge", 0, 34.05f, 12, 0.1f, DeckLevel - 0.15f, 0.2f, g.M("Ship_TrimBlue"), Kind.Prop, minimap: false);
            g.Stairs(t, "Command Deck Steps", 0, 34f - run, 4, new Vector2(0, 1), 0f, DeckLevel, g.M("Ship_Grate"));
            g.Box(t, "Chair Captain", 0, 37.5f, 1.2f, 1.2f, DeckLevel, 1.2f, g.M("Ship_TrimRed"), Kind.Prop);

            CeilingLight(t, g, "Bridge Port", -10, 34, 6f, CoolWhite, 10f, 6f);
            CeilingLight(t, g, "Bridge Middle", 0, 34, 6f, CoolWhite, 10f, 6f);
            CeilingLight(t, g, "Bridge Starboard", 10, 34, 6f, CoolWhite, 10f, 6f);
        }

        private static void CrateStack(Transform t, LevelGeometry g, string name, float x, float z)
        {
            g.Box(t, name + " 1", x, z, 1.6f, 1.6f, 0f, 1.6f, g.M("Ship_Panel"), Kind.Prop);
            g.Box(t, name + " 2", x + 1.7f, z + 0.1f, 1.4f, 1.4f, 0f, 1.4f, g.M("Ship_TrimOrange"), Kind.Prop);
            g.Box(t, name + " 3", x + 0.2f, z - 0.1f, 1.2f, 1.2f, 1.6f, 1.2f, g.M("Ship_Panel"), Kind.Prop);
        }

        #endregion
    }
}
