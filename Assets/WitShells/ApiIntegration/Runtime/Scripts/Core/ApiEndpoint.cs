using System;

namespace WitShells.ApiIntegration
{
    /// <summary>
    /// Supported HTTP methods.
    /// </summary>
    public enum HttpMethod
    {
        GET,
        POST,
        PUT,
        DELETE,
        PATCH
    }

    public enum ContentType
    {
        JSON,
        WWWForm,
        Query
    }

    /// <summary>
    /// Base class for all API endpoints.
    /// </summary>
    [Serializable]
    public class ApiEndpoint
    {
        public HttpMethod Method;
        public ContentType ContentType = ContentType.JSON;
        public string Path;
        public bool IsSecure;

        public ApiEndpoint(HttpMethod method, string path, bool isSecure = false)
        {
            Method = method;
            Path = path;
            IsSecure = isSecure;
        }

        // Parameterless constructor for serialization
        public ApiEndpoint() { }
    }

    /// <summary>
    /// Represents an API endpoint with a request body of type T (e.g., POST, PUT).
    /// </summary>
    [Serializable]
    public class ApiEndpointWithBody<T> : ApiEndpoint
    {
        public T Body;

        public ApiEndpointWithBody(HttpMethod method, string path, T body, bool isSecure = false)
            : base(method, path, isSecure)
        {
            Body = body;
        }

        // Parameterless constructor for serialization
        public ApiEndpointWithBody() { }
    }

    /// <summary>
    /// Factory for creating endpoints, with or without request body.
    /// </summary>
    public static class EndpointFactory
    {
        public static ApiEndpoint Create(HttpMethod method, string path, bool isSecure = false)
        {
            return new ApiEndpoint(method, path, isSecure);
        }

        public static ApiEndpointWithBody<T> CreateWithBody<T>(HttpMethod method, string path, T body, bool isSecure = false)
        {
            return new ApiEndpointWithBody<T>(method, path, body, isSecure);
        }

        // copy apiendpoint to ApiEndpointWithBody<T>
        public static ApiEndpointWithBody<T> ToEndpointWithBody<T>(this ApiEndpoint endpoint, T body)
        {
            return new ApiEndpointWithBody<T>(endpoint.Method, endpoint.Path, body, endpoint.IsSecure);
        }
    }
}