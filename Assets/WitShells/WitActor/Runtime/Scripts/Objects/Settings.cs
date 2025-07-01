namespace WitShells.WitActor
{
    using UnityEngine;

    /// <summary>
    /// Settings for the WitActor component.
    /// </summary>
    [CreateAssetMenu(fileName = "WitActorSettings", menuName = "WitShells/WitActor/Settings", order = 1)]
    public class Settings : ScriptableObject
    {
        public static Settings Instance
        {
            get
            {
                return Resources.Load<Settings>("WitActorSettings") ?? CreateInstance<Settings>();
            }
        }

        [Header("Footstep Sound Sfx")]
        public bool enableFootstepSound = true;

    }
}