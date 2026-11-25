using System;
using System.Collections.Generic;

namespace Com.GCTC.ZombCube
{
    /// <summary>Which saved number an achievement counts towards (None = unlocked by an event).</summary>
    public enum AchievementStat
    {
        None,
        CubesEliminated,
        ProjectilesFired,
        TotalPoints,
        ArenaSoloWave,
        ArenaPartyWave
    }

    public sealed class AchievementDef
    {
        public readonly string Id;          // also the Steam / Game Center API name
        public readonly string Name;
        public readonly string Description;
        public readonly AchievementStat Stat;
        public readonly int Target;

        public AchievementDef(string id, string name, string description, AchievementStat stat = AchievementStat.None, int target = 1)
        {
            Id = id;
            Name = name;
            Description = description;
            Stat = stat;
            Target = target;
        }

        public bool HasProgress => Stat != AchievementStat.None;
    }

    /// <summary>
    /// Every achievement in the game, in display order. IDs match the Steam and Game Center achievements.
    /// Stat achievements unlock from the saved stats; event achievements are saved in SaveData.achievements
    /// when they happen, so players who aren't signed in to Steam or Game Center earn them too.
    /// </summary>
    public static class AchievementCatalog
    {
        public static readonly IReadOnlyList<AchievementDef> All = new[]
        {
            new AchievementDef("lets_play", "Let's Play", "Play a Solo game."),
            new AchievementDef("lets_play_together", "Let's Play Together", "Play a CO-OP game."),
            new AchievementDef("lets_party", "Let's Party", "Play a Party game."),
            new AchievementDef("stayin_alive_solo", "Stayin' Alive Alone", "Reach wave 50 on the starting level in Solo mode.", AchievementStat.ArenaSoloWave, 50),
            new AchievementDef("stayin_alive_party", "Stayin' Alive Together", "Reach wave 50 on the starting level in Party mode.", AchievementStat.ArenaPartyWave, 50),
            new AchievementDef("ricochet_king", "Ricochet King", "Eliminate 5 ZombCubes with a single projectile."),
            new AchievementDef("n_gamer_1", "NGamer1", "Change your player name to NGamer1."),
            new AchievementDef("cube_destroyer_i", "Cube Destroyer I", "Eliminate 10,000 ZombCubes.", AchievementStat.CubesEliminated, 10000),
            new AchievementDef("cube_destroyer_ii", "Cube Destroyer II", "Eliminate 100,000 ZombCubes.", AchievementStat.CubesEliminated, 100000),
            new AchievementDef("cube_destroyer_iii", "Cube Destroyer III", "Eliminate 1,000,000 ZombCubes.", AchievementStat.CubesEliminated, 1000000),
            new AchievementDef("trigger_happy_i", "Trigger Happy I", "Launch 100,000 projectiles.", AchievementStat.ProjectilesFired, 100000),
            new AchievementDef("trigger_happy_ii", "Trigger Happy II", "Launch 1,000,000 projectiles.", AchievementStat.ProjectilesFired, 1000000),
            new AchievementDef("trigger_happy_iii", "Trigger Happy III", "Launch 10,000,000 projectiles.", AchievementStat.ProjectilesFired, 10000000),
            new AchievementDef("point_racker_i", "Point Racker I", "Earn 100,000 total points.", AchievementStat.TotalPoints, 100000),
            new AchievementDef("point_racker_ii", "Point Racker II", "Earn 1,000,000 total points.", AchievementStat.TotalPoints, 1000000),
            new AchievementDef("point_racker_iii", "Point Racker III", "Earn 10,000,000 total points.", AchievementStat.TotalPoints, 10000000),
        };

        public static AchievementDef Get(string id)
        {
            foreach (AchievementDef a in All)
                if (a.Id == id) return a;
            return null;
        }

        /// <summary>Adds an ID to a saved unlock list (no duplicates). Returns the (possibly new) array.</summary>
        public static string[] AddUnlock(string[] unlocked, string id, out bool added)
        {
            added = false;
            unlocked = unlocked ?? new string[0];
            if (string.IsNullOrEmpty(id) || Array.IndexOf(unlocked, id) >= 0) return unlocked;
            var grown = new string[unlocked.Length + 1];
            Array.Copy(unlocked, grown, unlocked.Length);
            grown[unlocked.Length] = id;
            added = true;
            return grown;
        }

        /// <summary>Progress 0..1 towards a stat achievement (1 for unlocked event achievements).</summary>
        public static float Progress(AchievementDef a, long statValue, bool unlockedByEvent)
        {
            if (!a.HasProgress) return unlockedByEvent ? 1f : 0f;
            if (a.Target <= 0) return 1f;
            return Math.Max(0f, Math.Min(1f, (float)statValue / a.Target));
        }
    }
}
