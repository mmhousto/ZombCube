using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Com.GCTC.ZombCube
{
    /// <summary>
    /// Lifts the roof off indoor levels while a boss cube is alive, so the giant boss fits and can be seen.
    /// Roof parts are objects named "... Ceiling", the "Light ..." / "Point Light ..." fixtures next to
    /// them (same parent), and anything with a <see cref="RoofPart"/>. Levels without ceilings are untouched.
    /// Called from BossCube and NetworkBossCube, so it runs on every client in Party.
    /// </summary>
    public class BossRoof : MonoBehaviour
    {
        public const float LiftHeight = 60f;     // world units
        public const float LiftSeconds = 2.5f;

        private static BossRoof runner;
        private static int bossesAlive;

        private readonly List<Transform> parts = new List<Transform>();
        private readonly List<Vector3> restPositions = new List<Vector3>();
        private Coroutine motion;

        /// <summary>A boss appeared: lift the roof (once, however many bosses there are).</summary>
        public static void BossSpawned()
        {
            bossesAlive++;
            if (bossesAlive == 1) Runner().Lift(true);
        }

        /// <summary>A boss died or was removed: put the roof back once no boss is left.</summary>
        public static void BossGone()
        {
            if (bossesAlive == 0) return;
            bossesAlive--;
            if (bossesAlive == 0 && runner != null) runner.Lift(false);
        }

        private static BossRoof Runner()
        {
            if (runner == null) runner = new GameObject("BossRoof").AddComponent<BossRoof>();
            return runner;
        }

        private void OnDestroy()
        {
            if (runner == this) { runner = null; bossesAlive = 0; }
        }

        private void Lift(bool off)
        {
            if (off) CollectParts();
            if (parts.Count == 0) return;
            if (motion != null) StopCoroutine(motion);
            motion = StartCoroutine(Move(off));
        }

        private void CollectParts()
        {
            parts.Clear();
            restPositions.Clear();
            var found = new HashSet<Transform>();
            Scene scene = SceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(false))
                {
                    if (t.name.EndsWith(" Ceiling"))
                    {
                        found.Add(t);
                        if (t.parent != null)
                            foreach (Transform sibling in t.parent)
                                if (sibling.name.StartsWith("Light ") || sibling.name.StartsWith("Point Light ")) found.Add(sibling);
                    }
                    else if (t.GetComponent<RoofPart>() != null) found.Add(t);
                }
            }
            foreach (Transform t in found)
            {
                parts.Add(t);
                restPositions.Add(t.position);
            }
        }

        private IEnumerator Move(bool off)
        {
            if (!off)
                foreach (Transform t in parts) if (t != null) t.gameObject.SetActive(true);

            for (float time = 0f; time < LiftSeconds; time += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, time / LiftSeconds);
                float lift = (off ? k : 1f - k) * LiftHeight;
                for (int i = 0; i < parts.Count; i++)
                    if (parts[i] != null) parts[i].position = restPositions[i] + Vector3.up * lift;
                yield return null;
            }

            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] == null) continue;
                parts[i].position = restPositions[i] + (off ? Vector3.up * LiftHeight : Vector3.zero);
                if (off) parts[i].gameObject.SetActive(false);
            }
            motion = null;
        }
    }
}
