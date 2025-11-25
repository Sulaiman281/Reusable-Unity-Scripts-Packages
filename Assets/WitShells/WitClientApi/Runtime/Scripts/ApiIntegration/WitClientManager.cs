using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Concurrent;
using System.Text;
using UnityEngine.Events;
using WitShells.ThreadingJob;
using WitShells.DesignPatterns.Core;

namespace WitShells.WitClientApi
{
    public class ApiClientManager : MonoSingleton<ApiClientManager>
    {
        [Tooltip("Resource path (without extension) to the endpoints JSON. Default: Endpoints/endpoints")]
        public string EndpointsResourcePath = "Endpoints/endpoints";

        [Tooltip("Optional ApiConfig ScriptableObject (assign in Inspector)")]
        public ApiConfig ApiConfig;

        [SerializeField]
        public string[] AvailableEndpoints = Array.Empty<string>();

        [Header("Logging")]
        [Tooltip("Enable verbose request/response logging (thread-safe). Logs will be flushed to Unity Console on the main thread.")]
        public bool VerboseLogging = true;

        private ConcurrentQueue<(string level, string message)> _logQueue = new ConcurrentQueue<(string, string)>();

        protected JsonEndpointReader _reader;
        protected DefaultHttpHandler _httpHandler;
        protected ITokenStorage _tokenStorage;
        protected IAuthService _authService;
        protected ResponseParser _responseParser;

        public ResponseParser ResponseParser => _responseParser;

#if UNITY_EDITOR
        [ContextMenu("Test Endpoint: Email Exists")]
        public void TestEmailExists()
        {
            if (_reader == null)
            {
                Debug.LogError("JsonEndpointReader not initialized.");
                return;
            }

            CallEndpoint("api/Auth/emailExists", new { email = "sayedsulaiman607@gmail.com" },
                (result) => Debug.Log($"Success: {Json.Serialize(result)}"),
                (error) => Debug.LogError($"Error: {error}"));
        }

        [ContextMenu("Test Sign-In")]
        public void TestSignIn()
        {
            var credentials = new
            {
                email = "witshells@gmail.com",
                password = "12ta442ta11"
            };

            _authService.SignInAsync(credentials, (tr) =>
            {
                Debug.Log($"Signed in via AuthService. Access Token: {tr?.AccessToken}");
            }, (fail) =>
            {
                Debug.LogError($"Sign-in failed via AuthService. Error: \n{fail}");
            });
        }

        public string fetchPage;
        [ContextMenu("Test Fetch Page")]
        public void TestFetchPage()
        {
            CallEndpoint("api/WitCoin/page", new { pageKey = fetchPage, refresh = false },
                (result) => Debug.Log($"Success: {Json.Serialize(result)}"),
                (error) => Debug.LogError($"Error: {error}"));
        }

#endif

        public override void Awake()
        {
            base.Awake();
            InitializeServices();
        }

        protected virtual void InitializeServices()
        {
            _reader = new JsonEndpointReader(EndpointsResourcePath);
            AvailableEndpoints = _reader.AvailableKeys();
            _httpHandler = new DefaultHttpHandler();

            // default token storage implementation
            _tokenStorage = new PlayerPrefsTokenStorage();

            if (ApiConfig == null)
            {
                ApiConfig = ApiConfig.LoadFromResources();
            }
            // initialize token storage cache from PlayerPrefs on main thread
            PlayerPrefsTokenStorage.InitializeFromPlayerPrefs();

            // initialize auth service
            _authService = new AuthService(ApiConfig, _httpHandler, _tokenStorage);
            _responseParser = new ResponseParser();
        }

        protected override void OnDestroy()
        {
            _httpHandler?.Dispose();
            base.OnDestroy();
        }

        private void Update()
        {
            if (_logQueue.IsEmpty) return;
            while (_logQueue.TryDequeue(out var entry))
            {
                try
                {
                    if (entry.level == "ERROR") Debug.LogError(entry.message);
                    else if (entry.level == "WARN") Debug.LogWarning(entry.message);
                    else Debug.Log(entry.message);
                }
                catch { }
            }
            // flush any pending PlayerPrefs writes queued by PlayerPrefsTokenStorage
            PlayerPrefsTokenStorage.FlushPendingWrites();
        }

        private string GetBaseUrl()
        {
            return ApiConfig != null ? ApiConfig.GetBaseUrl() : "";
        }

        public void CallEndpoint(string key, object parameters, UnityAction<object> onSuccess, UnityAction<string> onFail)
        {
            // run the endpoint execution on a background thread, callbacks will be dispatched back to main thread by QuickThreadJobs
            QuickThreadJobs.RunFunctionAsync(async () => await ExecuteEndpointAsync<object, object>(key, parameters, CancellationToken.None),
                (result) => onSuccess?.Invoke(result),
                (ex) => onFail?.Invoke(ex.Message));
        }

