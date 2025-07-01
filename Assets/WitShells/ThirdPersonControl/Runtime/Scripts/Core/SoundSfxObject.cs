namespace WitShells.ThirdPersonControl
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "SoundSfxObject", menuName = "WitShells/ThirdPersonControl/SoundSfxObject")]
    public class SoundSfxObject : ScriptableObject
    {
        public static SoundSfxObject Instance
        {
            get
            {
                return Resources.Load<SoundSfxObject>("SoundSfxObject") ?? CreateInstance<SoundSfxObject>();
            }
        }

        [Range(0, 1)]
        [SerializeField] private float volume = 1.0f;

        [Header("Footstep Sounds")]
        public AudioClip[] FootstepSounds;
        public AudioClip landSound;

        public void PlayFootStep(Vector3 position)
        {
            if (FootstepSounds.Length == 0) return;

            int randomIndex = Random.Range(0, FootstepSounds.Length);
            AudioClip clip = FootstepSounds[randomIndex];
            AudioSource.PlayClipAtPoint(clip, position, volume);
        }

        public void PlayLandSound(Vector3 position)
        {
            if (landSound == null) return;
            AudioSource.PlayClipAtPoint(landSound, position, volume);
        }
    }
}