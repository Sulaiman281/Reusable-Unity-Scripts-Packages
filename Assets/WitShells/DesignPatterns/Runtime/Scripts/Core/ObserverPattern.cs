namespace WitShells.DesignPatterns.Core
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A lightweight, generic implementation of the <b>Observer</b> pattern.
    /// Maintains a list of subscriber callbacks and notifies them all when a value is published.
    /// Prefer this over C# events when you need runtime subscribe/unsubscribe with no delegate leak risk,
    /// or when you want to store the observer list as a field rather than as a static event.
    /// </summary>
    /// <typeparam name="T">The type of data passed to each observer when notified.</typeparam>
    /// <example>
    /// <code>
    /// var onHealthChanged = new ObserverPattern&lt;int&gt;();
    /// onHealthChanged.Subscribe(hp =&gt; Debug.Log($"HP: {hp}"));
    /// onHealthChanged.NotifyObservers(50);
    /// </code>
    /// </example>
    public class ObserverPattern<T>
    {
        private readonly List<Action<T>> _observers = new List<Action<T>>();

        /// <summary>
        /// Registers a callback as an observer. Duplicate subscriptions are ignored.
        /// </summary>
        /// <param name="observer">The callback to invoke when a notification is broadcast.</param>
        public void Subscribe(Action<T> observer)
        {
            if (!_observers.Contains(observer))
                _observers.Add(observer);
        }

        /// <summary>
        /// Removes a previously registered observer callback.
        /// </summary>
        /// <param name="observer">The callback to remove.</param>
        public void Unsubscribe(Action<T> observer)
        {
            if (_observers.Contains(observer))
                _observers.Remove(observer);
        }

        /// <summary>
        /// Invokes all registered observer callbacks with the supplied value.
        /// Null callbacks are skipped safely.
        /// </summary>
        /// <param name="value">The data to broadcast to every observer.</param>
        public void NotifyObservers(T value)
        {
            foreach (var observer in _observers)
            {
                observer?.Invoke(value);
            }
        }
    }
}