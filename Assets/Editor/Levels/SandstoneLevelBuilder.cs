using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Kind = Com.GCTC.ZombCube.EditorTools.LevelGeometry.Kind;

namespace Com.GCTC.ZombCube.EditorTools
{
    /// <summary>
    /// Builds "Sandstone", an original sun-baked desert town (real-world metres, scaled to the player):
    ///
    ///  - The Well (start): a walled plaza with a dry fountain, market stalls and crates.
    ///  - Bazaar street: a long, straight street under canvas awnings, up the west side to the fort gate.
    ///  - Alleys: narrow lanes winding round the east side to the fort's east gate.
    ///  - Tunnel: under a house north of the plaza into a back courtyard, then a passage to the fort.
    ///  - Terrace: the house roof, reached by stairs from the bazaar and the courtyard, overlooking the plaza.
    ///  - Ruined fort: an open walled courtyard with a collapsed tower climbed by rubble steps.
    ///
    /// The town blocks between the streets are solid buildings; everything is open to the sky. Enemy
    /// spawns are at the far ends of the routes, never in the plaza where the players start, and
    /// SpawnPointPicker also skips any spawn point near a player at runtime.
    /// </summary>
    public static class SandstoneLevelBuilder
    {
        public const string LevelId = "sandstone";
        public const string DisplayName = "Sandstone";
        public const string SoloSceneName = "SandstoneScene";
        public const string PartySceneName = "NetworkSandstoneScene";
        public const string DefaultFolder = "Assets/Scenes/Levels/Sandstone";
        public const string LevelRootName = "==== SANDSTONE ====";
        public const string MaterialFolder = "Assets/Materials/Levels/Sandstone";

        // Real-world metres.
        public const float S = LevelGeometry.S;
        public const float RoofLevel = 4f;    // terrace over the tunnel house
        public const float TowerLevel = 3f;   // collapsed tower platform in the fort
        public const float Riser = LevelGeometry.Riser;

        /// <summary>Enemy spawns are at least this far (metres) from every player spawn.</summary>
        public const float MinSpawnDistance = 15f;

        #region Layout (metres: x west to east, z south (plaza) to north (fort), y = floor height)

        // Players start in the plaza, around the dry fountain.
        public static readonly Vector3[] PlayerSpawns =
        {
            new Vector3(-6, 0, -14), new Vector3(6, 0, -14),
            new Vector3(-6, 0, -26), new Vector3(6, 0, -26),
        };

        // Far ends of every route: never in the plaza.
        public static readonly Vector3[] EnemySpawns =
        {
            new Vector3(-9, 0, 26), new Vector3(-8, 0, 8),                              // bazaar
            new Vector3(22, 0, -22), new Vector3(28, 0, -4), new Vector3(28, 0, 18),    // east alleys
            new Vector3(22, 0, 34),                                                     // alley to the fort
            new Vector3(-4, 0, 13), new Vector3(0, 0, 24),                              // courtyard and passage
            new Vector3(-3, RoofLevel, 2),                                              // terrace
            new Vector3(-14, 0, 42), new Vector3(-14, 0, 30), new Vector3(4, 0, 42),    // fort
            new Vector3(13, 0, 30), new Vector3(11, TowerLevel, 41),                    // fort and tower
        };

        public static readonly Vector3[] HealthPackSpots =
        {
            new Vector3(-11.5f, 0, -31),            // plaza corner
            new Vector3(-11.4f, 0, 6),              // bazaar
            new Vector3(29.4f, 0, -10),             // east alley
            new Vector3(5, RoofLevel, -6),          // terrace
            new Vector3(-15.2f, 0, 36),             // fort
        };

