using System.Collections.Generic;
using UnityEngine.Networking;

namespace WitShells.ApiIntegration
{
    public static class ApiUtils
    {
        public static UnityWebRequest AddUrl(this UnityWebRequest request, string url)
        {
            request.url = url;
            return request;
        }

        public static UnityWebRequest SetMethod(this UnityWebRequest request, string method)
        {
            request.method = method;
            return request;
        }

        public static UnityWebRequest SetHeaders(this UnityWebRequest request, Dictionary<string, string> headers)
        {
            foreach (var header in headers)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }
            return request;
        }

        public static UnityWebRequest SetBody(this UnityWebRequest request, string body)
        {
            var bodyBytes = System.Text.Encoding.UTF8.GetBytes(body);
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            return request;
        }

        public static UnityWebRequest SetJsonBody(this UnityWebRequest request, string json)
        {
            request.SetBody(json);
            request.SetRequestHeader("Content-Type", "application/json");
            return request;
        }

        public static UnityWebRequest SetTimeout(this UnityWebRequest request, int timeoutSeconds)
        {
            request.timeout = timeoutSeconds;
            return request;
        }

        public static UnityWebRequest SetAuth(this UnityWebRequest request, string token, string scheme = "Bearer")
        {
            request.SetRequestHeader("Authorization", $"{scheme} {token}");
            return request;
        }

        public static UnityWebRequest SetUserAgent(this UnityWebRequest request, string userAgent)
        {
            request.SetRequestHeader("User-Agent", userAgent);
            return request;
        }

        public static UnityWebRequest AddQueryParam(this UnityWebRequest request, string key, string value)
        {
            var separator = request.url.Contains("?") ? "&" : "?";
            request.url += $"{separator}{UnityWebRequest.EscapeURL(key)}={UnityWebRequest.EscapeURL(value)}";
            return request;
        }

        public static UnityWebRequest AddQueryParams(this UnityWebRequest request, Dictionary<string, string> parameters)
        {
            foreach (var param in parameters)
            {
                request.AddQueryParam(param.Key, param.Value);
            }
            return request;
        }

        public static UnityWebRequest Create()
        {
            return new UnityWebRequest();
        }

        public static UnityWebRequest SetMultipartForm(this UnityWebRequest request, List<IMultipartFormSection> formData)
        {
            request.uploadHandler = new UploadHandlerRaw(UnityWebRequest.SerializeFormSections(formData, UnityWebRequest.GenerateBoundary()));
            request.SetRequestHeader("Content-Type", "multipart/form-data; boundary=" + System.Text.Encoding.UTF8.GetString(UnityWebRequest.GenerateBoundary()));
            return request;
        }

        public static UnityWebRequest UploadFile(this UnityWebRequest request, string fieldName, byte[] fileData, string fileName, string mimeType = "application/octet-stream")
        {
            var formData = new List<IMultipartFormSection>
            {
                new MultipartFormFileSection(fieldName, fileData, fileName, mimeType)
            };
            return request.SetMultipartForm(formData);
        }

        public static UnityWebRequest UploadFileWithFields(this UnityWebRequest request, string fieldName, byte[] fileData, string fileName, Dictionary<string, string> formFields, string mimeType = "application/octet-stream")
        {
            var formData = new List<IMultipartFormSection>
            {
                new MultipartFormFileSection(fieldName, fileData, fileName, mimeType)
            };

            foreach (var field in formFields)
            {
                formData.Add(new MultipartFormDataSection(field.Key, field.Value));
            }

            return request.SetMultipartForm(formData);
        }

        public static UnityWebRequest SetChunkedUpload(this UnityWebRequest request, byte[] data, int chunkSize = 8192)
        {
            request.uploadHandler = new UploadHandlerRaw(data)
            {
                contentType = "application/octet-stream"
            };
            request.SetRequestHeader("Transfer-Encoding", "chunked");
            return request;
        }

        public static UnityWebRequest SetLargeDataUpload(this UnityWebRequest request, byte[] data, string contentType = "application/octet-stream")
        {
            request.uploadHandler = new UploadHandlerRaw(data)
            {
                contentType = contentType
            };
            request.SetRequestHeader("Content-Length", data.Length.ToString());
            return request;
        }

        public static void SendRequest<T>(this UnityWebRequest request, UnityEngine.Events.UnityAction<T> onSuccess, UnityEngine.Events.UnityAction<string> onError)
        {
            var operation = request.SendWebRequest();
            operation.completed += _ =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        if (typeof(T) == typeof(string))
                        {
                            onSuccess?.Invoke((T)(object)request.downloadHandler.text);
                        }
                        else if (typeof(T) == typeof(byte[]))
                        {
                            onSuccess?.Invoke((T)(object)request.downloadHandler.data);
                        }
                        else
                        {
                            var json = request.downloadHandler.text;
                            var result = UnityEngine.JsonUtility.FromJson<T>(json);
                            onSuccess?.Invoke(result);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        onError?.Invoke($"Failed to parse response: {ex.Message}");
                    }
                }
                else
                {
                    onError?.Invoke($"Request failed: {request.error}");
                }
                request.Dispose();
            };
        }

        public static void SendRequest(this UnityWebRequest request, UnityEngine.Events.UnityAction<string> onSuccess, UnityEngine.Events.UnityAction<string> onError)
        {
            request.SendRequest<string>(onSuccess, onError);
        }

        public static UnityWebRequest SetContentType(this UnityWebRequest request, string contentType)
        {
            request.SetRequestHeader("Content-Type", contentType);
            return request;
        }

        public static string CombineUrl(string baseUrl, string endpoint)
        {
            if (string.IsNullOrEmpty(baseUrl))
                return endpoint ?? string.Empty;

            if (string.IsNullOrEmpty(endpoint))
                return baseUrl;

            var trimmedBase = baseUrl.TrimEnd('/');
            var trimmedEndpoint = endpoint.TrimStart('/');

            return $"{trimmedBase}/{trimmedEndpoint}";
        }

        public static UnityWebRequest SetUrl(this UnityWebRequest request, string baseUrl, string endpoint)
        {
            request.url = CombineUrl(baseUrl, endpoint);
            return request;
        }
    }

}