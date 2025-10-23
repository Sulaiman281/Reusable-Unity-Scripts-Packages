using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using UnityEngine;

namespace WitShells.WitClientApi
{
    /// <summary>
    /// Thread-safe token storage that keeps tokens in-memory for background threads
    /// and queues PlayerPrefs writes to be flushed on the main thread.
    /// Call InitializeFromPlayerPrefs() on the main thread at startup (WitClientManager does this)
    /// and call FlushPendingWrites() periodically on the main thread (WitClientManager.Update does this).
    /// </summary>
    public class PlayerPrefsTokenStorage : ITokenStorage
    {
        private const string AccessKey = "WitShells_AccessToken";
        private const string RefreshKey = "WitShells_RefreshToken";

        private static readonly object _cacheLock = new object();
        private static string _cachedAccess;
        private static string _cachedRefresh;

        // Queue of actions that must run on the main thread (PlayerPrefs writes)
        private static readonly ConcurrentQueue<Action> _pendingWrites = new ConcurrentQueue<Action>();

        /// <summary>
        /// Read PlayerPrefs on main thread to prime the in-memory cache. Call from Awake on main thread.
        /// </summary>
        public static void InitializeFromPlayerPrefs()
        {
            lock (_cacheLock)
            {
                if (PlayerPrefs.HasKey(AccessKey)) _cachedAccess = PlayerPrefs.GetString(AccessKey);
                else _cachedAccess = null;

                if (PlayerPrefs.HasKey(RefreshKey)) _cachedRefresh = PlayerPrefs.GetString(RefreshKey);
                else _cachedRefresh = null;
            }
        }

        /// <summary>
        /// Flush queued PlayerPrefs actions on main thread. Call from Update.
        /// </summary>
        public static void FlushPendingWrites()
        {
            while (_pendingWrites.TryDequeue(out var action))
            {
                try { action?.Invoke(); } catch { }
            }
            // ensure PlayerPrefs saved at end of flush
            try { PlayerPrefs.Save(); } catch { }
        }

        public Task SignInAsync(TokenResponse tokens)
        {
            if (tokens == null) return Task.CompletedTask;

            lock (_cacheLock)
            {
                if (!string.IsNullOrEmpty(tokens.AccessToken)) _cachedAccess = tokens.AccessToken;
                if (!string.IsNullOrEmpty(tokens.RefreshToken)) _cachedRefresh = tokens.RefreshToken;
            }

            // enqueue PlayerPrefs writes to be executed on main thread
            if (!string.IsNullOrEmpty(tokens.AccessToken))
            {
                var a = tokens.AccessToken;
                _pendingWrites.Enqueue(() => PlayerPrefs.SetString(AccessKey, a));
            }
            if (!string.IsNullOrEmpty(tokens.RefreshToken))
            {
                var r = tokens.RefreshToken;
                _pendingWrites.Enqueue(() => PlayerPrefs.SetString(RefreshKey, r));
            }

            return Task.CompletedTask;
        }

        public Task SignOutAsync()
        {
            lock (_cacheLock)
            {
                _cachedAccess = null;
                _cachedRefresh = null;
            }

            _pendingWrites.Enqueue(() => PlayerPrefs.DeleteKey(AccessKey));
            _pendingWrites.Enqueue(() => PlayerPrefs.DeleteKey(RefreshKey));

            return Task.CompletedTask;
        }

        public Task<string> GetAccessTokenAsync()
        {
            lock (_cacheLock)
            {
                return Task.FromResult(_cachedAccess);
            }
        }

        public Task<string> GetRefreshTokenAsync()
        {
            lock (_cacheLock)
            {
                return Task.FromResult(_cachedRefresh);
            }
        }

        public Task<TokenResponse> GetTokensAsync()
        {
            lock (_cacheLock)
            {
                var tr = new TokenResponse
                {
                    AccessToken = _cachedAccess,
                    RefreshToken = _cachedRefresh
                };
                return Task.FromResult(tr);
            }
        }
    }
}
