namespace WitShells.DesignPatterns.Core
{
    using System;
    using System.Collections.Generic;

    public interface IFlyweight
    {
        void Operation(object extrinsicState);
    }

    public class FlyweightFactory<TKey, TFlyweight> where TFlyweight : IFlyweight
    {
        private readonly Dictionary<TKey, TFlyweight> _flyweights = new Dictionary<TKey, TFlyweight>();

        public TFlyweight GetFlyweight(TKey key, Func<TFlyweight> createFunc)
        {
            if (!_flyweights.TryGetValue(key, out var flyweight))
            {
                flyweight = createFunc();
                _flyweights[key] = flyweight;
            }
            return flyweight;
        }

        public int Count => _flyweights.Count;
    }

    public class TreeFlyweight : IFlyweight
    {
        public string Mesh;
        public string Texture;

        public void Operation(object extrinsicState)
        {
            // Use extrinsicState for position, scale, etc.
            Console.WriteLine($"Drawing tree with mesh {Mesh}, texture {Texture}, at {extrinsicState}");
        }
    }
}