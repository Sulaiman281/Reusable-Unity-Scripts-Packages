using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace WitShells.McqUI
{
    /// <summary>
    /// Main MCQ page component that manages question display and option interactions
    /// </summary>
    public class McqPage : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private Transform optionsContainer;
        [SerializeField] private McqOptionItem optionItemPrefab;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private GameObject timerContainer;
        
        [Header("Override Settings (Leave unchecked to use global settings)")]
        [SerializeField] private bool overrideSettings = false;
        
        [Header("Behavior Settings Override")]
        [SerializeField] private bool allowMultipleSelections = false;
        [SerializeField] private bool autoRevealCorrectAnswer = true;
        [SerializeField] private float autoRevealDelay = 2f;
        [SerializeField] private bool useTypewriterForQuestion = false;
        [SerializeField] private float questionTypewriterSpeed = 0.03f;
        
        [Header("Events")]
        public UnityEvent<string, bool> OnAnswerSelected = new UnityEvent<string, bool>();
        public UnityEvent<McqData, string, bool> OnQuestionCompleted = new UnityEvent<McqData, string, bool>();
        public UnityEvent OnTimeUp = new UnityEvent();
        public UnityEvent<McqData> OnQuestionStarted = new UnityEvent<McqData>();
        
        // Public Properties
        public McqData CurrentQuestion { get; private set; }
        public bool IsAnswered { get; private set; }
        public string SelectedAnswer { get; private set; }
        public bool IsCorrectAnswer { get; private set; }
        public float RemainingTime { get; private set; }
        
        // Private fields
        private List<McqOptionItem> _optionItems = new List<McqOptionItem>();
        private Coroutine _timerCoroutine;
        private Coroutine _questionTypewriterCoroutine;
        private McqOptionItem _selectedOption;
        
        private void Awake()
        {
            ValidateReferences();
        }
        
        private void OnDestroy()
        {
            StopAllCoroutines();
        }
        
        /// <summary>
        /// Setup and display a new MCQ question
        /// </summary>
        /// <param name="mcqData">The question data to display</param>
        public void SetupQuestion(McqData mcqData)
        {
            if (mcqData == null || !mcqData.IsValid())
            {
                Debug.LogError("[McqPage] Invalid MCQ data provided");
                return;
            }
            
            // Stop any ongoing processes
            StopAllCoroutines();
            
            // Reset question state
            CurrentQuestion = mcqData;
            IsAnswered = false;
            SelectedAnswer = "";
            IsCorrectAnswer = false;
            _selectedOption = null;
            
            // Clear existing options FIRST - this is crucial
            ClearOptions();
            
            // Setup question text
            SetupQuestionText(mcqData.Question);
            
            // Setup timer
            SetupTimer(mcqData.TimeLimit);
            
            // Create NEW option items
            CreateOptionItems(mcqData);
            
            // Notify question started
            OnQuestionStarted?.Invoke(mcqData);
        }
        
        /// <summary>
        /// Get the current question's progress as a percentage (0-1)
        /// </summary>
        public float GetProgress()
        {
            if (CurrentQuestion == null || CurrentQuestion.TimeLimit <= 0f)
                return 0f;
                
            return 1f - (RemainingTime / CurrentQuestion.TimeLimit);
        }
        
        /// <summary>
        /// Force reveal the correct answer and mark wrong answers as wrong
        /// </summary>
        public void RevealCorrectAnswer()
        {
            Debug.Log($"[McqPage] Revealing correct answer. Total options: {_optionItems.Count}");
            
            int correctAnswersFound = 0;
            foreach (var option in _optionItems)
            {
                if (option.IsCorrect)
                {
                    Debug.Log($"[McqPage] Revealing correct option: {option.OptionText}");
                    option.RevealCorrect();
                    correctAnswersFound++;
                }
                else
                {
                    // If this wrong option was selected, reveal it as wrong
                    if (option.IsSelected)
                    {
                        Debug.Log($"[McqPage] Revealing selected wrong option: {option.OptionText}");
                        option.RevealWrong();
                    }
                    else
                    {
                        option.SetInteractable(false);
                    }
                }
            }
            
            if (correctAnswersFound == 0)
            {
                Debug.LogWarning("[McqPage] No correct answers found to reveal!");
            }
        }
        
        /// <summary>
        /// Reset the MCQ page to initial state
        /// </summary>
        public void Reset()
        {
            StopAllCoroutines();
            ClearOptions();
            
            if (questionText != null)
                questionText.text = "";
                
            if (timerContainer != null)
                timerContainer.SetActive(false);
                
            CurrentQuestion = null;
            IsAnswered = false;
            SelectedAnswer = "";
            IsCorrectAnswer = false;
            _selectedOption = null;
        }
        
        /// <summary>
        /// Enable or disable all option interactions
        /// </summary>
        public void SetOptionsInteractable(bool interactable)
        {
            foreach (var option in _optionItems)
            {
                option.SetInteractable(interactable);
            }
        }
        
        /// <summary>
        /// Setup the question text with optional typewriter effect
        /// </summary>
        private void SetupQuestionText(string question)
        {
            if (questionText == null) return;
            
            var settings = McqSettings.Instance;
            bool shouldUseTypewriter = overrideSettings ? useTypewriterForQuestion : settings.UseTypewriterForQuestion;
            
            if (shouldUseTypewriter)
            {
                StartQuestionTypewriter(question);
            }
            else
            {
                questionText.text = question;
            }
        }
        
        /// <summary>
        /// Setup the timer display and start countdown
        /// </summary>
        private void SetupTimer(float timeLimit)
        {
            if (timerContainer != null)
            {
                timerContainer.SetActive(timeLimit > 0f);
            }
            
            if (timeLimit > 0f)
            {
                RemainingTime = timeLimit;
                UpdateTimerDisplay();
                StartTimer();
            }
        }
        
        /// <summary>
        /// Create option items from MCQ data
        /// </summary>
        private void CreateOptionItems(McqData mcqData)
        {
            if (optionItemPrefab == null || optionsContainer == null)
            {
                Debug.LogError("[McqPage] Missing option prefab or container");
                return;
            }
            
            // Double-check that options are cleared
            if (_optionItems.Count > 0)
            {
                Debug.LogWarning("[McqPage] Options list not empty before creating new options. Clearing now.");
                ClearOptions();
            }
            
            var allOptions = mcqData.AllOptions;
            
            // Create new option items
            for (int i = 0; i < allOptions.Count; i++)
            {
                var optionObject = Instantiate(optionItemPrefab, optionsContainer);
                var optionItem = optionObject.GetComponent<McqOptionItem>();
                
                if (optionItem == null)
                {
                    Debug.LogError($"[McqPage] Option prefab missing McqOptionItem component at index {i}");
                    Destroy(optionObject);
                    continue;
                }
                
                bool isCorrect = mcqData.IsCorrectAnswer(allOptions[i]);
                optionItem.SetupOption(allOptions[i], isCorrect);
                
                // Subscribe to option events
                optionItem.OnOptionSelected.AddListener(OnOptionSelected);
                optionItem.OnAnswerConfirmed.AddListener(OnAnswerConfirmed);
                
                _optionItems.Add(optionItem);
            }
            
            Debug.Log($"[McqPage] Created {_optionItems.Count} option items for question: {mcqData.Question}");
        }
        
        /// <summary>
        /// Clear all existing option items
        /// </summary>
        private void ClearOptions()
        {
            // Stop any running coroutines first
            StopAllCoroutines();
            
            // Clear and destroy existing options
            foreach (var option in _optionItems)
            {
                if (option != null)
                {
                    // Unsubscribe from events
                    option.OnOptionSelected.RemoveAllListeners();
                    option.OnAnswerConfirmed.RemoveAllListeners();
                    
                    // Destroy the GameObject
                    #if UNITY_EDITOR
                    if (Application.isPlaying)
                        Destroy(option.gameObject);
                    else
                        DestroyImmediate(option.gameObject);
                    #else
                    Destroy(option.gameObject);
                    #endif
                }
            }
            _optionItems.Clear();
            
            // Reset selection state
            _selectedOption = null;
        }
        
        /// <summary>
        /// Handle option selection
        /// </summary>
        private void OnOptionSelected(McqOptionItem selectedOption)
        {
            if (IsAnswered) return;
            
            var settings = McqSettings.Instance;
            bool allowMultiple = overrideSettings ? allowMultipleSelections : settings.AllowMultipleSelections;
            
            // Cancel other selections if not allowing multiple
            if (!allowMultiple && _selectedOption != null && _selectedOption != selectedOption)
            {
                _selectedOption.CancelSelection();
            }
            
            _selectedOption = selectedOption;
        }
        
        /// <summary>
        /// Handle answer confirmation from option item
        /// </summary>
        private void OnAnswerConfirmed(string answer, bool isCorrect)
        {
            if (IsAnswered) return;
            
            var settings = McqSettings.Instance;
            
            IsAnswered = true;
            SelectedAnswer = answer;
            IsCorrectAnswer = isCorrect;
            
            // Stop timer
            StopTimer();
            
            // Disable all options
            SetOptionsInteractable(false);
            
            // Auto-reveal correct answer if enabled
            bool shouldAutoReveal = overrideSettings ? autoRevealCorrectAnswer : (settings?.AutoRevealCorrectAnswer ?? true);
            if (shouldAutoReveal)
            {
                if (!isCorrect)
                {
                    // Wrong answer selected - reveal correct answer after delay
                    StartCoroutine(AutoRevealCoroutine());
                }
                else
                {
                    // Correct answer selected - reveal immediately to show success
                    RevealCorrectAnswer();
                }
            }
            
            // Trigger events
            OnAnswerSelected?.Invoke(answer, isCorrect);
            OnQuestionCompleted?.Invoke(CurrentQuestion, answer, isCorrect);
        }
        
        /// <summary>
        /// Start the question typewriter effect
        /// </summary>
        private void StartQuestionTypewriter(string question)
        {
            if (_questionTypewriterCoroutine != null)
                StopCoroutine(_questionTypewriterCoroutine);
                
            _questionTypewriterCoroutine = StartCoroutine(QuestionTypewriterCoroutine(question));
        }
        
        /// <summary>
        /// Coroutine for question typewriter effect
        /// </summary>
        private IEnumerator QuestionTypewriterCoroutine(string question)
        {
            var settings = McqSettings.Instance;
            float speed = overrideSettings ? questionTypewriterSpeed : settings.QuestionTypewriterSpeed;
            
            questionText.text = "";
            
            for (int i = 0; i <= question.Length; i++)
            {
                questionText.text = question.Substring(0, i);
                yield return new WaitForSeconds(speed);
            }
        }
        
        /// <summary>
        /// Start the countdown timer
        /// </summary>
        private void StartTimer()
        {
            if (_timerCoroutine != null)
                StopCoroutine(_timerCoroutine);
                
            _timerCoroutine = StartCoroutine(TimerCoroutine());
        }
        
        /// <summary>
        /// Stop the countdown timer
        /// </summary>
        private void StopTimer()
        {
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }
        }
        
        /// <summary>
        /// Timer countdown coroutine
        /// </summary>
        private IEnumerator TimerCoroutine()
        {
            while (RemainingTime > 0f && !IsAnswered)
            {
                RemainingTime -= Time.deltaTime;
                UpdateTimerDisplay();
                yield return null;
            }
            
            if (!IsAnswered)
            {
                // Time's up
                RemainingTime = 0f;
                UpdateTimerDisplay();
                HandleTimeUp();
            }
        }
        
        /// <summary>
        /// Handle when time runs out
        /// </summary>
        private void HandleTimeUp()
        {
            var settings = McqSettings.Instance;
            
            IsAnswered = true;
            SetOptionsInteractable(false);
            
            bool shouldAutoReveal = overrideSettings ? autoRevealCorrectAnswer : (settings?.AutoRevealCorrectAnswer ?? true);
            if (shouldAutoReveal)
            {
                RevealCorrectAnswer();
            }
            
            OnTimeUp?.Invoke();
        }
        
        /// <summary>
        /// Update the timer display
        /// </summary>
        private void UpdateTimerDisplay()
        {
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(RemainingTime / 60f);
                int seconds = Mathf.FloorToInt(RemainingTime % 60f);
                timerText.text = $"{minutes:00}:{seconds:00}";
            }
        }
        
        /// <summary>
        /// Coroutine for auto-revealing correct answer
        /// </summary>
        private IEnumerator AutoRevealCoroutine()
        {
            var settings = McqSettings.Instance;
            float delay = overrideSettings ? autoRevealDelay : (settings?.AutoRevealDelay ?? 2f);
            
            Debug.Log($"[McqPage] Auto-revealing correct answer in {delay} seconds...");
            yield return new WaitForSeconds(delay);
            
            RevealCorrectAnswer();
        }
        
        /// <summary>
        /// Validate required references
        /// </summary>
        private void ValidateReferences()
        {
            if (questionText == null)
                Debug.LogWarning("[McqPage] questionText is not assigned");
                
            if (optionsContainer == null)
                Debug.LogWarning("[McqPage] optionsContainer is not assigned");
                
            if (optionItemPrefab == null)
                Debug.LogWarning("[McqPage] optionItemPrefab is not assigned");
        }
    }
}