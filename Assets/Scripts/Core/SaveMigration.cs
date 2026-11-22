using System.Collections.Generic;

namespace Com.GCTC.ZombCube
{
    /// <summary>
    /// Converts between the old index-based ownership flags and catalog IDs.
    /// The default colour is always owned.
    /// </summary>
    public static class SaveMigration
    {
        public static string[] IdsFromFlags(int[] flags)
        {
            var ids = new List<string>();
            if (flags != null)
            {
                for (int i = 0; i < flags.Length && i < CosmeticCatalog.ColorIds.Length; i++)
                {
                    if (flags[i] == 1)
                        ids.Add(CosmeticCatalog.ColorIds[i]);
                }
            }
            if (!ids.Contains(CosmeticCatalog.ColorIds[0]))
                ids.Insert(0, CosmeticCatalog.ColorIds[0]);
            return ids.ToArray();
        }

        /// <summary>A new owned-flag array with only the default cosmetic owned.</summary>
        public static int[] NewOwnership()
        {
            var flags = new int[CosmeticCatalog.OwnershipSize];
            flags[0] = 1;
            return flags;
        }

        /// <summary>
        /// Copies flags into an array of the current catalog size. Shorter arrays are
        /// padded and longer ones trimmed; the default cosmetic stays owned.
        /// </summary>
        public static int[] ResizeFlags(int[] flags)
        {
            var resized = NewOwnership();
            if (flags != null)
                System.Array.Copy(flags, resized, System.Math.Min(flags.Length, resized.Length));
            resized[0] = 1;
            return resized;
        }

        public static int[] FlagsFromIds(string[] ids)
        {
            return FlagsFromIds(ids, CosmeticCatalog.OwnershipSize);
        }

        public static int[] FlagsFromIds(string[] ids, int size)
        {
            var flags = new int[size];
            if (ids != null)
            {
                foreach (string id in ids)
                {
                    int index = CosmeticCatalog.IndexOf(id);
                    if (index >= 0 && index < size)
                        flags[index] = 1;
                }
            }
            flags[0] = 1;
            return flags;
        }

        public static string EquippedIdFromIndex(int index)
        {
            return CosmeticCatalog.IdAt(index) ?? CosmeticCatalog.ColorIds[0];
        }

        public static int EquippedIndexFromId(string id)
        {
            int index = CosmeticCatalog.IndexOf(id);
            return index < 0 ? 0 : index;
        }
    }
}
