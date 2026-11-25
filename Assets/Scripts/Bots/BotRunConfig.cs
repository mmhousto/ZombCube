using UnityEngine;

namespace Com.GCTC.ZombCube
{
    /// <summary>Prefabs a bot run needs to set up Co-op without the lobby (Resources/BotRunConfig).</summary>
    [CreateAssetMenu(menuName = "ZombCube/Bot Run Config")]
    public class BotRunConfig : ScriptableObject
    {
        public const string ResourcePath = "BotRunConfig";

        [Tooltip("Player spawned for each co-op player (CouchCoopManager.playerPrefab).")]
        public GameObject coopPlayerPrefab;

        [Tooltip("The PlayerInput object the co-op lobby creates for each player (CouchCoopRoomManager.playerInputPrefab).")]
        public GameObject playerInputPrefab;
    }
}
