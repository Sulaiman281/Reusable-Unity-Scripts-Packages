using System;
using UnityEngine;

namespace WitShells.DialogsManager
{
    /// <summary>
    /// Represents a single dialog entry with content, audio, and metadata.
    /// Create via Assets > Create > WitShells > Dialogs Manager > Dialog Object.
    /// </summary>
    [CreateAssetMenu(fileName = "New Dialog", menuName = "WitShells/Dialogs Manager/Dialog Object")]
    public class DialogObject : ScriptableObject
    {
        #region Serialized Fields

        [Header("Dialog Content")]
        [Tooltip("The title or speaker name for this dialog.")]
        [SerializeField] private string title;

        [Tooltip("The main content/text of the dialog.")]
        [SerializeField, TextArea(3, 10)] private string content;

        [Header("Audio")]
        [Tooltip("Optional audio clip for this dialog.")]
        [SerializeField] private AudioClip audio;

        [Header("Display Settings")]
        [Tooltip("How long to display this dialog (in seconds). Use 0 for auto (based on audio length or content length).")]
        [SerializeField, Min(0)] private float displayDuration = 0f;

        [Tooltip("Optional character typing speed for text animation (characters per second). Use 0 to disable.")]
        [SerializeField, Min(0)] private float typingSpeed = 0f;

        [Header("Metadata")]
        [Tooltip("Optional unique identifier for this dialog.")]
        [SerializeField] private string dialogId;

        [Tooltip("Optional tags for filtering or categorizing dialogs.")]
        [SerializeField] private string[] tags = Array.Empty<string>();

        [Header("Visual")]
        [Tooltip("Optional portrait/avatar image for the speaker.")]
        [SerializeField] private Sprite portrait;

        [Tooltip("Optional emotion/mood indicator.")]
        [SerializeField] private DialogEmotion emotion = DialogEmotion.Neutral;

        #endregion

        #region Properties

        /// <summary>
        /// The title or speaker name for this dialog.
        /// </summary>
        public string Title
        {
            get => title;
            set => title = value;
        }

        /// <summary>
        /// The main content/text of the dialog.
        /// </summary>
        public string Content
        {
            get => content;
            set => content = value;
        }

        /// <summary>
        /// Optional audio clip for this dialog.
        /// </summary>
        public AudioClip Audio
        {
            get => audio;
            set => audio = value;
        }

        /// <summary>
        /// The duration to display this dialog.
        /// Returns audio length if available, otherwise calculated from content length.
        /// </summary>
        public float DisplayDuration
        {
            get
            {
                if (displayDuration > 0)
                    return displayDuration;

                if (audio != null)
                    return audio.length;

                // Estimate based on content length (~150 words per minute reading speed)
                if (!string.IsNullOrEmpty(content))
                {
                    int wordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                    return Mathf.Max(2f, wordCount / 2.5f);
                }

                return 2f; // Default minimum duration
            }
            set => displayDuration = value;
        }

        /// <summary>
        /// Character typing speed for text animation (characters per second).
        /// </summary>
        public float TypingSpeed
        {
            get => typingSpeed;
            set => typingSpeed = value;
        }

        /// <summary>
        /// Optional unique identifier for this dialog.
        /// </summary>
        public string DialogId
        {
            get => dialogId;
            set => dialogId = value;
        }

        /// <summary>
        /// Optional tags for filtering or categorizing dialogs.
        /// </summary>
        public string[] Tags => tags;

        /// <summary>
        /// Optional portrait/avatar image for the speaker.
        /// </summary>
        public Sprite Portrait
        {
            get => portrait;
            set => portrait = value;
        }

        /// <summary>
        /// Optional emotion/mood indicator.
        /// </summary>
        public DialogEmotion Emotion
        {
            get => emotion;
            set => emotion = value;
        }

        /// <summary>
        /// Whether this dialog has audio content.
        /// </summary>
        public bool HasAudio => audio != null;

        /// <summary>
        /// Whether this dialog has text content.
        /// </summary>
        public bool HasContent => !string.IsNullOrEmpty(content);

        /// <summary>
        /// The length of the audio clip in seconds, or 0 if no audio.
        /// </summary>
        public float AudioLength => audio != null ? audio.length : 0f;

        #endregion

        #region Public Methods

        /// <summary>
        /// Checks if this dialog has a specific tag.
        /// </summary>
        /// <param name="tag">The tag to check for.</param>
        /// <returns>True if the dialog has the tag.</returns>
        public bool HasTag(string tag)
        {
            if (tags == null || tags.Length == 0 || string.IsNullOrEmpty(tag))
                return false;

            foreach (var t in tags)
            {
                if (string.Equals(t, tag, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the estimated reading time for the content in seconds.
        /// </summary>
        public float GetEstimatedReadingTime()
        {
            if (string.IsNullOrEmpty(content))
                return 0f;

            int wordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            return wordCount / 2.5f; // ~150 words per minute
        }

        /// <summary>
        /// Gets the time required to type out the content at the specified typing speed.
        /// </summary>
        public float GetTypingDuration()
        {
            if (typingSpeed <= 0 || string.IsNullOrEmpty(content))
                return 0f;

            return content.Length / typingSpeed;
        }

        #endregion

        #region Editor Methods

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-generate ID from asset name if empty
            if (string.IsNullOrEmpty(dialogId))
            {
                dialogId = name;
            }
        }
#endif

        #endregion
    }

    /// <summary>
    /// Emotion/mood indicators for dialogs.
    /// </summary>
    public enum DialogEmotion
    {
        Neutral,
        Happy,
        Sad,
        Angry,
        Surprised,
        Confused,
        Scared,
        Excited,
        Thoughtful,
        Serious
    }
}