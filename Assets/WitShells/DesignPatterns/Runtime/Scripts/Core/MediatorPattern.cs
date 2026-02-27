namespace WitShells.DesignPatterns.Core
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Defines the contract for the <b>Mediator</b> pattern.
    /// The mediator decouples components (colleagues) by acting as a central hub for
    /// communication — instead of referencing each other directly, components send
    /// messages through the mediator.
    /// </summary>
    public interface IMediator
    {
        /// <summary>
        /// Broadcasts an event to all subscribers registered under <paramref name="eventKey"/>.
        /// </summary>
        /// <param name="sender">The object raising the event.</param>
        /// <param name="eventKey">A string identifier for the event channel (e.g. <c>"OnPlayerDied"</c>).</param>
        /// <param name="data">Optional payload accompanying the event.</param>
        void Notify(object sender, string eventKey, object data = null);
    }

    /// <summary>
    /// A concrete, event-bus style <b>Mediator</b> implementation.
    /// Components subscribe to named event channels and are notified whenever
    /// another component calls <see cref="Notify"/>.
    /// </summary>
    /// <remarks>
    /// Useful for cross-system communication (e.g. UI reacting to game-world events)
    /// without creating direct dependencies between the systems.
    /// </remarks>
    /// <example>
    /// <code>
    /// var mediator = new Mediator();
    /// mediator.Subscribe("OnScoreChanged", (sender, data) =&gt; Debug.Log($"Score: {data}"));
    /// mediator.Notify(this, "OnScoreChanged", 42);
    /// </code>
    /// </example>
    public class Mediator : IMediator
    {
        private readonly Dictionary<string, List<Action<object, object>>> _subscribers = new();

        /// <summary>
        /// Subscribes <paramref name="callback"/> to the named event channel.
        /// </summary>
        /// <param name="eventKey">The event channel identifier.</param>
        /// <param name="callback">Callback invoked with (sender, data) when the event fires.</param>
        public void Subscribe(string eventKey, Action<object, object> callback)
        {
            if (!_subscribers.ContainsKey(eventKey))
                _subscribers[eventKey] = new List<Action<object, object>>();
            _subscribers[eventKey].Add(callback);
        }

        /// <summary>
        /// Removes <paramref name="callback"/> from the named event channel.
        /// </summary>
        /// <param name="eventKey">The event channel identifier.</param>
        /// <param name="callback">The callback to remove.</param>
        public void Unsubscribe(string eventKey, Action<object, object> callback)
        {
            if (_subscribers.ContainsKey(eventKey))
                _subscribers[eventKey].Remove(callback);
        }

        /// <inheritdoc />
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