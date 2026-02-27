using System;
using System.Collections.Generic;

namespace WitShells.DesignPatterns.Core
{
    /// <summary>
    /// Abstract <b>Singleton-per-key Factory</b> that creates at most one instance of
    /// <typeparamref name="TProduct"/> per <typeparamref name="TKey"/>.
    /// On the first call to <see cref="Get"/> for a given key the creator is invoked and the
    /// result is cached. Subsequent calls return the cached instance.
    /// </summary>
    /// <typeparam name="TKey">The lookup key type.</typeparam>
    /// <typeparam name="TProduct">The product type returned by the factory.</typeparam>
    /// <remarks>
    /// Typical use-case: per-type managers, per-level configurations, or any resource that
    /// should be created lazily and then reused. Subclass to expose a typed public API.
    /// </remarks>
    public abstract class GenericSingleFactory<TKey, TProduct>
    {
        private readonly Dictionary<TKey, Func<TProduct>> _creators = new Dictionary<TKey, Func<TProduct>>();
        private readonly Dictionary<TKey, TProduct> _instances = new Dictionary<TKey, TProduct>();

        /// <summary>
        /// Registers (or replaces) the creator delegate for <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key to associate with this creator.</param>
        /// <param name="creator">A delegate invoked the first time this key is requested.</param>
        public void Register(TKey key, Func<TProduct> creator)
        {
            _creators[key] = creator;
        }

        /// <summary>
        /// Returns <c>true</c> if a creator has been registered for the given key.
        /// </summary>
        public bool IsRegistered(TKey key)
        {
            return _creators.ContainsKey(key);
        }

        /// <summary>
        /// Returns the cached instance for <paramref name="key"/>, creating it on first access.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns>The single instance associated with this key.</returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when no creator has been registered for <paramref name="key"/>.
        /// </exception>
        public TProduct Get(TKey key)
        {
            if (_instances.TryGetValue(key, out var instance))
            {
                return instance;
            }

            if (_creators.TryGetValue(key, out var creator))
            {
                instance = creator();
                _instances[key] = instance;
                return instance;
            }

            throw new KeyNotFoundException($"No creator registered for key: {key}");
        }
    }
}