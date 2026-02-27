namespace WitShells.DesignPatterns.Core
{
    /// <summary>
    /// Defines the contract for the <b>Prototype</b> pattern.
    /// Classes implementing this interface are responsible for producing
    /// a deep copy of themselves, allowing the creation of new objects
    /// by cloning an existing configured instance rather than constructing
    /// one from scratch.
    /// </summary>
    /// <typeparam name="T">The type of the object being cloned.</typeparam>
    /// <remarks>
    /// Useful when object creation is expensive and you have an existing instance
    /// whose state you want to duplicate (e.g. prefab-like configurations, level templates).
    /// </remarks>
    public interface IPrototype<T>
    {
        /// <summary>
        /// Creates a deep copy of the current instance.
        /// </summary>
        /// <returns>A new instance that is a copy of the current instance.</returns>
        T Clone();
    }
}