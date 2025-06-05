namespace WitShells.DesignPatterns.Core
{

    using System;
    using System.Collections.Generic;

    // Generic Object Pool
    public class ObjectPool<T> where T : class, new()
    {
        private readonly Stack<T> _pool = new Stack<T>();
        private readonly Func<T> _factoryMethod;

        public ObjectPool(Func<T> factoryMethod = null, int initialCapacity = 0)
        {
            _factoryMethod = factoryMethod ?? (() => new T());
            for (int i = 0; i < initialCapacity; i++)
            {
                _pool.Push(_factoryMethod());
            }
        }

        public T Get()
        {
            return _pool.Count > 0 ? _pool.Pop() : _factoryMethod();
        }

        public void Release(T obj)
        {
            _pool.Push(obj);
        }

        public int Count => _pool.Count;
    }
}