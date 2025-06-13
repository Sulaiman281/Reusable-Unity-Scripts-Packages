using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace WitShells.ApiIntegration
{
    public enum ApiEnvironment
    {
        Local,
        Testing,
        Production
    }

    /// <summary>
    /// ScriptableObject to hold API configuration and runtime data.
    /// </summary>
    [CreateAssetMenu(fileName = "RestApiConfig", menuName = "WitShells/API/RestApiConfig")]
    public class RestApiConfig : ScriptableObject
    {
        public static RestApiConfig Instance
        {
            get
            {
                return Resources.Load<RestApiConfig>("RestApiConfig");
            }
        }

        [Header("Environment Settings")]
        public ApiEnvironment environment = ApiEnvironment.Production;

        [Header("Base API URLs")]
        public string localUrl;
        public string localTestUrl;
        public string productionUrl;

        [Header("Default Headers (Optional)")]
        public List<Header> defaultHeaders = new List<Header>();

        [Header("Logging")]
        public bool showLog = true;

        [Header("Authorization Caching")]
        public bool cacheAuthorizationToken = false;

        public string accessToken;
        public string refreshToken;

        /// <summary>
        /// Returns the base URL according to the selected environment.
        /// </summary>
        public string BaseUrl
        {
            get
            {
                return environment switch
                {
                    ApiEnvironment.Local => localUrl,
                    ApiEnvironment.Testing => localTestUrl,
                    ApiEnvironment.Production => productionUrl,
                    _ => throw new ArgumentOutOfRangeException(nameof(environment), environment, null)
                };
            }

        }

        /// <summary>
        /// Helper to get all default headers as a dictionary.
        /// </summary>
        public Dictionary<string, string> GetDefaultHeaders()
        {
            var dict = new Dictionary<string, string>();
            foreach (var header in defaultHeaders)
            {
                if (!string.IsNullOrEmpty(header.key))
                    dict[header.key] = header.value;
            }
            return dict;
        }

        public void OnEnable()
        {
            if (cacheAuthorizationToken)
            {
                accessToken = PlayerPrefs.GetString("WitShells_AccessToken", null);
                refreshToken = PlayerPrefs.GetString("WitShells_RefreshToken", null);
            }
        }

        public void SetAuthorizationData(string accessTokenKey, string refreshTokenKey, string data)
        {
            accessToken = null;
            refreshToken = null;

            if (string.IsNullOrEmpty(data))
                return;

            try
            {
                var jObj = JObject.Parse(data);

                // Helper function to recursively find a key
                string FindValue(JToken token, string key)
                {
                    if (token.Type == JTokenType.Object)
                    {
                        foreach (var prop in ((JObject)token).Properties())
                        {
                            if (string.Equals(prop.Name, key, StringComparison.OrdinalIgnoreCase))
                                return prop.Value.ToString();

                            var found = FindValue(prop.Value, key);
                            if (found != null)
                                return found;
                        }
                    }
                    else if (token.Type == JTokenType.Array)
                    {
                        foreach (var item in token)
                        {
                            var found = FindValue(item, key);
                            if (found != null)
                                return found;
                        }
                    }
                    return null;
                }

                if (!string.IsNullOrEmpty(accessTokenKey))
                    accessToken = FindValue(jObj, accessTokenKey);

                if (!string.IsNullOrEmpty(refreshTokenKey))
                    refreshToken = FindValue(jObj, refreshTokenKey);

                // Save tokens if caching is enabled
                if (cacheAuthorizationToken)
                {
                    if (!string.IsNullOrEmpty(accessToken))
                        PlayerPrefs.SetString("WitShells_AccessToken", accessToken);
                    if (!string.IsNullOrEmpty(refreshToken))
                        PlayerPrefs.SetString("WitShells_RefreshToken", refreshToken);
                    PlayerPrefs.Save();
                }
            }
            catch
            {
                accessToken = null;
                refreshToken = null;
            }
        }
    }

    [Serializable]
    public class Header
    {
        public string key;
        public string value;
    }
}