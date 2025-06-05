namespace WitShells.DesignPatterns.Core
{
    using System;

    public abstract class TemplateMethod<TInput, TResult>
    {
        // The template method defines the skeleton of the algorithm
        public TResult Execute(TInput input)
        {
            PreProcess(input);
            TResult result = Process(input);
            PostProcess(result);
            return result;
        }

        // Steps that can be overridden by subclasses
        protected abstract void PreProcess(TInput input);


        // The main step that must be implemented by subclasses
        protected abstract TResult Process(TInput input);

        protected abstract void PostProcess(TResult result);
    }

    // Example: A template for parsing and validating user input
    public class UserInputTemplate : TemplateMethod<string, int>
    {
        protected override void PreProcess(string input)
        {
            Console.WriteLine($"Pre-processing input: {input}");
        }

        protected override int Process(string input)
        {
            Console.WriteLine("Processing input...");
            if (int.TryParse(input, out int value))
                return value;
            throw new ArgumentException("Input is not a valid integer.");
        }

        protected override void PostProcess(int result)
        {
            Console.WriteLine($"Post-processing result: {result}");
        }
    }
}