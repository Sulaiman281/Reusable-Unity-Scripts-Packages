namespace WitShells.DesignPatterns.Core
{
    using System;
    using System.Collections.Generic;

    // Base Component
    public abstract class Component { }

    // Example Components
    public class HealthComponent : Component
    {
        public int Health;
    }

    public class MovementComponent : Component
    {
        public float Speed;
    }

    // Entity holds components
    public class Entity
    {
        private Dictionary<Type, Component> _components = new Dictionary<Type, Component>();

        public void AddComponent<T>(T component) where T : Component
        {
            _components[typeof(T)] = component;
        }

        public T GetComponent<T>() where T : Component
        {
            _components.TryGetValue(typeof(T), out Component component);
            return component as T;
        }

        public bool HasComponent<T>() where T : Component
        {
            return _components.ContainsKey(typeof(T));
        }
    }

    // Example System
    public class HealthSystem
    {
        public void Update(Entity entity)
        {
            if (entity.HasComponent<HealthComponent>())
            {
                var health = entity.GetComponent<HealthComponent>();
                // Do something with health
            }
        }
    }
}