namespace WitShells.DesignPatterns.Core
{
    public interface IPrototype<T>
    {
        /// <summary>
        /// Creates a deep copy of the current instance.
        /// </summary>
        /// <returns>A new instance that is a copy of the current instance.</returns>
        T Clone();
    }
}