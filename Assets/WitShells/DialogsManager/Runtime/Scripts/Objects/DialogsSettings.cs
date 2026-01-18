using UnityEngine;

namespace WitShells.DialogsManager
{
    /// <summary>
    /// Settings for the Dialogs Manager system.
    /// Create via Assets > Create > WitShells > Dialogs Manager > Dialogs Settings.
    /// </summary>
    [CreateAssetMenu(fileName = "Dialogs Settings", menuName = "WitShells/Dialogs Manager/Dialogs Settings")]
    public class DialogsSettings : ScriptableObject
    {

        public static DialogsSettings Instance
        {
            get
            {
                return Resources.Load<DialogsSettings>("Dialogs Settings");
            }
        }

        #region Serialized Fields

        [Header("Default Subtitle Settings")]
        [Tooltip("The default subtitle text ID to use if none is specified in the dialog.")]
        [SerializeField] private string defaultSubtitleTextId = "default_subtitle";
        [SerializeField] private bool useTypingEffect = true;
        [SerializeField, Min(0)] private float defaultTypingSpeed = 30f;
        [SerializeField, Min(0)] private float dialogActionRepeatDelay = 5.2f;
        [SerializeField] private int maxRetryAttempts = 3;

        #endregion

        #region Properties

        /// <summary>
        /// The default subtitle text ID to use if none is specified in the dialog.
        /// </summary>
        public string DefaultSubtitleTextId
        {
            get => defaultSubtitleTextId;
            set => defaultSubtitleTextId = value;
        }

        public bool UseTypingEffect
        {
            get => useTypingEffect;
            set => useTypingEffect = value;
        }

        public float DefaultTypingSpeed
        {
            get => defaultTypingSpeed;
            set => defaultTypingSpeed = value;
        }

        public float DialogActionRepeatDelay
        {
            get => dialogActionRepeatDelay;
            set => dialogActionRepeatDelay = value;
        }

        public int MaxRetryAttempts
        {
            get => maxRetryAttempts;
            set => maxRetryAttempts = value;
        }

        #endregion
    }
}