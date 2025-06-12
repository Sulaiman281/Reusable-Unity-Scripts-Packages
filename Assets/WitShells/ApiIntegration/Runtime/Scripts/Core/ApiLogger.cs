using UnityEngine;

namespace WitShells.ApiIntegration
{
    public static class ApiLogger
    {
        public static void Log(string message)
        {
            if (RestApiConfig.Instance.showLog)
                Debug.Log(message);
        }

        public static void LogWarning(string message)
        {
            if (RestApiConfig.Instance.showLog)
                Debug.LogWarning(message);
        }

        public static void LogError(string message)
        {
            if (RestApiConfig.Instance.showLog)
                Debug.LogError(message);
        }
    }
}