        // Same order as the weapon pickups: SMB, AB, Shotblaster, Sniper.
        public static readonly Vector3[] WeaponSpots =
        {
            new Vector3(-5.2f, 0, 14),              // SMB in the courtyard
            new Vector3(29.4f, 0, 26),              // AB in the east alley
            new Vector3(-11.4f, 0, 26.5f),          // Shotblaster at the end of the bazaar
            new Vector3(14, TowerLevel, 42),        // Sniper on the tower
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
            previewColor = new Color(0.86f, 0.68f, 0.42f, 1f),
            floorLevels = new[] { TowerLevel, RoofLevel },
            notWalkablePrefixes = new[] { "Stall", "Crates", "Barrel", "Fountain", "Awning", "Rubble", "Door", "Pillar", "Lamp", "Ruin" },
            buildGeometry = BuildGeometry,
        };

        [MenuItem("ZombCube/Levels/Build Sandstone")]
        private static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if ((File.Exists($"{DefaultFolder}/{SoloSceneName}.unity") || File.Exists($"{DefaultFolder}/{PartySceneName}.unity")) &&
                !EditorUtility.DisplayDialog("Rebuild Sandstone?",
                    $"This replaces {SoloSceneName} and {PartySceneName}, including any manual changes in them.", "Rebuild", "Cancel"))
                return;

            LevelBuildResult result = Build(DefaultFolder, true);
            foreach (string warning in result.warnings) Debug.LogWarning("[Sandstone] " + warning);
            EditorSceneManager.OpenScene(result.soloScenePath, OpenSceneMode.Single);
            Debug.Log($"[Sandstone] Built {result.soloScenePath} and {result.partyScenePath}. They're in Build Settings and the LevelDatabase.");
        }

        [MenuItem("ZombCube/Levels/Bake Sandstone NavMesh (both scenes)")]
        private static void BakeFromMenu() => LevelPipeline.BakeBothScenes(Spec());

        /// <summary>Builds both scenes into <paramref name="folder"/>; registers them when <paramref name="register"/> is true.</summary>
        public static LevelBuildResult Build(string folder, bool register)
            => LevelPipeline.Build(Spec(folder), NewGeometry(), folder, register);

        private static LevelGeometry NewGeometry() => new LevelGeometry(MaterialFolder) { wallTop = 7f, wallThickness = 0.6f };

        #region Geometry

        private static void BuildGeometry(LevelGeometry g, Transform root)
        {
            Transform ground = LevelGeometry.Group(root, "Ground");
            g.Box(ground, "Sand", 7, 6, 50, 80, -0.5f, 0.5f, g.M("Sand_Ground"), Kind.Floor);
            g.Box(ground, "Plaza Paving", 0, -20, 24, 24, 0f, 0.02f, g.M("Sand_Paving"), Kind.Floor);
            g.Box(ground, "Bazaar Cobbles", -9, 10, 6, 36, 0f, 0.02f, g.M("Sand_Paving"), Kind.Floor);
            g.Box(ground, "Fort Flagstones", 0, 36, 32, 16, 0f, 0.02f, g.M("Sand_Stone"), Kind.Floor);

            BuildTown(LevelGeometry.Group(root, "Town Blocks"), g);
            BuildPlaza(LevelGeometry.Group(root, "The Well"), g);
            BuildBazaar(LevelGeometry.Group(root, "Bazaar"), g);
            BuildTunnelHouse(LevelGeometry.Group(root, "Tunnel House"), g);
            BuildCourtyard(LevelGeometry.Group(root, "Courtyard"), g);
            BuildAlleys(LevelGeometry.Group(root, "Alleys"), g);
            BuildFort(LevelGeometry.Group(root, "Ruined Fort"), g);
        }

        /// <summary>The outer wall and the solid buildings between the streets.</summary>
        private static void BuildTown(Transform t, LevelGeometry g)
        {
            Material wall = g.M("Sand_Sandstone");
            g.WallX(t, "Town Wall South", -32, -16, 30, 0, wall);
            g.WallX(t, "Town Wall North", 44, -16, 30, 0, wall);
            g.WallZ(t, "Town Wall West", -16, -32, 44, 0, wall);
            g.WallZ(t, "Town Wall East", 30, -32, 44, 0, wall);

            var rng = new System.Random(7);
            Block(t, g, rng, "Block West", -16, -12, -32, 28);
            Block(t, g, rng, "Block East", 6, 26, -8, 28);
            Block(t, g, rng, "Block North East", 2, 6, 16, 28);
            Block(t, g, rng, "Block North West", -6, -2, 16, 28);
            Block(t, g, rng, "Block Alley North", 12, 26, -20, -8);
            Block(t, g, rng, "Block Alley South", 12, 30, -32, -24);
            Block(t, g, rng, "Block Fort East", 16, 26, 28, 32);
            Block(t, g, rng, "Block Far North", 16, 30, 36, 44);
        }

