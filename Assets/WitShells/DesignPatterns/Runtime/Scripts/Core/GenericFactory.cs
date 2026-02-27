namespace WitShells.DesignPatterns.Core
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A generic, key-based <b>Factory</b> that creates new product instances on demand.
    /// Creator delegates are registered once and called every time a new object is needed,
    /// so each call to <see cref="Create"/> returns a fresh instance.
    /// </summary>
    /// <typeparam name="TKey">The lookup key type (e.g. a string, enum, or int).</typeparam>
    /// <typeparam name="TProduct">The base type or interface of the objects being created.</typeparam>
    /// <remarks>
    /// Use <see cref="GenericSingleFactory{TKey,TProduct}"/> instead when you want exactly one
    /// instance per key (singleton-per-key semantics).
    /// </remarks>
    /// <example>
    /// <code>
    /// var factory = new GenericFactory&lt;string, IWeapon&gt;();
    /// factory.Register("sword", () =&gt; new Sword());
    /// factory.Register("bow",   () =&gt; new Bow());
    /// IWeapon weapon = factory.Create("sword");
    /// </code>
    /// </example>
    public class GenericFactory<TKey, TProduct>
    {
        private readonly Dictionary<TKey, Func<TProduct>> _creators = new Dictionary<TKey, Func<TProduct>>();

        /// <summary>
        /// Registers a creator delegate for the given key.
        /// If the key is already registered the call is silently ignored.
        /// </summary>
        /// <param name="key">The key to associate with the creator.</param>
        /// <param name="creator">A delegate that returns a new product instance.</param>
        public void Register(TKey key, Func<TProduct> creator)
        {
            if (!_creators.ContainsKey(key))
                _creators.Add(key, creator);
        }

        /// <summary>
        /// Creates and returns a new product for the given key by invoking its registered creator.
        /// </summary>
        /// <param name="key">The key whose creator should be invoked.</param>
        /// <returns>A new instance of the product.</returns>
        /// <exception cref="ArgumentException">Thrown when no creator has been registered for <paramref name="key"/>.</exception>
        public TProduct Create(TKey key)
        {
            if (_creators.TryGetValue(key, out var creator))
                return creator();
            throw new ArgumentException($"No creator registered for key: {key}");
        }
    }
}