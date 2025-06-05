namespace WitShells.DesignPatterns.Core
{

    using System;

    // Strategy interface with generic context and result
    public interface IStrategy<TContext, TResult>
    {
        TResult Execute(TContext context);
    }

    // Strategy context class
    public class StrategyContext<TContext, TResult>
    {
        private IStrategy<TContext, TResult> _strategy;

        public StrategyContext(IStrategy<TContext, TResult> initialStrategy)
        {
            _strategy = initialStrategy;
        }

        public void SetStrategy(IStrategy<TContext, TResult> strategy)
        {
            _strategy = strategy;
        }

        public TResult ExecuteStrategy(TContext context)
        {
            if (_strategy == null)
                throw new InvalidOperationException("Strategy not set.");
            return _strategy.Execute(context);
        }
    }

    // Example strategies
    public class AddStrategy : IStrategy<(int a, int b), int>
    {
        public int Execute((int a, int b) context)
        {
            return context.a + context.b;
        }
    }

    public class MultiplyStrategy : IStrategy<(int a, int b), int>
    {
        public int Execute((int a, int b) context)
        {
            return context.a * context.b;
        }
    }
}