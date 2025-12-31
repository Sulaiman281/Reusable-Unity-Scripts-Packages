using System;
using UnityEngine;

namespace WitShells.DialogsManager
{
    /// <summary>
    /// Represents a conversation containing a sequence of dialogs.
    /// Create via Assets > Create > WitShells > Dialogs Manager > Conversation.
    /// </summary>
    [CreateAssetMenu(fileName = "New Conversation", menuName = "WitShells/Dialogs Manager/Conversation")]
    public class Conversation : ScriptableObject
    {
        #region Serialized Fields

        [Header("Conversation Info")]
        [Tooltip("The name of this conversation.")]
        [SerializeField] private string conversationName;

        [Tooltip("Optional description for this conversation.")]
        [SerializeField, TextArea(2, 4)] private string description;

        [Header("Dialogs")]
        [Tooltip("The sequence of dialogs in this conversation.")]
        [SerializeField] private DialogObject[] dialogs = Array.Empty<DialogObject>();

        [Header("Settings")]
        [Tooltip("If true, the conversation will loop back to the start after the last dialog.")]
        [SerializeField] private bool loop = false;

        #endregion

        #region Properties

        /// <summary>
        /// The name of this conversation.
        /// </summary>
        public string ConversationName
        {
            get => conversationName;
            set => conversationName = value;
        }

        /// <summary>
        /// Optional description for this conversation.
        /// </summary>
        public string Description
        {
            get => description;
            set => description = value;
        }

        /// <summary>
        /// The array of dialogs in this conversation.
        /// </summary>
        public DialogObject[] Dialogs => dialogs;

        /// <summary>
        /// The total number of dialogs in this conversation.
        /// </summary>
        public int TotalDialogs => dialogs?.Length ?? 0;

        /// <summary>
        /// The current dialog index (0-based). Returns -1 if not started.
        /// </summary>
        public int CurrentDialogIndex => currentDialogIndex;

        /// <summary>
        /// Whether the conversation will loop.
        /// </summary>
        public bool Loop
        {
            get => loop;
            set => loop = value;
        }

        /// <summary>
        /// Whether there are more dialogs to play.
        /// </summary>
        public bool HasMoreDialogs()
        {
            if (dialogs == null || dialogs.Length == 0)
                return false;

            if (loop)
                return true;

            return currentDialogIndex + 1 < dialogs.Length;
        }

        /// <summary>
        /// Whether the conversation has been started.
        /// </summary>
        public bool HasStarted => currentDialogIndex >= 0;

        /// <summary>
        /// Whether the conversation is at the last dialog.
        /// </summary>
        public bool IsAtLastDialog => currentDialogIndex >= dialogs.Length - 1;

        /// <summary>
        /// Gets the current dialog without advancing.
        /// </summary>
        public DialogObject CurrentDialog
        {
            get
            {
                if (currentDialogIndex < 0 || currentDialogIndex >= dialogs.Length)
                    return null;
                return dialogs[currentDialogIndex];
            }
        }

        #endregion

        #region Private Fields

        [NonSerialized]
        private int currentDialogIndex = -1;

        #endregion

        #region Public Methods

        /// <summary>
        /// Resets the conversation to the beginning.
        /// </summary>
        public void ResetConversation()
        {
            currentDialogIndex = -1;
        }

        /// <summary>
        /// Gets the next dialog and advances the index.
        /// </summary>
        /// <returns>The next dialog, or null if no more dialogs.</returns>
        public DialogObject GetNextDialog()
        {
            if (dialogs == null || dialogs.Length == 0)
                return null;

            if (currentDialogIndex + 1 < dialogs.Length)
            {
                currentDialogIndex++;
                return dialogs[currentDialogIndex];
            }
            else if (loop)
            {
                currentDialogIndex = 0;
                return dialogs[currentDialogIndex];
            }

            return null;
        }

        /// <summary>
        /// Gets a dialog at a specific index without changing the current index.
        /// </summary>
        /// <param name="index">The index of the dialog to get.</param>
        /// <returns>The dialog at the specified index, or null if out of range.</returns>
        public DialogObject GetDialogAt(int index)
        {
            if (dialogs == null || index < 0 || index >= dialogs.Length)
                return null;

            return dialogs[index];
        }

        /// <summary>
        /// Sets the current dialog index.
        /// </summary>
        /// <param name="index">The index to set. Use -1 to reset to beginning.</param>
        public void SetDialogIndex(int index)
        {
            currentDialogIndex = Mathf.Clamp(index, -1, dialogs.Length - 1);
        }

        /// <summary>
        /// Gets the previous dialog and moves the index back.
        /// </summary>
        /// <returns>The previous dialog, or null if at the beginning.</returns>
        public DialogObject GetPreviousDialog()
        {
            if (dialogs == null || dialogs.Length == 0)
                return null;

            if (currentDialogIndex > 0)
            {
                currentDialogIndex--;
                return dialogs[currentDialogIndex];
            }
            else if (loop && currentDialogIndex == 0)
            {
                currentDialogIndex = dialogs.Length - 1;
                return dialogs[currentDialogIndex];
            }

            return null;
        }

        /// <summary>
        /// Gets the progress of the conversation (0-1).
        /// </summary>
        public float GetProgress()
        {
            if (dialogs == null || dialogs.Length == 0)
                return 0f;

            return (float)(currentDialogIndex + 1) / dialogs.Length;
        }

        /// <summary>
        /// Gets the total duration of all audio clips in the conversation.
        /// </summary>
        public float GetTotalAudioDuration()
        {
            if (dialogs == null)
                return 0f;

            float totalDuration = 0f;
            foreach (var dialog in dialogs)
            {
                if (dialog != null && dialog.Audio != null)
                {
                    totalDuration += dialog.Audio.length;
                }
            }
            return totalDuration;
        }

        #endregion

        #region Editor Methods

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-generate name from asset name if empty
            if (string.IsNullOrEmpty(conversationName))
            {
                conversationName = name;
            }
        }
#endif

        #endregion
    }
}