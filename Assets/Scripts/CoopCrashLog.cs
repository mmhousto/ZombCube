using UnityEngine;

namespace Com.GCTC.ZombCube
{
    /// <summary>
    /// TEMPORARY breadcrumbs for the co-op Restart / Main Menu editor crash. Every step is written to
    /// Editor.log immediately, so the last "[CoopCrash]" line before a crash names the step that died.
    /// Remove once the crash is fixed.
    /// </summary>
    public static class CoopCrashLog
    {
        public static void Step(string message)
        {
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "[CoopCrash] f{0} t{1} {2}", Time.frameCount, Time.timeScale, message);
        }
    }
}
