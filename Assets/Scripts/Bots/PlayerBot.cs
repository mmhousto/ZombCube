using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Com.GCTC.ZombCube
{
    /// <summary>
    /// Plays a player like a person would, through the normal controls (see <see cref="BotPlayerAdapter"/>):
    /// - Fights: picks the most dangerous visible cube (or the boss), leads its shots and keeps its distance,
    ///   backing away along open NavMesh directions instead of into walls or corners.
    /// - Shops: buys a wall gun (AB, SMB, Shotblaster, Sniper), ammo for it, or a health pack when it needs one,
    ///   has the points, and nothing is close enough to catch it on the way.
    /// - Works with the team: stays near the other players, keeps out of their line of fire and helps whoever
    ///   is being swarmed or is low on health.
    /// Runs before the player's own scripts each frame so its inputs are used that frame.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class PlayerBot : MonoBehaviour
    {
        // Tuning (world units: the player is about 3.4 tall).
        private const float SafeDistance = 18f;       // no enemy this close to shop
        private const float PanicDistance = 7f;       // enemy this close: stop shopping and fight
        private const float PreferredRange = 16f;     // keep cubes about this far away
        private const float TeamRadius = 14f;         // stay within this of the team
        private const float HealthToHeal = 60f;
        private const int HealthPackCost = 500;
        private const int WeaponCost = 1500;
        private const float ProjectileSpeed = 100f;   // for leading shots

        public static readonly List<PlayerBot> All = new List<PlayerBot>();

        public BotPlayerAdapter Player { get; private set; }
        public int Purchases { get; private set; }
        public int HealthPacksBought { get; private set; }
        public string Activity { get; private set; } = "starting";

        private static readonly List<Transform> enemies = new List<Transform>();
        private static Transform boss;
        private static float enemiesScannedAt = -1f;

        private int navAgentType;
        private bool navReady;
        private Vector3 moveDir;
        private Transform target;
        private float nextTargetTime;

        // Shopping
        private Collider shopTarget;
        private float shopStarted, interactUntil, shopCooldownUntil;
        private bool interacting;
        private NavMeshPath path;
        private int pathCorner;
        private float pathTime;

        // Stuck detection
        private Vector3 lastPos;
        private float lastProgressTime;
        private Vector3 unstickDir;
        private float unstickUntil;

        public static PlayerBot Attach(GameObject player)
        {
            if (player == null || player.GetComponent<PlayerBot>() != null) return null;
            BotPlayerAdapter adapter = BotPlayerAdapter.TryCreate(player);
            if (adapter == null) return null;
            PlayerBot bot = player.AddComponent<PlayerBot>();
            bot.Player = adapter;
            return bot;
        }

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        private void Start()
        {
            path = new NavMeshPath();
            lastPos = transform.position;
            lastProgressTime = Time.time;
            FindNavMeshAgentType();
        }

        private void Update()
        {
            if (Player == null || !Player.Alive)
            {
                if (Player != null) { Player.Fire(false); Player.Interact(false); Player.Move(Vector3.zero); }
                return;
            }

            ScanEnemies();
            ManageWeapons();

            Transform nearest = Nearest(Player.Position, out float nearestDist);
            bool safe = nearestDist > SafeDistance;

            // Shopping has priority when it's safe; fighting when anything is close.
            if (shopTarget == null && Time.time > shopCooldownUntil && (safe || enemies.Count == 0))
                shopTarget = ChooseShopTarget();
            if (shopTarget != null && (nearestDist < PanicDistance || !StillWorthBuying(shopTarget) || Time.time - shopStarted > 25f))
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
            }

            desired = Unstick(desired);
            moveDir = Vector3.Lerp(moveDir, desired, 0.35f);
            bool jumpNow = Time.time < unstickUntil && Random.value < 0.05f;
            Player.Move(moveDir, jumpNow);

            AimAndFire();
        }

        // ------------------------------------------------------------------ enemies

        private static void ScanEnemies()
        {
            if (Time.time - enemiesScannedAt < 0.2f) return;
            enemiesScannedAt = Time.time;
            enemies.Clear();
            foreach (GameObject e in GameObject.FindGameObjectsWithTag("Enemy"))
                if (e.activeInHierarchy) enemies.Add(e.transform);
            GameObject b = GameObject.FindWithTag("BossCube");
            boss = b != null && b.activeInHierarchy ? b.transform : null;
            if (boss != null) enemies.Add(boss);
        }

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
            Vector3 from = Player.Eye;
            Vector3 to = Center(t);
            Vector3 dir = to - from;
            if (!Physics.Raycast(from, dir.normalized, out RaycastHit hit, dir.magnitude + 1f, ~0, QueryTriggerInteraction.Ignore))
                return true;
            return hit.transform == t || hit.transform.IsChildOf(t) || t.IsChildOf(hit.transform.root) && hit.transform.root == t.root;
        }

        // Threat: close cubes first, the boss when nothing is close, and cubes going after teammates.
        private Transform ChooseTarget()
        {
            Transform best = null;
            float bestScore = float.MaxValue;
            foreach (Transform e in enemies)
            {
                if (e == null) continue;
                float d = Vector3.Distance(Player.Position, e.position);
                if (d > 120f || !CanSee(e)) continue;
                float score = d;
                if (e == boss) score = d < 40f ? d * 0.6f : d * 0.9f;
                foreach (PlayerBot mate in All)
                    if (mate != this && mate.Player != null && mate.Player.Alive && Vector3.Distance(mate.Player.Position, e.position) < 8f)
                        score -= mate.Player.Health < 50f ? 8f : 4f;   // help a teammate who's being swarmed
                if (score < bestScore) { bestScore = score; best = e; }
            }
            return best;
        }

        private void AimAndFire()
        {
            if (target == null)
            {
                Player.Fire(false);
                if (shopTarget != null) Player.LookAt(shopTarget.bounds.center);
                else if (moveDir.sqrMagnitude > 0.01f) Player.LookAt(Player.Eye + moveDir * 10f);
                return;
            }

            Vector3 aim = Center(target);
            NavMeshAgent agent = target.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                float time = Vector3.Distance(Player.Eye, aim) / ProjectileSpeed;
                aim += agent.velocity * Mathf.Min(time, 0.6f);
            }
            Player.LookAt(aim);
            Player.Fire(ClearShot(aim));
        }

        // Don't shoot through a teammate.
        private bool ClearShot(Vector3 aim)
        {
            Vector3 from = Player.Eye;
            Vector3 dir = aim - from;
            foreach (RaycastHit hit in Physics.RaycastAll(from, dir.normalized, dir.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.root == transform.root) continue;
                if (hit.transform.CompareTag("Player") || hit.transform.root.CompareTag("Player")) return false;
            }
            return true;
        }

        // ------------------------------------------------------------------ movement

        private Vector3 CombatMovement(Transform nearest, float nearestDist)
        {
            // Repulsion from cubes (stronger when close), pull towards the team, and back to range when far.
            Vector3 away = Vector3.zero;
            foreach (Transform e in enemies)
            {
                if (e == null) continue;
                Vector3 off = Player.Position - e.position;
                off.y = 0f;
                float d = Mathf.Max(off.magnitude, 0.5f);
                if (d < 35f) away += off.normalized * (e == boss ? 2f : 1f) / (d * d) * 100f;
            }

            Vector3 team = TeamPull();
            Vector3 desired = away + team;

            // Nothing threatening: walk towards the closest cube to keep the wave moving.
            if (nearest != null && nearestDist > PreferredRange * 1.6f && away.magnitude < 0.3f)
                desired += PathDirection(nearest.position) * 0.8f;

            if (desired.sqrMagnitude < 0.01f) return Vector3.zero;
            return BestOpenDirection(desired.normalized);
        }

        private Vector3 TeamPull()
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (GameObject p in GameObject.FindGameObjectsWithTag("Player"))
            {
                if (p.transform.root == transform.root || !p.activeInHierarchy) continue;
                sum += p.transform.position;
                count++;
            }
            if (count == 0) return Vector3.zero;
            Vector3 centre = sum / count;
            Vector3 off = centre - Player.Position;
            off.y = 0f;
            float d = off.magnitude;
            if (d < 3f) return -off.normalized * 0.5f;         // don't stand on each other
            if (d < TeamRadius) return Vector3.zero;
            return PathDirection(centre) * Mathf.Clamp01((d - TeamRadius) / TeamRadius) * 1.5f;
        }

        // Of 16 directions, the most open one close to where we want to go (NavMesh raycasts).
        private Vector3 BestOpenDirection(Vector3 wanted)
        {
            if (!navReady) return wanted;
            Vector3 origin = Player.Position;
            Vector3 best = wanted;
            float bestScore = float.MinValue;
            var filter = new NavMeshQueryFilter { agentTypeID = navAgentType, areaMask = NavMesh.AllAreas };
            if (!NavMesh.SamplePosition(origin, out NavMeshHit here, 4f, filter)) return wanted;
            for (int i = 0; i < 16; i++)
            {
                float angle = i * 22.5f;
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                float open = NavMesh.Raycast(here.position, here.position + dir * 10f, out NavMeshHit hit, filter) ? hit.distance : 10f;
                float score = Vector3.Dot(dir, wanted) * 2f + open / 10f * 1.5f;
                // Avoid running into a cube.
                foreach (Transform e in enemies)
                {
                    if (e == null) continue;
                    Vector3 toEnemy = e.position - origin;
                    toEnemy.y = 0f;
                    if (toEnemy.magnitude < 10f && Vector3.Dot(dir, toEnemy.normalized) > 0.7f) score -= 2f;
                }
                if (score > bestScore) { bestScore = score; best = dir; }
            }
            return best;
        }

        private Vector3 PathDirection(Vector3 destination)
        {
            if (!navReady) return Flat(destination - Player.Position).normalized;
            var filter = new NavMeshQueryFilter { agentTypeID = navAgentType, areaMask = NavMesh.AllAreas };
            if (Time.time - pathTime > 0.5f || path.status == NavMeshPathStatus.PathInvalid)
            {
                pathTime = Time.time;
                pathCorner = 1;
                if (!NavMesh.SamplePosition(Player.Position, out NavMeshHit a, 4f, filter) ||
                    !NavMesh.SamplePosition(destination, out NavMeshHit b, 6f, filter) ||
                    !NavMesh.CalculatePath(a.position, b.position, filter, path))
                    return Flat(destination - Player.Position).normalized;
            }
            Vector3[] corners = path.corners;
            while (pathCorner < corners.Length && Flat(corners[pathCorner] - Player.Position).magnitude < 1.5f) pathCorner++;
            Vector3 next = pathCorner < corners.Length ? corners[pathCorner] : destination;
            return Flat(next - Player.Position).normalized;
        }

        private Vector3 Unstick(Vector3 desired)
        {
            if (Time.time < unstickUntil) return unstickDir;
            if (desired.sqrMagnitude > 0.1f)
            {
                if (Flat(Player.Position - lastPos).magnitude > 1f)
                {
                    lastPos = Player.Position;
                    lastProgressTime = Time.time;
                }
                else if (Time.time - lastProgressTime > 1.5f)
                {
                    unstickDir = Quaternion.Euler(0f, Random.Range(90f, 270f), 0f) * desired.normalized;
                    unstickUntil = Time.time + 0.8f;
                    lastProgressTime = Time.time;
                    pathTime = -1f;
                }
            }
            else
            {
                lastPos = Player.Position;
                lastProgressTime = Time.time;
            }
            return desired;
        }

        private static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        private void FindNavMeshAgentType()
        {
            // Use whichever baked NavMesh is under the player (the levels bake for the enemy agent type).
            int count = NavMesh.GetSettingsCount();
            for (int i = 0; i < count; i++)
            {
                int id = NavMesh.GetSettingsByIndex(i).agentTypeID;
                var filter = new NavMeshQueryFilter { agentTypeID = id, areaMask = NavMesh.AllAreas };
                if (NavMesh.SamplePosition(transform.position, out _, 6f, filter))
                {
                    navAgentType = id;
                    navReady = true;
                    return;
                }
            }
        }

        // ------------------------------------------------------------------ weapons

        // Weapon kinds (SwapManager.weaponImages): 0 Pistol, 1 Grenade, 2 SMB, 3 AB, 4 Shotblaster, 5 Sniper.
        private static readonly int[] WeaponPreference = { 3, 2, 4, 5, 0 };

        private void ManageWeapons()
        {
            int[] slots = Player.Slots();
            Behaviour equipped = Player.EquippedWeapon();

            // Reload an empty clip if there's reserve ammo.
            if (equipped != null && BotPlayerAdapter.Clip(equipped) == 0 && BotPlayerAdapter.Ammo(equipped) > 0)
                BotPlayerAdapter.Reload(equipped);

            // Best weapon that still has ammo; the sniper for the boss at range.
            bool bossFar = target != null && target == boss && Vector3.Distance(Player.Position, boss.position) > 30f;
            int wantedSlot = -1;
            if (bossFar) wantedSlot = SlotWith(slots, 5, true);
            if (wantedSlot < 0)
                foreach (int kind in WeaponPreference)
                {
                    wantedSlot = SlotWith(slots, kind, kind != 0);
                    if (wantedSlot >= 0) break;
                }
            if (wantedSlot >= 0 && Player.WeaponFor(slots[wantedSlot]) != equipped && Time.frameCount % 15 == 0)
            {
                Player.Fire(false);
                Player.SwapToSlot(wantedSlot);
            }
        }

        private int SlotWith(int[] slots, int kind, bool needsAmmo)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != kind) continue;
                if (!needsAmmo) return i;
                int ammo = BotPlayerAdapter.Ammo(Player.WeaponFor(kind));
                if (ammo != 0) return i;
            }
            return -1;
        }

        private bool HasWallGunWithAmmo(out int bestAmmoPercent)
        {
            bestAmmoPercent = 0;
            int[] slots = Player.Slots();
            int[] fullAmmo = { 0, 0, 120, 240, 43, 25 };   // roughly a fresh purchase (clip + reserve)
            bool any = false;
            foreach (int kind in slots)
            {
                if (kind < 2) continue;
                int ammo = BotPlayerAdapter.Ammo(Player.WeaponFor(kind));
                if (ammo <= 0) continue;
                any = true;
                bestAmmoPercent = Mathf.Max(bestAmmoPercent, Mathf.RoundToInt(100f * ammo / Mathf.Max(1, fullAmmo[kind])));
            }
            return any;
        }

        // ------------------------------------------------------------------ shopping

        private static readonly string[] GunTags = { "AB", "SMB", "Shotblaster", "Sniper" };

        private Collider ChooseShopTarget()
        {
            int points = Player.Points;

            if (Player.Health <= HealthToHeal && points >= HealthPackCost)
            {
                Collider pack = NearestUsable("HealthPack");
                if (pack != null && PathIsSafe(pack.bounds.center)) return BeginShopping(pack);
            }

            bool hasGun = HasWallGunWithAmmo(out int ammoPercent);
            if (points >= WeaponCost && (!hasGun || ammoPercent < 35))
            {
                foreach (string tag in GunTags)
                {
                    Collider gun = NearestUsable(tag);
                    if (gun != null && PathIsSafe(gun.bounds.center)) return BeginShopping(gun);
                }
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
                float d = Vector3.Distance(Player.Position, c.bounds.center);
                if (d < bestDist) { bestDist = d; best = c; }
            }
            return best;
        }

        private static bool IsUsable(GameObject pickup)
        {
            foreach (MonoBehaviour b in pickup.GetComponents<MonoBehaviour>())
            {
                var field = b != null ? b.GetType().GetField("isUsable") : null;
                if (field != null) return (bool)field.GetValue(b);
            }
            return false;
        }

        private bool StillWorthBuying(Collider c) => c != null && c.gameObject.activeInHierarchy && IsUsable(c.gameObject);

        // No cube close to the pickup or near the way there.
        private bool PathIsSafe(Vector3 destination)
        {
            foreach (Transform e in enemies)
            {
                if (e == null) continue;
                if (Vector3.Distance(e.position, destination) < SafeDistance) return false;
                Vector3 a = Player.Position, b = destination, p = e.position;
                Vector3 ab = Flat(b - a);
                float t = ab.sqrMagnitude > 0.01f ? Mathf.Clamp01(Vector3.Dot(Flat(p - a), ab) / ab.sqrMagnitude) : 0f;
                if (Flat(a + ab * t - p).magnitude < SafeDistance * 0.6f) return false;
            }
            return true;
        }

        private Vector3 ShopMovement()
        {
            Vector3 point = shopTarget.ClosestPoint(Player.Position);
            float distance = Flat(point - Player.Position).magnitude;
            bool inside = shopTarget.bounds.Contains(Player.Position) || distance < 0.6f;

            if (inside)
            {
                // Hold Interact (the game needs a 0.25 s hold), then let go and see if it bought.
                if (!interacting)
                {
                    interacting = true;
                    interactUntil = Time.time + 0.45f;
                    Player.Interact(true);
                }
                else if (Time.time > interactUntil)
                {
                    Player.Interact(false);
                    interacting = false;
                    if (!IsUsable(shopTarget.gameObject))
                    {
                        Purchases++;
                        if (shopTarget.CompareTag("HealthPack")) HealthPacksBought++;
                        BotRunController.RecordPurchase(this, shopTarget.tag);
                        StopShopping(1f);
                    }
                }
                return Flat(point - Player.Position) * 0.3f;   // stay in the trigger
            }

            if (interacting) { Player.Interact(false); interacting = false; }
            return distance < 6f ? Flat(point - Player.Position).normalized : PathDirection(shopTarget.bounds.center);
        }

        private void StopShopping(float cooldown)
        {
            if (interacting) Player.Interact(false);
            interacting = false;
            shopTarget = null;
            shopCooldownUntil = Time.time + cooldown;
        }
    }
}
