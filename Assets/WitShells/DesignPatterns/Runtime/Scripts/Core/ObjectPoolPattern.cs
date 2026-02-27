namespace WitShells.DesignPatterns.Core
{

    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A generic <b>Object Pool</b> that recycles instances of <typeparamref name="T"/> to reduce
    /// heap allocations and GC pressure. Objects are retrieved with <see cref="Get"/> and returned
    /// with <see cref="Release"/> after use.
    /// </summary>
    /// <typeparam name="T">
    /// The pooled type. Must be a reference type. If no factory is provided it must also have
    /// a public parameterless constructor (<c>new()</c> constraint).
    /// </typeparam>
    /// <remarks>
    /// Commonly used in Unity to pool GameObjects, particle effects, projectiles, or any
    /// frequently instantiated/destroyed object.
    /// </remarks>
    /// <example>
    /// <code>
    /// var pool = new ObjectPool&lt;Bullet&gt;(() =&gt; new Bullet(), initialCapacity: 20);
    /// Bullet b = pool.Get();   // reuse or create
    /// pool.Release(b);         // return after use
    /// </code>
    /// </example>
    public class ObjectPool<T> where T : class, new()
    {
        private readonly Stack<T> _pool = new Stack<T>();
        private readonly Func<T> _factoryMethod;

        /// <summary>
        /// Creates a new pool.
        /// </summary>
        /// <param name="factoryMethod">
        /// Optional factory delegate used to create new instances when the pool is empty.
        /// Defaults to <c>new T()</c> if not supplied.
        /// </param>
        /// <param name="initialCapacity">Number of instances to pre-warm the pool with.</param>
        public ObjectPool(Func<T> factoryMethod = null, int initialCapacity = 0)
        {
            _factoryMethod = factoryMethod ?? (() => new T());
            for (int i = 0; i < initialCapacity; i++)
            {
                _pool.Push(_factoryMethod());
            }
        }

        /// <summary>
        /// Returns a pooled instance if one is available; otherwise creates a new one via the factory.
        /// </summary>
        public T Get()
        {
            return _pool.Count > 0 ? _pool.Pop() : _factoryMethod();
        }

        /// <summary>
        /// Returns an instance to the pool so it can be reused later.
        /// Ensure the object is fully reset before or after calling this method.
        /// </summary>
        /// <param name="obj">The instance to return to the pool.</param>
        public void Release(T obj)
        {
            _pool.Push(obj);
        }

        /// <summary>The number of instances currently sitting idle in the pool.</summary>
        public int Count => _pool.Count;
    }
}