using UnityEngine;
using UnityEngine.AI;

namespace WitShells.WitActor
{
    public enum FollowState
    {
        Idle,
        Walking,
        Running,
    }

    public class FollowStateBehavior : ABehaviorState<FollowState>
    {
        public const float stopDistance = 1.5f;
        public const float walkSpeed = 2.0f;
        public const float runSpeed = 4.0f;
        public const float waitFromIdleToWalk = 0.5f;

        public Transform target;
        public string animatorParameterBlend = "Speed";

        public FollowStateBehavior(Animator animator, NavMeshAgent agent, FollowState initialState)
            : base(animator, agent, initialState)
        {
            metaTags = new[] { "follow", "walk", "run", "chase", "pursue" };
        }

        public override void OnEnter()
        {
            base.OnEnter();
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
        }
    }
}