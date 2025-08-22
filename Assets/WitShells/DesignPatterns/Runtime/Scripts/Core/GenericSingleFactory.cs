using System;
using System.Collections.Generic;

namespace WitShells.DesignPatterns.Core
{
    public abstract class GenericSingleFactory<TKey, TProduct>
    {
        private readonly Dictionary<TKey, Func<TProduct>> _creators = new Dictionary<TKey, Func<TProduct>>();
        private readonly Dictionary<TKey, TProduct> _instances = new Dictionary<TKey, TProduct>();

        public void Register(TKey key, Func<TProduct> creator)
        {
            _creators[key] = creator;
        }

        public bool IsRegistered(TKey key)
        {
            return _creators.ContainsKey(key);
        }

        public TProduct Get(TKey key)
        {
            if (_instances.TryGetValue(key, out var instance))
            {
                return instance;
            }

            if (_creators.TryGetValue(key, out var creator))
            {
                instance = creator();
                _instances[key] = instance;
                return instance;
            }

            throw new KeyNotFoundException($"No creator registered for key: {key}");
        }
    }
}