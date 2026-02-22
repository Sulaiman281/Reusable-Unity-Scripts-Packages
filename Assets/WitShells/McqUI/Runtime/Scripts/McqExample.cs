using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WitShells.McqUI
{
    /// <summary>
    /// Example script showing how to use the MCQ UI system
    /// </summary>
    public class McqExample : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private McqPage mcqPage;
        [SerializeField] private McqDataSet mcqDataSet;
        
        [Header("Example Options")]
        [SerializeField] private bool useScriptableObjectQuestions = true;
        [SerializeField] private bool useGlobalAutoStartSetting = true;
        [SerializeField] private bool autoStartOnEnable = true;
        
        private List<McqData> _currentQuestions;
        private int _currentQuestionIndex = 0;
        private int _correctAnswers = 0;
        
        private void OnEnable()
        {
            bool shouldAutoStart = useGlobalAutoStartSetting ? 
                (McqSettings.Instance?.AutoStartOnEnable ?? true) : 
                autoStartOnEnable;
                
            if (shouldAutoStart)
            {
                StartQuiz();
            }
        }
        
        private void Start()
        {
            // Subscribe to MCQ page events
            if (mcqPage != null)
            {
                mcqPage.OnAnswerSelected.AddListener(OnAnswerSelected);
                mcqPage.OnQuestionCompleted.AddListener(OnQuestionCompleted);
                mcqPage.OnTimeUp.AddListener(OnTimeUp);
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (mcqPage != null)
            {
                mcqPage.OnAnswerSelected.RemoveListener(OnAnswerSelected);
                mcqPage.OnQuestionCompleted.RemoveListener(OnQuestionCompleted);
                mcqPage.OnTimeUp.RemoveListener(OnTimeUp);
            }
        }
        
        /// <summary>
        /// Start the quiz with either ScriptableObject data or hardcoded examples
        /// </summary>
        public void StartQuiz()
        {
            if (mcqPage == null)
            {
                Debug.LogError("[McqExample] MCQ Page is not assigned!");
                return;
            }
            
            _currentQuestionIndex = 0;
            _correctAnswers = 0;
            
            // Get questions from either ScriptableObject or create examples
            if (useScriptableObjectQuestions && mcqDataSet != null)
            {
                _currentQuestions = mcqDataSet.GetQuestions(randomize: true);
            }
            else
            {
                _currentQuestions = CreateExampleQuestions();
            }
            
            if (_currentQuestions.Count > 0)
            {
                ShowCurrentQuestion();
            }
            else
            {
                Debug.LogWarning("[McqExample] No questions available to display!");
            }
        }
        
        /// <summary>
        /// Show the next question or finish the quiz
        /// </summary>
        public void ShowNextQuestion()
        {
            _currentQuestionIndex++;
            
            if (_currentQuestionIndex < _currentQuestions.Count)
            {
                ShowCurrentQuestion();
            }
            else
            {
                FinishQuiz();
            }
        }
        
        /// <summary>
        /// Show the current question based on index
        /// </summary>
        private void ShowCurrentQuestion()
        {
            if (_currentQuestionIndex >= 0 && _currentQuestionIndex < _currentQuestions.Count)
            {
                var currentQuestion = _currentQuestions[_currentQuestionIndex];
                mcqPage.SetupQuestion(currentQuestion);
                
                Debug.Log($"[McqExample] Showing question {_currentQuestionIndex + 1} of {_currentQuestions.Count}");
            }
        }
        
        /// <summary>
        /// Handle when an answer is selected
        /// </summary>
        private void OnAnswerSelected(string selectedAnswer, bool isCorrect)
        {
            Debug.Log($"[McqExample] Answer selected: '{selectedAnswer}' - {(isCorrect ? "Correct!" : "Wrong!")}");
            
            if (isCorrect)
            {
                _correctAnswers++;
            }
        }
        
        /// <summary>
        /// Handle when a question is completed
        /// </summary>
        private void OnQuestionCompleted(McqData questionData, string selectedAnswer, bool isCorrect)
        {
            Debug.Log($"[McqExample] Question completed. Moving to next question in 2 seconds...");
            
            // Wait a moment before showing next question
            StartCoroutine(ShowNextQuestionAfterDelay(2f));
        }
        
        /// <summary>
        /// Handle when time runs out
        /// </summary>
        private void OnTimeUp()
        {
            Debug.Log("[McqExample] Time's up! Moving to next question...");
            StartCoroutine(ShowNextQuestionAfterDelay(1f));
        }
        
        /// <summary>
        /// Show next question after a delay
        /// </summary>
        private IEnumerator ShowNextQuestionAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ShowNextQuestion();
        }
        
        /// <summary>
        /// Finish the quiz and show results
        /// </summary>
        private void FinishQuiz()
        {
            float scorePercentage = (_correctAnswers / (float)_currentQuestions.Count) * 100f;
            
            Debug.Log($"[McqExample] Quiz completed! Score: {_correctAnswers}/{_currentQuestions.Count} ({scorePercentage:F1}%)");
            
            // You can implement UI to show final results here
            mcqPage.Reset();
        }
        
        /// <summary>
        /// Create example MCQ questions for demonstration
        /// </summary>
        private List<McqData> CreateExampleQuestions()
        {
            var questions = new List<McqData>();
            
            // Example 1: Basic question
            questions.Add(McqUtilities.CreateSimpleMcq(
                "What is the capital of France?",
                "Paris",
                "London", "Berlin", "Madrid"
            ));
            
            // Example 2: Timed question
            questions.Add(McqUtilities.CreateTimedMcq(
                "Which planet is closest to the Sun?",
                "Mercury",
                30f, // 30 seconds
                "Venus", "Earth", "Mars"
            ));
            
            // Example 3: Unity-specific question
            questions.Add(new McqData(
                "Which component is required for a GameObject to be rendered?",
                "Renderer",
                new string[] { "Collider", "Rigidbody", "AudioSource" },
                shuffleOptions: true,
                timeLimit: 0f
            ));
            
            // Example 4: Programming question
            questions.Add(new McqData(
                "What does 'OOP' stand for in programming?",
                "Object-Oriented Programming",
                new string[] { 
                    "Object-Oriented Process", 
                    "Operational Object Programming", 
                    "Optimized Object Processing" 
                }
            ));
            
            return questions;
        }
        
        // Public methods for UI buttons
        public void RestartQuiz()
        {
            StartQuiz();
        }
        
        public void SkipCurrentQuestion()
        {
            ShowNextQuestion();
        }
    }
}