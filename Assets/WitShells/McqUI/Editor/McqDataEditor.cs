// using UnityEngine;
// using UnityEditor;

// namespace WitShells.McqUI.Editor
// {
//     /// <summary>
//     /// Custom property drawer for McqData to improve editor experience
//     /// </summary>
//     [CustomPropertyDrawer(typeof(McqData))]
//     public class McqDataDrawer : PropertyDrawer
//     {
//         public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
//         {
//             EditorGUI.BeginProperty(position, label, property);
            
//             // Calculate rects
//             var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            
//             // Foldout
//             property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
            
//             if (property.isExpanded)
//             {
//                 EditorGUI.indentLevel++;
                
//                 var currentY = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                
//                 // Question field
//                 var questionRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
//                 var questionProp = property.FindPropertyRelative("question");
//                 EditorGUI.PropertyField(questionRect, questionProp, new GUIContent("Question"));
//                 currentY += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                
//                 // Correct answer field
//                 var correctAnswerRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
//                 var correctAnswerProp = property.FindPropertyRelative("correctAnswer");
//                 EditorGUI.PropertyField(correctAnswerRect, correctAnswerProp, new GUIContent("Correct Answer"));
//                 currentY += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                
//                 // Wrong options array
//                 var wrongOptionsProp = property.FindPropertyRelative("wrongOptions");
//                 var wrongOptionsRect = new Rect(position.x, currentY, position.width, EditorGUI.GetPropertyHeight(wrongOptionsProp));
//                 EditorGUI.PropertyField(wrongOptionsRect, wrongOptionsProp, new GUIContent("Wrong Options"), true);
//                 currentY += EditorGUI.GetPropertyHeight(wrongOptionsProp) + EditorGUIUtility.standardVerticalSpacing;
                
//                 // Settings
//                 var shuffleRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
//                 var shuffleProp = property.FindPropertyRelative("shuffleOptions");
//                 EditorGUI.PropertyField(shuffleRect, shuffleProp, new GUIContent("Shuffle Options"));
//                 currentY += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                
//                 var timeLimitRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
//                 var timeLimitProp = property.FindPropertyRelative("timeLimit");
//                 EditorGUI.PropertyField(timeLimitRect, timeLimitProp, new GUIContent("Time Limit (0 = no limit)"));
                
//                 EditorGUI.indentLevel--;
//             }
            
//             EditorGUI.EndProperty();
//         }
        
//         public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
//         {
//             if (!property.isExpanded)
//                 return EditorGUIUtility.singleLineHeight;
            
//             var height = EditorGUIUtility.singleLineHeight; // Foldout
//             height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Question
//             height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Correct Answer
            
//             var wrongOptionsProp = property.FindPropertyRelative("wrongOptions");
//             height += EditorGUI.GetPropertyHeight(wrongOptionsProp) + EditorGUIUtility.standardVerticalSpacing; // Wrong Options
            
//             height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Shuffle
//             height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Time Limit
            
//             return height;
//         }
//     }
    
//     /// <summary>
//     /// Custom editor for McqDataSet to improve usability
//     /// </summary>
//     [CustomEditor(typeof(McqDataSet))]
//     public class McqDataSetEditor : UnityEditor.Editor
//     {
//         private SerializedProperty mcqQuestionsProperty;
//         private SerializedProperty setTitleProperty;
//         private SerializedProperty descriptionProperty;
//         private SerializedProperty useGlobalRandomSettingProperty;
//         private SerializedProperty randomizeOrderProperty;
        
//         private bool showQuestions = true;
        
//         private void OnEnable()
//         {
//             mcqQuestionsProperty = serializedObject.FindProperty("mcqQuestions");
//             setTitleProperty = serializedObject.FindProperty("setTitle");
//             descriptionProperty = serializedObject.FindProperty("description");
//             useGlobalRandomSettingProperty = serializedObject.FindProperty("useGlobalRandomSetting");
//             randomizeOrderProperty = serializedObject.FindProperty("randomizeOrder");
//         }
        
