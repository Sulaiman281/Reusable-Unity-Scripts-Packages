namespace WitShells.DesignPatterns.Core
{
    public interface IBuilder<T>
    {
        T Build();
    }

    // Generic builder base class
    public abstract class Builder<T> : IBuilder<T>
    {
        public abstract T Build();
    }

    // Example: Building a simple Player object
    public class Player
    {
        public string Name { get; set; }
        public int Health { get; set; }
        public float Speed { get; set; }
    }

    // Concrete builder for Player
    public class PlayerBuilder : Builder<Player>
    {
        private readonly Player _player = new Player();

        public PlayerBuilder SetName(string name)
        {
            _player.Name = name;
            return this;
        }

        public PlayerBuilder SetHealth(int health)
        {
            _player.Health = health;
            return this;
        }

        public PlayerBuilder SetSpeed(float speed)
        {
            _player.Speed = speed;
            return this;
        }

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