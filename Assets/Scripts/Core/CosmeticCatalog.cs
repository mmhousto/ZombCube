namespace Com.GCTC.ZombCube
{
    /// <summary>
    /// Stable IDs for cosmetic colours. Each ID's position is its index in the
    /// runtime material list, which skins and blasters both use.
    /// Never rename or reorder an ID once it has shipped: saves store the ID.
    /// Add new colours at the end and add their material to MaterialSelector too.
    /// </summary>
    public static class CosmeticCatalog
    {
        /// <summary>Size of the owned flag arrays used by Player and SaveData.</summary>
        public const int OwnershipSize = 9;

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
