using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WitShells.WitClientApi
{
    public class DefaultHttpHandler : IHttpHandler, IDisposable
    {
        private static readonly HttpClient _sharedClient = new HttpClient();

        public async Task<HttpResponse> SendAsync(HttpRequest request, CancellationToken ct)
        {
            try
            {
                var url = request.GetFullUrl();
                using (var httpReq = new HttpRequestMessage(new HttpMethod(request.Method), url))
                {
                    var body = request.GetRequestBody();
                    if (body != null && body.Length > 0)
                    {
                        httpReq.Content = new ByteArrayContent(body);
                        if (!string.IsNullOrEmpty(request.ContentType))
                            httpReq.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(request.ContentType);
                    }

                    // Add headers
                    var allHeaders = request.GetAllHeaders();
                    if (allHeaders != null)
                    {
                        foreach (var kv in allHeaders)
                        {
                            // Some headers must go to Content headers
                            if (string.Equals(kv.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                            {
                                if (httpReq.Content != null && !string.IsNullOrEmpty(kv.Value))
                                    httpReq.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(kv.Value);
                            }
                            else
                            {
                                httpReq.Headers.Remove(kv.Key);
                                httpReq.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
                            }
                        }
                    }

                    using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                    {
                        var resp = await _sharedClient.SendAsync(httpReq, HttpCompletionOption.ResponseContentRead, cts.Token).ConfigureAwait(false);
                        var data = resp.Content != null ? await resp.Content.ReadAsByteArrayAsync().ConfigureAwait(false) : Array.Empty<byte>();
                        return new HttpResponse
                        {
                            StatusCode = (int)resp.StatusCode,
                            Data = data ?? Array.Empty<byte>(),
                            Error = resp.IsSuccessStatusCode ? null : resp.ReasonPhrase
                        };
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return new HttpResponse { StatusCode = 0, Data = Array.Empty<byte>(), Error = "Canceled" };
            }
            catch (Exception ex)
            {
                return new HttpResponse { StatusCode = 0, Data = Array.Empty<byte>(), Error = ex.Message };
            }
        }

        public void Dispose()
        {
            // HttpClient is shared/static; do not dispose here.
        }
    }
}