using System.Collections.Generic;
using UnityEngine;

namespace WitShells.McqUI
{
    /// <summary>
    /// ScriptableObject asset for storing MCQ data sets that can be created and edited in the Unity Editor
    /// </summary>
    [CreateAssetMenu(fileName = "New MCQ Set", menuName = "WitShells/MCQ UI/MCQ Data Set", order = 1)]
    public class McqDataSet : ScriptableObject
    {
        [Header("MCQ Collection")]
        [SerializeField] private List<McqData> mcqQuestions = new List<McqData>();
        
        [Header("Set Settings")]
        [SerializeField] private string setTitle = "MCQ Set";
        [SerializeField] private string description = "";
        [SerializeField] private bool useGlobalRandomSetting = true;
        [SerializeField] private bool randomizeOrder = true;
        
        /// <summary>
        /// All MCQ questions in this set
        /// </summary>
        public List<McqData> Questions => mcqQuestions;
        
        /// <summary>
        /// Title of this MCQ set
        /// </summary>
        public string SetTitle => setTitle;
        
        /// <summary>
        /// Description of this MCQ set
        /// </summary>
        public string Description => description;
        
        /// <summary>
        /// Whether questions should be randomized when retrieved
        /// </summary>
        public bool RandomizeOrder 
        { 
            get 
            { 
                if (useGlobalRandomSetting)
                {
                    var settings = McqSettings.Instance;
                    return settings != null ? settings.RandomizeOrder : true;
                }
                return randomizeOrder;
            }
        }
        
        /// <summary>
        /// Total number of questions in this set
        /// </summary>
        public int QuestionCount => mcqQuestions.Count;
        
        /// <summary>
        /// Get all questions, optionally randomized
        /// </summary>
        public List<McqData> GetQuestions(bool randomize = true)
        {
            var questions = new List<McqData>(mcqQuestions);
            
            if (randomize && randomizeOrder)
            {
                McqUtilities.ShuffleMcqList(questions);
            }
            
            return questions;
        }
        
        /// <summary>
        /// Get a specific number of questions from this set
        /// </summary>
        public List<McqData> GetQuestions(int count, bool randomize = true)
        {
            var allQuestions = GetQuestions(randomize);
            
            if (count >= allQuestions.Count)
                return allQuestions;
                
            return allQuestions.GetRange(0, count);
        }
        
        /// <summary>
        /// Get a random question from this set
        /// </summary>
        public McqData GetRandomQuestion()
        {
            if (mcqQuestions.Count == 0)
                return null;
                
            return mcqQuestions[Random.Range(0, mcqQuestions.Count)];
        }
        
        /// <summary>
        /// Add a new question to this set
        /// </summary>
        public void AddQuestion(McqData mcqData)
        {
            if (mcqData != null && mcqData.IsValid())
            {
                mcqQuestions.Add(mcqData);
            }
        }
        
        /// <summary>
        /// Remove a question at specified index
        /// </summary>
        public void RemoveQuestion(int index)
        {
            if (index >= 0 && index < mcqQuestions.Count)
            {
                mcqQuestions.RemoveAt(index);
            }
        }
        
        /// <summary>
        /// Clear all questions from this set
        /// </summary>
        public void ClearQuestions()
        {
            mcqQuestions.Clear();
        }
        
        /// <summary>
        /// Validate all questions in this set
        /// </summary>
        public bool ValidateSet(out string errorMessage)
        {
            return McqUtilities.ValidateMcqSet(mcqQuestions, out errorMessage);
        }
        
