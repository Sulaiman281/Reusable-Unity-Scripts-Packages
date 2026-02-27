namespace WitShells.DesignPatterns.Core
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Defines the contract for a <b>Flyweight</b> object.
    /// The flyweight stores only <i>intrinsic</i> (shared, immutable) state;
    /// <i>extrinsic</i> (per-instance) state is passed in via <see cref="Operation"/>.
    /// </summary>
    public interface IFlyweight
    {
        /// <summary>
        /// Performs an operation using the flyweight's intrinsic state combined with the
        /// supplied extrinsic state (e.g. world position, scale).
        /// </summary>
        /// <param name="extrinsicState">Per-instance data that does not reside in the flyweight itself.</param>
        void Operation(object extrinsicState);
    }

    /// <summary>
    /// Generic <b>Flyweight Factory</b> that caches and reuses flyweight instances keyed by
    /// <typeparamref name="TKey"/>. If a flyweight for a given key already exists it is returned
    /// directly, otherwise it is created via the supplied factory delegate and cached.
    /// </summary>
    /// <typeparam name="TKey">The lookup key type (e.g. a string mesh name or an enum value).</typeparam>
    /// <typeparam name="TFlyweight">The flyweight type, must implement <see cref="IFlyweight"/>.</typeparam>
    /// <remarks>
    /// Use this pattern when you need to render thousands of objects that share the same
    /// visual data (e.g. trees, bullets, enemies of the same type). Only one flyweight
    /// instance is kept per unique key regardless of how many objects use it.
    /// </remarks>
    public class FlyweightFactory<TKey, TFlyweight> where TFlyweight : IFlyweight
    {
        private readonly Dictionary<TKey, TFlyweight> _flyweights = new Dictionary<TKey, TFlyweight>();

        /// <summary>
        /// Returns the cached flyweight for <paramref name="key"/>.
        /// If none exists, <paramref name="createFunc"/> is called to create and cache a new one.
        /// </summary>
        /// <param name="key">Unique identifier for the flyweight.</param>
        /// <param name="createFunc">Factory delegate invoked only when a new flyweight is needed.</param>
        public TFlyweight GetFlyweight(TKey key, Func<TFlyweight> createFunc)
        {
            if (!_flyweights.TryGetValue(key, out var flyweight))
            {
                flyweight = createFunc();
                _flyweights[key] = flyweight;
            }
            return flyweight;
        }

        /// <summary>Number of unique flyweight instances currently cached.</summary>
        public int Count => _flyweights.Count;
    }

    /// <summary>
    /// Example <see cref="IFlyweight"/> that stores shared tree mesh and texture data.
    /// Many tree instances in the scene can reference a single <see cref="TreeFlyweight"/>;
    /// each instance provides its own position via the extrinsic state argument.
    /// </summary>
    public class TreeFlyweight : IFlyweight
    {
        /// <summary>Shared mesh asset name/path (intrinsic state).</summary>
        public string Mesh;

        /// <summary>Shared texture asset name/path (intrinsic state).</summary>
        public string Texture;

        /// <summary>
        /// Draws this tree using the shared mesh/texture and the per-instance position
        /// provided via <paramref name="extrinsicState"/>.
        /// </summary>
        /// <param name="extrinsicState">The world position (or transform) of this particular tree instance.</param>
        public void Operation(object extrinsicState)
        {
            // Use extrinsicState for position, scale, etc.
            Console.WriteLine($"Drawing tree with mesh {Mesh}, texture {Texture}, at {extrinsicState}");
        }
    }
}