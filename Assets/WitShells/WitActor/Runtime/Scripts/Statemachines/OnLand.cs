using UnityEngine;

namespace WitShells.WitActor
{
    public class OnLand : StateMachineBehaviour
    {
        // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // Play land footstep sound
            SoundSfx.Instance.OnLandFootstep(animator.transform.position);
        }

        // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
        // override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        // {
        //     // No update logic needed for landing state
        // }

        // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
        // override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        // {
        //     // set OnGround to true
        //     // No exit logic needed for landing state
        // }
    }
}