        /// <summary>A row of houses filling a rectangle, with varied heights and colours.</summary>
        private static void Block(Transform parent, LevelGeometry g, System.Random rng, string name, float x0, float x1, float z0, float z1)
        {
            Transform t = LevelGeometry.Group(parent, name);
            string[] looks = { "Sand_Sandstone", "Sand_Plaster", "Sand_Terracotta", "Sand_Sandstone" };
            bool alongX = (x1 - x0) >= (z1 - z0);
            float start = alongX ? x0 : z0, end = alongX ? x1 : z1;
            int house = 0;
            while (start < end - 0.01f)
            {
                float length = Mathf.Min(end - start, 6f + (float)rng.NextDouble() * 5f);
                if (end - (start + length) < 3f) length = end - start;   // no slivers
                float height = 5.5f + (float)rng.NextDouble() * 3f;
                float mid = start + length / 2f;
                Material look = g.M(looks[rng.Next(looks.Length)]);
                if (alongX) g.Box(t, $"{name} House {++house}", mid, (z0 + z1) / 2f, length, z1 - z0, 0f, height, look, Kind.Wall);
                else g.Box(t, $"{name} House {++house}", (x0 + x1) / 2f, mid, x1 - x0, length, 0f, height, look, Kind.Wall);
                start += length;
            }
        }

        private static void BuildPlaza(Transform t, LevelGeometry g)
        {
            Material stone = g.M("Sand_Stone"), sandstone = g.M("Sand_Sandstone");

            // Dry fountain in the middle of the plaza.
            g.Box(t, "Fountain Basin", 0, -20, 5.5f, 5.5f, 0f, 0.15f, stone, Kind.Prop);
            g.Box(t, "Fountain Rim North", 0, -17.25f, 6, 0.5f, 0f, 0.8f, sandstone, Kind.Prop);
            g.Box(t, "Fountain Rim South", 0, -22.75f, 6, 0.5f, 0f, 0.8f, sandstone, Kind.Prop);
            g.Box(t, "Fountain Rim West", -2.75f, -20, 0.5f, 5, 0f, 0.8f, sandstone, Kind.Prop);
            g.Box(t, "Fountain Rim East", 2.75f, -20, 0.5f, 5, 0f, 0.8f, sandstone, Kind.Prop);
            g.Box(t, "Fountain Pillar", 0, -20, 0.8f, 0.8f, 0f, 2.2f, stone, Kind.Prop);
            g.Box(t, "Fountain Bowl", 0, -20, 2.2f, 2.2f, 2.2f, 0.3f, sandstone, Kind.Prop);

            // Market stalls in the corners and stacked crates for cover.
            Stall(t, g, "Stall South West", -9, -29, true, "Sand_CanvasRed");
            Stall(t, g, "Stall South East", 9, -29, true, "Sand_CanvasBlue");
            Stall(t, g, "Stall North West", -9.5f, -11, true, "Sand_CanvasBlue");
            Stall(t, g, "Stall North East", 9.5f, -11, true, "Sand_CanvasRed");
            Crates(t, g, "Crates Plaza East", 10, -18);
            Crates(t, g, "Crates Plaza West", -10, -22);
            g.Box(t, "Barrel Plaza 1", 3.5f, -30.5f, 0.7f, 0.7f, 0f, 1f, g.M("Sand_Wood"), Kind.Prop);
            g.Box(t, "Barrel Plaza 2", 4.4f, -30.8f, 0.7f, 0.7f, 0f, 1f, g.M("Sand_Wood"), Kind.Prop);
            g.Box(t, "Door Plaza South", -4, -31.65f, 2, 0.1f, 0f, 2.6f, g.M("Sand_DoorBlue"), Kind.Prop);
            g.Box(t, "Door Plaza South East", 6, -31.65f, 2, 0.1f, 0f, 2.6f, g.M("Sand_DoorBlue"), Kind.Prop);
        }