//         public override void OnInspectorGUI()
//         {
//             var mcqDataSet = (McqDataSet)target;
            
//             serializedObject.Update();
            
//             EditorGUI.BeginChangeCheck();
            
//             // Header
//             EditorGUILayout.Space();
//             EditorGUILayout.LabelField("MCQ Data Set", EditorStyles.largeLabel);
//             EditorGUILayout.Space();
            
//             // Set Settings Section
//             EditorGUILayout.LabelField("Set Settings", EditorStyles.boldLabel);
//             EditorGUILayout.PropertyField(setTitleProperty, new GUIContent("Title"));
//             EditorGUILayout.PropertyField(descriptionProperty, new GUIContent("Description"));
//             EditorGUILayout.PropertyField(useGlobalRandomSettingProperty, new GUIContent("Use Global Random Setting"));
            
//             // Only show randomizeOrder if not using global setting
//             if (!useGlobalRandomSettingProperty.boolValue)
//             {
//                 EditorGUI.indentLevel++;
//                 EditorGUILayout.PropertyField(randomizeOrderProperty, new GUIContent("Randomize Order"));
//                 EditorGUI.indentLevel--;
//             }
            
//             EditorGUILayout.Space();
            
//             // MCQ Questions Section
//             showQuestions = EditorGUILayout.Foldout(showQuestions, $"MCQ Questions ({mcqQuestionsProperty.arraySize})", true, EditorStyles.foldoutHeader);
            
//             if (showQuestions)
//             {
//                 EditorGUI.indentLevel++;
                
//                 // Array size field
//                 EditorGUILayout.BeginHorizontal();
//                 EditorGUILayout.PropertyField(mcqQuestionsProperty.FindPropertyRelative("Array.size"), new GUIContent("Size"));
                
//                 // Quick add button
//                 if (GUILayout.Button("Add Question", GUILayout.Width(100)))
//                 {
//                     mcqQuestionsProperty.arraySize++;
//                     var newElement = mcqQuestionsProperty.GetArrayElementAtIndex(mcqQuestionsProperty.arraySize - 1);
//                     // Initialize with default values
//                     var questionProp = newElement.FindPropertyRelative("question");
//                     var correctAnswerProp = newElement.FindPropertyRelative("correctAnswer");
//                     var wrongOptionsProp = newElement.FindPropertyRelative("wrongOptions");
                    
//                     questionProp.stringValue = "New Question";
//                     correctAnswerProp.stringValue = "Correct Answer";
//                     wrongOptionsProp.arraySize = 3;
//                     wrongOptionsProp.GetArrayElementAtIndex(0).stringValue = "Wrong Option 1";
//                     wrongOptionsProp.GetArrayElementAtIndex(1).stringValue = "Wrong Option 2";
//                     wrongOptionsProp.GetArrayElementAtIndex(2).stringValue = "Wrong Option 3";
//                 }
//                 EditorGUILayout.EndHorizontal();
                
//                 EditorGUILayout.Space(5);
                
//                 // Draw each MCQ question with proper spacing
//                 for (int i = 0; i < mcqQuestionsProperty.arraySize; i++)
//                 {
//                     var elementProperty = mcqQuestionsProperty.GetArrayElementAtIndex(i);
                    
//                     EditorGUILayout.BeginVertical(GUI.skin.box);
                    
//                     EditorGUILayout.BeginHorizontal();
//                     EditorGUILayout.LabelField($"Question {i + 1}", EditorStyles.boldLabel, GUILayout.Width(80));
                    
//                     GUILayout.FlexibleSpace();
                    
//                     // Delete button
//                     if (GUILayout.Button("×", GUILayout.Width(20), GUILayout.Height(20)))
//                     {
//                         mcqQuestionsProperty.DeleteArrayElementAtIndex(i);
//                         break; // Exit the loop since we modified the array
//                     }
//                     EditorGUILayout.EndHorizontal();
                    
//                     // Draw the MCQ data property
//                     EditorGUILayout.PropertyField(elementProperty, GUIContent.none);
                    
