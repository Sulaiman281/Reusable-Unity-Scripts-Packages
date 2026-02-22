using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WitShells.McqUI
{
    /// <summary>
    /// Data structure containing a multiple choice question with correct and incorrect options
    /// </summary>
    [Serializable]
    public class McqData
    {
        [Header("Question")]
        [SerializeField] private string question;
        
        [Header("Answer Options")]
        [SerializeField] private string correctAnswer;
        [SerializeField] private string[] wrongOptions;
        
        [Header("Settings")]
        [SerializeField] private bool shuffleOptions = true;
        [SerializeField] private float timeLimit = 0f; // 0 = no time limit
        
        /// <summary>
        /// The question text to display
        /// </summary>
        public string Question => question;
        
        /// <summary>
        /// The correct answer for this question
        /// </summary>
        public string CorrectAnswer => correctAnswer;
        
        /// <summary>
        /// Array of incorrect answer options
        /// </summary>
        public string[] WrongOptions => wrongOptions;
        
        /// <summary>
        /// Whether to shuffle the order of options when displaying
        /// </summary>
        public bool ShuffleOptions => shuffleOptions;
        
        /// <summary>
        /// Time limit for answering in seconds (0 = no limit)
        /// </summary>
        public float TimeLimit => timeLimit;
        
        /// <summary>
        /// All options combined (correct + wrong) in display order
        /// </summary>
        public List<string> AllOptions
        {
            get
            {
                var options = new List<string> { correctAnswer };
                if (wrongOptions != null)
                    options.AddRange(wrongOptions);
                
                if (shuffleOptions)
                    options = ShuffleList(options);
                    
                return options;
            }
        }
        
        /// <summary>
        /// Total number of options available
        /// </summary>
        public int OptionCount => 1 + (wrongOptions?.Length ?? 0);
        
        public McqData()
        {
            // Safely access settings, with fallbacks for serialization contexts
            var settings = McqSettings.Instance;
            question = "";
            correctAnswer = "";
            wrongOptions = new string[0];
            shuffleOptions = settings != null ? settings.ShuffleOptions : true;
            timeLimit = settings != null ? settings.DefaultTimeLimit : 0f;
        }
        
        /// <summary>
        /// Create a new MCQ data with question, correct answer, and wrong options
        /// </summary>
        /// <param name="question">The question text</param>
        /// <param name="correctAnswer">The correct answer</param>
        /// <param name="wrongOptions">Array of incorrect options</param>
        /// <param name="shuffleOptions">Whether to shuffle options when displayed (uses global setting if null)</param>
        /// <param name="timeLimit">Time limit in seconds (uses global setting if negative)</param>
        public McqData(string question, string correctAnswer, string[] wrongOptions, bool? shuffleOptions = null, float timeLimit = -1f)
        {
            // Safely access settings, with fallbacks for serialization contexts
            var settings = McqSettings.Instance;
            
            this.question = question;
            this.correctAnswer = correctAnswer;
            this.wrongOptions = wrongOptions ?? new string[0];
            this.shuffleOptions = shuffleOptions ?? (settings != null ? settings.ShuffleOptions : true);
            this.timeLimit = timeLimit >= 0f ? timeLimit : (settings != null ? settings.DefaultTimeLimit : 0f);
        }
        
        /// <summary>
        /// Check if a given answer is correct
        /// </summary>
        /// <param name="answer">The answer to check</param>
        /// <returns>True if the answer matches the correct answer</returns>
        public bool IsCorrectAnswer(string answer)
        {
            return string.Equals(answer, correctAnswer, StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// Validate that the MCQ data is properly configured
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(question) && 
                   !string.IsNullOrEmpty(correctAnswer) && 
                   wrongOptions != null && 
                   wrongOptions.Length > 0 &&
                   wrongOptions.All(option => !string.IsNullOrEmpty(option));
        }
        
        private List<T> ShuffleList<T>(List<T> list)
        {
            var shuffled = new List<T>(list);
            for (int i = 0; i < shuffled.Count; i++)
            {
                var randomIndex = UnityEngine.Random.Range(i, shuffled.Count);
                (shuffled[i], shuffled[randomIndex]) = (shuffled[randomIndex], shuffled[i]);
            }
            return shuffled;
        }
    }
}