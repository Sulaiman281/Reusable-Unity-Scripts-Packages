using UnityEngine;

namespace WitShells.DesignPatterns
{
    public static class WitLogger
    {
        public static bool EnableLogging = true;

        public static void Log(string message)
        {
            if (EnableLogging)
            {
                Debug.Log(message);
            }
        }

        public static void LogWarning(string message)
        {
            if (EnableLogging)
            {
                Debug.LogWarning(message);
            }
        }

        public static void LogError(string message)
        {
            if (EnableLogging)
            {
                Debug.LogError(message);
            }
        }

        public static void LogException(System.Exception exception)
        {
            if (EnableLogging)
            {
                Debug.LogException(exception);
            }
        }

        public static void LogFormat(string format, params object[] args)
        {
            if (EnableLogging)
            {
                Debug.LogFormat(format, args);
            }
        }

        public static void LogWarningFormat(string format, params object[] args)
        {
            if (EnableLogging)
            {
                Debug.LogWarningFormat(format, args);
            }
        }

        public static void LogErrorFormat(string format, params object[] args)
        {
            if (EnableLogging)
            {
                Debug.LogErrorFormat(format, args);
            }
        }
    }
}