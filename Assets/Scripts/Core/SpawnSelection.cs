using System;

namespace Com.GCTC.ZombCube
{
    /// <summary>
    /// Picks an enemy spawn point that is not on top of a player: a random point at least
    /// <c>minDistance</c> from every player, or the farthest point when none is far enough.
    /// </summary>
    public static class SpawnSelection
    {
        /// <param name="distanceToNearestPlayer">For each spawn point, its distance to the closest player
        /// (use float.MaxValue when there are no players).</param>
        /// <returns>The chosen index, or -1 when there are no spawn points.</returns>
        public static int Choose(float[] distanceToNearestPlayer, float minDistance, Random rng)
        {
            if (distanceToNearestPlayer == null || distanceToNearestPlayer.Length == 0) return -1;

            int safeCount = 0;
            foreach (float d in distanceToNearestPlayer)
                if (d >= minDistance) safeCount++;

            if (safeCount > 0)
            {
                int pick = rng.Next(safeCount);
                for (int i = 0; i < distanceToNearestPlayer.Length; i++)
                    if (distanceToNearestPlayer[i] >= minDistance && pick-- == 0)
                        return i;
            }

            int farthest = 0;
            for (int i = 1; i < distanceToNearestPlayer.Length; i++)
                if (distanceToNearestPlayer[i] > distanceToNearestPlayer[farthest]) farthest = i;
            return farthest;
        }
    }
}
