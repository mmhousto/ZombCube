using System;
using System.Collections.Generic;

namespace Com.GCTC.ZombCube
{
    /// <summary>How a game is being played, for wave records and leaderboards.</summary>
    public enum PlayMode
    {
        Solo,
        Coop,
        Party
    }

    /// <summary>One highest-wave leaderboard on each platform. A null ID means that platform has no board.</summary>
    public sealed class WaveBoard
    {
        public readonly string LevelId;
        public readonly PlayMode Mode;
        public readonly string AppleId;
        public readonly string GoogleId;
        public readonly string SteamName;

        public WaveBoard(string levelId, PlayMode mode, string appleId, string googleId, string steamName)
        {
            LevelId = levelId;
            Mode = mode;
            AppleId = appleId;
            GoogleId = googleId;
            SteamName = steamName;
        }
    }

    /// <summary>Best wave reached on one map in one mode. Saved in SaveData.waveRecords.</summary>
    [Serializable]
    public class WaveRecord
    {
        public string level;
        public string mode;
        public int wave;
    }

    /// <summary>
    /// The highest-wave leaderboards for every map and mode, and helpers for the saved wave records.
    /// Level IDs match LevelDatabase entry IDs.
    /// </summary>
    public static class WaveLeaderboards
    {
        public const string Arena = "arena";
        public const string Spaceship = "spaceship";
        public const string Theatre = "theatre";

        public static readonly IReadOnlyList<WaveBoard> All = new[]
        {
            new WaveBoard(Arena, PlayMode.Solo, "highest_wave_on_starting_level", "CgkI07-ynroOEAIQAQ", "Highest Solo Wave"),
            new WaveBoard(Arena, PlayMode.Party, "highest_party_wave_on_starting_level", "CgkI07-ynroOEAIQAg", "Highest Party Wave"),
            new WaveBoard(Arena, PlayMode.Coop, "highest_coop_wave_on_starting_level", null, "Highest COOP Wave"),

            new WaveBoard(Spaceship, PlayMode.Solo, "highest_wave_spaceship", null, "Highest Solo Wave - Spaceship"),
            new WaveBoard(Spaceship, PlayMode.Party, "highest_party_wave_spaceship", null, "Highest Party Wave - Spaceship"),
            new WaveBoard(Spaceship, PlayMode.Coop, "highest_coop_wave_spaceship", null, "Highest COOP Wave - Spaceship"),

            new WaveBoard(Theatre, PlayMode.Solo, "highest_wave_theatre", null, "Highest Solo Wave - Theatre"),
            new WaveBoard(Theatre, PlayMode.Party, "highest_party_wave_theatre", null, "Highest Party Wave - Theatre"),
            new WaveBoard(Theatre, PlayMode.Coop, "highest_coop_wave_theatre", null, "Highest COOP Wave - Theatre"),
        };

        /// <summary>The board for a map and mode, or null if there isn't one.</summary>
        public static WaveBoard For(string levelId, PlayMode mode)
        {
            foreach (WaveBoard board in All)
                if (board.LevelId == levelId && board.Mode == mode) return board;
            return null;
        }

        public static string ModeKey(PlayMode mode) => mode.ToString().ToLowerInvariant();

        /// <summary>Best saved wave for a map and mode (0 if none).</summary>
        public static int Best(WaveRecord[] records, string levelId, PlayMode mode)
        {
            if (records == null || string.IsNullOrEmpty(levelId)) return 0;
            string key = ModeKey(mode);
            int best = 0;
            foreach (WaveRecord r in records)
                if (r != null && r.level == levelId && r.mode == key && r.wave > best) best = r.wave;
            return best;
        }

        /// <summary>
        /// Records a wave if it beats the saved best. Returns the (possibly new) array;
        /// <paramref name="improved"/> tells whether the best went up.
        /// </summary>
        public static WaveRecord[] Record(WaveRecord[] records, string levelId, PlayMode mode, int wave, out bool improved)
        {
            improved = false;
            if (string.IsNullOrEmpty(levelId) || wave <= 0) return records ?? new WaveRecord[0];
            records = records ?? new WaveRecord[0];
            string key = ModeKey(mode);

            foreach (WaveRecord r in records)
            {
                if (r == null || r.level != levelId || r.mode != key) continue;
                if (wave > r.wave)
                {
                    r.wave = wave;
                    improved = true;
                }
                return records;
            }

            var grown = new WaveRecord[records.Length + 1];
            Array.Copy(records, grown, records.Length);
            grown[records.Length] = new WaveRecord { level = levelId, mode = key, wave = wave };
            improved = true;
            return grown;
        }
    }
}
