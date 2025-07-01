using UnityEngine;

namespace WitShells.WitActor
{
    public class LocoMotion : StateMachineBehaviour
    {
        public float walkStepInterval = 0.5f; // Time between each footstep sound while walking
        public float runStepInterval = 0.3f; // Time between each footstep sound while running

        private float stepTimer;
        // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            stepTimer = 0f;
        }

        // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
        override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            float speed = animator.GetFloat("Speed");
            if (speed > 0)
            {
                // Determine the step interval based on the speed
                float stepInterval = Mathf.Lerp(walkStepInterval, runStepInterval, Mathf.InverseLerp(0.5f, 1f, speed));

                if (speed > .2f)
                {
                    // Update the step timer
                    stepTimer += Time.deltaTime;

                    // Check if it's time to play a footstep sound
                    if (stepTimer >= stepInterval)
                    {
                        // Play footstep sound
                        SoundSfx.Instance.PlayFootStep(animator.transform.position);
                        stepTimer = 0f; // Reset the timer
                    }
                }
            }
        }

        // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
        override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {

        }

        // OnStateMove is called right after Animator.OnAnimatorMove()
        override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // Implement code that processes and affects root motion
        }

        // OnStateIK is called right after Animator.OnAnimatorIK()
        override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // Implement code that sets up animation IK (inverse kinematics)
        }
    }
}