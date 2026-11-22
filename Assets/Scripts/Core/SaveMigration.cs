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

        public static int[] FlagsFromIds(string[] ids, int size = CosmeticCatalog.OwnershipSize)
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
