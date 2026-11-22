namespace Com.GCTC.ZombCube
{
    /// <summary>
    /// Stable IDs for cosmetic colours. Each ID's position is its index in the
    /// runtime material list, which skins and blasters both use.
    /// Never rename or reorder an ID once it has shipped: saves store the ID.
    /// Add new cosmetics at the end, and add a matching entry (same order) to
    /// Resources/CosmeticDatabase.asset. A test checks the two stay in sync.
    /// </summary>
    public static class CosmeticCatalog
    {
        /// <summary>Owned-flag arrays were nine long before the catalog grew.</summary>
        public const int LegacyOwnershipSize = 9;

        /// <summary>Size of the owned-flag arrays used by Player and SaveData.</summary>
        public static int OwnershipSize =>
            ColorIds.Length > LegacyOwnershipSize ? ColorIds.Length : LegacyOwnershipSize;

        public static readonly string[] ColorIds =
        {
            // Original nine. Indexes must match the shipped material order.
            "default", // DefaultBlaster.mat
            "green",   // GreenBlaster.mat
            "blue",    // BlueBlaster.mat
            "yellow",  // YellowBlaster.mat
            "pink",    // PinkBlaster.mat
            "legend",  // ForceField.mat
            "osu",     // ForceField OSU.mat
            "ou",      // ForceField OU.mat
            "gold",    // Gold.mat

            // Step 2: bright solids (Cosmetic_<id>.mat)
            "red", "orange", "lime", "cyan", "purple", "teal", "black", "silver",
            // Stripes
            "stripes_candy", "stripes_hazard", "stripes_ocean", "stripes_tiger",
            // Patterns
            "checker", "polka", "neon_grid",
            // Camo
            "camo_woodland", "camo_desert", "camo_urban", "camo_arctic", "camo_neon",
        };

        public static int IndexOf(string id)
        {
            if (string.IsNullOrEmpty(id)) return -1;
            return System.Array.IndexOf(ColorIds, id);
        }

        public static string IdAt(int index)
        {
            if (index < 0 || index >= ColorIds.Length) return null;
            return ColorIds[index];
        }
    }
}
