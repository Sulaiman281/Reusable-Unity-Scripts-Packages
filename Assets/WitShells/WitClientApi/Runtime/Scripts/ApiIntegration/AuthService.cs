using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Events;

namespace WitShells.WitClientApi
{
    public interface IAuthService
    {
        Task SignInAsync(object credentials, UnityAction<TokenResponse> onSuccess, CancellationToken ct);
        Task SignOutAsync(CancellationToken ct);
        Task<bool> RefreshTokenAsync(CancellationToken ct);
    }

    /// <summary>
    /// Handles authentication flows (sign-in, sign-out, refresh) separate from WitClientManager.
    /// Methods are virtual so a project can inherit and override behavior.
    /// </summary>
    public class AuthService : IAuthService
    {
        protected readonly ApiConfig _config;
        protected readonly IHttpHandler _httpHandler;
        protected readonly ITokenStorage _tokenStorage;
        protected readonly ResponseParser _responseParser = new ResponseParser();

        public AuthService(ApiConfig config, IHttpHandler httpHandler, ITokenStorage tokenStorage)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _httpHandler = httpHandler ?? throw new ArgumentNullException(nameof(httpHandler));
            _tokenStorage = tokenStorage ?? throw new ArgumentNullException(nameof(tokenStorage));
        }

        public virtual async Task SignInAsync(object credentials, UnityAction<TokenResponse> onSuccess, CancellationToken ct)
        {
            var path = _config.SignInPath;
            ApiClientManager.Instance.CallEndpoint(path, credentials, (res) =>
            {
                var tr = _responseParser.ParseResponse<TokenResponse>(res);
                _tokenStorage.SignInAsync(tr).ConfigureAwait(false);
                onSuccess?.Invoke(tr);
            }, (fail) =>
            {

            });
        }

        public virtual async Task SignOutAsync(CancellationToken ct)
        {
            var path = _config.SignOutPath;
            ApiClientManager.Instance.CallEndpoint(path, null, (res) =>
            {
                _tokenStorage.SignOutAsync().ConfigureAwait(false);
            }, (fail) =>
            {
                _tokenStorage.SignOutAsync().ConfigureAwait(false);
            });
        }

        public virtual async Task<bool> RefreshTokenAsync(CancellationToken ct)
        {
            var token = await _tokenStorage.GetTokensAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(token.RefreshToken)) return false;
            if (string.IsNullOrEmpty(token.AccessToken)) return false;

            var path = _config.RefreshTokenPath;
            var req = HttpRequestBuilder.Create(_config.GetBaseUrl()).WithMethod("POST").WithPath(path).WithJsonBody(token);
            var resp = await _httpHandler.SendAsync(req, ct).ConfigureAwait(false);
            if (resp == null) return false;
            var text = System.Text.Encoding.UTF8.GetString(resp.Data ?? Array.Empty<byte>());
            if (resp.StatusCode >= 200 && resp.StatusCode < 300)
            {
                var tr = ParseTokenResponse(text);
                if (tr != null)
                {
                    await _tokenStorage.SignInAsync(tr).ConfigureAwait(false);
                    return true;
                }
            }
            return false;
        }
        protected virtual TokenResponse ParseTokenResponse(string json)
        {
            return _responseParser.ParseResponse<TokenResponse>(json);
        }
    }
}
