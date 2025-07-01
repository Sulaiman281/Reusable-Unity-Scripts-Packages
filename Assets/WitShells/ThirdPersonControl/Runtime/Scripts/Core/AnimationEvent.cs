namespace WitShells.ThirdPersonControl
{
    using UnityEngine;

    public class AnimationEvent : MonoBehaviour
    {
        public void OnFootStep(AnimationEvent animationEvent)
        {
            SoundSfxObject.Instance.PlayFootStep(transform.position);
        }

        public void OnLand(AnimationEvent animationEvent)
        {
            SoundSfxObject.Instance.PlayLandSound(transform.position);
        }
    }
}