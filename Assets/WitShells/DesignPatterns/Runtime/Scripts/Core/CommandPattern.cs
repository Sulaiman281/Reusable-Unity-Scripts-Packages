namespace WitShells.DesignPatterns.Core
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Defines the contract for the <b>Command</b> pattern.
    /// Each command encapsulates a single action and its reversal, enabling
    /// undo/redo stacks, macro recording, and deferred execution.
    /// </summary>
    public interface ICommand
    {
        /// <summary>Performs the command's primary action.</summary>
        void Execute();

        /// <summary>Reverses the command's action, restoring previous state.</summary>
        void Undo();
    }

    /// <summary>
    /// The <b>Invoker</b> in the Command pattern.
    /// Executes <see cref="ICommand"/> instances and maintains a history stack
    /// that allows undoing operations in LIFO order.
    /// </summary>
    /// <remarks>
    /// Typical usage: wire UI buttons (Do / Undo) to
    /// <see cref="ExecuteCommand"/> and <see cref="UndoLastCommand"/> respectively.
    /// </remarks>
    public class CommandInvoker
    {
        private readonly Stack<ICommand> _commandHistory = new Stack<ICommand>();

        /// <summary>
        /// Executes the given command and pushes it onto the undo history stack.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        public void ExecuteCommand(ICommand command)
        {
            command.Execute();
            _commandHistory.Push(command);
        }

        /// <summary>
        /// Pops and undoes the most recently executed command.
        /// Does nothing if the history is empty.
        /// </summary>
        public void UndoLastCommand()
        {
            if (_commandHistory.Count > 0)
            {
                var command = _commandHistory.Pop();
                command.Undo();
            }
        }
    }

    /// <summary>
    /// Example <see cref="ICommand"/> implementation that prints a message to the console.
    /// <see cref="Undo"/> logs an acknowledgement that the print was undone.
    /// </summary>
    public class PrintCommand : ICommand
    {
        private readonly string _message;

        /// <summary>Creates a new <see cref="PrintCommand"/> with the given message.</summary>
        /// <param name="message">The text to print when this command is executed.</param>
        public PrintCommand(string message)
        {
            _message = message;
        }

        /// <inheritdoc />
        public void Execute()
        {
            Console.WriteLine(_message);
        }

        /// <inheritdoc />
        public void Undo()
        {
            Console.WriteLine($"Undo: {_message}");
        }
    }
}