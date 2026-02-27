namespace WitShells.DesignPatterns.Core
{

    using System;

    /// <summary>
    /// Defines the contract for the <b>Strategy</b> pattern.
    /// Each strategy encapsulates one interchangeable algorithm that transforms a
    /// <typeparamref name="TContext"/> value into a <typeparamref name="TResult"/> value.
    /// </summary>
    /// <typeparam name="TContext">Input type passed to the algorithm.</typeparam>
    /// <typeparam name="TResult">Output type produced by the algorithm.</typeparam>
    public interface IStrategy<TContext, TResult>
    {
        /// <summary>Executes the algorithm using the supplied context.</summary>
        /// <param name="context">The input data for the algorithm.</param>
        /// <returns>The result produced by the algorithm.</returns>
        TResult Execute(TContext context);
    }

    /// <summary>
    /// The <b>Context</b> class in the Strategy pattern.
    /// Holds a reference to the current strategy and delegates execution to it.
    /// The active strategy can be swapped at runtime via <see cref="SetStrategy"/>.
    /// </summary>
    /// <typeparam name="TContext">Input type for the strategy.</typeparam>
    /// <typeparam name="TResult">Output type returned by the strategy.</typeparam>
    /// <example>
    /// <code>
    /// var ctx = new StrategyContext&lt;(int,int), int&gt;(new AddStrategy());
    /// int sum = ctx.ExecuteStrategy((3, 4));  // 7
    /// ctx.SetStrategy(new MultiplyStrategy());
    /// int product = ctx.ExecuteStrategy((3, 4));  // 12
    /// </code>
    /// </example>
    public class StrategyContext<TContext, TResult>
    {
        private IStrategy<TContext, TResult> _strategy;

        /// <summary>Initialises the context with a starting strategy.</summary>
        /// <param name="initialStrategy">The strategy to use until replaced.</param>
        public StrategyContext(IStrategy<TContext, TResult> initialStrategy)
        {
            _strategy = initialStrategy;
        }

        /// <summary>Replaces the current strategy at runtime.</summary>
        /// <param name="strategy">The new strategy to use from this point on.</param>
        public void SetStrategy(IStrategy<TContext, TResult> strategy)
        {
            _strategy = strategy;
        }

        /// <summary>Delegates execution to the currently active strategy.</summary>
        /// <param name="context">Input data for the strategy.</param>
        /// <returns>The result produced by the active strategy.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no strategy has been set.</exception>
        public TResult ExecuteStrategy(TContext context)
        {
            if (_strategy == null)
                throw new InvalidOperationException("Strategy not set.");
            return _strategy.Execute(context);
        }
    }

    /// <summary>Example strategy that adds two integers together.</summary>
    public class AddStrategy : IStrategy<(int a, int b), int>
    {
        /// <inheritdoc />
        public int Execute((int a, int b) context)
        {
            return context.a + context.b;
        }
    }

    /// <summary>Example strategy that multiplies two integers together.</summary>
    public class MultiplyStrategy : IStrategy<(int a, int b), int>
    {
        /// <inheritdoc />
        public int Execute((int a, int b) context)
        {
            return context.a * context.b;
        }
    }
}