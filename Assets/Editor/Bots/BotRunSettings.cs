using UnityEngine;

namespace Com.GCTC.ZombCube.EditorTools.Bots
{
    /// <summary>
    /// Map, speed and limits for bot runs started from the "Bots - ..." Multiplayer Play Mode scenarios
    /// (Assets/Editor/Bots/BotRunSettings.asset). The scenario decides the mode and number of players.
    /// </summary>
    [CreateAssetMenu(menuName = "ZombCube/Bot Run Settings")]
    public class BotRunSettings : ScriptableObject
    {
        public const string AssetPath = "Assets/Editor/Bots/BotRunSettings.asset";

        public enum Map { Arena, Spaceship, Theatre }

        [Tooltip("Map the bots play.")]
        public Map map = Map.Arena;

        [Range(1f, 4f), Tooltip("Game speed (Time.timeScale) while the bots play.")]
        public float speed = 1f;

        [Tooltip("Give up after this many minutes.")]
        public float timeoutMinutes = 60f;

        [Tooltip("Photon room the Party scenario players meet in.")]
        public string partyRoom = "zcbot-local";
    }
}
