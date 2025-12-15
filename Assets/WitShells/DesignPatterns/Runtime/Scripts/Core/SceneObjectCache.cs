using System;
using System.Collections.Generic;
using UnityEngine;

namespace WitShells.DesignPatterns.Core
{
    /// <summary>
    /// A lightweight, non-singleton cache for scene objects.
    /// - Finds the first object of type <typeparamref name="T"/> in the scene using Unity's FindFirstObjectByType.
    /// - Caches the found instance per-type to avoid repeated scene searches.
    /// - If the cached object gets destroyed or is missing, the cache re-finds it on the next access.
    ///
    /// Use this when multiple systems need access to a shared scene object without forcing a Singleton pattern.
    /// </summary>
    public static class SceneObjectCache
    {
        private static readonly Dictionary<Type, UnityEngine.Object> _cache = new Dictionary<Type, UnityEngine.Object>();

        /// <summary>
        /// Gets a cached instance of <typeparamref name="T"/> if available, otherwise finds the first instance in the scene,
        /// caches it, and returns it. Returns null if none found.
        /// </summary>
        public static T Get<T>() where T : UnityEngine.Object
        {
            var key = typeof(T);

            if (_cache.TryGetValue(key, out var cached) && cached)
            {
                return (T)cached;
            }

            // Remove stale/null entry if present
            if (_cache.ContainsKey(key)) _cache.Remove(key);

            // Find and cache
            var found = UnityEngine.Object.FindFirstObjectByType<T>();
            if (found != null)
            {
                _cache[key] = found;
            }
            return found;
        }

        /// <summary>
        /// Tries to get a cached/found instance of <typeparamref name="T"/>.
        /// </summary>
        public static bool TryGet<T>(out T instance) where T : UnityEngine.Object
        {
            instance = Get<T>();
            return instance != null;
        }

        /// <summary>
        /// Manually sets (or replaces) the cached instance for <typeparamref name="T"/>.
        /// Pass null to clear the entry for this type.
        /// </summary>
        public static void Set<T>(T instance) where T : UnityEngine.Object
        {
            var key = typeof(T);
            if (instance == null)
            {
                _cache.Remove(key);
            }
            else
            {
                _cache[key] = instance;
            }
        }

        /// <summary>
        /// Removes the cached entry for <typeparamref name="T"/>, if any.
        /// </summary>
        public static void Invalidate<T>() where T : UnityEngine.Object
        {
            _cache.Remove(typeof(T));
        }

        /// <summary>
        /// Sweeps the cache and removes entries whose targets have been destroyed.
        /// </summary>
        public static void SweepDestroyed()
        {
            var deadKeys = new List<Type>();
            foreach (var kv in _cache)
            {
                if (!kv.Value) // UnityEngine.Object null-check (destroyed)
                {
                    deadKeys.Add(kv.Key);
                }
            }
            for (int i = 0; i < deadKeys.Count; i++)
            {
                _cache.Remove(deadKeys[i]);
            }
        }

        /// <summary>
        /// Clears all cached entries.
        /// </summary>
        public static void Clear()
        {
            _cache.Clear();
        }
    }
}
