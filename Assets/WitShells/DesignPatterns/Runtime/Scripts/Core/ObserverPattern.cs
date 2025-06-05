namespace WitShells.DesignPatterns.Core
{
    using System;
    using System.Collections.Generic;

    public class ObserverPattern<T>
    {
        private readonly List<Action<T>> _observers = new List<Action<T>>();

        public void Subscribe(Action<T> observer)
        {
            if (!_observers.Contains(observer))
                _observers.Add(observer);
        }

        public void Unsubscribe(Action<T> observer)
        {
            if (_observers.Contains(observer))
                _observers.Remove(observer);
        }

        protected void NotifyObservers(T value)
        {
            foreach (var observer in _observers)
            {
                observer?.Invoke(value);
            }
        }
    }
}