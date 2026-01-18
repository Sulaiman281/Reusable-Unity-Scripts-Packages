using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace WitShells.DialogsManager
{
    /// <summary>
    /// A base UI controller for displaying dialogs.
    /// Extend this class or use it directly for basic dialog UI.
    /// </summary>
    public class DialogUIController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("UI References")]
        [Tooltip("The root panel/container for the dialog UI.")]
        [SerializeField] protected GameObject dialogPanel;

        [Tooltip("Text component for the speaker's name/title.")]
        [SerializeField] protected TMP_Text titleText;

        [Tooltip("Text component for the dialog content.")]
        [SerializeField] protected TMP_Text contentText;

        [Tooltip("Image component for the speaker's portrait.")]
        [SerializeField] protected Image portraitImage;

        [Tooltip("Button to advance to the next dialog.")]
        [SerializeField] protected Button nextButton;

        [Tooltip("Button to skip the current dialog.")]
        [SerializeField] protected Button skipButton;

        [Header("Progress UI")]
        [Tooltip("Optional slider to show conversation progress.")]
        [SerializeField] protected Slider progressSlider;

        [Tooltip("Optional text to show dialog count (e.g., '1/5').")]
        [SerializeField] protected TMP_Text progressText;

        [Header("Animation Settings")]
        [Tooltip("Use typewriter effect for text.")]
        [SerializeField] protected bool useTypewriterEffect = true;

        [Tooltip("Characters per second for typewriter effect.")]
        [SerializeField, Min(1)] protected float typewriterSpeed = 50f;

        [Tooltip("Sound to play for each character typed.")]
        [SerializeField] protected AudioClip typewriterSound;

        [Tooltip("Audio source for typewriter sound.")]
        [SerializeField] protected AudioSource typewriterAudioSource;

        [Header("Input Actions")]
        [Tooltip("Input action for advancing dialog.")]
        [SerializeField] protected InputActionReference advanceAction;

        [Tooltip("Input action for skipping typewriter effect.")]
        [SerializeField] protected InputActionReference skipAction;

        [Tooltip("Input action for click/tap to advance.")]
        [SerializeField] protected InputActionReference clickAction;

        [Tooltip("Allow clicking anywhere to advance.")]
        [SerializeField] protected bool clickToAdvance = true;

        #endregion

        #region Properties

        /// <summary>
        /// Whether the typewriter effect is currently running.
        /// </summary>
        public bool IsTyping { get; protected set; }

        public float TypeSpeed => DialogsSettings.Instance.DefaultTypingSpeed;

        /// <summary>
        /// The currently displayed dialog.
        /// </summary>
        public DialogObject CurrentDialog { get; protected set; }

        #endregion

        #region Private Fields

        protected Coroutine typewriterCoroutine;
        protected string fullContent;

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            if (dialogPanel != null)
            {
                dialogPanel.SetActive(false);
            }
        }

        protected virtual void OnEnable()
        {
            SubscribeToEvents();
            SetupButtons();
            EnableInputActions();
        }

        protected virtual void OnDisable()
        {
            UnsubscribeFromEvents();
            DisableInputActions();
        }

        #endregion

        #region Input System

        protected virtual void EnableInputActions()
        {
            if (advanceAction != null && advanceAction.action != null)
            {
                advanceAction.action.Enable();
                advanceAction.action.performed += OnAdvancePerformed;
            }

            if (skipAction != null && skipAction.action != null)
            {
                skipAction.action.Enable();
                skipAction.action.performed += OnSkipPerformed;
            }

            if (clickAction != null && clickAction.action != null && clickToAdvance)
            {
                clickAction.action.Enable();
                clickAction.action.performed += OnClickPerformed;
            }
        }

        protected virtual void DisableInputActions()
        {
            if (advanceAction != null && advanceAction.action != null)
            {
                advanceAction.action.performed -= OnAdvancePerformed;
            }

            if (skipAction != null && skipAction.action != null)
            {
                skipAction.action.performed -= OnSkipPerformed;
            }

            if (clickAction != null && clickAction.action != null)
            {
                clickAction.action.performed -= OnClickPerformed;
            }
        }

        protected virtual void OnAdvancePerformed(InputAction.CallbackContext context)
        {
            if (DialogManager.Instance != null && DialogManager.Instance.HasActiveConversation)
            {
                AdvanceDialog();
            }
        }

        protected virtual void OnSkipPerformed(InputAction.CallbackContext context)
        {
            if (DialogManager.Instance != null && DialogManager.Instance.HasActiveConversation && IsTyping)
            {
                CompleteTypewriter();
            }
        }

        protected virtual void OnClickPerformed(InputAction.CallbackContext context)
        {
            if (clickToAdvance && DialogManager.Instance != null && DialogManager.Instance.HasActiveConversation)
            {
                AdvanceDialog();
            }
        }

        #endregion

        #region Event Handlers

        protected virtual void SubscribeToEvents()
        {
            if (DialogManager.Instance != null)
            {
                DialogManager.Instance.OnDialogStarted.AddListener(OnDialogStarted);
                DialogManager.Instance.OnDialogFinished.AddListener(OnDialogFinished);
                DialogManager.Instance.OnConversationStarted.AddListener(OnConversationStarted);
                DialogManager.Instance.OnConversationEnded.AddListener(OnConversationEnded);
            }
        }

        protected virtual void UnsubscribeFromEvents()
        {
            if (DialogManager.Instance != null)
            {
                DialogManager.Instance.OnDialogStarted.RemoveListener(OnDialogStarted);
                DialogManager.Instance.OnDialogFinished.RemoveListener(OnDialogFinished);
                DialogManager.Instance.OnConversationStarted.RemoveListener(OnConversationStarted);
                DialogManager.Instance.OnConversationEnded.RemoveListener(OnConversationEnded);
            }
        }

        protected virtual void OnDialogStarted(DialogEventArgs args)
        {
            CurrentDialog = args.Dialog;
            DisplayDialog(args.Dialog, args.DialogIndex, args.Conversation);
        }

        protected virtual void OnDialogFinished(DialogEventArgs args)
        {
            // Optional: Add fade out or other transition effects here
        }

        protected virtual void OnConversationStarted(ConversationEventArgs args)
        {
            ShowDialogPanel();
            UpdateProgress(args.Conversation);
        }

        protected virtual void OnConversationEnded(ConversationEventArgs args)
        {
            HideDialogPanel();
            StopTypewriter();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Displays a dialog in the UI.
        /// </summary>
        public virtual void DisplayDialog(DialogObject dialog, int index, Conversation conversation)
        {
            if (dialog == null)
                return;

            // Set title
            if (titleText != null)
            {
                titleText.text = dialog.Title;
            }

            // Set portrait
            if (portraitImage != null)
            {
                if (dialog.Portrait != null)
                {
                    portraitImage.sprite = dialog.Portrait;
                    portraitImage.gameObject.SetActive(true);
                }
                else
                {
                    portraitImage.gameObject.SetActive(false);
                }
            }

            // Set content with optional typewriter effect
            fullContent = dialog.Content;
            if (useTypewriterEffect && TypeSpeed > 0)
            {
                StartTypewriter(dialog.Content, TypeSpeed);
            }
            else if (useTypewriterEffect)
            {
                StartTypewriter(dialog.Content, typewriterSpeed);
            }
            else
            {
                if (contentText != null)
                {
                    contentText.text = dialog.Content;
                }
            }

            // Update progress
            UpdateProgress(conversation);
        }

        /// <summary>
        /// Advances to the next dialog.
        /// </summary>
        public virtual void AdvanceDialog()
        {
            if (IsTyping)
            {
                CompleteTypewriter();
            }
            else
            {
                DialogManager.Instance?.PlayNextDialog();
            }
        }

        /// <summary>
        /// Skips the current dialog.
        /// </summary>
        public virtual void SkipDialog()
        {
            StopTypewriter();
            DialogManager.Instance?.SkipCurrentDialog();
        }

        /// <summary>
        /// Shows the dialog panel.
        /// </summary>
        public virtual void ShowDialogPanel()
        {
            if (dialogPanel != null)
            {
                dialogPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Hides the dialog panel.
        /// </summary>
        public virtual void HideDialogPanel()
        {
            if (dialogPanel != null)
            {
                dialogPanel.SetActive(false);
            }
        }

        #endregion

        #region Protected Methods

        protected virtual void SetupButtons()
        {
            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(AdvanceDialog);
            }

            if (skipButton != null)
            {
                skipButton.onClick.RemoveAllListeners();
                skipButton.onClick.AddListener(SkipDialog);
            }
        }

        protected virtual void UpdateProgress(Conversation conversation)
        {
            if (conversation == null)
                return;

            float progress = conversation.GetProgress();

            if (progressSlider != null)
            {
                progressSlider.value = progress;
            }

            if (progressText != null)
            {
                progressText.text = $"{conversation.CurrentDialogIndex + 1}/{conversation.TotalDialogs}";
            }
        }

        #endregion

        #region Typewriter Effect

        protected virtual void StartTypewriter(string text, float speed)
        {
            StopTypewriter();
            typewriterCoroutine = StartCoroutine(TypewriterCoroutine(text, speed));
        }

        protected virtual void StopTypewriter()
        {
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }
            IsTyping = false;
        }

        protected virtual void CompleteTypewriter()
        {
            StopTypewriter();
            if (contentText != null)
            {
                contentText.text = fullContent;
            }
        }

        protected virtual IEnumerator TypewriterCoroutine(string text, float speed)
        {
            IsTyping = true;
            
            if (contentText != null)
            {
                contentText.text = "";
            }

            float delay = 1f / speed;
            foreach (char c in text)
            {
                if (contentText != null)
                {
                    contentText.text += c;
                }

                // Play typewriter sound
                if (typewriterSound != null && typewriterAudioSource != null && !char.IsWhiteSpace(c))
                {
                    typewriterAudioSource.PlayOneShot(typewriterSound);
                }

                yield return new WaitForSeconds(delay);
            }

            IsTyping = false;
            typewriterCoroutine = null;
        }

        #endregion
    }
}