        private static void BuildBazaar(Transform t, LevelGeometry g)
        {
            // Stalls along the west side of the street and awnings across it.
            Stall(t, g, "Stall Bazaar 1", -11, 12, false, "Sand_CanvasRed");
            Stall(t, g, "Stall Bazaar 2", -11, 18, false, "Sand_CanvasBlue");
            Stall(t, g, "Stall Bazaar 3", -11, 23, false, "Sand_CanvasRed");
            g.Box(t, "Awning Bazaar 1", -9, 0, 6, 3, 3.3f, 0.08f, g.M("Sand_CanvasBlue"), Kind.Prop);
            g.Box(t, "Awning Bazaar 2", -9, 9, 6, 2.5f, 3.4f, 0.08f, g.M("Sand_CanvasRed"), Kind.Prop);
            g.Box(t, "Awning Bazaar 3", -9, 20, 6, 3, 3.3f, 0.08f, g.M("Sand_CanvasBlue"), Kind.Prop);
            Crates(t, g, "Crates Bazaar", -11, -6);
            for (int i = 0; i < 3; i++)
                g.Box(t, $"Door Bazaar {i + 1}", -11.95f, 3 + i * 9, 0.1f, 1.8f, 0f, 2.6f, g.M("Sand_DoorBlue"), Kind.Prop);

            // Stairs up the east side of the street to the terrace.
            float run = LevelGeometry.StairRun(RoofLevel);
            g.Box(t, "Terrace Landing", -7.5f, -3, 3, 2, 0f, RoofLevel, g.M("Sand_Sandstone"), Kind.Floor);
            g.Stairs(t, "Bazaar Stairs", -7.5f, -2f + run, 3, new Vector2(0, -1), 0f, RoofLevel, g.M("Sand_Stone"));
            g.Box(t, "Bazaar Stairs Rail", -9.05f, -3f + run / 2f, 0.3f, run + 2f, 0f, RoofLevel + g.railHeight, g.M("Sand_Sandstone"), Kind.Wall);
        }

        /// <summary>The house over the tunnel; its roof is the terrace overlooking the plaza.</summary>
        private static void BuildTunnelHouse(Transform t, LevelGeometry g)
        {
            Material wall = g.M("Sand_Plaster"), stone = g.M("Sand_Stone");
            float under = RoofLevel - g.slabThickness;
            g.Box(t, "Tunnel House West", -4, -1, 4, 14, 0f, under, wall, Kind.Wall);
            g.Box(t, "Tunnel House East", 4, -1, 4, 14, 0f, under, wall, Kind.Wall);
            g.Slab(t, "Terrace", 0, -1, 12, 14, RoofLevel, g.M("Sand_Paving"));
            g.Box(t, "Tunnel Floor", 0, -1, 4, 14, 0f, 0.02f, stone, Kind.Floor);
            g.Box(t, "Lamp Tunnel South", -1.85f, -6, 0.2f, 0.4f, 2.4f, 0.4f, g.M("Sand_Wood"), Kind.Prop);
            g.Box(t, "Lamp Tunnel North", 1.85f, 3, 0.2f, 0.4f, 2.4f, 0.4f, g.M("Sand_Wood"), Kind.Prop);

            // Parapets, open where the two staircases arrive.
            Material parapet = g.M("Sand_Sandstone");
            g.Rail(t, "Terrace Parapet South", 0, -7.85f, 12, true, RoofLevel, parapet);
            g.Rail(t, "Terrace Parapet West 1", -5.85f, -6, 4, false, RoofLevel, parapet);
            g.Rail(t, "Terrace Parapet West 2", -5.85f, 2f, 8, false, RoofLevel, parapet);
            g.Rail(t, "Terrace Parapet North 1", -2, 5.85f, 8, true, RoofLevel, parapet);
            g.Rail(t, "Terrace Parapet North 2", 5.5f, 5.85f, 1, true, RoofLevel, parapet);
            Crates(t, g, "Crates Terrace", 3.2f, -2, RoofLevel);
        }

