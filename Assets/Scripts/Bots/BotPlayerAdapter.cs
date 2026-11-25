using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Com.GCTC.ZombCube
{
    /// <summary>
    /// Lets a bot drive a player through the same input methods the controls use, for both the solo/co-op
    /// player (PlayerMovement, MouseLook, ShootProjectile, PlayerManager, SwapManager) and the Party player
    /// (NetworkPlayerMovement, NetworkMouseLook, NetworkShootProjectile, NetworkPlayerManager, NetworkSwapManager).
    /// Private state (health, look angles, weapon slots) is read through reflection so the game code stays as is.
    /// </summary>
    public class BotPlayerAdapter
    {
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public readonly GameObject Root;
        public readonly bool IsNetwork;

        private readonly Component movement, look, manager, swap;
        private readonly Transform lookTransform;
        private readonly FieldInfo xRotation, yRotation, healthField, pitchField, yawField;
        private readonly MethodInfo moveSolo, moveNetwork, jump, interact, swapTo;
        private readonly FieldInfo slotImages, weaponImages, weapons;

        public static BotPlayerAdapter TryCreate(GameObject root)
        {
            if (root == null) return null;
            var adapter = new BotPlayerAdapter(root);
            return adapter.movement != null && adapter.look != null && adapter.manager != null ? adapter : null;
        }

        private BotPlayerAdapter(GameObject root)
        {
            Root = root;
            manager = root.GetComponent("NetworkPlayerManager");
            IsNetwork = manager != null;
            if (!IsNetwork) manager = root.GetComponent<PlayerManager>();

            movement = IsNetwork ? root.GetComponent("NetworkPlayerMovement") : root.GetComponent<PlayerMovement>();
            look = IsNetwork ? (Component)root.GetComponentInChildren(Type.GetType("Com.GCTC.ZombCube.NetworkMouseLook"), true)
                             : root.GetComponentInChildren<MouseLook>(true);
            swap = root.GetComponent<SwapManager>();   // NetworkSwapManager derives from SwapManager

            if (movement != null)
            {
                Type t = movement.GetType();
                moveSolo = t.GetMethod("MoveInput", new[] { typeof(Vector2) });
                moveNetwork = t.GetMethod("MoveInput", new[] { typeof(float), typeof(float) });
                jump = t.GetMethod("JumpInput", new[] { typeof(bool) });
            }
            if (look != null)
            {
                Type t = look.GetType();
                lookTransform = look.transform;
                xRotation = t.GetField("xRotation", Any);
                yRotation = t.GetField("yRotation", Any);   // solo MouseLook only
                pitchField = t.GetField("pitch", Any);
                yawField = t.GetField("yaw", Any);
            }
            if (manager != null)
            {
                Type t = manager.GetType();
                healthField = t.GetField("healthPoints", Any);
                interact = t.GetMethod("InteractInput", new[] { typeof(bool) });
            }
            if (swap != null)
            {
                Type t = typeof(SwapManager);
                swapTo = swap.GetType().GetMethod("SwapToWeapon", Any, null, new[] { typeof(int) }, null);
                slotImages = t.GetField("currentWeaponImages", Any);
                weaponImages = t.GetField("weaponImages", Any);
                weapons = t.GetField("weapons", Any);
            }
        }

        public bool Alive => Root != null && Root.activeInHierarchy && Health > 0f;

        public float Health => healthField != null && manager != null ? Convert.ToSingle(healthField.GetValue(manager)) : 100f;

        public int Points
        {
            get => IsNetwork ? NetworkPoints : PlayerManager.currentPoints;
            set { if (IsNetwork) NetworkPoints = value; else PlayerManager.currentPoints = value; }
        }

        private static FieldInfo networkPoints;
        private static int NetworkPoints
        {
            get { Field(); return networkPoints != null ? (int)networkPoints.GetValue(null) : 0; }
            set { Field(); networkPoints?.SetValue(null, value); }
        }
        private static void Field()
        {
            if (networkPoints == null)
                networkPoints = Type.GetType("Com.GCTC.ZombCube.NetworkPlayerManager")?.GetField("currentPoints", BindingFlags.Static | BindingFlags.Public);
        }

        /// <summary>Camera / eye position the player aims from.</summary>
        public Vector3 Eye => lookTransform != null ? lookTransform.position : Root.transform.position + Vector3.up * 3f;

        public Vector3 Position => Root.transform.position;

        /// <summary>Move in world space (x/z), converted to the player's local input.</summary>
        public void Move(Vector3 worldDirection, bool jumpNow = false)
        {
            worldDirection.y = 0f;
            Vector3 local = Root.transform.InverseTransformDirection(Vector3.ClampMagnitude(worldDirection, 1f));
            if (moveSolo != null) moveSolo.Invoke(movement, new object[] { new Vector2(local.x, local.z) });
            else moveNetwork?.Invoke(movement, new object[] { local.x, local.z });
            jump?.Invoke(movement, new object[] { jumpNow });
        }

        /// <summary>Turns the player and camera to look at a point (set before the look scripts run).</summary>
        public void LookAt(Vector3 point)
        {
            Vector3 dir = point - Eye;
            if (dir.sqrMagnitude < 0.0001f) return;
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float pitch = -Mathf.Asin(Mathf.Clamp(dir.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;

            pitchField?.SetValue(look, 0f);   // no mouse input
            yawField?.SetValue(look, 0f);
            xRotation?.SetValue(look, pitch);

            Transform parent = Root.transform.parent;
            float parentYaw = parent != null ? parent.eulerAngles.y : 0f;
            if (yRotation != null) yRotation.SetValue(look, -(yaw - parentYaw));   // MouseLook rebuilds the body rotation from this
            else Root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);           // NetworkMouseLook only adds to it
        }

        /// <summary>Fire (or stop firing) whatever weapon is equipped.</summary>
        public void Fire(bool firing)
        {
            foreach (Behaviour weapon in Weapons())
                if (weapon.enabled) Invoke(weapon, "FireInput", firing);
        }

        public void Interact(bool held) => interact?.Invoke(manager, new object[] { held });

        /// <summary>All shooting components (pistol, wall guns, grenades), solo or network.</summary>
        public IEnumerable<Behaviour> Weapons()
        {
            foreach (Behaviour b in Root.GetComponents<Behaviour>())
            {
                if (b == null) continue;
                Type t = b.GetType();
                if (typeof(ShootProjectile).IsAssignableFrom(t) || t.Name.StartsWith("Network") && t.GetMethod("FireInput", new[] { typeof(bool) }) != null && t.Name != "NetworkPlayerManager")
                    yield return b;
            }
        }

        public Behaviour EquippedWeapon()
        {
            foreach (Behaviour w in Weapons())
                if (w.enabled && !IsGrenade(w)) return w;
            return null;
        }

        public static bool IsGrenade(Behaviour weapon) => weapon.GetType().Name.Contains("Grenade");

        /// <summary>Clip + reserve ammo of a wall gun; -1 for weapons with unlimited ammo (pistol).</summary>
        public static int Ammo(Behaviour weapon)
        {
            if (weapon == null) return 0;
            FieldInfo clip = weapon.GetType().GetField("currentAmmoInClip", Any);
            FieldInfo reserve = weapon.GetType().GetField("reserveAmmo", Any);
            if (clip == null || reserve == null) return -1;
            return Convert.ToInt32(clip.GetValue(weapon)) + Convert.ToInt32(reserve.GetValue(weapon));
        }

        public static int Clip(Behaviour weapon)
        {
            FieldInfo clip = weapon != null ? weapon.GetType().GetField("currentAmmoInClip", Any) : null;
            return clip != null ? Convert.ToInt32(clip.GetValue(weapon)) : -1;
        }

        public static void Reload(Behaviour weapon) => Invoke(weapon, "ReloadInput", true);

        /// <summary>
        /// Weapon kinds in the four quick slots (index into SwapManager.weaponImages:
        /// 0 Pistol, 1 Grenade, 2 SMB, 3 AB, 4 Shotblaster, 5 Sniper), -1 for an empty slot.
        /// </summary>
        public int[] Slots()
        {
            var result = new[] { -1, -1, -1, -1 };
            if (swap == null || slotImages == null || weaponImages == null) return result;
            var images = slotImages.GetValue(swap) as System.Collections.IList;
            var sprites = weaponImages.GetValue(swap) as List<Sprite>;
            if (images == null || sprites == null) return result;
            for (int i = 0; i < Mathf.Min(4, images.Count); i++)
            {
                var image = images[i] as UnityEngine.UI.Image;
                if (image != null && image.sprite != null) result[i] = sprites.IndexOf(image.sprite);
            }
            return result;
        }

        /// <summary>The shooting component for a weapon kind (see <see cref="Slots"/>), via SwapManager.weapons.</summary>
        public Behaviour WeaponFor(int kind)
        {
            string[] solo = { "ShootProjectile", "LaunchGrenade", "FullyAuto", "AssaultBlaster", "Shotblaster", "SniperBlaster" };
            if (kind < 0 || kind >= solo.Length) return null;
            string name = IsNetwork ? (kind == 0 ? "NetworkShootProjectile" : kind == 1 ? "NetworkLaunchGrenade" : kind == 2 ? "NetworkFullyAuto" : kind == 3 ? "NetworkAB" : kind == 4 ? "NetworkShotblaster" : "NetworkSniper") : solo[kind];
            foreach (Behaviour b in Root.GetComponents<Behaviour>())
                if (b != null && b.GetType().Name == name) return b;
            return null;
        }

        public void SwapToSlot(int slot) => swapTo?.Invoke(swap, new object[] { slot });

        private static void Invoke(Behaviour target, string method, bool value)
        {
            MethodInfo m = target != null ? target.GetType().GetMethod(method, new[] { typeof(bool) }) : null;
            m?.Invoke(target, new object[] { value });
        }
    }
}
