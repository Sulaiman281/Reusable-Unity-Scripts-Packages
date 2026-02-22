using UnityEngine;

namespace WitShells.McqUI
{
    /// <summary>
    /// Centralized settings for the MCQ UI system. Singleton ScriptableObject that can be accessed globally.
    /// </summary>
    [CreateAssetMenu(fileName = "MCQ Settings", menuName = "WitShells/MCQ UI/MCQ Settings", order = 0)]
    public class McqSettings : ScriptableObject
    {
        private static McqSettings _instance;
        private static bool _isInitializing = false;
        
        /// <summary>
        /// Singleton instance of MCQ Settings. Creates default settings if none exists.
        /// Safe to call during serialization - will return null if not available.
        /// </summary>
        public static McqSettings Instance
        {
            get
            {
                if (_instance == null && !_isInitializing)
                {
                    // Avoid calling Resources.Load during serialization
                    if (!IsInSerializationContext())
                    {
                        _isInitializing = true;
                        _instance = Resources.Load<McqSettings>("MCQ Settings");
                        
                        if (_instance == null)
                        {
                            _instance = CreateDefaultSettings();
                        }
                        _isInitializing = false;
                    }
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// Get settings instance safely, with fallback to default values if not available during serialization
        /// </summary>
        public static McqSettings SafeInstance
        {
            get
            {
                var instance = Instance;
                return instance ?? CreateDefaultSettings();
            }
        }
        
        [Header("Visual Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color correctColor = Color.green;
        [SerializeField] private Color wrongColor = Color.red;
        [SerializeField] private Color selectedColor = Color.blue;
        
        [Header("Animation Settings")]
        [SerializeField] private float fillDuration = 3f;
        [SerializeField] private float revealDelay = 0.8f;
        [SerializeField] private float autoRevealDelay = 2f;
        
        [Header("Typewriter Effects")]
        [SerializeField] private bool useTypewriterEffect = false;
        [SerializeField] private float typewriterSpeed = 0.05f;
        [SerializeField] private bool useTypewriterForQuestion = false;
        [SerializeField] private float questionTypewriterSpeed = 0.03f;
        
        [Header("Behavior Settings")]
        [SerializeField] private bool allowMultipleSelections = false;
        [SerializeField] private bool autoRevealCorrectAnswer = true;
        [SerializeField] private bool shuffleOptions = true;
        [SerializeField] private bool randomizeOrder = true;
        
        [Header("Default Values")]
        [SerializeField] private float defaultTimeLimit = 0f; // 0 = no time limit
        [SerializeField] private bool autoStartOnEnable = true;
        
        // Public Properties for accessing settings
        public Color NormalColor => normalColor;
        public Color CorrectColor => correctColor;
        public Color WrongColor => wrongColor;
        public Color SelectedColor => selectedColor;
        
        public float FillDuration => fillDuration;
        public float RevealDelay => revealDelay;
        public float AutoRevealDelay => autoRevealDelay;
        
        public bool UseTypewriterEffect => useTypewriterEffect;
        public float TypewriterSpeed => typewriterSpeed;
        public bool UseTypewriterForQuestion => useTypewriterForQuestion;
        public float QuestionTypewriterSpeed => questionTypewriterSpeed;
        
        public bool AllowMultipleSelections => allowMultipleSelections;
        public bool AutoRevealCorrectAnswer => autoRevealCorrectAnswer;
        public bool ShuffleOptions => shuffleOptions;
        public bool RandomizeOrder => randomizeOrder;
        
        public float DefaultTimeLimit => defaultTimeLimit;
        public bool AutoStartOnEnable => autoStartOnEnable;
        
        /// <summary>
        /// Check if we're currently in a serialization context where Resources.Load is not allowed
        /// </summary>
        private static bool IsInSerializationContext()
        {
            try
            {
                #if UNITY_EDITOR
                // In editor, check if we're in a compilation or updating state
                if (UnityEditor.EditorApplication.isUpdating || UnityEditor.EditorApplication.isCompiling)
                    return true;
                
                // Try to detect serialization context by checking if we can safely access Application properties
                var _ = Application.platform; // This is safe to call
                return false;
                #else
                // In build, try to detect serialization by attempting to access a safe Application property
                var _ = Application.platform; // This is safe during serialization
                return false;
                #endif
            }
            catch
            {
                // If any exception occurs, assume we're in serialization context
                return true;
            }
        }
        
        /// <summary>
        /// Create default settings instance
        /// </summary>
        private static McqSettings CreateDefaultSettings()
        {
            var settings = CreateInstance<McqSettings>();
            settings.name = "Default MCQ Settings";
            return settings;
        }
        
        /// <summary>
        /// Reset all settings to default values
        /// </summary>
        [ContextMenu("Reset to Defaults")]
        public void ResetToDefaults()
        {
            normalColor = Color.white;
            correctColor = Color.green;
            wrongColor = Color.red;
            selectedColor = Color.blue;
            
            fillDuration = 3f;
            revealDelay = 0.8f;
            autoRevealDelay = 2f;
            
            useTypewriterEffect = false;
            typewriterSpeed = 0.05f;
            useTypewriterForQuestion = false;
            questionTypewriterSpeed = 0.03f;
            
            allowMultipleSelections = false;
            autoRevealCorrectAnswer = true;
            shuffleOptions = true;
            randomizeOrder = true;
            
            defaultTimeLimit = 0f;
            autoStartOnEnable = true;
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
        
        /// <summary>
        /// Validate settings values
        /// </summary>
        private void OnValidate()
        {
            fillDuration = Mathf.Max(0.1f, fillDuration);
            revealDelay = Mathf.Max(0f, revealDelay);
            autoRevealDelay = Mathf.Max(0f, autoRevealDelay);
            typewriterSpeed = Mathf.Max(0.001f, typewriterSpeed);
            questionTypewriterSpeed = Mathf.Max(0.001f, questionTypewriterSpeed);
            defaultTimeLimit = Mathf.Max(0f, defaultTimeLimit);
        }
        
        #if UNITY_EDITOR
        /// <summary>
        /// Create settings asset in Resources folder
        /// </summary>
        [UnityEditor.MenuItem("Tools/WitShells/Create MCQ Settings Asset")]
        public static void CreateSettingsAsset()
        {
            var settings = CreateInstance<McqSettings>();
            
            // Ensure Resources folder exists
            string resourcesPath = "Assets/Resources";
            if (!UnityEditor.AssetDatabase.IsValidFolder(resourcesPath))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
            }
            
            string assetPath = $"{resourcesPath}/MCQ Settings.asset";
            UnityEditor.AssetDatabase.CreateAsset(settings, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            
            UnityEditor.Selection.activeObject = settings;
            Debug.Log($"MCQ Settings asset created at: {assetPath}");
        }
        #endif
    }
}