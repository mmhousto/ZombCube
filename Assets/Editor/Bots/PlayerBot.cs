using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace Com.GCTC.ZombCube.EditorTools.Bots
{
    /// <summary>
    /// Plays one player through a <see cref="VirtualGamepad"/>, like a person with a controller:
    /// - Fights: picks the most dangerous visible cube (or the boss), leads its shots and keeps its distance,
    ///   backing away along open NavMesh directions instead of into walls or corners.
    /// - Shops: buys a wall gun (AB, SMB, Shotblaster, Sniper), ammo for it, or a health pack when it needs one,
    ///   has the points, and nothing is close enough to catch it on the way (holds West at the wall).
    /// - Works with the team: stays near the other players, keeps out of their line of fire and helps whoever
    ///   is being swarmed or is low on health.
    /// It only reads the game's state; every action goes through the controller.
    /// </summary>
    public class PlayerBot
    {
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        // Tuning (world units: the player is about 3.4 tall).
        private const float SafeDistance = 18f;
        private const float PanicDistance = 7f;
        private const float PreferredRange = 16f;
        private const float TeamRadius = 14f;
        private const float HealthToHeal = 60f;
        private const int HealthPackCost = 500;
        private const int WeaponCost = 1500;
        private const float ProjectileSpeed = 100f;
        private const float LookScale = 5f;   // PlayerActionControls: Look <Gamepad>/rightStick ScaleVector2(5,5)

        public static readonly List<PlayerBot> All = new List<PlayerBot>();

        public readonly GameObject Root;
        public readonly VirtualGamepad Pad;
        public readonly bool IsNetwork;
        public string Name => Root != null ? Root.name : "bot";
        public string Activity { get; private set; } = "starting";

        private readonly Component manager;
        private readonly Transform lookTransform;
        private readonly SwapManager swap;
        private readonly FieldInfo healthField, slotImagesField, weaponImagesField;

        private static readonly List<Transform> enemies = new List<Transform>();
        private static Transform boss;
        private static float enemiesScannedAt = -1f;

        private int navAgentType;
        private bool navReady;
        private Vector3 moveDir;
        private Transform target;
        private float nextTargetTime;

        private Collider shopTarget;
        private float shopStarted, westUntil, shopCooldownUntil, swapCooldownUntil;
        private bool holdingToBuy;
        private NavMeshPath path = new NavMeshPath();
        private int pathCorner;
        private float pathTime;

        private Vector3 lastPos;
        private float lastProgressTime;
        private Vector3 unstickDir;
        private float unstickUntil;

        public PlayerBot(GameObject root, VirtualGamepad pad)
        {
            Root = root;
            Pad = pad;
            manager = root.GetComponent("NetworkPlayerManager");
            IsNetwork = manager != null;
            if (!IsNetwork) manager = root.GetComponent<PlayerManager>();
            lookTransform = IsNetwork
                ? (root.GetComponentInChildren(Type.GetType("Com.GCTC.ZombCube.NetworkMouseLook, Assembly-CSharp"), true) as Component)?.transform
                : root.GetComponentInChildren<MouseLook>(true)?.transform;
            swap = root.GetComponent<SwapManager>();
            healthField = manager != null ? manager.GetType().GetField("healthPoints", Any) : null;
            slotImagesField = typeof(SwapManager).GetField("currentWeaponImages", Any);
            weaponImagesField = typeof(SwapManager).GetField("weaponImages", Any);
            lastPos = root.transform.position;
            lastProgressTime = Time.time;
            FindNavMeshAgentType();
            All.Add(this);
        }

        public void Dispose()
        {
            All.Remove(this);
            Pad.Release();
        }

        // ------------------------------------------------------------------ state (read only)

        public bool Alive => Root != null && Root.activeInHierarchy && Health > 0f;
        public float Health => healthField != null && manager != null ? Convert.ToSingle(healthField.GetValue(manager)) : 100f;
        public int Points => IsNetwork ? NetworkPoints() : PlayerManager.currentPoints;
        public Vector3 Position => Root.transform.position;
        private Vector3 Eye => lookTransform != null ? lookTransform.position : Position + Vector3.up * 3f;

        private static FieldInfo networkPoints;
        private static int NetworkPoints()
        {
            if (networkPoints == null)
                networkPoints = Type.GetType("Com.GCTC.ZombCube.NetworkPlayerManager, Assembly-CSharp")?.GetField("currentPoints", BindingFlags.Public | BindingFlags.Static);
            return networkPoints != null ? (int)networkPoints.GetValue(null) : 0;
        }

        /// <summary>Weapon kinds in the 4 slots (0 Pistol, 1 Grenade, 2 SMB, 3 AB, 4 Shotblaster, 5 Sniper), -1 empty.</summary>
        private int[] Slots()
        {
            var result = new[] { -1, -1, -1, -1 };
            if (swap == null) return result;
            var images = slotImagesField?.GetValue(swap) as System.Collections.IList;
            var sprites = weaponImagesField?.GetValue(swap) as List<Sprite>;
            if (images == null || sprites == null) return result;
            for (int i = 0; i < Mathf.Min(4, images.Count); i++)
            {
                var image = images[i] as UnityEngine.UI.Image;
                if (image != null && image.sprite != null) result[i] = sprites.IndexOf(image.sprite);
            }
            return result;
        }

        private Behaviour WeaponFor(int kind)
        {
            string[] solo = { "ShootProjectile", "LaunchGrenade", "FullyAuto", "AssaultBlaster", "Shotblaster", "SniperBlaster" };
            string[] network = { "NetworkShootProjectile", "NetworkLaunchGrenade", "NetworkFullyAuto", "NetworkAB", "NetworkShotblaster", "NetworkSniper" };
            if (kind < 0 || kind >= solo.Length) return null;
            string name = IsNetwork ? network[kind] : solo[kind];
            foreach (Behaviour b in Root.GetComponents<Behaviour>())
                if (b != null && b.GetType().Name == name) return b;
            return null;
        }

        private Behaviour EquippedWeapon()
        {
            for (int kind = 0; kind <= 5; kind++)
            {
                Behaviour w = WeaponFor(kind);
                if (w != null && w.enabled && kind != 1) return w;
            }
            return null;
        }

        private static int Ammo(Behaviour weapon)
        {
            if (weapon == null) return 0;
            FieldInfo clip = weapon.GetType().GetField("currentAmmoInClip", Any);
            FieldInfo reserve = weapon.GetType().GetField("reserveAmmo", Any);
            if (clip == null || reserve == null) return -1;   // unlimited (pistol)
            return Convert.ToInt32(clip.GetValue(weapon)) + Convert.ToInt32(reserve.GetValue(weapon));
        }

        private static int Clip(Behaviour weapon)
        {
            FieldInfo clip = weapon != null ? weapon.GetType().GetField("currentAmmoInClip", Any) : null;
            return clip != null ? Convert.ToInt32(clip.GetValue(weapon)) : -1;
        }

        // ------------------------------------------------------------------ each frame

        public void Tick()
        {
            if (!Alive)
            {
                Pad.Release();
                return;
            }

            ScanEnemies();
            Transform nearest = Nearest(Position, out float nearestDist);
            bool safe = nearestDist > SafeDistance;

            if (shopTarget == null && Time.time > shopCooldownUntil && (safe || enemies.Count == 0))
                shopTarget = ChooseShopTarget();
            if (shopTarget != null && (nearestDist < PanicDistance || !IsUsable(shopTarget.gameObject) || Time.time - shopStarted > 25f))
                StopShopping(nearestDist < PanicDistance ? 2f : 6f);

            if (Time.time >= nextTargetTime || target == null)
            {
                target = ChooseTarget();
                nextTargetTime = Time.time + 0.25f;
            }

            Vector3 desired;
            if (shopTarget != null)
            {
                Activity = "buying " + shopTarget.tag;
                desired = ShopMovement();
            }
            else
            {
                Activity = target != null ? (target == boss ? "fighting the boss" : "fighting") : "regrouping";
                desired = CombatMovement(nearest, nearestDist);
                ManageWeapons();
            }

            desired = Unstick(desired);
            moveDir = Vector3.Lerp(moveDir, desired, 0.35f);
            Vector3 local = Root.transform.InverseTransformDirection(Vector3.ClampMagnitude(Flat(moveDir), 1f));
            Pad.Move = new Vector2(local.x, local.z);
            Pad.Jump = Time.time < unstickUntil && Random.value < 0.1f;

            AimAndFire();
            Pad.West = Time.time < westUntil;
            Pad.Send();
        }

        // ------------------------------------------------------------------ aiming (right stick, closed loop)

        private void LookAt(Vector3 point)
        {
            Vector3 dir = point - Eye;
            if (dir.sqrMagnitude < 0.0001f) { Pad.Look = Vector2.zero; return; }
            float wantYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float wantPitch = -Mathf.Asin(Mathf.Clamp(dir.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;   // + is down

            float yaw = Root.transform.eulerAngles.y;
            float pitch = lookTransform != null ? Mathf.DeltaAngle(0f, lookTransform.localEulerAngles.x) : 0f;
            float dt = Mathf.Max(Time.deltaTime, 0.005f);
            float sensX = Mathf.Max(0.01f, PreferencesManager.GetHorizontalSens());
            float sensY = Mathf.Max(0.01f, PreferencesManager.GetVerticalSens());

            // Degrees turned next frame = stick * LookScale * sensitivity * dt; aim to close most of the gap.
            float stickX = Mathf.DeltaAngle(yaw, wantYaw) * 0.7f / (LookScale * sensX * dt);
            float stickY = -(wantPitch - pitch) * 0.7f / (LookScale * sensY * dt);
            Pad.Look = new Vector2(Mathf.Clamp(stickX, -1f, 1f), Mathf.Clamp(stickY, -1f, 1f));
        }

        private void AimAndFire()
        {
            if (target == null)
            {
                Pad.Fire = false;
                if (shopTarget != null) LookAt(shopTarget.bounds.center);
                else if (moveDir.sqrMagnitude > 0.01f) LookAt(Eye + Flat(moveDir) * 10f);
                else Pad.Look = Vector2.zero;
                return;
            }

            Vector3 aim = Center(target);
            NavMeshAgent agent = target.GetComponent<NavMeshAgent>();
            if (agent != null) aim += agent.velocity * Mathf.Min(Vector3.Distance(Eye, aim) / ProjectileSpeed, 0.6f);
            LookAt(aim);

            // Fire once roughly on target, and never through a teammate.
            Vector3 forward = lookTransform != null ? lookTransform.forward : Root.transform.forward;
            float error = Vector3.Angle(forward, aim - Eye);
            Pad.Fire = error < 12f && ClearShot(aim);
        }

        private bool ClearShot(Vector3 aim)
        {
            Vector3 dir = aim - Eye;
            foreach (RaycastHit hit in Physics.RaycastAll(Eye, dir.normalized, dir.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.root == Root.transform.root) continue;
                if (hit.transform.CompareTag("Player") || hit.transform.root.CompareTag("Player")) return false;
            }
            return true;
        }

        // ------------------------------------------------------------------ enemies

        private static void ScanEnemies()
        {
            if (Time.time - enemiesScannedAt < 0.2f && enemiesScannedAt >= 0f) return;
            enemiesScannedAt = Time.time;
            enemies.Clear();
            foreach (GameObject e in GameObject.FindGameObjectsWithTag("Enemy"))
                if (e.activeInHierarchy) enemies.Add(e.transform);
            GameObject b = GameObject.FindWithTag("BossCube");
            boss = b != null && b.activeInHierarchy ? b.transform : null;
            if (boss != null) enemies.Add(boss);
        }

        public static void ResetShared() => enemiesScannedAt = -1f;

        private static Transform Nearest(Vector3 from, out float distance)
        {
            Transform best = null;
            distance = float.MaxValue;
            foreach (Transform e in enemies)
            {
                if (e == null) continue;
                float d = Vector3.Distance(from, e.position);
                if (d < distance) { distance = d; best = e; }
            }
            return best;
        }

        private static Vector3 Center(Transform t)
        {
            Collider c = t.GetComponentInChildren<Collider>();
            return c != null ? c.bounds.center : t.position;
        }

        private bool CanSee(Transform t)
        {
            Vector3 to = Center(t);
            Vector3 dir = to - Eye;
            if (!Physics.Raycast(Eye, dir.normalized, out RaycastHit hit, dir.magnitude + 1f, ~0, QueryTriggerInteraction.Ignore)) return true;
            return hit.transform == t || hit.transform.IsChildOf(t) || hit.transform.root == t.root;
        }

        private Transform ChooseTarget()
        {
            Transform best = null;
            float bestScore = float.MaxValue;
            foreach (Transform e in enemies)
            {
                if (e == null) continue;
                float d = Vector3.Distance(Position, e.position);
                if (d > 120f || !CanSee(e)) continue;
                float score = d;
                if (e == boss) score = d < 40f ? d * 0.6f : d * 0.9f;
                foreach (PlayerBot mate in All)
                    if (mate != this && mate.Alive && Vector3.Distance(mate.Position, e.position) < 8f)
                        score -= mate.Health < 50f ? 8f : 4f;   // help a teammate who's being swarmed
                if (score < bestScore) { bestScore = score; best = e; }
            }
            return best;
        }

        // ------------------------------------------------------------------ movement

        private Vector3 CombatMovement(Transform nearest, float nearestDist)
        {
            Vector3 away = Vector3.zero;
            foreach (Transform e in enemies)
            {
                if (e == null) continue;
                Vector3 off = Flat(Position - e.position);
                float d = Mathf.Max(off.magnitude, 0.5f);
                if (d < 35f) away += off.normalized * (e == boss ? 2f : 1f) / (d * d) * 100f;
            }

            Vector3 desired = away + TeamPull();
            if (nearest != null && nearestDist > PreferredRange * 1.6f && away.magnitude < 0.3f)
                desired += PathDirection(nearest.position) * 0.8f;   // nothing close: go find the wave
            if (desired.sqrMagnitude < 0.01f) return Vector3.zero;
            return BestOpenDirection(desired.normalized);
        }

        private Vector3 TeamPull()
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (GameObject p in GameObject.FindGameObjectsWithTag("Player"))
            {
                if (p.transform.root == Root.transform.root || !p.activeInHierarchy) continue;
                sum += p.transform.position;
                count++;
            }
            if (count == 0) return Vector3.zero;
            Vector3 off = Flat(sum / count - Position);
            float d = off.magnitude;
            if (d < 3f) return -off.normalized * 0.5f;
            if (d < TeamRadius) return Vector3.zero;
            return PathDirection(sum / count) * Mathf.Clamp01((d - TeamRadius) / TeamRadius) * 1.5f;
        }

        private Vector3 BestOpenDirection(Vector3 wanted)
        {
            if (!navReady) return wanted;
            var filter = new NavMeshQueryFilter { agentTypeID = navAgentType, areaMask = NavMesh.AllAreas };
            if (!NavMesh.SamplePosition(Position, out NavMeshHit here, 4f, filter)) return wanted;
            Vector3 best = wanted;
            float bestScore = float.MinValue;
            for (int i = 0; i < 16; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, i * 22.5f, 0f) * Vector3.forward;
                float open = NavMesh.Raycast(here.position, here.position + dir * 10f, out NavMeshHit hit, filter) ? hit.distance : 10f;
                float score = Vector3.Dot(dir, wanted) * 2f + open / 10f * 1.5f;
                foreach (Transform e in enemies)
                {
                    if (e == null) continue;
                    Vector3 toEnemy = Flat(e.position - Position);
                    if (toEnemy.magnitude < 10f && Vector3.Dot(dir, toEnemy.normalized) > 0.7f) score -= 2f;
                }
                if (score > bestScore) { bestScore = score; best = dir; }
            }
            return best;
        }

        private Vector3 PathDirection(Vector3 destination)
        {
            if (!navReady) return Flat(destination - Position).normalized;
            var filter = new NavMeshQueryFilter { agentTypeID = navAgentType, areaMask = NavMesh.AllAreas };
            if (Time.time - pathTime > 0.5f || path.status == NavMeshPathStatus.PathInvalid)
            {
                pathTime = Time.time;
                pathCorner = 1;
                if (!NavMesh.SamplePosition(Position, out NavMeshHit a, 4f, filter) ||
                    !NavMesh.SamplePosition(destination, out NavMeshHit b, 6f, filter) ||
                    !NavMesh.CalculatePath(a.position, b.position, filter, path))
                    return Flat(destination - Position).normalized;
            }
            Vector3[] corners = path.corners;
            while (pathCorner < corners.Length && Flat(corners[pathCorner] - Position).magnitude < 1.5f) pathCorner++;
            Vector3 next = pathCorner < corners.Length ? corners[pathCorner] : destination;
            return Flat(next - Position).normalized;
        }

        private Vector3 Unstick(Vector3 desired)
        {
            if (Time.time < unstickUntil) return unstickDir;
            if (desired.sqrMagnitude > 0.1f)
            {
                if (Flat(Position - lastPos).magnitude > 1f) { lastPos = Position; lastProgressTime = Time.time; }
                else if (Time.time - lastProgressTime > 1.5f)
                {
                    unstickDir = Quaternion.Euler(0f, Random.Range(90f, 270f), 0f) * desired.normalized;
                    unstickUntil = Time.time + 0.8f;
                    lastProgressTime = Time.time;
                    pathTime = -1f;
                }
            }
            else { lastPos = Position; lastProgressTime = Time.time; }
            return desired;
        }

        private static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        private void FindNavMeshAgentType()
        {
            int count = NavMesh.GetSettingsCount();
            for (int i = 0; i < count; i++)
            {
                int id = NavMesh.GetSettingsByIndex(i).agentTypeID;
                var filter = new NavMeshQueryFilter { agentTypeID = id, areaMask = NavMesh.AllAreas };
                if (NavMesh.SamplePosition(Root.transform.position, out _, 6f, filter))
                {
                    navAgentType = id;
                    navReady = true;
                    return;
                }
            }
        }

        // ------------------------------------------------------------------ weapons (West to reload, D-pad to swap)

        private static readonly int[] WeaponPreference = { 3, 2, 4, 5, 0 };   // AB, SMB, Shotblaster, Sniper, Pistol

        private void ManageWeapons()
        {
            int[] slots = Slots();
            Behaviour equipped = EquippedWeapon();

            if (equipped != null && Clip(equipped) == 0 && Ammo(equipped) > 0 && Time.time > westUntil)
                westUntil = Time.time + 0.1f;   // tap West to reload

            bool bossFar = target != null && target == boss && Vector3.Distance(Position, boss.position) > 30f;
            int wantedSlot = bossFar ? SlotWith(slots, 5, true) : -1;
            if (wantedSlot < 0)
                foreach (int kind in WeaponPreference)
                {
                    wantedSlot = SlotWith(slots, kind, kind != 0);
                    if (wantedSlot >= 0) break;
                }
            if (wantedSlot >= 0 && WeaponFor(slots[wantedSlot]) != equipped && Time.time > swapCooldownUntil)
            {
                Pad.DpadSlot = wantedSlot;
                swapCooldownUntil = Time.time + 0.6f;
            }
        }

        private int SlotWith(int[] slots, int kind, bool needsAmmo)
        {
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] == kind && (!needsAmmo || Ammo(WeaponFor(kind)) != 0)) return i;
            return -1;
        }

        private bool HasWallGunWithAmmo(out int bestAmmoPercent)
        {
            bestAmmoPercent = 0;
            int[] fullAmmo = { 0, 0, 120, 240, 43, 25 };   // about a fresh purchase (clip + reserve)
            bool any = false;
            foreach (int kind in Slots())
            {
                if (kind < 2) continue;
                int ammo = Ammo(WeaponFor(kind));
                if (ammo <= 0) continue;
                any = true;
                bestAmmoPercent = Mathf.Max(bestAmmoPercent, Mathf.RoundToInt(100f * ammo / Mathf.Max(1, fullAmmo[kind])));
            }
            return any;
        }

        // ------------------------------------------------------------------ shopping (hold West at the wall)

        private static readonly string[] GunTags = { "AB", "SMB", "Shotblaster", "Sniper" };

        private Collider ChooseShopTarget()
        {
            int points = Points;
            if (Health <= HealthToHeal && points >= HealthPackCost)
            {
                Collider pack = NearestUsable("HealthPack");
                if (pack != null && PathIsSafe(pack.bounds.center)) return BeginShopping(pack);
            }

            bool hasGun = HasWallGunWithAmmo(out int ammoPercent);
            if (points >= WeaponCost && (!hasGun || ammoPercent < 35))
                foreach (string tag in GunTags)
                {
                    Collider gun = NearestUsable(tag);
                    if (gun != null && PathIsSafe(gun.bounds.center)) return BeginShopping(gun);
                }
            return null;
        }

        private Collider BeginShopping(Collider c)
        {
            shopStarted = Time.time;
            pathTime = -1f;
            return c;
        }

        private Collider NearestUsable(string tag)
        {
            Collider best = null;
            float bestDist = float.MaxValue;
            foreach (GameObject go in GameObject.FindGameObjectsWithTag(tag))
            {
                if (!go.activeInHierarchy || !IsUsable(go)) continue;
                Collider c = go.GetComponent<Collider>();
                if (c == null) continue;
                float d = Vector3.Distance(Position, c.bounds.center);
                if (d < bestDist) { bestDist = d; best = c; }
            }
            return best;
        }

        private static bool IsUsable(GameObject pickup)
        {
            if (pickup == null || !pickup.activeInHierarchy) return false;
            foreach (MonoBehaviour b in pickup.GetComponents<MonoBehaviour>())
            {
                FieldInfo field = b != null ? b.GetType().GetField("isUsable") : null;
                if (field != null) return (bool)field.GetValue(b);
            }
            return false;
        }

        private bool PathIsSafe(Vector3 destination)
        {
            foreach (Transform e in enemies)
            {
                if (e == null) continue;
                if (Vector3.Distance(e.position, destination) < SafeDistance) return false;
                Vector3 ab = Flat(destination - Position);
                float t = ab.sqrMagnitude > 0.01f ? Mathf.Clamp01(Vector3.Dot(Flat(e.position - Position), ab) / ab.sqrMagnitude) : 0f;
                if (Flat(Position + ab * t - e.position).magnitude < SafeDistance * 0.6f) return false;
            }
            return true;
        }

        private Vector3 ShopMovement()
        {
            Pad.Fire = false;
            Vector3 point = shopTarget.ClosestPoint(Position);
            float distance = Flat(point - Position).magnitude;
            bool inside = shopTarget.bounds.Contains(Position) || distance < 0.6f;

            if (inside)
            {
                // Hold West like a player (the Interact action is a hold), then let go and check it bought.
                if (!holdingToBuy)
                {
                    holdingToBuy = true;
                    westUntil = Time.time + 1.0f;
                }
                else if (Time.time > westUntil + 0.3f)
                {
                    holdingToBuy = false;
                    if (!IsUsable(shopTarget.gameObject))
                    {
                        BotRun.RecordPurchase(this, shopTarget.tag);
                        StopShopping(1f);
                    }
                }
                return Flat(point - Position) * 0.3f;
            }

            holdingToBuy = false;
            return distance < 6f ? Flat(point - Position).normalized : PathDirection(shopTarget.bounds.center);
        }

        private void StopShopping(float cooldown)
        {
            holdingToBuy = false;
            westUntil = 0f;
            shopTarget = null;
            shopCooldownUntil = Time.time + cooldown;
        }
    }
}