        /// <summary>
        /// Generate dummy MCQ data for testing purposes
        /// </summary>
        [ContextMenu("Generate Dummy MCQ Set")]
        public void GenerateDummyMcqSet()
        {
            ClearQuestions();
            
            // General Knowledge Questions
            AddQuestion(new McqData(
                "What is the capital of France?",
                "Paris",
                new string[] { "London", "Berlin", "Madrid" }
            ));
            
            AddQuestion(new McqData(
                "Which planet is closest to the Sun?",
                "Mercury",
                new string[] { "Venus", "Earth", "Mars" }
            ));
            
            AddQuestion(new McqData(
                "What is the largest ocean on Earth?",
                "Pacific Ocean",
                new string[] { "Atlantic Ocean", "Indian Ocean", "Arctic Ocean" }
            ));
            
            // Programming Questions
            AddQuestion(new McqData(
                "What does 'OOP' stand for in programming?",
                "Object-Oriented Programming",
                new string[] { 
                    "Object-Oriented Process", 
                    "Operational Object Programming", 
                    "Optimized Object Processing" 
                }
            ));
            
            AddQuestion(new McqData(
                "Which of the following is NOT a primitive data type in C#?",
                "string",
                new string[] { "int", "bool", "float" }
            ));
            
            // Unity-specific Questions
            AddQuestion(new McqData(
                "Which component is required for a GameObject to be rendered?",
                "Renderer",
                new string[] { "Collider", "Rigidbody", "AudioSource" }
            ));
            
            AddQuestion(new McqData(
                "What is the default frame rate target for Unity games?",
                "60 FPS",
                new string[] { "30 FPS", "120 FPS", "90 FPS" }
            ));
            
            // Math Questions
            AddQuestion(new McqData(
                "What is the square root of 64?",
                "8",
                new string[] { "6", "7", "9" }
            ));
            
            AddQuestion(new McqData(
                "What is 15% of 200?",
                "30",
                new string[] { "25", "35", "40" }
            ));
            
            // Timed Question Example
            AddQuestion(McqUtilities.CreateTimedMcq(
                "Quick! What is 2 + 2?",
                "4",
                10f, // 10 seconds
                "3", "5", "6"
            ));
            
            setTitle = "Generated Test MCQ Set";
            description = "Auto-generated MCQ set for testing functionality. Contains questions about general knowledge, programming, Unity, and math.";
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
            
            Debug.Log($"[McqDataSet] Generated {mcqQuestions.Count} dummy MCQ questions for testing.");
        }
        
        /// <summary>
        /// Generate a small sample set for quick testing
        /// </summary>
        [ContextMenu("Generate Small Test Set (3 Questions)")]
        public void GenerateSmallTestSet()
        {
            ClearQuestions();
            
            AddQuestion(new McqData(
                "What color do you get when you mix red and blue?",
                "Purple",
                new string[] { "Green", "Orange", "Yellow" }
            ));
            
            AddQuestion(new McqData(
                "How many sides does a triangle have?",
                "3",
                new string[] { "2", "4", "5" }
            ));
            
            AddQuestion(McqUtilities.CreateTimedMcq(
                "What is the first letter of the alphabet?",
                "A",
                5f, // 5 seconds
                "B", "C", "Z"
            ));
            
            setTitle = "Small Test Set";
            description = "Quick test set with 3 simple questions.";
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
            
            Debug.Log($"[McqDataSet] Generated small test set with {mcqQuestions.Count} questions.");
        }
        
        /// <summary>
        /// Clear all questions from the set
        /// </summary>
        [ContextMenu("Clear All Questions")]
        private void ClearAllQuestions()
        {
            if (mcqQuestions.Count > 0)
            {
                ClearQuestions();
                setTitle = "Empty MCQ Set";
                description = "";
                
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                #endif
                
                Debug.Log("[McqDataSet] All questions cleared.");
            }
        }
        
        private void OnValidate()
        {
            // Ensure we have valid data
            for (int i = mcqQuestions.Count - 1; i >= 0; i--)
            {
                if (mcqQuestions[i] == null || !mcqQuestions[i].IsValid())
                {
                    Debug.LogWarning($"[McqDataSet] Invalid MCQ data at index {i} in {name}");
                }
            }
        }
    }
}