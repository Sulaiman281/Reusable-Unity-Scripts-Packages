namespace WitShells.DesignPatterns.Core
{
    using System;
    using System.Collections.Generic;

    // Generic State interface
    public interface IState
    {
        void Enter();
        void Execute();
        void Exit();
    }

    // Generic State Machine
    public class StateMachine
    {
        private IState _currentState;

        public void ChangeState(IState newState)
        {
            if (_currentState != null)
                _currentState.Exit();

            _currentState = newState;

            if (_currentState != null)
                _currentState.Enter();
        }

        public void Update()
        {
            _currentState?.Execute();
        }
    }

    // Example usage:
    public class IdleState : IState
    {
        public void Enter() { /* Enter logic */ }
        public void Execute() { /* Idle logic */ }
        public void Exit() { /* Exit logic */ }
    }

    public class MoveState : IState
    {
        public void Enter() { /* Enter logic */ }
        public void Execute() { /* Move logic */ }
        public void Exit() { /* Exit logic */ }
    }
}