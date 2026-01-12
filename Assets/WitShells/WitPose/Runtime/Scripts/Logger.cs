using UnityEngine;

namespace WitShells.WitPose
{
    public static class Logger
    {
        public static bool IsLoggingEnabled { get; set; } = true;

        /// <summary>
        /// Log an info message if logging is enabled
        /// </summary>
        public static void Log(string message)
        {
            if (IsLoggingEnabled)
            {
                Debug.Log($"[WitPose] {message}");
            }
        }

        /// <summary>
        /// Log an info message with formatted string if logging is enabled
        /// </summary>
        public static void Log(string format, params object[] args)
        {
            if (IsLoggingEnabled)
            {
                Debug.Log($"[WitPose] {string.Format(format, args)}");
            }
        }

        /// <summary>
        /// Log a warning message if logging is enabled
        /// </summary>
        public static void LogWarning(string message)
        {
            if (IsLoggingEnabled)
            {
                Debug.LogWarning($"[WitPose] {message}");
            }
        }

        /// <summary>
        /// Log a warning message with formatted string if logging is enabled
        /// </summary>
        public static void LogWarning(string format, params object[] args)
        {
            if (IsLoggingEnabled)
            {
                Debug.LogWarning($"[WitPose] {string.Format(format, args)}");
            }
        }

        /// <summary>
        /// Log an error message (always shown regardless of logging setting)
        /// </summary>
        public static void LogError(string message)
        {
            Debug.LogError($"[WitPose] {message}");
        }

        /// <summary>
        /// Log an error message with formatted string (always shown regardless of logging setting)
        /// </summary>
        public static void LogError(string format, params object[] args)
        {
            Debug.LogError($"[WitPose] {string.Format(format, args)}");
        }
    }
}