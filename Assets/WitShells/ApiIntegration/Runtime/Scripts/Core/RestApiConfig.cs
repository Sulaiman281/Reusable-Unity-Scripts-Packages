using System;
using System.Collections.Generic;
using UnityEngine;

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

        [NonSerialized] public string accessToken;

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
    }

    [Serializable]
    public class Header
    {
        public string key;
        public string value;
    }
}