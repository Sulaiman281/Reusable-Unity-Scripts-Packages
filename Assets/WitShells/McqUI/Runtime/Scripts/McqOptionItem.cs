using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace WitShells.McqUI
{
    /// <summary>
    /// Individual option item for MCQ questions with selection feedback and animations
    /// </summary>
    public class McqOptionItem : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI References")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image fillImage;
        [SerializeField] private TMP_Text optionIndexText;
        [SerializeField] private TMP_Text optionText;
        [SerializeField] private Button optionButton;
        
        [Header("Override Settings (Leave unchecked to use global settings)")]
        [SerializeField] private bool overrideSettings = false;
        
        [Header("Visual Settings Override")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color correctColor = Color.green;
        [SerializeField] private Color wrongColor = Color.red;
        [SerializeField] private Color selectedColor = Color.blue;
        
        [Header("Animation Settings Override")]
        [SerializeField] private float fillDuration = 3f;
        [SerializeField] private float revealDelay = 0.8f;
        [SerializeField] private bool useTypewriterEffect = false;
        [SerializeField] private float typewriterSpeed = 0.05f;
        
        [Header("Events")]
        public UnityEvent<McqOptionItem> OnOptionSelected = new UnityEvent<McqOptionItem>();
        public UnityEvent<string, bool> OnAnswerConfirmed = new UnityEvent<string, bool>();
        
        // Public Properties
        public string OptionText => optionText.text;
        public bool IsCorrect { get; private set; }
        public bool IsLocked { get; private set; }
        public bool IsSelected { get; private set; }
        
        // Private fields
        private bool _editMode = false;
        private Coroutine _lockCoroutine;
        private Coroutine _typewriterCoroutine;
        private float _speedMultiplier = 1f;
        private bool _isDoubleClicked = false;
        
        private void Awake()
        {
            ValidateReferences();
            InitializeUI();
        }
        
        private void OnDestroy()
        {
            StopAllCoroutines();
        }
        
        /// <summary>
        /// Setup this option item with text and correctness
        /// </summary>
        /// <param name="optionText">The text to display for this option</param>
        /// <param name="isCorrect">Whether this option is the correct answer</param>
        /// <param name="editMode">If true, clicking won't start the lock animation</param>
        public void SetupOption(string optionText, bool isCorrect = false, bool editMode = false)
        {
            IsCorrect = isCorrect;
            _editMode = editMode;
            
            // Set option index (A, B, C, D, etc.)
            char optionIndex = (char)('A' + transform.GetSiblingIndex());
            optionIndexText.text = optionIndex.ToString();
            
            // Set option text with optional typewriter effect
            var settings = McqSettings.Instance;
            bool shouldUseTypewriter = overrideSettings ? useTypewriterEffect : settings.UseTypewriterEffect;
            
            if (shouldUseTypewriter && !editMode)
            {
                StartTypewriterEffect(optionText);
            }
            else
            {
                this.optionText.text = optionText;
            }
            
            ResetVisuals();
        }
        
        /// <summary>
        /// Handle click events on this option
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsLocked || _editMode) return;
            
            SelectOption();
        }
        
        /// <summary>
        /// Programmatically select this option
        /// </summary>
        public void SelectOption()
        {
            if (IsLocked) return;
            
            // Check if this option is already selected and being processed
            if (IsSelected && _lockCoroutine != null)
            {
                Debug.Log($"[McqOptionItem] Double-click detected on {optionText?.text}. Increasing speed!");
                _speedMultiplier = 2f;
                _isDoubleClicked = true;
                return;
            }
            
            IsSelected = true;
            _speedMultiplier = 1f;
            _isDoubleClicked = false;
            OnOptionSelected?.Invoke(this);
            
            if (!_editMode)
            {
                StartLockSequence();
            }
        }
        
        /// <summary>
        /// Cancel the current selection and reset visuals
        /// </summary>
        public void CancelSelection()
        {
            if (_lockCoroutine != null)
            {
                StopCoroutine(_lockCoroutine);
                _lockCoroutine = null;
            }
            
            IsSelected = false;
            ResetVisuals();
        }
        
        /// <summary>
        /// Reveal this option as correct immediately
        /// </summary>
        public void RevealCorrect()
        {
            if (!IsCorrect) 
            {
                Debug.LogWarning($"[McqOptionItem] Attempted to reveal non-correct option: {optionText?.text}");
                return;
            }
            
            Debug.Log($"[McqOptionItem] Revealing correct option: {optionText?.text}");
            
            var settings = McqSettings.Instance;
            StopAllCoroutines();
            
            Color correctColorToUse = overrideSettings ? correctColor : (settings?.CorrectColor ?? Color.green);
            SetVisualState(correctColorToUse, 1f);
            IsLocked = true;
        }
        
        /// <summary>
        /// Reveal this option as wrong immediately
        /// </summary>
        public void RevealWrong()
        {
            if (IsCorrect) 
            {
                Debug.LogWarning($"[McqOptionItem] Attempted to reveal correct option as wrong: {optionText?.text}");
                return;
            }
            
            Debug.Log($"[McqOptionItem] Revealing wrong option: {optionText?.text}");
            
            var settings = McqSettings.Instance;
            StopAllCoroutines();
            
            Color wrongColorToUse = overrideSettings ? wrongColor : (settings?.WrongColor ?? Color.red);
            SetVisualState(wrongColorToUse, 1f);
            IsLocked = true;
        }
        
        /// <summary>
        /// Enable or disable this option for interaction
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            if (optionButton != null)
                optionButton.interactable = interactable;
        }
        
        /// <summary>
        /// Reset all visual elements to default state
        /// </summary>
        private void ResetVisuals()
        {
            var settings = McqSettings.Instance;
            SetVisualState(overrideSettings ? normalColor : (settings?.NormalColor ?? Color.white), 0f);
            IsLocked = false;
            IsSelected = false;
            _speedMultiplier = 1f;
            _isDoubleClicked = false;
        }
        
        /// <summary>
        /// Set the visual state of the option
        /// </summary>
        private void SetVisualState(Color color, float fillAmount)
        {
            var settings = McqSettings.Instance;
            
            if (backgroundImage != null)
                backgroundImage.color = overrideSettings ? normalColor : settings.NormalColor;
                
            if (fillImage != null)
            {
                fillImage.color = color;
                fillImage.fillAmount = fillAmount;
            }
        }
        
        /// <summary>
        /// Start the lock sequence with fill animation
        /// </summary>
        private void StartLockSequence()
        {
            if (_lockCoroutine != null)
                StopCoroutine(_lockCoroutine);
                
            _lockCoroutine = StartCoroutine(LockSequenceCoroutine());
        }
        
        /// <summary>
        /// Coroutine for the option lock sequence
        /// </summary>
        private IEnumerator LockSequenceCoroutine()
        {
            var settings = McqSettings.Instance;
            
            // Fill animation
            fillImage.color = overrideSettings ? selectedColor : (settings?.SelectedColor ?? Color.blue);
            float elapsed = 0f;
            float baseDuration = overrideSettings ? fillDuration : (settings?.FillDuration ?? 3f);
            float actualDuration = baseDuration / _speedMultiplier;
            
            Debug.Log($"[McqOptionItem] Starting lock sequence for {optionText?.text}. Duration: {actualDuration}s (Speed: {_speedMultiplier}x)");
            
            while (elapsed < actualDuration)
            {
                elapsed += Time.deltaTime;
                fillImage.fillAmount = Mathf.Clamp01(elapsed / actualDuration);
                
                // Check for double-click speed increase
                if (_isDoubleClicked)
                {
                    actualDuration = baseDuration / _speedMultiplier;
                    _isDoubleClicked = false;
                    Debug.Log($"[McqOptionItem] Speed increased to {_speedMultiplier}x for {optionText?.text}");
                }
                
                yield return null;
            }
            
            // Reveal correct/incorrect state
            Color finalColor = IsCorrect ? 
                (overrideSettings ? correctColor : (settings?.CorrectColor ?? Color.green)) : 
                (overrideSettings ? wrongColor : (settings?.WrongColor ?? Color.red));
            fillImage.color = finalColor;
            fillImage.fillAmount = 1f;
            IsLocked = true;
            
            // Brief delay before confirming answer (also affected by speed multiplier)
            float delay = (overrideSettings ? revealDelay : (settings?.RevealDelay ?? 0.8f)) / _speedMultiplier;
            yield return new WaitForSeconds(delay);
            
            // Trigger answer confirmation
            OnAnswerConfirmed?.Invoke(optionText.text, IsCorrect);
        }
        
        /// <summary>
        /// Start typewriter effect for option text
        /// </summary>
        private void StartTypewriterEffect(string text)
        {
            if (_typewriterCoroutine != null)
                StopCoroutine(_typewriterCoroutine);
                
            _typewriterCoroutine = StartCoroutine(TypewriterCoroutine(text));
        }
        
        /// <summary>
        /// Coroutine for typewriter text effect
        /// </summary>
        private IEnumerator TypewriterCoroutine(string text)
        {
            var settings = McqSettings.Instance;
            float speed = overrideSettings ? typewriterSpeed : settings.TypewriterSpeed;
            
            optionText.text = "";
            
            for (int i = 0; i <= text.Length; i++)
            {
                optionText.text = text.Substring(0, i);
                yield return new WaitForSeconds(speed);
            }
        }
        
        /// <summary>
        /// Validate that all required references are assigned
        /// </summary>
        private void ValidateReferences()
        {
            if (fillImage == null)
                Debug.LogWarning($"[McqOptionItem] fillImage is not assigned on {gameObject.name}");
                
            if (optionIndexText == null)
                Debug.LogWarning($"[McqOptionItem] optionIndexText is not assigned on {gameObject.name}");
                
            if (optionText == null)
                Debug.LogWarning($"[McqOptionItem] optionText is not assigned on {gameObject.name}");
        }
        
        /// <summary>
        /// Initialize UI components
        /// </summary>
        private void InitializeUI()
        {
            // Setup button if available
            if (optionButton != null)
            {
                optionButton.onClick.RemoveAllListeners();
                optionButton.onClick.AddListener(() => OnPointerClick(null));
            }
        }
    }
}