        public void CallEndpoint<TRequest, TResponse>(string key, TRequest dto, UnityAction<TResponse> onSuccess, UnityAction<string> onFail)
        {
            QuickThreadJobs.RunFunctionAsync(async () => await ExecuteEndpointAsync<TRequest, TResponse>(key, dto, CancellationToken.None),
                (result) => onSuccess?.Invoke(result is TResponse tr ? tr : (TResponse)result),
                (ex) => onFail?.Invoke(ex.Message));
        }
        private async Task<object> ExecuteEndpointAsync<TRequest, TResponse>(string key, object parameters, CancellationToken ct)
        {
            var def = _reader.GetEndpoint(key);
            if (def == null) throw new Exception($"Endpoint '{key}' not found.");

            var baseUrl = GetBaseUrl();
            var req = HttpRequestBuilder.Create(baseUrl).WithMethod(def.Method).WithPath(def.Path ?? def.Key);

            // attach auth token if available
            var accessToken = await _tokenStorage.GetAccessTokenAsync().ConfigureAwait(false);
            if (!string.IsNullOrEmpty(accessToken)) req = req.WithAuth(accessToken);

            // determine parameters: if method allows body, put parameters in JsonBody; otherwise use query
            var methodUpper = (def.Method ?? "GET").ToUpperInvariant();
            bool hasBody = methodUpper == "POST" || methodUpper == "PUT" || methodUpper == "PATCH";

            if (parameters != null)
            {
                if (hasBody)
                {
                    req = req.WithJsonBody(parameters);
                }
                else
                {
                    var dict = ObjectToDictionary(parameters);
                    req = req.WithQueries(dict);
                }
            }

            if (VerboseLogging)
            {
                try
                {
                    var sbReq = new StringBuilder();
                    sbReq.AppendLine($"[WitClientManager] Request -> {req.Method} {req.GetFullUrl()}");
                    if (req.Headers != null && req.Headers.Count > 0)
                    {
                        sbReq.AppendLine("Headers:");
                        foreach (var h in req.Headers) sbReq.AppendLine($"  {h.Key}: {h.Value}");
                    }
                    if (req.QueryParams != null && req.QueryParams.Count > 0)
                    {
                        sbReq.AppendLine("Query:");
                        foreach (var q in req.QueryParams) sbReq.AppendLine($"  {q.Key}={q.Value}");
                    }
                    if (req.JsonBody != null)
                    {
                        try { sbReq.AppendLine("Body: " + Json.Serialize(req.JsonBody)); } catch { }
                    }
                    _logQueue.Enqueue(("INFO", sbReq.ToString()));
                }
                catch { }
            }

            var httpResp = await _httpHandler.SendAsync(req, ct).ConfigureAwait(false);
            // if unauthorized, attempt refresh via AuthService (only if access token was present)
            if (httpResp != null && httpResp.StatusCode == 401)
            {
                if (string.IsNullOrEmpty(accessToken))
                {
                    throw new Exception("Unauthorized and no access token available for refresh.");
                }

                var refreshed = await (_authService?.RefreshTokenAsync(ct) ?? Task.FromResult(false)).ConfigureAwait(false);
                if (refreshed)
                {
                    var newAccess = await _tokenStorage.GetAccessTokenAsync().ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(newAccess)) req = req.WithAuth(newAccess);
                    httpResp = await _httpHandler.SendAsync(req, ct).ConfigureAwait(false);
                }
            }
            if (httpResp == null) throw new Exception("No response from server");

            if (httpResp.StatusCode >= 200 && httpResp.StatusCode < 300)
            {
                var text = Encoding.UTF8.GetString(httpResp.Data ?? Array.Empty<byte>());
                if (VerboseLogging)
                {
                    try
                    {
                        var sbResp = new StringBuilder();
                        sbResp.AppendLine($"[WitClientManager] Response <- HTTP {httpResp.StatusCode} for {req.Method} {req.GetFullUrl()}");
                        if (!string.IsNullOrWhiteSpace(text)) sbResp.AppendLine("Body: " + text);
                        _logQueue.Enqueue(("INFO", sbResp.ToString()));
                    }
                    catch { }
                }
                if (!string.IsNullOrWhiteSpace(text))
                {
                    try
                    {
                        if (typeof(TResponse) == typeof(object) || typeof(TResponse) == typeof(System.Object))
                        {
                            return Newtonsoft.Json.Linq.JToken.Parse(text);
                        }
                        else
                        {
                            return Json.Deserialize<TResponse>(text);
                        }
                    }
                    catch (Exception)
                    {
                        return text;
                    }
                }
                return null;
            }
            else
            {
                var err = httpResp.Error ?? ($"HTTP {httpResp.StatusCode}");

                // Attempt to parse detailed error message from JSON body
                if (httpResp.Data != null && httpResp.Data.Length > 0)
                {
                    try
                    {
                        var errorBody = Encoding.UTF8.GetString(httpResp.Data);
                        if (VerboseLogging)
                        {
                            _logQueue.Enqueue(("ERROR", $"[WitClientManager] Error Body: {errorBody}"));
                        }

                        err = errorBody;
                    }
                    catch { /* ignore parsing errors */ }
                }

                if (VerboseLogging) _logQueue.Enqueue(("ERROR", $"[WitClientManager] HTTP Error {httpResp.StatusCode}: {err} for {req.Method} {req.GetFullUrl()}"));
                throw new Exception(err);
            }
        }

        // Sign-in/out handled by AuthService. Use _authService.SignInAsync / SignOutAsync from calling code if needed.

        private Dictionary<string, string> ObjectToDictionary(object obj)
        {
            var dict = new Dictionary<string, string>();
            if (obj == null) return dict;

            // if already a dictionary
            if (obj is Dictionary<string, string> ss)
            {
                return new Dictionary<string, string>(ss);
            }

            if (obj is System.Collections.IDictionary idict)
            {
                foreach (System.Collections.DictionaryEntry de in idict)
                {
                    if (de.Key != null)
                        dict[de.Key.ToString()] = de.Value?.ToString();
                }
                return dict;
            }

            // use JSON round-trip to JObject then flatten top-level properties
            try
            {
                var j = Newtonsoft.Json.Linq.JObject.FromObject(obj);
                foreach (var p in j.Properties())
                {
                    dict[p.Name] = p.Value.Type == Newtonsoft.Json.Linq.JTokenType.Null ? null : p.Value.ToString();
                }
            }
            catch
            {
                // fallback to ToString
                dict["value"] = obj.ToString();
            }

            return dict;
        }
    }
}
