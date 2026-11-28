using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Com.GCTC.ZombCube
{
    public class HandlePowerups : MonoBehaviourPun
    {
        /// <summary>How long Triple Shot, Full Auto, Speed and the point multipliers last.</summary>
        public const float PowerupDuration = 25f;

        public PlayerMovement playerMovement;
        public TripleShot tripleShotPowerup;
        public FullyAuto smB; // SMB
        public AssaultBlaster assaultBlaster; // AB
        public Shotblaster shotBlaster; // Shotblaster
        public LaunchGrenade launchGrenade;
        public ShootProjectile shoot;
        public NetworkShootProjectile networkShoot;
        public NetworkPlayerMovement networkPlayerMovement;
        public NetworkLaunchGrenade networkLaunchGrenade;
        public NetworkTripleShot networkTriple;
        public NetworkFullyAuto networkFullyAuto; // SMB
        public NetworkAB networkAB; // AB
        public NetworkShotblaster networkShotblaster; //Shotblaster

        [Tooltip("PowerUps indicator prefab, added to this player's HUD when the HUD doesn't already have one.")]
        public GameObject powerupIndicatorsPrefab;

        // When each active powerup runs out (Full Auto and Speed stack, one entry per pickup).
        private readonly List<float> fullAutoEnds = new List<float>();
        private readonly List<float> speedEnds = new List<float>();
        private float tripleShotEnds;
        private PowerupIndicator tripleShotIndicator, speedIndicator, fullAutoIndicator;
        private bool indicatorsSearched;

        private bool IsLocalPlayer => GameScenes.IsSoloActive || (GameScenes.IsPartyActive && photonView.IsMine);

        private void Start()
        {
            if (GameScenes.IsSoloActive)
            {
                shoot = GetComponent<ShootProjectile>();
                playerMovement = GetComponent<PlayerMovement>();
                tripleShotPowerup = GetComponent<TripleShot>();
                smB = GetComponent<FullyAuto>();
                assaultBlaster = GetComponent<AssaultBlaster>();
                shotBlaster = GetComponent<Shotblaster>();
                launchGrenade = GetComponent<LaunchGrenade>();
            }
            else if (GameScenes.IsPartyActive)
            {
                networkShoot = GetComponent<NetworkShootProjectile>();
                networkPlayerMovement = GetComponent<NetworkPlayerMovement>();
                networkLaunchGrenade = GetComponent<NetworkLaunchGrenade>();
                networkTriple = GetComponent<NetworkTripleShot>();
                networkFullyAuto = GetComponent<NetworkFullyAuto>();
                networkAB = GetComponent<NetworkAB>();
                networkShotblaster = GetComponent<NetworkShotblaster>();
            }
        }

        private void Update()
        {
            if (!IsLocalPlayer) return;
            if (!indicatorsSearched) FindIndicators();

            float now = Time.time;
            fullAutoEnds.RemoveAll(end => end <= now);
            speedEnds.RemoveAll(end => end <= now);

            Behaviour triple = GameScenes.IsSoloActive ? (Behaviour)tripleShotPowerup : networkTriple;
            bool tripleActive = triple != null && triple.enabled && tripleShotEnds > now;
            tripleShotIndicator?.Show(tripleActive, tripleActive ? (tripleShotEnds - now) / PowerupDuration : 0f, 1);
            speedIndicator?.Show(speedEnds.Count > 0, Remaining(speedEnds, now), speedEnds.Count);
            fullAutoIndicator?.Show(fullAutoEnds.Count > 0, Remaining(fullAutoEnds, now), fullAutoEnds.Count);
        }

        private static float Remaining(List<float> ends, float now)
        {
            float last = 0f;
            foreach (float end in ends) last = Mathf.Max(last, end);
            return Mathf.Clamp01((last - now) / PowerupDuration);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (GameScenes.IsSoloActive)
            {
                if (other.CompareTag("TripleShot"))
                {
                    StartTripleShotTimer(tripleShotPowerup);
                    tripleShotPowerup.enabled = true;
                    Destroy(other.gameObject);
                }
                else if(other.CompareTag("FullyAuto"))
                {
                    shoot.fireRate /= 2;
                    tripleShotPowerup.fireRate /= 2;
                    smB.fireRate /= 2;
                    assaultBlaster.fireRate /= 2;
                    shotBlaster.fireRate /= 2;
                    fullAutoEnds.Add(Time.time + PowerupDuration);
                    StartCoroutine(EndFullAutoPowerup());
                    Destroy(other.gameObject);
                }
                else if(other.CompareTag("SpeedBoost"))
                {
                    playerMovement.ActivateSpeedBoost();
                    speedEnds.Add(Time.time + PowerupDuration);
                    Destroy(other.gameObject);
                }
                else if(other.CompareTag("x2"))
                {
                    ApplyMultiplier(2);
                    Destroy(other.gameObject);
                }
                else if(other.CompareTag("x3"))
                {
                    ApplyMultiplier(3);
                    Destroy(other.gameObject);
                }
                else if(other.CompareTag("MaxNades") && GameManager.mode == 0)
                {
                    launchGrenade.grenadeCount = 4;
                    Destroy(other.gameObject);
                }
                else if (other.CompareTag("MaxNades") && GameManager.mode == 1)
                {
                    CouchCoopManager.Instance.SetMaxNades();
                    Destroy(other.gameObject);
                }
            }
            else if (GameScenes.IsPartyActive && photonView.IsMine)
            {
                if (!IsPowerup(other) || !other.gameObject.activeInHierarchy) return;

                if (other.CompareTag("TripleShot"))
                {
                    StartTripleShotTimer(networkTriple);
                    networkTriple.enabled = true;
                }
                else if (other.CompareTag("FullyAuto"))
                {
                    networkShoot.fireRate /= 2;
                    networkTriple.fireRate /= 2;
                    networkFullyAuto.fireRate /= 2;
                    networkAB.fireRate /= 2;
                    networkShotblaster.fireRate /= 2;
                    fullAutoEnds.Add(Time.time + PowerupDuration);
                    StartCoroutine(EndFullAutoPowerup());
                }
                else if (other.CompareTag("SpeedBoost"))
                {
                    networkPlayerMovement.ActivateSpeedBoost();
                    speedEnds.Add(Time.time + PowerupDuration);
                }
                else if (other.CompareTag("x2"))
                {
                    // Point multipliers are shared: everyone in the party gets it.
                    photonView.RPC(nameof(RPC_ApplyMultiplier), RpcTarget.All, 2);
                }
                else if (other.CompareTag("x3"))
                {
                    photonView.RPC(nameof(RPC_ApplyMultiplier), RpcTarget.All, 3);
                }
                else if (other.CompareTag("MaxNades"))
                {
                    networkLaunchGrenade.grenadeCount = 4;
                }

                TakeNetworkPowerup(other.gameObject);
            }
        }

        private static bool IsPowerup(Collider other) =>
            other.CompareTag("TripleShot") || other.CompareTag("FullyAuto") || other.CompareTag("SpeedBoost") ||
            other.CompareTag("x2") || other.CompareTag("x3") || other.CompareTag("MaxNades");

        private void StartTripleShotTimer(Behaviour triple)
        {
            // TripleShot ends itself 25 s after it's enabled; picking another up while it's on doesn't restart it.
            if (triple == null || !triple.enabled || tripleShotEnds <= Time.time)
                tripleShotEnds = Time.time + PowerupDuration;
        }

        // Party: hide the pickup straight away (so it can't be taken twice) and have the master client remove it
        // for everyone. Asking for ownership and destroying in the same frame never worked the first time,
        // because ownership only changes after the request goes round the network.
        private void TakeNetworkPowerup(GameObject pickup)
        {
            PhotonView view = pickup.GetComponent<PhotonView>();
            pickup.SetActive(false);
            if (view != null) photonView.RPC(nameof(RPC_DestroyPowerup), RpcTarget.MasterClient, view.ViewID);
            else Destroy(pickup);
        }

        [PunRPC]
        private void RPC_DestroyPowerup(int viewId)
        {
            PhotonView view = PhotonView.Find(viewId);
            if (view != null && PhotonNetwork.IsMasterClient) PhotonNetwork.Destroy(view.gameObject);
        }

        [PunRPC]
        private void RPC_ApplyMultiplier(int factor)
        {
            ApplyMultiplier(factor);
        }

        /// <summary>
        /// Multiplies the points per hit (max 120) for 25 s. The timer runs on the game manager, not this player,
        /// so it still ends if this player is eliminated or leaves.
        /// </summary>
        private static void ApplyMultiplier(int factor)
        {
            int currentPoints = Projectile.pointsToAdd;
            int pointsAdded = currentPoints * factor > 120 ? 120 - currentPoints : (currentPoints * factor) - currentPoints;
            Projectile.pointsToAdd = currentPoints * factor > 120 ? 120 : currentPoints * factor;

            MonoBehaviour runner = GameManager.Instance != null ? (MonoBehaviour)GameManager.Instance : NetworkGameManager.Instance;
            if (runner != null) runner.StartCoroutine(EndMultiplierPowerup(pointsAdded));
        }

        private static IEnumerator EndMultiplierPowerup(int pointsToMinus)
        {
            yield return new WaitForSeconds(PowerupDuration);
            Projectile.pointsToAdd -= pointsToMinus;
            if(Projectile.pointsToAdd < 10) Projectile.pointsToAdd = 10;
        }

        IEnumerator EndFullAutoPowerup()
        {
            yield return new WaitForSeconds(PowerupDuration);
            if (GameScenes.IsSoloActive)
            {
                tripleShotPowerup.fireRate *= 2;
                shoot.fireRate *= 2;
                smB.fireRate *= 2;
                assaultBlaster.fireRate *= 2;
                shotBlaster.fireRate *= 2;
            }
            else if (GameScenes.IsPartyActive)
            {
                networkTriple.fireRate *= 2;
                networkShoot.fireRate *= 2;
                networkFullyAuto.fireRate *= 2;
                networkAB.fireRate *= 2;
                networkShotblaster.fireRate *= 2;
            }
        }

        // ------------------------------------------------------------------ indicators

        // Uses this player's HUD: its own canvas in Co-op (found through its health bar), the scene HUD otherwise.
        // If that HUD has no PowerUps indicators, adds one from powerupIndicatorsPrefab at the top centre.
        private void FindIndicators()
        {
            indicatorsSearched = true;
            Transform hud = null;
            PlayerManager manager = GetComponent<PlayerManager>();
            if (manager != null && manager.healthBar != null && manager.healthBar.canvas != null)
                hud = manager.healthBar.canvas.rootCanvas.transform;

            Transform powerUps = hud != null ? FindChild(hud, "PowerUps") : null;
            if (powerUps == null && hud == null)
            {
                foreach (RectTransform rect in FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (rect.name == "PowerUps" && rect.GetComponentInParent<PlayerManager>(true) == null && rect.GetComponentInParent<NetworkPlayerManager>(true) == null)
                    {
                        powerUps = rect;
                        break;
                    }
            }

            if (powerUps == null && powerupIndicatorsPrefab != null)
            {
                Transform parent = hud;
                if (parent == null)
                {
                    Canvas canvas = FindFirstObjectByType<Canvas>();
                    parent = canvas != null ? canvas.rootCanvas.transform : null;
                }
                if (parent != null)
                {
                    Transform safeArea = parent.Find("SafeArea");
                    var rt = (RectTransform)Instantiate(powerupIndicatorsPrefab, safeArea != null ? safeArea : parent, false).transform;
                    rt.name = "PowerUps";
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(0f, -91f);
                    powerUps = rt;
                }
            }
            if (powerUps == null) return;

            powerUps.gameObject.SetActive(true);
            tripleShotIndicator = PowerupIndicator.Find(powerUps, "PowerUPIndicatorTripleShot");
            speedIndicator = PowerupIndicator.Find(powerUps, "PowerUPIndicatorSpeed");
            fullAutoIndicator = PowerupIndicator.Find(powerUps, "PowerUPIndicatorFullAuto");
        }

        private static Transform FindChild(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        /// <summary>One powerup icon: the Timer image's fill shows the time left, Mult shows the stack (x2, x3).</summary>
        private class PowerupIndicator
        {
            private GameObject root;
            private Image timer;
            private TextMeshProUGUI stack;

            public static PowerupIndicator Find(Transform parent, string name)
            {
                Transform t = FindChild(parent, name);
                if (t == null) return null;
                var indicator = new PowerupIndicator { root = t.gameObject };
                Transform timer = t.Find("Timer");
                Transform stack = t.Find("Mult");
                indicator.timer = timer != null ? timer.GetComponent<Image>() : null;
                indicator.stack = stack != null ? stack.GetComponent<TextMeshProUGUI>() : null;
                indicator.root.SetActive(false);
                return indicator;
            }

            public void Show(bool active, float fill, int count)
            {
                if (root.activeSelf != active) root.SetActive(active);
                if (!active) return;
                if (timer != null) timer.fillAmount = fill;
                if (stack != null)
                {
                    bool stacked = count > 1;
                    if (stack.gameObject.activeSelf != stacked) stack.gameObject.SetActive(stacked);
                    if (stacked) stack.text = "x" + count;
                }
            }
        }
    }
}
