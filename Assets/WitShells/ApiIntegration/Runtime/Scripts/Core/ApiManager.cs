using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using Newtonsoft.Json;
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

        /// <summary>
        /// Sends a UnityWebRequest and handles the response.
        /// The endpoint path is extracted from the request.url.
        /// </summary>
        public IEnumerator SendRequest(UnityWebRequest webRequest, UnityAction<Response> callback)
        {
            string endpointPath = GetEndpointPathFromUrl(webRequest.url);

            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                ApiLogger.LogWarning("No Internet Connection");
                ExceptionObserver.NotifyObservers(new ApiException(endpointPath, "No Internet Connection"));
                yield break;
            }

            ApiLogger.Log($"Sending API Request: {webRequest.url} with method {webRequest.method}");
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                if (webRequest.responseCode == 401)
                {
                    ApiLogger.LogWarning("Unauthorized");
                    ExceptionObserver.NotifyObservers(new ApiException(endpointPath, "Unauthorized"));
                }
                else
                {
                    HandleException(endpointPath, webRequest.downloadHandler.text);
                }
            }
            else
            {
                ApiLogger.Log($"API Response: {webRequest.downloadHandler.text}");
                try
                {
                    Response responseObj = string.IsNullOrEmpty(webRequest.downloadHandler.text)
                        ? new Response()
                        : JsonConvert.DeserializeObject<Response>(webRequest.downloadHandler.text);

                    callback?.Invoke(responseObj);
                }
                catch (Exception ex)
                {
                    ExceptionObserver.NotifyObservers(new ApiException(endpointPath, "Deserialization Error", ex.Message, ex));
                }
            }
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
            try
            {
                var exception = JsonConvert.DeserializeObject<ExceptionResponse>(responseText);
                ApiLogger.LogWarning($"API Exception at {endpoint}: {exception?.error} - {exception?.details}");
                ExceptionObserver.NotifyObservers(new ApiException(endpoint, exception?.error, exception?.details));
            }
            catch
            {
                ExceptionObserver.NotifyObservers(new ApiException(endpoint, "unknown exception", responseText));
            }
        }
    }

    [Serializable]
    public class Response
    {
        public string message;
        public int code;
        // Add more fields as needed
    }

    [Serializable]
    public class ExceptionResponse
    {
        public string error;
        public string details;
        // Add more fields as needed
    }
}