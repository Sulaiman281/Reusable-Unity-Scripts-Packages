using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace WitShells.ApiIntegration
{
    public enum BodyFieldType
    {
        String,
        Integer,
        Float,
        Boolean
    }

    [Serializable]
    public class BodyField
    {
        public string key;
        public BodyFieldType type = BodyFieldType.String;
        public string stringValue;
        public int intValue;
        public float floatValue;
        public bool boolValue;

        // This property is for serialization to dictionary
        public object Value
        {
            get
            {
                return type switch
                {
                    BodyFieldType.String => stringValue,
                    BodyFieldType.Integer => intValue,
                    BodyFieldType.Float => floatValue,
                    BodyFieldType.Boolean => boolValue,
                    _ => stringValue,
                };
            }
            set
            {
                switch (type)
                {
                    case BodyFieldType.String:
                        stringValue = value as string;
                        break;
                    case BodyFieldType.Integer:
                        intValue = Convert.ToInt32(value);
                        break;
                    case BodyFieldType.Float:
                        floatValue = Convert.ToSingle(value);
                        break;
                    case BodyFieldType.Boolean:
                        boolValue = Convert.ToBoolean(value);
                        break;
                }
            }
        }
    }

    [Serializable]
    public class ApiEndpointRequest
    {
        public string endpointName;
        public ApiEndpoint endpoint;
        public bool includeDefaultHeaders = true;
        public List<Header> customHeaders = new List<Header>();

        [Header("Body Settings")]
        public bool includeBody = false;
        [Tooltip("Key-value pairs for request body. Used if includeBody is true.")]
        public List<BodyField> bodyFields = new();

        public UnityEvent<Response> onSuccess;
        public UnityEvent<ApiException> onFail;

        /// <summary>
        /// Converts bodyFields to a Dictionary for use in ApiRequestBuilder.
        /// </summary>
        public Dictionary<string, object> GetBodyDictionary()
        {
            var dict = new Dictionary<string, object>();
            foreach (var h in bodyFields)
                if (!string.IsNullOrEmpty(h.key))
                    dict[h.key] = h.Value;
            return dict;
        }

        public WWWForm GetBodyForm()
        {
            var form = new WWWForm();
            foreach (var h in bodyFields)
            {
                if (!string.IsNullOrEmpty(h.key))
                {
                    if (h.Value is string strValue)
                    {
                        form.AddField(h.key, strValue);
                    }
                    else if (h.Value is int intValue)
                    {
                        form.AddField(h.key, intValue);
                    }
                    else if (h.Value is float floatValue)
                    {
                        form.AddField(h.key, floatValue.ToString());
                    }
                    else if (h.Value is bool boolValue)
                    {
                        form.AddField(h.key, boolValue ? "true" : "false");
                    }
                    // Add more types as needed
                }
            }
            return form;
        }

        public string GetQueryString()
        {
            var query = new List<string>();
            foreach (var h in bodyFields)
            {
                if (!string.IsNullOrEmpty(h.key) && h.Value != null)
                {
                    query.Add($"{UnityWebRequest.EscapeURL(h.key)}={UnityWebRequest.EscapeURL(h.Value.ToString())}");
                }
            }
            return query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
        }

        public UnityWebRequest Build()
        {
            ApiRequestBuilder req;

            if (includeBody)
            {
                switch (endpoint.ContentType)
                {
                    case ContentType.JSON:
                        ApiEndpointWithBody<Dictionary<string, object>> endpointWithBody = endpoint.ToEndpointWithBody(GetBodyDictionary());
                        req = ApiRequestBuilder.Create(endpointWithBody);
                        break;
                    case ContentType.WWWForm:
                        ApiEndpointWithBody<WWWForm> multipartEndpoint = endpoint.ToEndpointWithBody(GetBodyForm());
                        req = ApiRequestBuilder.Create(multipartEndpoint);
                        break;
                    case ContentType.Query:
                        req = ApiRequestBuilder.Create(endpoint.ToEndpointWithBody(GetQueryString()));
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported content type for body.");
                }
            }
            else
            {
                req = ApiRequestBuilder.Create(endpoint);
            }

            if (includeDefaultHeaders)
            {
                req.AddDefaultHeaders();
            }

            if (customHeaders != null && customHeaders.Count > 0)
            {
                var dict = new Dictionary<string, string>();
                foreach (var h in customHeaders)
                    dict[h.key] = h.value;
                req.AddCustomHeaders(dict);
            }

            return req.Build();
        }
    }

    /// <summary>
    /// Executes API requests and manages callbacks for success and failure.
    /// </summary>
    public class ApiExecutor : MonoBehaviour
    {
        [Header("Endpoints to Register")]
        public List<ApiEndpointRequest> endpoints = new List<ApiEndpointRequest>();

        private void OnEnable()
        {
            ApiManager.Instance.ExceptionObserver.Subscribe(OnApiException);
        }

        private void OnDisable()
        {
            ApiManager.Instance.ExceptionObserver.Unsubscribe(OnApiException);
        }

        /// <summary>
        /// Call this to execute an endpoint by index in the endpoints list.
        /// </summary>
        public void Execute(int endpointIndex)
        {
            if (endpointIndex < 0 || endpointIndex >= endpoints.Count)
            {
                Debug.LogError($"Invalid endpoint index: {endpointIndex}. Must be between 0 and {endpoints.Count - 1}.");
                return;
            }

            var req = endpoints[endpointIndex];
            var request = req.Build();

            if (request == null)
            {
                Debug.LogError("Failed to build request. Check endpoint configuration.");
                return;
            }

            StartCoroutine(
                        ApiManager.Instance.SendRequest(request, response =>
            {
                req.onSuccess?.Invoke(response);
            }));
        }

        /// <summary>
        /// Handles API exceptions and invokes the correct fail event.
        /// </summary>
        private void OnApiException(ApiException exception)
        {
            foreach (var req in endpoints)
            {
                // Match endpoint by path (can be improved for more complex matching)
                if (string.Equals(req.endpoint.Path, exception.endpoint, StringComparison.OrdinalIgnoreCase))
                {
                    req.onFail?.Invoke(exception);
                    return; // Exit after handling the first matching endpoint
                }
            }
        }
    }
}