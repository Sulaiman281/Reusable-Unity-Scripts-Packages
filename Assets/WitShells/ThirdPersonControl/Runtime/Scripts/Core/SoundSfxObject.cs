namespace WitShells.ThirdPersonControl
{
    using UnityEngine;

    /// <summary>
    /// ScriptableObject containing sound effects for the third-person controller.
    /// Create via: Create > WitShells > ThirdPersonControl > Sound Effects
    /// </summary>
    [CreateAssetMenu(fileName = "SoundSfxObject", menuName = "WitShells/ThirdPersonControl/Sound Effects")]
    public class SoundSfxObject : ScriptableObject
    {
        [Header("Volume")]
        [Range(0, 1)]
        [SerializeField] private float volume = 1.0f;

        [Header("Footstep Sounds")]
        [Tooltip("Array of footstep sound variations")]
        public AudioClip[] FootstepSounds;

        [Header("Action Sounds")]
        [Tooltip("Sound played when landing from a jump/fall")]
        public AudioClip landSound;

        [Tooltip("Sound played when jumping")]
        public AudioClip jumpSound;

        /// <summary>
        /// Master volume for all sounds.
        /// </summary>
        public float Volume
        {
            get => volume;
            set => volume = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Plays a random footstep sound at the specified position.
        /// </summary>
        public void PlayFootStep(Vector3 position)
        {
            if (FootstepSounds == null || FootstepSounds.Length == 0) return;

            int randomIndex = Random.Range(0, FootstepSounds.Length);
            AudioClip clip = FootstepSounds[randomIndex];
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, position, volume);
            }
        }

        /// <summary>
        /// Plays the landing sound at the specified position.
        /// </summary>
        public void PlayLandSound(Vector3 position)
        {
            if (landSound == null) return;
            AudioSource.PlayClipAtPoint(landSound, position, volume);
        }

        /// <summary>
        /// Plays the jump sound at the specified position.
        /// </summary>
        public void PlayJumpSound(Vector3 position)
        {
            if (jumpSound == null) return;
            AudioSource.PlayClipAtPoint(jumpSound, position, volume);
        }

        /// <summary>
        /// Plays a specific audio clip at the given position.
        /// </summary>
        public void PlayClip(AudioClip clip, Vector3 position)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position, volume);
        }

        /// <summary>
        /// Plays a specific audio clip at the given position with custom volume.
        /// </summary>
        public void PlayClip(AudioClip clip, Vector3 position, float customVolume)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position, customVolume * volume);
        }
    }
}