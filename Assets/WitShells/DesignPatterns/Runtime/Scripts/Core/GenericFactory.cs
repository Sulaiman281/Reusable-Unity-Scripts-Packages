namespace WitShells.DesignPatterns.Core
{
    using System;
    using System.Collections.Generic;

    public class GenericFactory<TKey, TProduct>
    {
        private readonly Dictionary<TKey, Func<TProduct>> _creators = new Dictionary<TKey, Func<TProduct>>();

        public void Register(TKey key, Func<TProduct> creator)
        {
            if (!_creators.ContainsKey(key))
                _creators.Add(key, creator);
        }

        public TProduct Create(TKey key)
        {
            if (_creators.TryGetValue(key, out var creator))
                return creator();
            throw new ArgumentException($"No creator registered for key: {key}");
        }
    }
}