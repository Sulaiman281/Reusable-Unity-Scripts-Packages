using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WitShells.DialogsManager
{
    /// <summary>
    /// Extension methods and utilities for the DialogsManager system.
    /// </summary>
    public static class DialogExtensions
    {
        #region Conversation Extensions

        /// <summary>
        /// Gets all dialogs with a specific tag from a conversation.
        /// </summary>
        public static IEnumerable<DialogObject> GetDialogsWithTag(this Conversation conversation, string tag)
        {
            if (conversation?.Dialogs == null)
                return Enumerable.Empty<DialogObject>();

            return conversation.Dialogs.Where(d => d != null && d.HasTag(tag));
        }

        /// <summary>
        /// Gets all dialogs with a specific emotion from a conversation.
        /// </summary>
        public static IEnumerable<DialogObject> GetDialogsByEmotion(this Conversation conversation, DialogEmotion emotion)
        {
            if (conversation?.Dialogs == null)
                return Enumerable.Empty<DialogObject>();

            return conversation.Dialogs.Where(d => d != null && d.Emotion == emotion);
        }

        /// <summary>
        /// Finds a dialog by its ID in a conversation.
        /// </summary>
        public static DialogObject FindDialogById(this Conversation conversation, string dialogId)
        {
            if (conversation?.Dialogs == null || string.IsNullOrEmpty(dialogId))
                return null;

            return conversation.Dialogs.FirstOrDefault(d => d != null && d.DialogId == dialogId);
        }

        /// <summary>
        /// Gets the total word count of all dialogs in a conversation.
        /// </summary>
        public static int GetTotalWordCount(this Conversation conversation)
        {
            if (conversation?.Dialogs == null)
                return 0;

            int count = 0;
            foreach (var dialog in conversation.Dialogs)
            {
                if (dialog != null && !string.IsNullOrEmpty(dialog.Content))
                {
                    count += dialog.Content.Split(' ', System.StringSplitOptions.RemoveEmptyEntries).Length;
                }
            }
            return count;
        }

        /// <summary>
        /// Creates a shallow copy of the conversation for runtime modifications.
        /// </summary>
        public static Conversation CreateRuntimeCopy(this Conversation conversation)
        {
            if (conversation == null)
                return null;

            var copy = ScriptableObject.CreateInstance<Conversation>();
            copy.ConversationName = conversation.ConversationName;
            copy.Description = conversation.Description;
            copy.Loop = conversation.Loop;
            // Note: Dialogs array is shared, create copies if you need to modify individual dialogs
            return copy;
        }

        #endregion

        #region DialogObject Extensions

        /// <summary>
        /// Gets the formatted content with title prefix.
        /// </summary>
        public static string GetFormattedContent(this DialogObject dialog, string format = "{0}: {1}")
        {
            if (dialog == null)
                return string.Empty;

            if (string.IsNullOrEmpty(dialog.Title))
                return dialog.Content ?? string.Empty;

            return string.Format(format, dialog.Title, dialog.Content);
        }

        /// <summary>
        /// Checks if the dialog has all required content (title and content).
        /// </summary>
        public static bool IsValid(this DialogObject dialog)
        {
            return dialog != null && 
                   !string.IsNullOrEmpty(dialog.Title) && 
                   !string.IsNullOrEmpty(dialog.Content);
        }

        /// <summary>
        /// Gets the optimal display duration considering all factors.
        /// </summary>
        public static float GetOptimalDuration(this DialogObject dialog, float minDuration = 2f, float wordsPerSecond = 2.5f)
        {
            if (dialog == null)
                return minDuration;

            // If audio exists, use audio length
            if (dialog.Audio != null)
                return Mathf.Max(minDuration, dialog.Audio.length);

            // Calculate based on content
            if (!string.IsNullOrEmpty(dialog.Content))
            {
                int wordCount = dialog.Content.Split(' ', System.StringSplitOptions.RemoveEmptyEntries).Length;
                return Mathf.Max(minDuration, wordCount / wordsPerSecond);
            }

            return minDuration;
        }

        #endregion

        #region DialogManager Extensions

        /// <summary>
        /// Starts a conversation and immediately plays the first dialog.
        /// </summary>
        public static void StartAndPlay(this DialogManager manager, Conversation conversation)
        {
            if (manager == null || conversation == null)
                return;

            manager.StartConversation(conversation);
            manager.PlayNextDialog();
        }

        /// <summary>
        /// Plays all remaining dialogs with a UnityAction callback when complete.
        /// </summary>
        public static void PlayAllDialogs(this DialogManager manager, UnityEngine.Events.UnityAction<ConversationEventArgs> onComplete = null)
        {
            if (manager == null || !manager.HasActiveConversation)
            {
                onComplete?.Invoke(null);
                return;
            }

            void OnConversationEnd(ConversationEventArgs args)
            {
                manager.OnConversationEnded.RemoveListener(OnConversationEnd);
                onComplete?.Invoke(args);
            }

            manager.OnConversationEnded.AddListener(OnConversationEnd);
            manager.PlayNextDialog();
        }

        #endregion
    }
}
