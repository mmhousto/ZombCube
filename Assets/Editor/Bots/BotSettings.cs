using System;
using System.Globalization;

namespace Com.GCTC.ZombCube.EditorTools.Bots
{
    /// <summary>
    /// Settings for a bot run (Editor only): bots play a map from wave <see cref="StartWave"/> to the boss on wave <see cref="BossWave"/>.
    /// Runs start from a Multiplayer Play Mode scenario ("Bots - ..." in Assets/Settings/PlayMode, see
    /// <see cref="BotScenarioLauncher"/>) or the BotRuns PlayMode tests. Arguments use the same names as before:
    ///   solo|coop|party  -zcbotMap arena|spaceship|theatre  -zcbotPlayers N  -zcbotSpeed X
    ///   -zcbotRoom NAME  -zcbotOffline  -zcbotChill (co-op)  -zcbotHardcore (party)  -zcbotResult PATH  -zcbotTimeout MINUTES
    /// Every run starts at the SignIn screen and goes through the menus (BotRun.Menus.cs).
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
        /// <summary>Co-op in Chill mode (eliminated players come back next wave).</summary>
        public static bool Chill;
        /// <summary>Party in Hardcore mode (one player out ends the game).</summary>
        public static bool Hardcore;
        public static string ResultPath;
        public static bool QuitWhenDone;
        public static float TimeoutMinutes = 60f;

        /// <summary>Bots play a normal game from wave 1.</summary>
        public const int StartWave = 1;
        /// <summary>The bot run spawns the boss when this wave starts (the game itself waits until wave 50).</summary>
        public const int BossWave = 5;
        /// <summary>The run ends once the boss is dead and this wave is cleared (beating the boss isn't required to pass).</summary>
        public const int FinalWave = BossWave + 1;

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
                    case "-zcbotchill": Chill = true; break;
                    case "-zcbothardcore": Hardcore = true; break;
                    case "-zcbotresult": ResultPath = v; break;
                    case "-zcbotquit": QuitWhenDone = true; break;
                    case "-zcbottimeout":
                        if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float t)) TimeoutMinutes = Math.Max(1f, t);
                        break;
                }
            }
            if (Mode == RunMode.Coop) Players = Clamp(Players, 2, 4);
            if (Mode != RunMode.Coop) Chill = false;
            if (Mode != RunMode.Party) Hardcore = false;
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
            Chill = false;
            Hardcore = false;
            ResultPath = null;
            QuitWhenDone = false;
            TimeoutMinutes = 60f;
        }

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
