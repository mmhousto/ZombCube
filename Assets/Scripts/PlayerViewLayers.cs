using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Com.GCTC.ZombCube
{
    /// <summary>
    /// Sets up a player's two cameras so other players are drawn by the Main Camera (hidden behind
    /// walls, culled with the right frustum) while the player's own gun is drawn only by its WeaponCam
    /// overlay (so it never clips into walls).
    ///
    /// Previously every player's WeaponCam drew the whole Player and Blaster layers, so other players'
    /// bodies and guns were drawn on top of the world: visible through walls and culled by the overlay.
    /// Each local player's gun now goes on its own layer (PlayerView1-4), which collides like Blaster.
    /// </summary>
    public static class PlayerViewLayers
    {
        public const string LayerPrefix = "PlayerView";
        public const int MaxPlayers = 4;

        public static int Layer(int index) => LayerMask.NameToLayer(LayerPrefix + (index + 1));

        /// <summary>Local player <paramref name="index"/> (0-3 in co-op, 0 for the Party local player).</summary>
        public static void ApplyLocal(GameObject player, int index)
        {
            int own = Layer(index);
            if (own < 0)
            {
                Debug.LogWarning($"[PlayerViewLayers] Layer {LayerPrefix}{index + 1} is missing from the Tags and Layers settings.");
                return;
            }

            // The player's own gun (everything on the Blaster layer) moves to its own layer.
            int blaster = LayerMask.NameToLayer("Blaster");
            foreach (Transform t in player.GetComponentsInChildren<Transform>(true))
                if (t.gameObject.layer == blaster)
                    t.gameObject.layer = own;

            int others = LayerMask.GetMask("Player", "Blaster");
            for (int i = 0; i < MaxPlayers; i++)
            {
                int layer = Layer(i);
                if (layer >= 0 && layer != own) others |= 1 << layer;
            }

            if (!FindCameras(player, out Camera main, out Camera weapon)) return;
            main.cullingMask = (main.cullingMask | others) & ~(1 << own);
            weapon.cullingMask = (1 << own) | (weapon.cullingMask & LayerMask.GetMask("UI"));
        }

        /// <summary>
        /// A Party player owned by someone else. Its cameras are only used for spectating; its overlay
        /// must not draw players over walls, and its gun (still on Blaster) is drawn by its Main Camera.
        /// </summary>
        public static void ApplyRemote(GameObject player)
        {
            if (!FindCameras(player, out Camera main, out Camera weapon)) return;
            main.cullingMask |= LayerMask.GetMask("Player", "Blaster");
            weapon.cullingMask &= LayerMask.GetMask("UI");
        }

        /// <summary>The player's base Main Camera and the WeaponCam overlay stacked on it.</summary>
        private static bool FindCameras(GameObject player, out Camera main, out Camera weapon)
        {
            main = null;
            weapon = null;
            foreach (Camera cam in player.GetComponentsInChildren<Camera>(true))
            {
                UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
                if (data == null || data.renderType != CameraRenderType.Base || data.cameraStack == null) continue;
                foreach (Camera stacked in data.cameraStack)
                {
                    if (stacked == null || !stacked.transform.IsChildOf(player.transform)) continue;
                    main = cam;
                    weapon = stacked;
                    return true;
                }
            }

            Debug.LogWarning($"[PlayerViewLayers] {player.name}: no Main Camera with a WeaponCam overlay found.");
            return false;
        }
    }
}
