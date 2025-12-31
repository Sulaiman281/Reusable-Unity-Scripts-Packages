namespace WitShells.ThirdPersonControl
{
    using UnityEngine;

    /// <summary>
    /// Handles animation events for the third-person character.
    /// Attach to the GameObject with the Animator component.
    /// </summary>
    [AddComponentMenu("WitShells/Third Person Control/Animation Event Handler")]
    public class AnimationEventHandler : MonoBehaviour
    {
        [Header("Sound Settings")]
        [Tooltip("Reference to the sound effects object. If null, will try to get from ThirdPersonSettings.")]
        [SerializeField] private SoundSfxObject soundEffects;

        [Tooltip("Reference to the ThirdPersonSettings. Used if soundEffects is null.")]
        [SerializeField] private ThirdPersonSettings settings;

        [Header("Audio Source (Optional)")]
        [Tooltip("Optional AudioSource for more control. If null, uses PlayClipAtPoint.")]
        [SerializeField] private AudioSource audioSource;

        private SoundSfxObject EffectiveSoundEffects
        {
            get
            {
                if (soundEffects != null) return soundEffects;
                if (settings != null) return settings.SoundEffects;
                return null;
            }
        }

        /// <summary>
        /// Called by animation event on footstep frames.
        /// </summary>
        public void OnFootStep(AnimationEvent animationEvent)
        {
            SoundSfxObject sfx = EffectiveSoundEffects;
            if (sfx != null)
            {
                sfx.PlayFootStep(transform.position);
            }
        }

        /// <summary>
        /// Called by animation event when landing.
        /// </summary>
        public void OnLand(AnimationEvent animationEvent)
        {
            SoundSfxObject sfx = EffectiveSoundEffects;
            if (sfx != null)
            {
                sfx.PlayLandSound(transform.position);
            }
        }

        /// <summary>
        /// Called by animation event when jumping.
        /// </summary>
        public void OnJump(AnimationEvent animationEvent)
        {
            SoundSfxObject sfx = EffectiveSoundEffects;
            if (sfx != null)
            {
                sfx.PlayJumpSound(transform.position);
            }
        }

        /// <summary>
        /// Overload to handle PlayerInput SendMessages callback.
        /// This prevents MissingMethodException when PlayerInput broadcasts OnJump.
        /// </summary>
        public void OnJump(UnityEngine.InputSystem.InputValue value)
        {
            // Intentionally empty - input handling is done by ThirdPersonInput
        }

        /// <summary>
        /// Generic method to play any sound from animation events.
        /// Pass the AudioClip as the objectReferenceParameter in the animation event.
        /// </summary>
        public void PlaySound(AnimationEvent animationEvent)
        {
            if (animationEvent.objectReferenceParameter is AudioClip clip)
            {
                SoundSfxObject sfx = EffectiveSoundEffects;
                float volume = sfx != null ? sfx.Volume : 1f;

                if (audioSource != null)
                {
                    audioSource.PlayOneShot(clip, volume);
                }
                else
                {
                    AudioSource.PlayClipAtPoint(clip, transform.position, volume);
                }
            }
        }

        /// <summary>
        /// Set the sound effects object at runtime.
        /// </summary>
        public void SetSoundEffects(SoundSfxObject sfx)
        {
            soundEffects = sfx;
        }

        /// <summary>
        /// Set the settings object at runtime.
        /// </summary>
        public void SetSettings(ThirdPersonSettings newSettings)
        {
            settings = newSettings;
        }
    }
}