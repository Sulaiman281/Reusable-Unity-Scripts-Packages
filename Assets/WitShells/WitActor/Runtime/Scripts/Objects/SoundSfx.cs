namespace WitShells.WitActor
{
    using UnityEngine;

    /// <summary>
    /// Settings for the WitActor component.
    /// </summary>
    [CreateAssetMenu(fileName = "WitActorSoundSfx", menuName = "WitShells/WitActor/SoundSfx", order = 1)]
    public class SoundSfx : ScriptableObject
    {
        public static SoundSfx Instance
        {
            get
            {
                return Resources.Load<SoundSfx>("WitActorSoundSfx") ?? CreateInstance<SoundSfx>();
            }
        }


        [Header("Footstep Sound Sfx")]
        public AudioClip[] footstepSounds;
        public AudioClip landFootstepSound;

        public void PlayFootStep(Vector3 position)
        {
            if (!Settings.Instance.enableFootstepSound) return;
            if (footstepSounds == null || footstepSounds.Length == 0)
            {
                Debug.LogWarning("No footstep sounds available to play.");
                return;
            }

            AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
            AudioSource.PlayClipAtPoint(clip, position);
        }

        public void OnLandFootstep(Vector3 position)
        {
            if (!Settings.Instance.enableFootstepSound) return;
            if (landFootstepSound == null)
            {
                Debug.LogWarning("No land footstep sound available to play.");
                return;
            }

            AudioSource.PlayClipAtPoint(landFootstepSound, position);
        }
    }
}