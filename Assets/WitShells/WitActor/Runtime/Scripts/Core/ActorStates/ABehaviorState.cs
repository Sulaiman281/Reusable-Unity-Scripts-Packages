using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace WitShells.WitActor
{
    [Serializable]
    public abstract class ABehaviorState<TState> : IState where TState : System.Enum
    {
        protected Animator animator;
        protected NavMeshAgent agent;
        public UnityEvent<TState> OnStateChanged;
        public TState state;
        public string[] metaTags;

        public ABehaviorState(Animator animator, NavMeshAgent agent, TState initialState)
        {
            this.animator = animator;
            this.agent = agent;
            this.state = initialState;
            OnEnter();
        }

        public virtual void OnTransitionTo(TState newState)
        {
            if (state.Equals(newState)) return;

            OnExit();
            state = newState;
            OnEnter();
            OnStateChanged?.Invoke(state);
        }

        /// <summary>
        /// Called when the state is entered.
        /// </summary>
        public virtual void OnEnter() { }

        /// <summary>
        /// Called when the state is exited.
        /// </summary>
        public virtual void OnExit() { }

        /// <summary>
        /// Called every frame while the state is active.
        /// </summary>
        public virtual void OnUpdate() { }

        public int GetMatchScore(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt)) return 0;
            int score = 0;
            string lowerPrompt = prompt.ToLowerInvariant();
            foreach (var tag in metaTags)
            {
                if (!string.IsNullOrWhiteSpace(tag) && lowerPrompt.Contains(tag.ToLowerInvariant()))
                    score++;
            }
            return score;
        }
    }
}