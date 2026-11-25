using System;
using System.Globalization;

namespace Com.GCTC.ZombCube
{
    /// <summary>
    /// Settings for an automated bot run (a test where bots play a map from wave <see cref="StartWave"/>
    /// through the boss). While <see cref="Active"/> is true the game starts at that wave, bots start with
    /// <see cref="StartPoints"/>, and nothing is saved to the player's profile, cloud save or leaderboards.
    ///
    /// Command line (builds and batch mode):
    ///   -zcbot solo|coop|party     start a bot run (required)
    ///   -zcbotMap arena|spaceship|theatre   (default arena)
    ///   -zcbotPlayers N            co-op bots (2-4) or Party instances to wait for (default 2)
    ///   -zcbotSpeed X              Time.timeScale while playing (default 1)
    ///   -zcbotRoom NAME            Photon room for multi-instance Party (default zcbot)
    ///   -zcbotOffline              Party in Photon offline mode (one bot, no network)
    ///   -zcbotResult PATH          write the result JSON here
    ///   -zcbotQuit                 quit the application when the run ends
    ///   -zcbotTimeout MINUTES      give up after this long (default 60)
    /// </summary>
    public static class BotSettings
    {
        public enum RunMode { Solo, Coop, Party }

        public static bool Active;
        public static RunMode Mode = RunMode.Solo;
        public static string MapId = "arena";
        public static int Players = 2;
        public static float Speed = 1f;
        public static string Room = "zcbot";
        public static bool PartyOffline;
        public static string ResultPath;
        public static bool QuitWhenDone;
        public static float TimeoutMinutes = 60f;

        /// <summary>Wave the run starts on (the boss arrives on wave 50).</summary>
        public const int StartWave = 49;
        /// <summary>Wave the boss spawns on (see Spawner / NetworkSpawner).</summary>
        public const int BossWave = 50;
        /// <summary>The run passes once the boss is dead and this wave is cleared.</summary>
        public const int FinalWave = BossWave + 1;
        /// <summary>Points each bot starts with, to buy wall guns, ammo and health packs.</summary>
        public const int StartPoints = 10000;
        /// <summary>Countdown before the first wave, so bots can buy a wall gun.</summary>
        public const int BuyWindowSeconds = 15;

        /// <summary>Reads the -zcbot arguments. Returns false (and changes nothing) if -zcbot isn't there.</summary>
        public static bool TryParse(string[] args)
        {
            int at = Array.FindIndex(args, a => string.Equals(a, "-zcbot", StringComparison.OrdinalIgnoreCase));
            if (at < 0) return false;

            Reset();
            Active = true;
            Mode = ParseMode(Value(args, at));
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i].ToLowerInvariant();
                string v = Value(args, i);
                switch (a)
                {
                    case "-zcbotmap": if (!string.IsNullOrEmpty(v)) MapId = v.ToLowerInvariant(); break;
                    case "-zcbotplayers": if (int.TryParse(v, out int p)) Players = Clamp(p, 1, 4); break;
                    case "-zcbotspeed":
                        if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float s)) Speed = Math.Max(0.25f, Math.Min(s, 8f));
                        break;
                    case "-zcbotroom": if (!string.IsNullOrEmpty(v)) Room = v; break;
                    case "-zcbotoffline": PartyOffline = true; break;
                    case "-zcbotresult": ResultPath = v; break;
                    case "-zcbotquit": QuitWhenDone = true; break;
                    case "-zcbottimeout":
                        if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float t)) TimeoutMinutes = Math.Max(1f, t);
                        break;
                }
            }
            if (Mode == RunMode.Coop) Players = Clamp(Players, 2, 4);
            return true;
        }

        /// <summary>Same settings from one string, e.g. "coop -zcbotMap theatre -zcbotPlayers 4".</summary>
        public static bool TryParse(string commandLine)
        {
            string[] parts = ("-zcbot " + (commandLine ?? "")).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return TryParse(parts);
        }

        public static void Reset()
        {
            Active = false;
            Mode = RunMode.Solo;
            MapId = "arena";
            Players = 2;
            Speed = 1f;
            Room = "zcbot";
            PartyOffline = false;
            ResultPath = null;
            QuitWhenDone = false;
            TimeoutMinutes = 60f;
        }

        /// <summary>
        /// Enemies in the first wave of a run, as if the game had been played from wave 1
        /// (solo and co-op: the count grows by count / wave after every wave).
        /// </summary>
        public static int SoloCubesAtWave(int wave, int players)
        {
            int cubes = 5 * Math.Max(1, players);
            for (int round = 1; round < wave; round++) cubes += cubes / round;
            return cubes;
        }

        /// <summary>Party: the count starts at 5 per player and grows by 5 after every wave.</summary>
        public static int PartyCubesAtWave(int wave, int players) => 5 * Math.Max(1, players) + 5 * (wave - 1);

        private static RunMode ParseMode(string value)
        {
            switch ((value ?? "").ToLowerInvariant())
            {
                case "coop":
                case "co-op": return RunMode.Coop;
                case "party": return RunMode.Party;
                default: return RunMode.Solo;
            }
        }

        private static string Value(string[] args, int index)
        {
            if (index + 1 >= args.Length) return null;
            string next = args[index + 1];
            return next.StartsWith("-") ? null : next;
        }

        private static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;
    }
}
