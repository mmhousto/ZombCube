using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Com.GCTC.ZombCube.EditorTools.Bots
{
    /// <summary>
    /// Starts a bot run when play mode starts in a Multiplayer Play Mode player tagged for bots, so bot runs
    /// are launched like the FourPlayers scenario: pick a "Bots - ..." scenario (Assets/Settings/PlayMode)
    /// and press Play. Tags (Project Settings > Multiplayer > Player Tags):
    ///   BotSolo, BotCoop2, BotCoop4: the main Editor plays Solo / Co-op with 2 or 4 bots.
    ///   BotParty2, BotParty4: every tagged player joins one Photon room and the game starts when all are in.
    /// Map, speed and time limit come from Assets/Editor/Bots/BotRunSettings.asset.
    /// </summary>
    [InitializeOnLoad]
    public static class BotScenarioLauncher
    {
        static BotScenarioLauncher()
        {
            EditorApplication.playModeStateChanged += change =>
            {
                if (change != PlayModeStateChange.EnteredPlayMode) return;
                string arguments = ArgumentsForTags(CurrentPlayerTags());
                if (arguments != null) BotRun.Start(arguments);
            };
        }

        public static string ArgumentsForTags(IEnumerable<string> tags)
        {
            foreach (string tag in tags)
            {
                string mode;
                int players;
                switch (tag)
                {
                    case "BotSolo": mode = "solo"; players = 1; break;
                    case "BotCoop2": mode = "coop"; players = 2; break;
                    case "BotCoop4": mode = "coop"; players = 4; break;
                    case "BotParty2": mode = "party"; players = 2; break;
                    case "BotParty3": mode = "party"; players = 3; break;
                    case "BotParty4": mode = "party"; players = 4; break;
                    default: continue;
                }

                BotRunSettings settings = AssetDatabase.LoadAssetAtPath<BotRunSettings>(BotRunSettings.AssetPath);
                string map = settings != null ? settings.map.ToString().ToLowerInvariant() : "arena";
                float speed = settings != null ? settings.speed : 1f;
                float timeout = settings != null ? settings.timeoutMinutes : 60f;
                string room = settings != null && !string.IsNullOrWhiteSpace(settings.partyRoom) ? settings.partyRoom.Replace(' ', '-') : "zcbot-local";
                return string.Format(CultureInfo.InvariantCulture, "{0} -zcbotMap {1} -zcbotPlayers {2} -zcbotSpeed {3} -zcbotTimeout {4} -zcbotRoom {5}",
                    mode, map, players, speed, timeout, room);
            }
            return null;
        }

        /// <summary>
        /// This player's Multiplayer Play Mode tags (CurrentPlayer.ReadOnlyTags). Found by reflection so the
        /// project still compiles if the package's namespace changes or it's removed.
        /// </summary>
        public static List<string> CurrentPlayerTags()
        {
            var tags = new List<string>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types; }
                foreach (Type type in types)
                {
                    if (type == null || type.Name != "CurrentPlayer" || type.Namespace == null || !type.Namespace.StartsWith("Unity.Multiplayer")) continue;
                    object result = null;
                    MethodInfo method = type.GetMethod("ReadOnlyTags", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                    if (method != null) result = method.Invoke(null, null);
                    else result = type.GetProperty("Tags", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                    if (result is IEnumerable list)
                        foreach (object tag in list)
                            if (tag != null) tags.Add(tag.ToString());
                    return tags;
                }
            }
            return tags;
        }
    }

    /// <summary>ZombCube > Bots > Status: what each bot is doing during a run.</summary>
    public class BotRunStatusWindow : EditorWindow
    {
        [MenuItem("ZombCube/Bots/Status")]
        private static void Open() => GetWindow<BotRunStatusWindow>("Bot Run");

        [MenuItem("ZombCube/Bots/Run Settings")]
        private static void SelectSettings()
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<BotRunSettings>(BotRunSettings.AssetPath);
        }

        private void OnInspectorUpdate() => Repaint();

        private void OnGUI()
        {
            if (BotRun.Current != null) EditorGUILayout.HelpBox(BotRun.Current.Status(), MessageType.None);
            else if (BotRun.ResultJson != null) EditorGUILayout.HelpBox((BotRun.Passed ? "PASSED\n" : "FAILED\n") + BotRun.ResultJson, BotRun.Passed ? MessageType.Info : MessageType.Error);
            else EditorGUILayout.HelpBox("No bot run. Pick a \"Bots - ...\" scenario in the Play Mode scenario menu and press Play.", MessageType.Info);
        }
    }
}
