using UnityEngine;
using UnityEditor;

namespace WitShells.McqUI.Editor
{
    /// <summary>
    /// Custom editor for McqSettings to improve usability
    /// </summary>
    [CustomEditor(typeof(McqSettings))]
    public class McqSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty normalColorProp;
        private SerializedProperty correctColorProp;
        private SerializedProperty wrongColorProp;
        private SerializedProperty selectedColorProp;
        
        private SerializedProperty fillDurationProp;
        private SerializedProperty revealDelayProp;
        private SerializedProperty autoRevealDelayProp;
        
        private SerializedProperty useTypewriterEffectProp;
        private SerializedProperty typewriterSpeedProp;
        private SerializedProperty useTypewriterForQuestionProp;
        private SerializedProperty questionTypewriterSpeedProp;
        
        private SerializedProperty allowMultipleSelectionsProp;
        private SerializedProperty autoRevealCorrectAnswerProp;
        private SerializedProperty shuffleOptionsProp;
        private SerializedProperty randomizeOrderProp;
        
        private SerializedProperty defaultTimeLimitProp;
        private SerializedProperty autoStartOnEnableProp;
        
        private bool showVisualSettings = true;
        private bool showAnimationSettings = true;
        private bool showTypewriterSettings = true;
        private bool showBehaviorSettings = true;
        private bool showDefaultSettings = true;
        
        private void OnEnable()
        {
            // Visual Colors
            normalColorProp = serializedObject.FindProperty("normalColor");
            correctColorProp = serializedObject.FindProperty("correctColor");
            wrongColorProp = serializedObject.FindProperty("wrongColor");
            selectedColorProp = serializedObject.FindProperty("selectedColor");
            
            // Animation Settings
            fillDurationProp = serializedObject.FindProperty("fillDuration");
            revealDelayProp = serializedObject.FindProperty("revealDelay");
            autoRevealDelayProp = serializedObject.FindProperty("autoRevealDelay");
            
            // Typewriter Effects
            useTypewriterEffectProp = serializedObject.FindProperty("useTypewriterEffect");
            typewriterSpeedProp = serializedObject.FindProperty("typewriterSpeed");
            useTypewriterForQuestionProp = serializedObject.FindProperty("useTypewriterForQuestion");
            questionTypewriterSpeedProp = serializedObject.FindProperty("questionTypewriterSpeed");
            
            // Behavior Settings
            allowMultipleSelectionsProp = serializedObject.FindProperty("allowMultipleSelections");
            autoRevealCorrectAnswerProp = serializedObject.FindProperty("autoRevealCorrectAnswer");
            shuffleOptionsProp = serializedObject.FindProperty("shuffleOptions");
            randomizeOrderProp = serializedObject.FindProperty("randomizeOrder");
            
            // Default Values
            defaultTimeLimitProp = serializedObject.FindProperty("defaultTimeLimit");
            autoStartOnEnableProp = serializedObject.FindProperty("autoStartOnEnable");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("MCQ UI Global Settings", EditorStyles.largeLabel);
            EditorGUILayout.HelpBox("These settings are used globally by all MCQ components unless overridden locally.", MessageType.Info);
            EditorGUILayout.Space();
            
            // Visual Settings
            showVisualSettings = EditorGUILayout.Foldout(showVisualSettings, "Visual Colors", true, EditorStyles.foldoutHeader);
            if (showVisualSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(normalColorProp, new GUIContent("Normal Color", "Default color for option backgrounds"));
                EditorGUILayout.PropertyField(correctColorProp, new GUIContent("Correct Color", "Color shown for correct answers"));
                EditorGUILayout.PropertyField(wrongColorProp, new GUIContent("Wrong Color", "Color shown for incorrect answers"));
                EditorGUILayout.PropertyField(selectedColorProp, new GUIContent("Selected Color", "Color shown while option is being selected"));
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
            
            // Animation Settings
            showAnimationSettings = EditorGUILayout.Foldout(showAnimationSettings, "Animation Settings", true, EditorStyles.foldoutHeader);
            if (showAnimationSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(fillDurationProp, new GUIContent("Fill Duration", "Time in seconds for option fill animation"));
                EditorGUILayout.PropertyField(revealDelayProp, new GUIContent("Reveal Delay", "Delay after selection before revealing result"));
                EditorGUILayout.PropertyField(autoRevealDelayProp, new GUIContent("Auto Reveal Delay", "Delay before auto-revealing correct answer"));
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
            
            // Typewriter Settings
            showTypewriterSettings = EditorGUILayout.Foldout(showTypewriterSettings, "Typewriter Effects", true, EditorStyles.foldoutHeader);
            if (showTypewriterSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(useTypewriterEffectProp, new GUIContent("Use Typewriter for Options", "Enable typewriter effect for option text"));
                if (useTypewriterEffectProp.boolValue)
                {
                    EditorGUILayout.PropertyField(typewriterSpeedProp, new GUIContent("Option Typewriter Speed", "Speed of typewriter effect for options"));
                }
                
                EditorGUILayout.PropertyField(useTypewriterForQuestionProp, new GUIContent("Use Typewriter for Questions", "Enable typewriter effect for question text"));
                if (useTypewriterForQuestionProp.boolValue)
                {
                    EditorGUILayout.PropertyField(questionTypewriterSpeedProp, new GUIContent("Question Typewriter Speed", "Speed of typewriter effect for questions"));
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
            
            // Behavior Settings
            showBehaviorSettings = EditorGUILayout.Foldout(showBehaviorSettings, "Behavior Settings", true, EditorStyles.foldoutHeader);
            if (showBehaviorSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(allowMultipleSelectionsProp, new GUIContent("Allow Multiple Selections", "Allow selecting multiple options simultaneously"));
                EditorGUILayout.PropertyField(autoRevealCorrectAnswerProp, new GUIContent("Auto Reveal Correct Answer", "Automatically show correct answer after wrong selection"));
                EditorGUILayout.PropertyField(shuffleOptionsProp, new GUIContent("Shuffle Options", "Randomize option order by default"));
                EditorGUILayout.PropertyField(randomizeOrderProp, new GUIContent("Randomize Question Order", "Randomize question order in sets by default"));
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
            
            // Default Settings
            showDefaultSettings = EditorGUILayout.Foldout(showDefaultSettings, "Default Values", true, EditorStyles.foldoutHeader);
            if (showDefaultSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(defaultTimeLimitProp, new GUIContent("Default Time Limit", "Default time limit for questions (0 = no limit)"));
                EditorGUILayout.PropertyField(autoStartOnEnableProp, new GUIContent("Auto Start on Enable", "Automatically start MCQ examples when enabled"));
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
            
            serializedObject.ApplyModifiedProperties();
            
            // Utility buttons
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset to Defaults"))
            {
                if (EditorUtility.DisplayDialog("Reset Settings", "Are you sure you want to reset all settings to their default values?", "Yes", "Cancel"))
                {
                    var settings = (McqSettings)target;
                    settings.ResetToDefaults();
                }
            }
            
            if (GUILayout.Button("Create Settings Asset"))
            {
                McqSettings.CreateSettingsAsset();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("To use these settings globally, ensure this asset is placed in a Resources folder and named 'MCQ Settings'.", MessageType.Info);
        }
    }
}