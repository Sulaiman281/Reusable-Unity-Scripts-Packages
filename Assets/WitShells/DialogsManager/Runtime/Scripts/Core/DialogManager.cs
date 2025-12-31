using System;
using UnityEngine;
using UnityEngine.Events;
using WitShells.DesignPatterns.Core;

namespace WitShells.DialogsManager
{
    /// <summary>
    /// Event arguments for dialog-related events.
    /// </summary>
    [Serializable]
    public class DialogEventArgs
    {
        public DialogObject Dialog { get; set; }
        public int DialogIndex { get; set; }
        public Conversation Conversation { get; set; }
    }

    /// <summary>
    /// Event arguments for conversation-related events.
    /// </summary>
    [Serializable]
    public class ConversationEventArgs
    {
        public Conversation Conversation { get; set; }
        public bool WasCompleted { get; set; }
    }

    /// <summary>
    /// Unity event for dialog events.
    /// </summary>
    [Serializable]
    public class DialogEvent : UnityEvent<DialogEventArgs> { }

    /// <summary>
    /// Unity event for conversation events.
    /// </summary>
    [Serializable]
    public class ConversationEvent : UnityEvent<ConversationEventArgs> { }

    /// <summary>
    /// Manages dialog playback and conversation flow.
    /// Singleton pattern ensures only one DialogManager exists in the scene.
    /// </summary>
    public class DialogManager : MonoSingleton<DialogManager>
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private AudioSource audioSource;

        [Header("Settings")]
        [SerializeField] private bool autoPlayNext = false;
        [SerializeField] private float autoPlayDelay = 0.5f;

        [Header("Unity Events - Conversation")]
        [SerializeField] private ConversationEvent onConversationStarted = new ConversationEvent();
        [SerializeField] private ConversationEvent onConversationEnded = new ConversationEvent();

        [Header("Unity Events - Dialog")]
        [SerializeField] private DialogEvent onDialogStarted = new DialogEvent();
        [SerializeField] private DialogEvent onDialogFinished = new DialogEvent();

        #endregion

        #region Properties

        /// <summary>
        /// The currently active conversation.
        /// </summary>
        public Conversation CurrentConversation { get; private set; }

        /// <summary>
        /// The currently playing dialog.
        /// </summary>
        public DialogObject CurrentDialog { get; private set; }

        /// <summary>
        /// Whether a dialog is currently playing.
        /// </summary>
        public bool IsPlaying { get; private set; }

        /// <summary>
        /// Whether a conversation is active.
        /// </summary>
        public bool HasActiveConversation => CurrentConversation != null;

        /// <summary>
        /// The audio source used for dialog audio playback.
        /// </summary>
        public AudioSource AudioSource => audioSource;

        /// <summary>
        /// Unity event accessor for conversation started.
        /// </summary>
        public ConversationEvent OnConversationStarted => onConversationStarted;

        /// <summary>
        /// Unity event accessor for conversation ended.
        /// </summary>
        public ConversationEvent OnConversationEnded => onConversationEnded;

        /// <summary>
        /// Unity event accessor for dialog started.
        /// </summary>
        public DialogEvent OnDialogStarted => onDialogStarted;

        /// <summary>
        /// Unity event accessor for dialog finished.
        /// </summary>
        public DialogEvent OnDialogFinished => onDialogFinished;

        #endregion

        #region Private Fields

