namespace WitShells.DesignPatterns.Core
{
    using System;

    /// <summary>
    /// Abstract base class for the <b>Template Method</b> pattern.
    /// Defines the skeleton of an algorithm in <see cref="Execute"/> (the <i>template method</i>)
    /// while delegating the concrete steps to subclasses via abstract methods.
    /// This lets subclasses change specific parts of the algorithm without altering its overall structure.
    /// </summary>
    /// <typeparam name="TInput">The type of data the algorithm operates on.</typeparam>
    /// <typeparam name="TResult">The type produced at the end of the algorithm.</typeparam>
    public abstract class TemplateMethod<TInput, TResult>
    {
        /// <summary>
        /// Runs the full algorithm: pre-process → process → post-process.
        /// Subclasses must not override this method; instead override the individual steps.
        /// </summary>
        /// <param name="input">The data to process.</param>
        /// <returns>The final result after all steps complete.</returns>
        public TResult Execute(TInput input)
        {
            PreProcess(input);
            TResult result = Process(input);
            PostProcess(result);
            return result;
        }

        /// <summary>
        /// Step 1: Preparation logic run before the main processing.
        /// Override to validate, sanitise, or log incoming data.
        /// </summary>
        /// <param name="input">The raw input before processing.</param>
        protected abstract void PreProcess(TInput input);

        /// <summary>
        /// Step 2: The core algorithm. Must be implemented by every subclass.
        /// </summary>
        /// <param name="input">The (pre-processed) input to transform.</param>
        /// <returns>The intermediate result passed to <see cref="PostProcess"/>.</returns>
        protected abstract TResult Process(TInput input);

        /// <summary>
        /// Step 3: Post-processing logic run after the main processing.
        /// Override to log, cache, or dispatch the result.
        /// </summary>
        /// <param name="result">The result produced by <see cref="Process"/>.</param>
        protected abstract void PostProcess(TResult result);
    }

    /// <summary>
    /// Example template that parses a raw string into an integer, with logging at each step.
    /// </summary>
    public class UserInputTemplate : TemplateMethod<string, int>
    {
        /// <inheritdoc />
        protected override void PreProcess(string input)
        {
            Console.WriteLine($"Pre-processing input: {input}");
        }

        /// <inheritdoc />
        protected override int Process(string input)
        {
            Console.WriteLine("Processing input...");
            if (int.TryParse(input, out int value))
                return value;
            throw new ArgumentException("Input is not a valid integer.");
        }

        /// <inheritdoc />
        protected override void PostProcess(int result)
        {
            Console.WriteLine($"Post-processing result: {result}");
        }
    }
}