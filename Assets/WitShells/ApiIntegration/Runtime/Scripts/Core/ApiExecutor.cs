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
        Boolean,
        Audio,
        Image,
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
        public string mediaPath;

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
                    BodyFieldType.Audio => mediaPath,
                    BodyFieldType.Image => mediaPath,
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
                    case BodyFieldType.Audio:
                        mediaPath = value as string;
                        break;
                    case BodyFieldType.Image:
                        mediaPath = value as string;
                        break;
                }
            }
        }
    }

    [Serializable]
    public class ApiSuccessResponse
    {
        public UnityEvent<string> onJsonData;
        public UnityEvent<byte[]> onBytesData;
        public UnityEvent<string> onTextData;

        public string accessTokenKey;
        public string refreshTokenKey;

        public void Invoke(object response, ResponseType responseType)
        {
            switch (responseType)
            {
                case ResponseType.Json:
                    onJsonData?.Invoke(response as string);
                    break;
                case ResponseType.Bytes:
                    onBytesData?.Invoke(response as byte[]);
                    break;
                case ResponseType.Text:
                    onTextData?.Invoke(response as string);
                    break;
                default:
                    Debug.LogWarning($"Unhandled response type: {responseType}");
                    break;
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

        // public UnityEvent<object> onSuccess;
        public ApiSuccessResponse onSuccess = new ApiSuccessResponse();
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
                    switch (h.type)
                    {
                        case BodyFieldType.String:
                            form.AddField(h.key, h.stringValue);
                            break;
                        case BodyFieldType.Integer:
                            form.AddField(h.key, h.intValue);
                            break;
                        case BodyFieldType.Float:
                            form.AddField(h.key, h.floatValue.ToString());
                            break;
                        case BodyFieldType.Boolean:
                            form.AddField(h.key, h.boolValue ? "true" : "false");
                            break;
                        case BodyFieldType.Image:
                        case BodyFieldType.Audio:
                            if (!string.IsNullOrEmpty(h.mediaPath) && System.IO.File.Exists(h.mediaPath))
                            {
                                byte[] fileData = System.IO.File.ReadAllBytes(h.mediaPath);
                                form.AddField(h.key, Convert.ToBase64String(fileData));
                                ApiLogger.Log($"Added media field '{h.key}' with size {fileData.Length} bytes.");
                            }
                            break;
                    }
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
                ApiLogger.Log($"Building request content type: {endpoint.ContentType}");
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
                    case ContentType.Media:
                        ApiLogger.Log($"Building request with media body for endpoint: {endpoint.Path}");
                        req = ApiRequestBuilder.Create(endpoint.ToEndpointWithBody(GetBodyForm()));
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
                ApiManager.Instance.SendRequest(request, req.endpoint, response =>
                {
                    if (req.endpoint.responseType == ResponseType.Authorize)
                    {
                        RestApiConfig.Instance.SetAuthorizationData(req.onSuccess.accessTokenKey, req.onSuccess.refreshTokenKey, response as string);
                        return;
                    }

                    req.onSuccess.Invoke(response, req.endpoint.responseType);
                })
            );
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