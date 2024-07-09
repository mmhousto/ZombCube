using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace AppleAuth.Editor
{
    public static class AppleAuthMacosPostprocessorHelper
    {
        /// <summary>
        /// Use this script to change the bundle identifier of the plugin's library bundle to replace it with a personalized one for your product.
        /// This should avoid CFBundleIdentifier Collision errors when uploading the app to the macOS App Store
        /// </summary>
        /// <remarks>Basically this should replace the plugin's bundle identifier from "com.lupidan.MacOSAppleAuthManager" to "{your.project.application.identifier}.MacOSAppleAuthManager"</remarks>
        /// <param name="target">The current build target, so it's only executed when building for MacOS</param>
        /// <param name="path">The path of the built .app file</param>
        public static void FixManagerBundleIdentifier(BuildTarget target, string path)
        {
            if (target != BuildTarget.StandaloneOSX)
                return;

            AppleAuthMacosPostprocessorHelper.FixManagerBundleIdentifier(target, path);
        }
    }
}
