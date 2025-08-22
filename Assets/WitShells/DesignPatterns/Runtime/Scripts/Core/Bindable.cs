using UnityEngine.Events;

namespace WitShells.DesignPatterns.Core
{
    public class Bindable<T>
    {
        private T _value;
        public UnityEvent<T> OnValueChanged;

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

        public Bindable(T initialValue = default)
        {
            _value = initialValue;
            OnValueChanged = new UnityEvent<T>();
        }
    }
}