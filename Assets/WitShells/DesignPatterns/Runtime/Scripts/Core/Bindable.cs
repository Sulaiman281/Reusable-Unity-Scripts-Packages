using UnityEngine.Events;

namespace WitShells.DesignPatterns.Core
{
    /// <summary>
    /// A reactive property wrapper that automatically fires a <see cref="UnityEvent{T}"/>
    /// whenever its value changes. Use this to implement the <b>Observer / Data-Binding</b>
    /// pattern without manual event wiring.
    /// </summary>
    /// <typeparam name="T">The type of the wrapped value.</typeparam>
    /// <example>
    /// <code>
    /// var health = new Bindable&lt;int&gt;(100);
    /// health.OnValueChanged.AddListener(v =&gt; Debug.Log($"Health changed to {v}"));
    /// health.Value = 50; // fires the event automatically
    /// </code>
    /// </example>
    public class Bindable<T>
    {
        private T _value;

        /// <summary>Event fired every time <see cref="Value"/> is assigned a different value.</summary>
        public UnityEvent<T> OnValueChanged;

        /// <summary>
        /// Gets or sets the wrapped value.
        /// The setter only fires <see cref="OnValueChanged"/> when the new value differs from the current one.
        /// </summary>
        public T Value
        {
            get => _value;
            set
            {
                if (!Equals(_value, value))
                {
                    _value = value;
                    OnValueChanged?.Invoke(_value);
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="Bindable{T}"/> with an optional initial value.
        /// </summary>
        /// <param name="initialValue">The starting value. Defaults to <c>default(T)</c>.</param>
        public Bindable(T initialValue = default)
        {
            _value = initialValue;
            OnValueChanged = new UnityEvent<T>();
        }
    }
}