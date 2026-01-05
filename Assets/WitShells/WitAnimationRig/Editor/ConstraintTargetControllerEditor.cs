namespace WitShells.AnimationRig.Editor
{
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Custom editor for ConstraintTargetController with improved UI.
    /// </summary>
    [CustomEditor(typeof(ConstraintTargetController))]
    public class ConstraintTargetControllerEditor : Editor
    {
        private SerializedProperty rigReferencesProperty;
        private SerializedProperty headTargetProperty;
        private SerializedProperty leftHandTargetProperty;
        private SerializedProperty rightHandTargetProperty;
        private SerializedProperty leftLegTargetProperty;
        private SerializedProperty rightLegTargetProperty;
        private SerializedProperty enableConstraintsProperty;
        private SerializedProperty masterWeightProperty;
        private SerializedProperty updateModeProperty;

        private bool showHeadSection = true;
        private bool showHandsSection = true;
        private bool showLegsSection = true;

        private GUIStyle headerStyle;
        private GUIStyle sectionStyle;
        private bool stylesInitialized;

        private void OnEnable()
        {
            rigReferencesProperty = serializedObject.FindProperty("rigReferences");
            headTargetProperty = serializedObject.FindProperty("headTarget");
            leftHandTargetProperty = serializedObject.FindProperty("leftHandTarget");
            rightHandTargetProperty = serializedObject.FindProperty("rightHandTarget");
            leftLegTargetProperty = serializedObject.FindProperty("leftLegTarget");
            rightLegTargetProperty = serializedObject.FindProperty("rightLegTarget");
            enableConstraintsProperty = serializedObject.FindProperty("enableConstraints");
            masterWeightProperty = serializedObject.FindProperty("masterWeight");
            updateModeProperty = serializedObject.FindProperty("updateMode");
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12
            };

            sectionStyle = new GUIStyle("box")
            {
                padding = new RectOffset(10, 10, 5, 5),
                margin = new RectOffset(0, 0, 5, 5)
            };

            stylesInitialized = true;
        }

        public override void OnInspectorGUI()
        {
            InitStyles();
            serializedObject.Update();

            // Header
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Constraint Target Controller", headerStyle);
            DrawSeparator();

            // Global Settings
            EditorGUILayout.BeginVertical(sectionStyle);
            EditorGUILayout.LabelField("Global Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(rigReferencesProperty);
            EditorGUILayout.PropertyField(enableConstraintsProperty);
            EditorGUILayout.PropertyField(masterWeightProperty);
            EditorGUILayout.PropertyField(updateModeProperty);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Head Section
            showHeadSection = EditorGUILayout.Foldout(showHeadSection, "🔵 Head Constraint", true);
            if (showHeadSection)
            {
                EditorGUI.indentLevel++;
                DrawConstraintTarget(headTargetProperty, "Head Target");
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(3);

            // Hands Section
            showHandsSection = EditorGUILayout.Foldout(showHandsSection, "🟢 Hand Constraints", true);
            if (showHandsSection)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.LabelField("Left Hand", EditorStyles.miniBoldLabel);
                DrawConstraintTarget(leftHandTargetProperty, "Left Hand Target");

                EditorGUILayout.Space(5);

                EditorGUILayout.LabelField("Right Hand", EditorStyles.miniBoldLabel);
                DrawConstraintTarget(rightHandTargetProperty, "Right Hand Target");

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(3);

            // Legs Section
            showLegsSection = EditorGUILayout.Foldout(showLegsSection, "🟠 Leg Constraints", true);
            if (showLegsSection)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.LabelField("Left Leg", EditorStyles.miniBoldLabel);
                DrawConstraintTarget(leftLegTargetProperty, "Left Leg Target");

                EditorGUILayout.Space(5);

                EditorGUILayout.LabelField("Right Leg", EditorStyles.miniBoldLabel);
                DrawConstraintTarget(rightLegTargetProperty, "Right Leg Target");

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);

            // Quick Actions
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Snap All", GUILayout.Height(25)))
            {
                var controller = (ConstraintTargetController)target;
                controller.SnapAll();
            }

            if (GUILayout.Button("Reset All", GUILayout.Height(25)))
            {
                var controller = (ConstraintTargetController)target;
                controller.ResetAll();
            }

            if (GUILayout.Button("Clear Sources", GUILayout.Height(25)))
            {
                var controller = (ConstraintTargetController)target;
                controller.ClearAllSources();
            }

            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawConstraintTarget(SerializedProperty property, string label)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Source and Target
            var sourceProp = property.FindPropertyRelative("source");
            var targetProp = property.FindPropertyRelative("target");
            var weightProp = property.FindPropertyRelative("weight");

            EditorGUILayout.PropertyField(sourceProp, new GUIContent("Source Transform"));
            EditorGUILayout.PropertyField(targetProp, new GUIContent("IK Target"));
            EditorGUILayout.Slider(weightProp, 0f, 1f, "Weight");

            // Position Constraint Section
            EditorGUILayout.Space(3);
            var constrainPosProp = property.FindPropertyRelative("constrainPosition");
            EditorGUILayout.PropertyField(constrainPosProp, new GUIContent("Constrain Position"));

            if (constrainPosProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Axes");

                var posXProp = property.FindPropertyRelative("constrainPositionX");
                var posYProp = property.FindPropertyRelative("constrainPositionY");
                var posZProp = property.FindPropertyRelative("constrainPositionZ");

                posXProp.boolValue = GUILayout.Toggle(posXProp.boolValue, "X", "Button", GUILayout.Width(30));
                posYProp.boolValue = GUILayout.Toggle(posYProp.boolValue, "Y", "Button", GUILayout.Width(30));
                posZProp.boolValue = GUILayout.Toggle(posZProp.boolValue, "Z", "Button", GUILayout.Width(30));

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(property.FindPropertyRelative("positionOffset"), new GUIContent("Offset"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("positionSmoothSpeed"), new GUIContent("Smooth Speed"));

                EditorGUI.indentLevel--;
            }

            // Rotation Constraint Section
            EditorGUILayout.Space(3);
            var constrainRotProp = property.FindPropertyRelative("constrainRotation");
            EditorGUILayout.PropertyField(constrainRotProp, new GUIContent("Constrain Rotation"));

            if (constrainRotProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Axes");

                var rotXProp = property.FindPropertyRelative("constrainRotationX");
                var rotYProp = property.FindPropertyRelative("constrainRotationY");
                var rotZProp = property.FindPropertyRelative("constrainRotationZ");

                rotXProp.boolValue = GUILayout.Toggle(rotXProp.boolValue, "X", "Button", GUILayout.Width(30));
                rotYProp.boolValue = GUILayout.Toggle(rotYProp.boolValue, "Y", "Button", GUILayout.Width(30));
                rotZProp.boolValue = GUILayout.Toggle(rotZProp.boolValue, "Z", "Button", GUILayout.Width(30));

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(property.FindPropertyRelative("rotationOffset"), new GUIContent("Offset"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("rotationSmoothSpeed"), new GUIContent("Smooth Speed"));

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSeparator()
        {
            EditorGUILayout.Space(3);
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            EditorGUILayout.Space(3);
        }
    }

    /// <summary>
    /// Custom editor for RigReferences with quick actions.
    /// </summary>
    [CustomEditor(typeof(RigReferences))]
    public class RigReferencesEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Rig References", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "This component holds references to all IK constraints. " +
                "Constraints are auto-assigned on validation.",
                MessageType.Info);

            EditorGUILayout.Space(5);

            DrawDefaultInspector();

            EditorGUILayout.Space(10);

            // Quick weight controls
            EditorGUILayout.LabelField("Quick Weight Controls", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("All Weights = 0"))
            {
                var refs = (RigReferences)target;
                refs.ResetAllWeights();
            }

            if (GUILayout.Button("All Weights = 1"))
            {
                var refs = (RigReferences)target;
                refs.SetAllWeights(1f);
            }

            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
