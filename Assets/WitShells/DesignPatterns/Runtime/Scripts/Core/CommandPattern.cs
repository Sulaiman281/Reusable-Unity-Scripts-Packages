namespace WitShells.DesignPatterns.Core
{
    using System;
    using System.Collections.Generic;

    // Command interface
    public interface ICommand
    {
        void Execute();
        void Undo();
    }

    // Generic invoker for commands
    public class CommandInvoker
    {
        private readonly Stack<ICommand> _commandHistory = new Stack<ICommand>();

        public void ExecuteCommand(ICommand command)
        {
            command.Execute();
            _commandHistory.Push(command);
        }

        public void UndoLastCommand()
        {
            if (_commandHistory.Count > 0)
            {
                var command = _commandHistory.Pop();
                command.Undo();
            }
        }
    }

    // Example command implementations
    public class PrintCommand : ICommand
    {
        private readonly string _message;

        public PrintCommand(string message)
        {
            _message = message;
        }

        public void Execute()
        {
            Console.WriteLine(_message);
        }

        public void Undo()
        {
            Console.WriteLine($"Undo: {_message}");
        }
    }
}