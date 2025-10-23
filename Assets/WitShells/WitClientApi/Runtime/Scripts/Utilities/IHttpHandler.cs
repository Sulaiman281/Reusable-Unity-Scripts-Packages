using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WitShells.WitClientApi
{
    public class HttpResponse
    {
        public int StatusCode;
        public byte[] Data = Array.Empty<byte>();
        public string Error;
    }

    public class HttpRequest
    {
        public string Method { get; set; } = "GET";
        public string BaseUrl { get; set; } = "";
        public string Path { get; set; } = "";
        public string ContentType { get; set; } = "application/json";
        public byte[] Body { get; set; } = null;
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> QueryParams { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> FormData { get; set; } = new Dictionary<string, string>();
        public object JsonBody { get; set; } = null;
        public string AuthToken { get; set; } = null;

        public string GetFullUrl()
        {
            var url = Path.StartsWith("/") ? BaseUrl.TrimEnd('/') + Path : BaseUrl.TrimEnd('/') + "/" + Path;

            if (QueryParams != null && QueryParams.Count > 0)
            {
                var queryString = string.Join("&", QueryParams.Select(kv => $"{System.Uri.EscapeDataString(kv.Key)}={System.Uri.EscapeDataString(kv.Value ?? "")}"));
                url += (url.Contains("?") ? "&" : "?") + queryString;
            }

            return url;
        }

        public byte[] GetRequestBody()
        {
            if (Body != null) return Body;

            if (JsonBody != null)
            {
                var json = Json.Serialize(JsonBody);
                return Encoding.UTF8.GetBytes(json);
            }

            if (FormData != null && FormData.Count > 0)
            {
                ContentType = "application/x-www-form-urlencoded";
                var formString = string.Join("&", FormData.Select(kv => $"{System.Uri.EscapeDataString(kv.Key)}={System.Uri.EscapeDataString(kv.Value ?? "")}"));
                return Encoding.UTF8.GetBytes(formString);
            }

            return null;
        }

        public Dictionary<string, string> GetAllHeaders()
        {
            var allHeaders = new Dictionary<string, string>(Headers);

            if (!string.IsNullOrEmpty(AuthToken))
            {
                allHeaders["Authorization"] = $"Bearer {AuthToken}";
            }

            return allHeaders;
        }
    }

    public static class HttpRequestBuilder
    {
        public static HttpRequest Create(string baseUrl)
        {
            return new HttpRequest { BaseUrl = baseUrl };
        }

        public static HttpRequest Get(string baseUrl, string path)
        {
            return new HttpRequest { BaseUrl = baseUrl, Path = path, Method = "GET" };
        }

        public static HttpRequest Post(string baseUrl, string path)
        {
            return new HttpRequest { BaseUrl = baseUrl, Path = path, Method = "POST" };
        }

        public static HttpRequest Put(string baseUrl, string path)
        {
            return new HttpRequest { BaseUrl = baseUrl, Path = path, Method = "PUT" };
        }

        public static HttpRequest Patch(string baseUrl, string path)
        {
            return new HttpRequest { BaseUrl = baseUrl, Path = path, Method = "PATCH" };
        }

        public static HttpRequest Delete(string baseUrl, string path)
        {
            return new HttpRequest { BaseUrl = baseUrl, Path = path, Method = "DELETE" };
        }
    }

    public static class HttpRequestExtensions
    {
        public static HttpRequest WithMethod(this HttpRequest request, string method)
        {
            request.Method = method;
            return request;
        }

        public static HttpRequest WithPath(this HttpRequest request, string path)
        {
            request.Path = path;
            return request;
        }

        public static HttpRequest WithAuth(this HttpRequest request, string token)
        {
            request.AuthToken = token;
            return request;
        }

        public static HttpRequest WithHeader(this HttpRequest request, string key, string value)
        {
            request.Headers[key] = value;
            return request;
        }

        public static HttpRequest WithHeaders(this HttpRequest request, Dictionary<string, string> headers)
        {
            if (headers != null)
            {
                foreach (var kv in headers)
                {
                    request.Headers[kv.Key] = kv.Value;
                }
            }
            return request;
        }

        public static HttpRequest WithQuery(this HttpRequest request, string key, string value)
        {
            request.QueryParams[key] = value;
            return request;
        }

        public static HttpRequest WithQueries(this HttpRequest request, Dictionary<string, string> queryParams)
        {
            if (queryParams != null)
            {
                foreach (var kv in queryParams)
                {
                    request.QueryParams[kv.Key] = kv.Value;
                }
            }
            return request;
        }

        public static HttpRequest WithFormData(this HttpRequest request, string key, string value)
        {
            request.FormData[key] = value;
            return request;
        }

        public static HttpRequest WithFormData(this HttpRequest request, Dictionary<string, string> formData)
        {
            if (formData != null)
            {
                foreach (var kv in formData)
                {
                    request.FormData[kv.Key] = kv.Value;
                }
            }
            return request;
        }

        public static HttpRequest WithJsonBody(this HttpRequest request, object body)
        {
            request.JsonBody = body;
            request.ContentType = "application/json";
            return request;
        }

        public static HttpRequest WithRawBody(this HttpRequest request, byte[] body, string contentType = "application/json")
        {
            request.Body = body;
            request.ContentType = contentType;
            return request;
        }

        public static HttpRequest WithContentType(this HttpRequest request, string contentType)
        {
            request.ContentType = contentType;
            return request;
        }
    }

    public interface IHttpHandler
    {
        Task<HttpResponse> SendAsync(HttpRequest request, CancellationToken ct);
    }
}