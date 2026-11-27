using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    /// <summary>
    /// Destroys an old co-op player one component per frame. Destroying the whole CoopPlayer in one go
    /// froze the editor on Restart / Main Menu; taking it apart over several frames does not. Lives on
    /// its own DontDestroyOnLoad object so it survives the scene change and the CoopManager being destroyed.
    /// </summary>
    public class CoopPlayerTeardown : MonoBehaviour
    {
        private static CoopPlayerTeardown runner;

        public static void Run(GameObject player)
        {
            if (player == null) return;

            if (runner == null)
            {
                var go = new GameObject("CoopPlayerTeardown");
                DontDestroyOnLoad(go);
                runner = go.AddComponent<CoopPlayerTeardown>();
            }

            // Detach so the old player is no longer a child of the persistent InputManager (new players
            // spawn there) and survives the scene unload until it is taken apart.
            player.transform.SetParent(null, false);
            DontDestroyOnLoad(player);
            player.SetActive(false);
            runner.StartCoroutine(runner.TearDown(player));
        }

        private IEnumerator TearDown(GameObject player)
        {
            var components = new List<Component>(player.GetComponentsInChildren<Component>(true));

            // Scripts first (deepest children first), then engine components; Transforms go with the object.
            components.Reverse();
            components.Sort((a, b) => (a is MonoBehaviour ? 0 : 1).CompareTo(b is MonoBehaviour ? 0 : 1));

            foreach (Component component in components)
            {
                if (component == null || component is Transform) continue;
                Destroy(component);
                yield return null;
            }

            if (player != null) Destroy(player);
            yield return null;
        }
    }
}
