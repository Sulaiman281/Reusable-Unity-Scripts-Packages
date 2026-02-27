namespace WitShells.DesignPatterns.Core
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Base class for all <b>ECS Components</b>.
    /// A component is pure data with no behaviour — it describes one aspect of an <see cref="Entity"/>
    /// (e.g. position, health, inventory). Derive from this class to create custom components.
    /// </summary>
    public abstract class Component { }

    /// <summary>
    /// Example ECS component that stores an entity's health points.
    /// </summary>
    public class HealthComponent : Component
    {
        /// <summary>Current health value of the entity.</summary>
        public int Health;
    }

    /// <summary>
    /// Example ECS component that stores an entity's movement speed.
    /// </summary>
    public class MovementComponent : Component
    {
        /// <summary>Movement speed in units per second.</summary>
        public float Speed;
    }

    /// <summary>
    /// Represents a game object in the <b>Entity-Component-System (ECS)</b> pattern.
    /// An entity is nothing more than a container of <see cref="Component"/> instances — it has no logic itself.
    /// Behaviour is provided by <i>systems</i> that query entities for required components.
    /// </summary>
    public class Entity
    {
        private Dictionary<Type, Component> _components = new Dictionary<Type, Component>();

        /// <summary>
        /// Adds (or replaces) a component of type <typeparamref name="T"/> on this entity.
        /// </summary>
        /// <typeparam name="T">The component type.</typeparam>
        /// <param name="component">The component data to attach.</param>
        public void AddComponent<T>(T component) where T : Component
        {
            _components[typeof(T)] = component;
        }

        /// <summary>
        /// Returns the component of type <typeparamref name="T"/> attached to this entity,
        /// or <c>null</c> if not present.
        /// </summary>
        /// <typeparam name="T">The component type to retrieve.</typeparam>
        public T GetComponent<T>() where T : Component
        {
            _components.TryGetValue(typeof(T), out Component component);
            return component as T;
        }

        /// <summary>
        /// Returns <c>true</c> if this entity has a component of type <typeparamref name="T"/>.
        /// </summary>
        public bool HasComponent<T>() where T : Component
        {
            return _components.ContainsKey(typeof(T));
        }
    }

    /// <summary>
    /// Example ECS <b>system</b> that operates on entities possessing a <see cref="HealthComponent"/>.
    /// Systems hold the game logic and iterate over relevant entities each update tick.
    /// </summary>
    public class HealthSystem
    {
        /// <summary>
        /// Processes health-related logic for the given entity.
        /// Only acts if the entity has a <see cref="HealthComponent"/>.
        /// </summary>
        /// <param name="entity">The entity to process.</param>
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