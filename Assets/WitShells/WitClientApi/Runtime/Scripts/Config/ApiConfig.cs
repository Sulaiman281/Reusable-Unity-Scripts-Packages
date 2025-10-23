using UnityEngine;

namespace WitShells.WitClientApi
{
    [CreateAssetMenu(menuName = "WitShells/ApiConfig", fileName = "ApiConfig")]
    public class ApiConfig : ScriptableObject
    {
        public enum EnvironmentOption { Local, Production }

        public EnvironmentOption Environment = EnvironmentOption.Local;

        [Header("Local")]
        public string LocalBaseUrl = "localhost";
        public int LocalPort = 5000;
        public bool UseHttpsForLocal = false;

        [Header("Production")]
        public string ProductionBaseUrl = "https://api.witshells.com";
        public int ProductionPort = 443;

        [Header("Paths")]
        public string BasePath = "/";

    [Header("Auth Endpoints (relative paths)")]
    [Tooltip("Relative path to sign-in endpoint, e.g. /api/Auth/login")]
    public string SignInPath = "/api/Auth/login";
    [Tooltip("Relative path to sign-out endpoint, e.g. /api/Auth/logout")]
    public string SignOutPath = "/api/Auth/logout";
    [Tooltip("Relative path to refresh-token endpoint, e.g. /api/Auth/refresh-token")]
    public string RefreshTokenPath = "/api/Auth/refresh-token";

        /// <summary>
        /// Build the final base URL based on the selected environment and settings.
        /// </summary>
        public string GetBaseUrl()
        {
            string scheme = "https";
            string host = "";
            int port = -1;

            if (Environment == EnvironmentOption.Local)
            {
                scheme = UseHttpsForLocal ? "https" : "http";
                host = LocalBaseUrl?.Trim() ?? "localhost";
                port = LocalPort;
            }
            else
            {
                // Production
                if (!string.IsNullOrWhiteSpace(ProductionBaseUrl))
                {
                    var trimmed = ProductionBaseUrl.Trim();
                    if (trimmed.StartsWith("http://") || trimmed.StartsWith("https://"))
                    {
                        var uri = new System.Uri(trimmed);
                        scheme = uri.Scheme;
                        host = uri.Host + (uri.IsDefaultPort ? "" : ":" + uri.Port);
                    }
                    else
                    {
                        host = trimmed;
                    }
                }
                port = ProductionPort;
            }

            // Normalize host (remove any trailing slash)
            host = host.TrimEnd('/');

            // Include port if provided and non-default
            string portPart = "";
            if (port > 0)
            {
                bool defaultPort = (scheme == "https" && port == 443) || (scheme == "http" && port == 80);
                if (!defaultPort && !host.Contains(":")) portPart = ":" + port.ToString();
            }

            string path = BasePath ?? "/";
            if (!path.StartsWith("/")) path = "/" + path;
            if (path.EndsWith("/")) path = path.TrimEnd('/');

            var baseUrl = $"{scheme}://{host}{portPart}{path}";
            // Trim trailing slash unless path is root "/"
            if (baseUrl.EndsWith("/") && path != "/") baseUrl = baseUrl.TrimEnd('/');
            return baseUrl;
        }

        public static ApiConfig LoadFromResources(string assetName = "ApiEnvironmentConfig")
        {
            return Resources.Load<ApiConfig>(assetName);
        }
    }
}