//                     EditorGUILayout.EndVertical();
                    
//                     EditorGUILayout.Space(2);
//                 }
                
//                 EditorGUI.indentLevel--;
//             }
            
//             if (EditorGUI.EndChangeCheck())
//             {
//                 serializedObject.ApplyModifiedProperties();
//                 EditorUtility.SetDirty(mcqDataSet);
//             }
            
//             EditorGUILayout.Space();
            
//             // Generation Section
//             EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);
//             EditorGUILayout.BeginHorizontal();
            
//             if (GUILayout.Button("Generate Full Test Set"))
//             {
//                 if (mcqQuestionsProperty.arraySize > 0)
//                 {
//                     if (EditorUtility.DisplayDialog("Generate Test Set", "This will replace all existing questions. Continue?", "Yes", "Cancel"))
//                     {
//                         mcqDataSet.GenerateDummyMcqSet();
//                         serializedObject.Update(); // Refresh the serialized object
//                     }
//                 }
//                 else
//                 {
//                     mcqDataSet.GenerateDummyMcqSet();
//                     serializedObject.Update();
//                 }
//             }
            
//             if (GUILayout.Button("Generate Small Set"))
//             {
//                 if (mcqQuestionsProperty.arraySize > 0)
//                 {
//                     if (EditorUtility.DisplayDialog("Generate Small Set", "This will replace all existing questions. Continue?", "Yes", "Cancel"))
//                     {
//                         mcqDataSet.GenerateSmallTestSet();
//                         serializedObject.Update();
//                     }
//                 }
//                 else
//                 {
//                     mcqDataSet.GenerateSmallTestSet();
//                     serializedObject.Update();
//                 }
//             }
            
//             EditorGUILayout.EndHorizontal();
            
//             EditorGUILayout.Space();
            
//             // Validation section
//             EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            
//             if (GUILayout.Button("Validate MCQ Set"))
//             {
//                 string errorMessage;
//                 bool isValid = mcqDataSet.ValidateSet(out errorMessage);
                
//                 if (isValid)
//                 {
//                     EditorUtility.DisplayDialog("Validation Result", "All MCQ data is valid!", "OK");
//                 }
//                 else
//                 {
//                     EditorUtility.DisplayDialog("Validation Result", $"Validation failed: {errorMessage}", "OK");
//                 }
//             }
            
//             EditorGUILayout.Space();
            
//             // Quick actions
//             EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            
//             EditorGUILayout.BeginHorizontal();
            
//             if (GUILayout.Button("Add Empty MCQ"))
//             {
//                 var newMcq = new McqData("New Question", "Correct Answer", new string[] { "Wrong 1", "Wrong 2", "Wrong 3" });
//                 mcqDataSet.AddQuestion(newMcq);
//                 serializedObject.Update();
//                 EditorUtility.SetDirty(mcqDataSet);
//             }
            
//             if (GUILayout.Button("Clear All"))
//             {
//                 if (EditorUtility.DisplayDialog("Clear All MCQs", "Are you sure you want to remove all MCQ data?", "Yes", "Cancel"))
//                 {
//                     mcqDataSet.ClearQuestions();
//                     serializedObject.Update();
//                     EditorUtility.SetDirty(mcqDataSet);
//                 }
//             }
            
//             EditorGUILayout.EndHorizontal();
            
//             // Statistics
//             EditorGUILayout.Space();
//             EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
//             EditorGUILayout.LabelField($"Total Questions: {mcqDataSet.QuestionCount}");
            
//             if (mcqDataSet.QuestionCount > 0)
//             {
//                 int timedQuestions = 0;
//                 foreach (var question in mcqDataSet.Questions)
//                 {
//                     if (question.TimeLimit > 0)
//                         timedQuestions++;
//                 }
//                 EditorGUILayout.LabelField($"Timed Questions: {timedQuestions}");
//                 EditorGUILayout.LabelField($"Regular Questions: {mcqDataSet.QuestionCount - timedQuestions}");
//             }
//         }
//     }
// }