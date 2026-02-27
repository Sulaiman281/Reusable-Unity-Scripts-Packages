namespace WitShells.DesignPatterns.Core
{
    /// <summary>
    /// Defines the contract for the <b>Builder</b> pattern.
    /// Implementors construct a product of type <typeparamref name="T"/> step-by-step
    /// and return it via <see cref="Build"/>.
    /// </summary>
    /// <typeparam name="T">The type of object being built.</typeparam>
    public interface IBuilder<T>
    {
        /// <summary>Finalises and returns the constructed object.</summary>
        T Build();
    }

    /// <summary>
    /// Abstract base class for the <b>Builder</b> pattern.
    /// Extend this class and add fluent setter methods to construct complex objects
    /// step-by-step, keeping construction logic separate from the product class itself.
    /// </summary>
    /// <typeparam name="T">The type of object being built.</typeparam>
    public abstract class Builder<T> : IBuilder<T>
    {
        /// <inheritdoc />
        public abstract T Build();
    }

    /// <summary>
    /// Example product class used to demonstrate the Builder pattern.
    /// Represents a game player with a name, health, and movement speed.
    /// </summary>
    public class Player
    {
        /// <summary>The display name of the player.</summary>
        public string Name { get; set; }

        /// <summary>The starting health points of the player.</summary>
        public int Health { get; set; }

        /// <summary>The movement speed of the player in units per second.</summary>
        public float Speed { get; set; }
    }

    /// <summary>
    /// Concrete builder that constructs a <see cref="Player"/> using a fluent API.
    /// Call the <c>Set*</c> methods in any order, then call <see cref="Build"/> to get the result.
    /// </summary>
    /// <example>
    /// <code>
    /// var player = new PlayerBuilder()
    ///     .SetName("Hero")
    ///     .SetHealth(100)
    ///     .SetSpeed(5.5f)
    ///     .Build();
    /// </code>
    /// </example>
    public class PlayerBuilder : Builder<Player>
    {
        private readonly Player _player = new Player();

        /// <summary>Sets the player's name.</summary>
        /// <param name="name">Display name for the player.</param>
        public PlayerBuilder SetName(string name)
        {
            _player.Name = name;
            return this;
        }

        /// <summary>Sets the player's starting health.</summary>
        /// <param name="health">Health point value (e.g. 100).</param>
        public PlayerBuilder SetHealth(int health)
        {
            _player.Health = health;
            return this;
        }

        /// <summary>Sets the player's movement speed.</summary>
        /// <param name="speed">Speed in units per second.</param>
        public PlayerBuilder SetSpeed(float speed)
        {
            _player.Speed = speed;
            return this;
        }

        /// <inheritdoc />
        public override Player Build()
        {
            return _player;
        }
    }
}

// Example usage
/*
var player = new PlayerBuilder()
    .SetName("Hero")
    .SetHealth(100)
    .SetSpeed(5.5f)
    .Build();
*/