        private static void BuildCourtyard(Transform t, LevelGeometry g)
        {
            Material wall = g.M("Sand_Sandstone");
            // Wall to the bazaar with a doorway.
            g.WallZ(t, "Courtyard West Wall", -6, 6, 16, 0, wall, g.Door(9, 12));

            // Stairs from the courtyard up to the terrace.
            float run = LevelGeometry.StairRun(RoofLevel);
            g.Stairs(t, "Courtyard Stairs", 3.5f, 6f + run, 3, new Vector2(0, -1), 0f, RoofLevel, g.M("Sand_Stone"));
            g.Box(t, "Courtyard Stairs Rail", 1.85f, 6f + run / 2f, 0.3f, run, 0f, RoofLevel + g.railHeight, wall, Kind.Wall);

            g.Box(t, "Pillar Courtyard", -3, 10.5f, 0.8f, 0.8f, 0f, 3f, g.M("Sand_Stone"), Kind.Prop);
            Crates(t, g, "Crates Courtyard", 4.5f, 15);
            g.Box(t, "Barrel Courtyard", -5.2f, 7, 0.7f, 0.7f, 0f, 1f, g.M("Sand_Wood"), Kind.Prop);
        }

        private static void BuildAlleys(Transform t, LevelGeometry g)
        {
            Crates(t, g, "Crates Alley Corner", 27.2f, -23.2f);
            g.Box(t, "Barrel Alley 3", 26.5f, 6, 0.7f, 0.7f, 0f, 1f, g.M("Sand_Wood"), Kind.Prop);
            g.Box(t, "Barrel Alley 1", 29.2f, -14, 0.7f, 0.7f, 0f, 1f, g.M("Sand_Wood"), Kind.Prop);
            g.Box(t, "Barrel Alley 2", 29.3f, 13, 0.7f, 0.7f, 0f, 1f, g.M("Sand_Wood"), Kind.Prop);
            g.Box(t, "Awning Alley", 28, 0, 4, 2.5f, 3.2f, 0.08f, g.M("Sand_CanvasRed"), Kind.Prop);
            g.Box(t, "Awning Alley Fort", 24, 34, 3, 4, 3.2f, 0.08f, g.M("Sand_CanvasBlue"), Kind.Prop);
            for (int i = 0; i < 3; i++)
                g.Box(t, $"Door Alley {i + 1}", 26.1f, -14 + i * 14, 0.1f, 1.6f, 0f, 2.5f, g.M("Sand_DoorBlue"), Kind.Prop);
        }

