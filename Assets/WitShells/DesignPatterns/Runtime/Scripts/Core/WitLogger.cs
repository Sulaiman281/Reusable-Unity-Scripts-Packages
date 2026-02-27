using UnityEngine;

namespace WitShells.DesignPatterns
{
    /// <summary>
    /// A thin wrapper around Unity's <see cref="Debug"/> logging API that supports a global
    /// enable/disable toggle via <see cref="EnableLogging"/>.
    /// Use this instead of <c>Debug.Log</c> directly so that all logging can be silenced
    /// in production builds from a single flag.
    /// </summary>
    public static class WitLogger
    {
        /// <summary>
        /// When <c>false</c>, all log calls become no-ops. Set to <c>false</c> in release
        /// builds to eliminate logging overhead.
        /// </summary>
        public static bool EnableLogging = true;

        /// <summary>Logs a plain informational message to the Unity console.</summary>
        /// <param name="message">The message to log.</param>
        public static void Log(string message)
        {
            if (EnableLogging)
            {
                Debug.Log(message);
            }
        }

        /// <summary>Logs a warning message (yellow) to the Unity console.</summary>
        /// <param name="message">The warning message to log.</param>
        public static void LogWarning(string message)
        {
            if (EnableLogging)
            {
                Debug.LogWarning(message);
            }
        }

        /// <summary>Logs an error message (red) to the Unity console.</summary>
        /// <param name="message">The error message to log.</param>
        public static void LogError(string message)
        {
            if (EnableLogging)
            {
                Debug.LogError(message);
            }
        }

        /// <summary>
        /// Logs an exception to the Unity console, printing the stack trace.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        public static void LogException(System.Exception exception)
        {
            if (EnableLogging)
            {
                Debug.LogException(exception);
            }
        }

        /// <summary>Logs a formatted informational message using <see cref="string.Format"/> syntax.</summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">Arguments to format into the string.</param>
        public static void LogFormat(string format, params object[] args)
        {
            if (EnableLogging)
            {
                Debug.LogFormat(format, args);
            }
        }

        /// <summary>Logs a formatted warning message using <see cref="string.Format"/> syntax.</summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">Arguments to format into the string.</param>
        public static void LogWarningFormat(string format, params object[] args)
        {
            if (EnableLogging)
            {
                Debug.LogWarningFormat(format, args);
            }
        }

        /// <summary>Logs a formatted error message using <see cref="string.Format"/> syntax.</summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">Arguments to format into the string.</param>
        public static void LogErrorFormat(string format, params object[] args)
        {
            if (EnableLogging)
            {
                Debug.LogErrorFormat(format, args);
            }
        }
    }
}