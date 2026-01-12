#if UNITY_EDITOR
using UnityEditor;

namespace WitShells.WitPose.Editor
{
    /// <summary>
    /// Editor utility to test the WitPose logging system
    /// </summary>
    public static class WitPoseLogging
    {
        [MenuItem("Tools/WitPose/Logging System")] 
        public static void ToggleLoggingSystem()
        {
            Logger.IsLoggingEnabled = !Logger.IsLoggingEnabled;
            UnityEngine.Debug.Log($"WitPose Logging System: {(Logger.IsLoggingEnabled ? "ENABLED" : "DISABLED")}");
        }
    }
}
#endif