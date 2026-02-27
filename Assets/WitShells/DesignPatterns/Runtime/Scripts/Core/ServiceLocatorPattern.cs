namespace WitShells.DesignPatterns.Core
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A global, type-keyed <b>Service Locator</b> that acts as a lightweight alternative to
    /// dependency injection. Systems register their implementations once and any other system
    /// can retrieve them by interface/type without knowing the concrete class.
    /// </summary>
    /// <remarks>
    /// <b>When to use:</b> Small-to-medium projects where full DI container setup is overkill.
    /// <b>Caution:</b> Overuse hides dependencies and makes unit testing harder. Prefer constructor
    /// injection or <see cref="MonoSingleton{T}"/> for single-instance MonoBehaviours.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Registration (e.g. in a bootstrap scene)
    /// ServiceLocator.Register&lt;IAudioService&gt;(new AudioService());
    ///
    /// // Retrieval (anywhere in the project)
    /// var audio = ServiceLocator.Get&lt;IAudioService&gt;();
    /// audio.PlaySound("explosion");
    /// </code>
    /// </example>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        /// <summary>
        /// Registers a service instance under its type key <typeparamref name="TService"/>.
        /// If a service of the same type is already registered it is replaced.
        /// </summary>
        /// <typeparam name="TService">The interface or concrete type used as the lookup key.</typeparam>
        /// <param name="service">The service instance to register.</param>
        public static void Register<TService>(TService service)
        {
            var type = typeof(TService);
            if (_services.ContainsKey(type))
                _services[type] = service;
            else
                _services.Add(type, service);
        }

        /// <summary>
        /// Retrieves the registered service of type <typeparamref name="TService"/>.
        /// </summary>
        /// <typeparam name="TService">The type to look up.</typeparam>
        /// <returns>The registered service instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no service of this type has been registered.</exception>
        public static TService Get<TService>()
        {
            var type = typeof(TService);
            if (_services.TryGetValue(type, out var service))
                return (TService)service;
            throw new InvalidOperationException($"Service of type {type} not registered.");
        }

        /// <summary>
        /// Removes the registered service for type <typeparamref name="TService"/>.
        /// </summary>
        /// <typeparam name="TService">The type whose registration should be removed.</typeparam>
        public static void Unregister<TService>()
        {
            var type = typeof(TService);
            _services.Remove(type);
        }

        /// <summary>Removes all registered services from the locator.</summary>
        public static void Clear()
        {
            _services.Clear();
        }
    }
}