        private static void BuildFort(Transform t, LevelGeometry g)
        {
            Material stone = g.M("Sand_Stone"), rubble = g.M("Sand_Rubble");
            float saved = g.wallThickness;
            g.wallThickness = 1.2f;
            g.WallX(t, "Fort Wall South", 28, -16, 16, 0, stone, new LevelGeometry.Opening(-12, -6, 0, 7), new LevelGeometry.Opening(-2, 2, 0, 7));
            g.WallZ(t, "Fort Wall East", 16, 28, 44, 0, stone, new LevelGeometry.Opening(32, 36, 0, 7));
            g.wallThickness = saved;

            // Gate posts.
            foreach (float x in new[] { -12.4f, -5.6f, -2.4f, 2.4f })
                g.Box(t, "Pillar Gate", x, 28, 1, 1.6f, 0f, 7.6f, stone, Kind.Wall);

            // Collapsed tower in the north-east corner, climbed by rubble steps.
            float run = LevelGeometry.StairRun(TowerLevel);
            g.Box(t, "Tower Platform", 12, 40, 6, 6, 0f, TowerLevel, stone, Kind.Floor);
            g.Stairs(t, "Tower Rubble Steps", 12, 37f - run, 3, new Vector2(0, 1), 0f, TowerLevel, rubble);
            g.Box(t, "Ruin Tower North", 12, 42.7f, 6, 0.6f, TowerLevel, 3.5f, stone, Kind.Wall);
            g.Box(t, "Ruin Tower East", 14.7f, 41, 0.6f, 3.4f, TowerLevel, 2.2f, stone, Kind.Wall);
            g.Box(t, "Ruin Tower Stub", 14.7f, 38, 0.6f, 1.8f, TowerLevel, 1.1f, stone, Kind.Wall);
            g.Box(t, "Ruin Tower Parapet West", 9.3f, 41.5f, 0.6f, 3, TowerLevel, 1f, stone, Kind.Wall);
            g.Box(t, "Ruin Tower Parapet South West", 9.75f, 37.3f, 1.5f, 0.6f, TowerLevel, 1f, stone, Kind.Wall);
            g.Box(t, "Ruin Tower Parapet South East", 14.25f, 37.3f, 1.5f, 0.6f, TowerLevel, 1f, stone, Kind.Wall);

            // Broken pillars, fallen stones and crates in the courtyard.
            g.Box(t, "Pillar Broken 1", -8, 36, 1, 1, 0f, 2.6f, stone, Kind.Prop);
            g.Box(t, "Pillar Broken 2", -8, 40, 1, 1, 0f, 1.2f, stone, Kind.Prop);
            g.Box(t, "Pillar Broken 3", 0, 40, 1, 1, 0f, 3.4f, stone, Kind.Prop);
            g.Box(t, "Rubble Fallen Column", -3, 36.5f, 3.2f, 0.9f, 0f, 0.9f, rubble, Kind.Prop);
            g.Box(t, "Rubble Pile 1", 7, 42.5f, 2.4f, 1.6f, 0f, 0.8f, rubble, Kind.Prop);
            g.Box(t, "Rubble Pile 2", -13, 33, 2, 2, 0f, 0.7f, rubble, Kind.Prop);
            Crates(t, g, "Crates Fort", -13.5f, 40);
        }

        private static void Stall(Transform parent, LevelGeometry g, string name, float x, float z, bool alongX, string canvas)
        {
            Transform t = LevelGeometry.Group(parent, name);
            float w = alongX ? 3f : 1f, d = alongX ? 1f : 3f;
            Material wood = g.M("Sand_Wood");
            g.Box(t, name + " Table", x, z, w, d, 0f, 1f, wood, Kind.Prop);
            float hx = (alongX ? 1.6f : 0.9f), hz = (alongX ? 0.9f : 1.6f);
            foreach (Vector2 c in new[] { new Vector2(-hx, -hz), new Vector2(hx, -hz), new Vector2(-hx, hz), new Vector2(hx, hz) })
                g.Box(t, name + " Post", x + c.x, z + c.y, 0.12f, 0.12f, 0f, 2.4f, wood, Kind.Prop);
            g.Box(t, name + " Awning", x, z, alongX ? 3.6f : 2f, alongX ? 2f : 3.6f, 2.4f, 0.08f, g.M(canvas), Kind.Prop);
        }

        private static void Crates(Transform parent, LevelGeometry g, string name, float x, float z, float level = 0f)
        {
            Material wood = g.M("Sand_Wood");
            g.Box(parent, name + " 1", x, z, 1.2f, 1.2f, level, 1.2f, wood, Kind.Prop);
            g.Box(parent, name + " 2", x + 1.25f, z + 0.1f, 1f, 1f, level, 1f, wood, Kind.Prop);
            g.Box(parent, name + " 3", x + 0.2f, z, 0.9f, 0.9f, level + 1.2f, 0.9f, wood, Kind.Prop);
        }

        #endregion
    }
}
