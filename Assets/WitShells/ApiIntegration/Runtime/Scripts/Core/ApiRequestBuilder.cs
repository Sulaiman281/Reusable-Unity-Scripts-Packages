using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace WitShells.ApiIntegration
{
    /// <summary>
    /// Builder pattern for constructing UnityWebRequest with fluent API.
    /// </summary>
    public class ApiRequestBuilder
    {
        private UnityWebRequest _request;

        private ApiRequestBuilder(UnityWebRequest request)
        {
            _request = request;
        }

        /// <summary>
        /// Creates a builder for endpoints without a request body.
        /// </summary>
        public static ApiRequestBuilder Create(ApiEndpoint endpoint)
        {
            var url = CombineUrl(RestApiConfig.Instance.BaseUrl, endpoint.Path);
            var req = new UnityWebRequest(url, endpoint.Method.ToString());
            req.uploadHandler = new UploadHandlerRaw(new byte[] { 0 });
            req.downloadHandler = new DownloadHandlerBuffer();
            req.useHttpContinue = true;
            req.SetRequestHeader("Content-Type", GetContentTypeString(endpoint.ContentType));

            if (endpoint.IsSecure)
            {
                var config = RestApiConfig.Instance;
                if (!string.IsNullOrEmpty(config.accessToken))
                {
                    req.SetRequestHeader("Authorization", "Bearer " + config.accessToken);
                }
            }
            return new ApiRequestBuilder(req);
        }

        /// <summary>
        /// Creates a builder for endpoints with a request body.
        /// </summary>
        public static ApiRequestBuilder Create<T>(ApiEndpointWithBody<T> endpoint)
        {
            ApiLogger.Log($"[ApiRequestBuilder] ContentType: {endpoint.ContentType}, Method: {endpoint.Method}, Path: {endpoint.Path}");
            var url = CombineUrl(RestApiConfig.Instance.BaseUrl, endpoint.Path);
            UnityWebRequest req;
            if (endpoint.Body is string strData)
            {
                req = new UnityWebRequest(url + strData, endpoint.Method.ToString())
                {
                    uploadHandler = new UploadHandlerRaw(new byte[] { 0 }),
                    downloadHandler = new DownloadHandlerBuffer()
                };
                req.SetRequestHeader("Content-Type", GetContentTypeString(endpoint.ContentType));
            }
            else if (endpoint.Body is WWWForm form)
            {
                req = new UnityWebRequest(url, endpoint.Method.ToString())
                {
                    uploadHandler = new UploadHandlerRaw(form.data),
                    downloadHandler = new DownloadHandlerBuffer()
                };
                req.SetRequestHeader("Content-Type", GetContentTypeString(endpoint.ContentType));
            }
            else
            {
                req = new UnityWebRequest(url, endpoint.Method.ToString())
                {
                    uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(endpoint.Body))),
                    downloadHandler = new DownloadHandlerBuffer()
                };
                req.SetRequestHeader("Content-Type", GetContentTypeString(endpoint.ContentType));
            }

            if (endpoint.IsSecure)
            {
                var config = RestApiConfig.Instance;
                if (!string.IsNullOrEmpty(config.accessToken))
                {
                    req.SetRequestHeader("Authorization", "Bearer " + config.accessToken);
                }
            }
            return new ApiRequestBuilder(req);
        }

        /// <summary>
        /// Adds default headers from RestApiConfig.
        /// </summary>  
        public ApiRequestBuilder AddDefaultHeaders()
        {
            var headers = RestApiConfig.Instance.GetDefaultHeaders();
            foreach (var header in headers)
            {
                _request.SetRequestHeader(header.Key, header.Value);
            }
            return this;
        }

        /// <summary>
        /// Adds custom headers.
        /// </summary>
        public ApiRequestBuilder AddCustomHeaders(Dictionary<string, string> customHeaders)
        {
            if (customHeaders != null)
            {
                foreach (var header in customHeaders)
                {
                    _request.SetRequestHeader(header.Key, header.Value);
                }
            }
            return this;
        }

        /// <summary>
        /// Returns the built UnityWebRequest for further customization or sending.
        /// </summary>
        public UnityWebRequest Build()
        {
            return _request;
        }

        /// <summary>
        /// Converts ContentType enum to the correct string for HTTP headers.
        /// </summary>
        public static string GetContentTypeString(ContentType contentType)
        {
            var content = contentType switch
            {
                ContentType.Query or ContentType.JSON => "application/json",
                ContentType.WWWForm => "application/x-www-form-urlencoded",
                ContentType.Media => "multipart/form-data",
                _ => throw new ArgumentOutOfRangeException(nameof(contentType), contentType, "Unsupported ContentType"),
            };
            ApiLogger.Log($"[ApiRequestBuilder] Getting Content-Type for: {contentType} - {content}");
            return content;
        }

        /// <summary>
        /// Combines base URL and endpoint path, ensuring no duplicate slashes.
        /// </summary>
        private static string CombineUrl(string baseUrl, string path)
        {
            if (string.IsNullOrEmpty(baseUrl)) return path ?? "";
            if (string.IsNullOrEmpty(path)) return baseUrl;

            string combined = baseUrl + path;

            // Warn if double or triple slash (excluding protocol part)
            var protocolSplit = combined.Split(new[] { "//" }, 3, StringSplitOptions.None);
            string afterProtocol = protocolSplit.Length > 1 ? protocolSplit[1] + (protocolSplit.Length > 2 ? "//" + protocolSplit[2] : "") : combined;
            if (afterProtocol.Contains("//"))
            {
                Debug.LogWarning($"[ApiRequestBuilder] Combined URL contains double slashes: '{combined}'. Please check your BaseUrl and Endpoint Path.");
            }

            return combined;
        }
    }
}