        private Coroutine autoPlayCoroutine;
        private Coroutine dialogPlaybackCoroutine;

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            // Check if audio finished playing
            if (IsPlaying && CurrentDialog != null && audioSource != null)
            {
                if (!audioSource.isPlaying && CurrentDialog.Audio != null)
                {
                    OnDialogPlaybackComplete();
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            StopAllCoroutines();
        }

        #endregion

        #region Public Methods - Conversation

        /// <summary>
        /// Starts a new conversation.
        /// </summary>
        /// <param name="conversation">The conversation to start.</param>
        /// <param name="resetConversation">Whether to reset the conversation to the beginning.</param>
        public void StartConversation(Conversation conversation, bool resetConversation = true)
        {
            if (conversation == null)
            {
                Debug.LogWarning("[DialogManager] Cannot start null conversation.");
                return;
            }

            // End any existing conversation
            if (CurrentConversation != null)
            {
                EndConversation(false);
            }

            CurrentConversation = conversation;

            if (resetConversation)
            {
                CurrentConversation.ResetConversation();
            }

            var args = new ConversationEventArgs
            {
                Conversation = conversation,
                WasCompleted = false
            };

            // Fire events
            onConversationStarted?.Invoke(args);

            Debug.Log($"[DialogManager] Started conversation: {conversation.ConversationName}");
        }

        /// <summary>
        /// Ends the current conversation.
        /// </summary>
        /// <param name="wasCompleted">Whether the conversation was completed naturally.</param>
        public void EndConversation(bool wasCompleted = true)
        {
            if (CurrentConversation == null)
            {
                return;
            }

            // Stop any playing dialog
            StopCurrentDialog();

            var args = new ConversationEventArgs
            {
                Conversation = CurrentConversation,
                WasCompleted = wasCompleted
            };

            var conversationName = CurrentConversation.ConversationName;
            CurrentConversation = null;

            // Fire events
            onConversationEnded?.Invoke(args);

            Debug.Log($"[DialogManager] Ended conversation: {conversationName} (Completed: {wasCompleted})");
        }

        /// <summary>
        /// Skips to a specific conversation and optionally starts from a specific dialog index.
        /// </summary>
        public void JumpToConversation(Conversation conversation, int startDialogIndex = 0)
        {
            if (conversation == null)
            {
                Debug.LogWarning("[DialogManager] Cannot jump to null conversation.");
                return;
            }

            StartConversation(conversation, true);
            conversation.SetDialogIndex(startDialogIndex - 1); // -1 because GetNextDialog increments first
        }

        #endregion

        #region Public Methods - Dialog

        /// <summary>
        /// Plays the next dialog in the current conversation.
        /// </summary>
        /// <returns>True if a dialog was played, false if no more dialogs.</returns>
        public bool PlayNextDialog()
        {
            if (CurrentConversation == null)
            {
                Debug.LogWarning("[DialogManager] No active conversation to play dialog from.");
                return false;
            }

            // Finish current dialog if playing
            if (IsPlaying)
            {
                FinishCurrentDialog();
            }

            DialogObject nextDialog = CurrentConversation.GetNextDialog();
            if (nextDialog != null)
            {
                PlayDialog(nextDialog);
                return true;
            }
            else
            {
                EndConversation(true);
                return false;
            }
        }

        /// <summary>
        /// Plays a specific dialog.
        /// </summary>
        /// <param name="dialog">The dialog to play.</param>
        public void PlayDialog(DialogObject dialog)
        {
            if (dialog == null)
            {
                Debug.LogWarning("[DialogManager] Cannot play null dialog.");
                return;
            }

            CurrentDialog = dialog;
            IsPlaying = true;

            var args = CreateDialogEventArgs(dialog);

            // Fire dialog started events
            onDialogStarted?.Invoke(args);

            // Play audio if available
            if (dialog.Audio != null && audioSource != null)
            {
                audioSource.clip = dialog.Audio;
                audioSource.Play();
            }
            else if (dialog.Audio == null)
            {
                // No audio, mark as complete after a short delay or immediately
                OnDialogPlaybackComplete();
            }

            Debug.Log($"[DialogManager] Playing dialog: {dialog.Title}");
        }

        /// <summary>
        /// Skips the current dialog and optionally plays the next one.
        /// </summary>
        /// <param name="playNext">Whether to automatically play the next dialog.</param>
        public void SkipCurrentDialog(bool playNext = true)
        {
            if (!IsPlaying)
            {
                return;
            }

            FinishCurrentDialog();

            if (playNext)
            {
                PlayNextDialog();
            }
        }

        /// <summary>
        /// Stops the current dialog without firing finish events.
        /// </summary>
        public void StopCurrentDialog()
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            IsPlaying = false;
            CurrentDialog = null;

            if (autoPlayCoroutine != null)
            {
                StopCoroutine(autoPlayCoroutine);
                autoPlayCoroutine = null;
            }
        }

        /// <summary>
        /// Pauses the current dialog audio.
        /// </summary>
        public void PauseDialog()
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Pause();
            }
        }

        /// <summary>
        /// Resumes the paused dialog audio.
        /// </summary>
        public void ResumeDialog()
        {
            if (audioSource != null && !audioSource.isPlaying && IsPlaying)
            {
                audioSource.UnPause();
            }
        }

        #endregion

        #region Private Methods

        private void OnDialogPlaybackComplete()
        {
            if (!IsPlaying || CurrentDialog == null)
            {
                return;
            }

            FinishCurrentDialog();

            // Auto-play next dialog if enabled
            if (autoPlayNext && CurrentConversation != null)
            {
                autoPlayCoroutine = StartCoroutine(AutoPlayNextCoroutine());
            }
        }

        private void FinishCurrentDialog()
        {
            if (CurrentDialog == null)
            {
                return;
            }

            var args = CreateDialogEventArgs(CurrentDialog);

            // Stop audio
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            IsPlaying = false;

            // Fire dialog finished events
            onDialogFinished?.Invoke(args);

            Debug.Log($"[DialogManager] Finished dialog: {CurrentDialog.Title}");

            CurrentDialog = null;
        }

        private DialogEventArgs CreateDialogEventArgs(DialogObject dialog)
        {
            return new DialogEventArgs
            {
                Dialog = dialog,
                DialogIndex = CurrentConversation?.CurrentDialogIndex ?? -1,
                Conversation = CurrentConversation
            };
        }

        private System.Collections.IEnumerator AutoPlayNextCoroutine()
        {
            yield return new WaitForSeconds(autoPlayDelay);
            PlayNextDialog();
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Sets the audio source to use for dialog playback.
        /// </summary>
        public void SetAudioSource(AudioSource source)
        {
            audioSource = source;
        }

        /// <summary>
        /// Gets the progress of the current conversation (0-1).
        /// </summary>
        public float GetConversationProgress()
        {
            if (CurrentConversation == null || CurrentConversation.TotalDialogs == 0)
            {
                return 0f;
            }

            return (float)(CurrentConversation.CurrentDialogIndex + 1) / CurrentConversation.TotalDialogs;
        }

        /// <summary>
        /// Gets the remaining dialogs count in the current conversation.
        /// </summary>
        public int GetRemainingDialogsCount()
        {
            if (CurrentConversation == null)
            {
                return 0;
            }

            return CurrentConversation.TotalDialogs - CurrentConversation.CurrentDialogIndex - 1;
        }

        /// <summary>
        /// Checks if the current conversation has more dialogs.
        /// </summary>
        public bool HasMoreDialogs()
        {
            return CurrentConversation != null && CurrentConversation.HasMoreDialogs();
        }

        /// <summary>
        /// Gets the current dialog's audio playback progress (0-1).
        /// </summary>
        public float GetDialogAudioProgress()
        {
            if (audioSource == null || audioSource.clip == null || !audioSource.isPlaying)
            {
                return 0f;
            }

            return audioSource.time / audioSource.clip.length;
        }

        #endregion
    }
}