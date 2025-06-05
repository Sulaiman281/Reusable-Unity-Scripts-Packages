namespace WitShells.DesignPatterns.Core
{
    using System;
    using System.Collections.Generic;

    // Mediator interface
    public interface IMediator
    {
        void Notify(object sender, string eventKey, object data = null);
    }

    // Generic Mediator implementation
    public class Mediator : IMediator
    {
        private readonly Dictionary<string, List<Action<object, object>>> _subscribers = new();

        // Subscribe to an event
        public void Subscribe(string eventKey, Action<object, object> callback)
        {
            if (!_subscribers.ContainsKey(eventKey))
                _subscribers[eventKey] = new List<Action<object, object>>();
            _subscribers[eventKey].Add(callback);
        }

        // Unsubscribe from an event
        public void Unsubscribe(string eventKey, Action<object, object> callback)
        {
            if (_subscribers.ContainsKey(eventKey))
                _subscribers[eventKey].Remove(callback);
        }

        // Notify all subscribers of an event
        public void Notify(object sender, string eventKey, object data = null)
        {
            if (_subscribers.TryGetValue(eventKey, out var callbacks))
            {
                foreach (var callback in callbacks)
                    callback(sender, data);
            }
        }
    }
}