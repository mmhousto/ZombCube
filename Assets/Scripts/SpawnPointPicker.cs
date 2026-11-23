using UnityEngine;

namespace Com.GCTC.ZombCube
{
    /// <summary>Chooses enemy spawn points away from the players (see <see cref="SpawnSelection"/>).</summary>
    public static class SpawnPointPicker
    {
        /// <summary>Spawn points closer than this (world units, about 9.5 m) to a player are skipped.</summary>
        public const float SafeDistance = 18f;

        private static readonly System.Random rng = new System.Random();
        private static float[] distances = new float[0];

        /// <summary>
        /// Where a boss should spawn: one of the level's "BossSpawnPoint" objects (big open rooms), chosen
        /// away from the players, or a regular spawn point when the level has none.
        /// </summary>
        public static Transform PickBoss(GameObject[] spawnPoints)
        {
            var bossPoints = new System.Collections.Generic.List<GameObject>();
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (t.name.StartsWith("BossSpawnPoint")) bossPoints.Add(t.gameObject);

            GameObject[] pool = bossPoints.Count > 0 ? bossPoints.ToArray() : spawnPoints;
            return pool[Pick(pool)].transform;
        }

        public static int Pick(GameObject[] spawnPoints)
        {
            if (spawnPoints == null || spawnPoints.Length == 0) return 0;

            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            if (distances.Length != spawnPoints.Length) distances = new float[spawnPoints.Length];

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                float nearest = float.MaxValue;
                if (spawnPoints[i] != null)
                {
                    Vector3 p = spawnPoints[i].transform.position;
                    foreach (GameObject player in players)
                        if (player != null && player.activeInHierarchy)
                            nearest = Mathf.Min(nearest, Vector3.Distance(p, player.transform.position));
                }
                else nearest = -1f;   // destroyed spawn point: never pick it while others exist
                distances[i] = nearest;
            }

            int index = SpawnSelection.Choose(distances, SafeDistance, rng);
            return index < 0 ? 0 : index;
        }
    }
}
