namespace WitShells.DesignPatterns.Core
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Defines the contract for a single state in the <b>State Machine</b> pattern.
    /// Each state owns its own entry, update, and exit logic, keeping behaviour
    /// self-contained and easy to extend.
    /// </summary>
    public interface IState
    {
        /// <summary>Called once when the state machine transitions <i>into</i> this state.</summary>
        void Enter();

        /// <summary>Called every frame (or update tick) while this state is active.</summary>
        void Execute();

        /// <summary>Called once when the state machine transitions <i>out of</i> this state.</summary>
        void Exit();
    }

    /// <summary>
    /// A simple, non-MonoBehaviour <b>State Machine</b> that drives an <see cref="IState"/>.
    /// Call <see cref="ChangeState"/> to transition and <see cref="Update"/> each frame to tick the active state.
    /// </summary>
    /// <remarks>
    /// Attach the state machine as a field on a MonoBehaviour and call <c>Update()</c>  from
    /// <c>MonoBehaviour.Update()</c> to drive it. For hierarchical or concurrent states consider
    /// extending this class.
    /// </remarks>
    public class StateMachine
    {
        private IState _currentState;

        /// <summary>
        /// Transitions to a new state: exits the current state (if any), then enters the new state.
        /// </summary>
        /// <param name="newState">The state to transition into. Pass <c>null</c> to leave no active state.</param>
        public void ChangeState(IState newState)
        {
            if (_currentState != null)
                _currentState.Exit();

            _currentState = newState;

            if (_currentState != null)
                _currentState.Enter();
        }

        /// <summary>
        /// Ticks the currently active state by calling <see cref="IState.Execute"/>.
        /// Call this from <c>MonoBehaviour.Update()</c> every frame.
        /// </summary>
        public void Update()
        {
            _currentState?.Execute();
        }
    }

    /// <summary>
    /// Example <see cref="IState"/> — represents an entity standing still.
    /// Replace the placeholder comments with your own idle behaviour.
    /// </summary>
    public class IdleState : IState
    {
        /// <inheritdoc />
        public void Enter() { /* Enter logic */ }
        /// <inheritdoc />
        public void Execute() { /* Idle logic */ }
        /// <inheritdoc />
        public void Exit() { /* Exit logic */ }
    }

    /// <summary>
    /// Example <see cref="IState"/> — represents an entity in motion.
    /// Replace the placeholder comments with your own movement behaviour.
    /// </summary>
    public class MoveState : IState
    {
        /// <inheritdoc />
        public void Enter() { /* Enter logic */ }
        /// <inheritdoc />
        public void Execute() { /* Move logic */ }
        /// <inheritdoc />
        public void Exit() { /* Exit logic */ }
    }
}