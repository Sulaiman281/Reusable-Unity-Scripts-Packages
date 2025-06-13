using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using System;
using WitShells.DesignPatterns.Core;

namespace WitShells.ApiIntegration
{
    /// <summary>
    /// Observer pattern for API exceptions.
    /// </summary>
    public class ExceptionObserver : ObserverPattern<ApiException>
    {
        // Inherits Subscribe/Unsubscribe/NotifyObservers from ObserverPattern<ApiException>
    }

    /// <summary>
    /// Represents an API exception with endpoint and details.
    /// </summary>
    [Serializable]
    public class ApiException
    {
        public string endpoint;
        public string error;
        public string details;
        public Exception systemException;

        public ApiException(string endpoint, string error, string details = null, Exception systemException = null)
        {
            this.endpoint = endpoint;
            this.error = error;
            this.details = details;
            this.systemException = systemException;
        }
    }

    public abstract class ApiManager : MonoSingleton<ApiManager>
    {
        public ExceptionObserver ExceptionObserver { get; private set; } = new ExceptionObserver();

        public UnityEvent<float> ApiInProgress = new UnityEvent<float>();
        public UnityEvent ApiFinished = new UnityEvent();

        /// <summary>
        /// Sends a UnityWebRequest and handles the response.
        /// The endpoint path is extracted from the request.url.
        /// </summary>
        public IEnumerator SendRequest(UnityWebRequest webRequest, ApiEndpoint endpoint, UnityAction<object> callback)
        {
            string endpointPath = GetEndpointPathFromUrl(webRequest.url);

            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                ApiLogger.LogWarning("No Internet Connection");
                ExceptionObserver.NotifyObservers(new ApiException(endpointPath, "No Internet Connection"));
                yield break;
            }

            ApiLogger.Log($"Sending API Request: {webRequest.url} with method {webRequest.method}");
            ApiInProgress?.Invoke(0f);
            var asyncOp = webRequest.SendWebRequest();
            while (!asyncOp.isDone)
            {
                float progress = webRequest.uploadProgress < 1f ? webRequest.uploadProgress : webRequest.downloadProgress;
                ApiInProgress?.Invoke(progress);
                yield return null;
            }
            ApiInProgress?.Invoke(1f);

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                if (webRequest.responseCode == 401)
                {
                    ApiLogger.LogWarning("Unauthorized");
                    ExceptionObserver.NotifyObservers(new ApiException(endpointPath, "Unauthorized"));
                }
                else
                {
                    ApiLogger.LogError($"Error: {webRequest.error} - Response Code: {webRequest.responseCode} - {webRequest.downloadHandler.text}");
                    HandleException(endpointPath, webRequest.downloadHandler.error ?? webRequest.downloadHandler.text);
                }
            }
            else
            {
                try
                {
                    object responseObj = null;
                    switch (endpoint.responseType)
                    {
                        case ResponseType.Authorize:
                        case ResponseType.Json:
                            // Return raw JSON string so user can deserialize as needed
                            responseObj = webRequest.downloadHandler.text;
                            ApiLogger.Log($"Response from {endpointPath}: {responseObj}");
                            break;
                        case ResponseType.Text:
                            responseObj = webRequest.downloadHandler.text;
                            ApiLogger.Log($"Response from {endpointPath}: {responseObj}");
                            break;
                        case ResponseType.Bytes:
                            responseObj = webRequest.downloadHandler.data;
                            ApiLogger.Log($"Bytes downloaded from {endpointPath}");
                            break;
                    }
                    callback?.Invoke(responseObj);
                }
                catch (Exception ex)
                {
                    ExceptionObserver.NotifyObservers(new ApiException(endpointPath, "Deserialization Error", ex.Message, ex));
                }
            }

            ApiFinished?.Invoke();
        }

        /// <summary>
        /// Extracts the endpoint path from the full URL.
        /// </summary>
        private string GetEndpointPathFromUrl(string url)
        {
            // Remove base URL if present
            string baseUrl = RestApiConfig.Instance.BaseUrl;
            if (!string.IsNullOrEmpty(baseUrl) && url.StartsWith(baseUrl))
            {
                return url.Substring(baseUrl.Length);
            }
            return url;
        }

        /// <summary>
        /// Handles API exceptions and logs or processes them as needed.
        /// This method is virtual so it can be overridden in derived classes.
        /// </summary>
        public virtual void HandleException(string endpoint, string responseText)
        {
            ExceptionObserver.NotifyObservers(new ApiException(endpoint, "API Error", responseText));
        }
    }
}