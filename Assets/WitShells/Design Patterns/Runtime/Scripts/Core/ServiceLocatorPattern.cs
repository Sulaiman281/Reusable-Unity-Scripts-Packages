namespace WitShells.DesignPatterns.Core
{
    using System;
    using System.Collections.Generic;

    // Generic Service Locator
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        // Register a service instance
        public static void Register<TService>(TService service)
        {
            var type = typeof(TService);
            if (_services.ContainsKey(type))
                _services[type] = service;
            else
                _services.Add(type, service);
        }

        // Get a registered service
        public static TService Get<TService>()
        {
            var type = typeof(TService);
            if (_services.TryGetValue(type, out var service))
                return (TService)service;
            throw new InvalidOperationException($"Service of type {type} not registered.");
        }

        // Remove a service
        public static void Unregister<TService>()
        {
            var type = typeof(TService);
            _services.Remove(type);
        }

        // Clear all services
        public static void Clear()
        {
            _services.Clear();
        }
    }
}