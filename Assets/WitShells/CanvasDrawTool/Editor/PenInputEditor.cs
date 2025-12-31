namespace WitShells.CanvasDrawTool.Editor
{
    using UnityEngine;
    using UnityEditor;
    using WitShells.CanvasDrawTool;  // Import runtime types

    /// <summary>
    /// Custom inspector for PenInput with pressure and pen settings.
    /// </summary>
    [CustomEditor(typeof(PenInput))]
    public class PenInputEditor : Editor
    {
        private PenInput _penInput;
        private bool _showPenStatus = true;
        private bool _showPressureTest = true;

        private void OnEnable()
        {
            _penInput = (PenInput)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (!Application.isPlaying) return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            DrawPenStatus();
            DrawPressureTest();
        }

        private void DrawPenStatus()
        {
            _showPenStatus = EditorGUILayout.Foldout(_showPenStatus, "Pen Status (Live)", true, EditorStyles.foldoutHeader);
            if (!_showPenStatus) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Pen availability
            string penStatus = _penInput.IsPenAvailable ? "✓ Pen Connected" : "✗ No Pen Detected";
            GUIStyle statusStyle = new GUIStyle(EditorStyles.boldLabel);
            statusStyle.normal.textColor = _penInput.IsPenAvailable ? Color.green : Color.red;
            EditorGUILayout.LabelField(penStatus, statusStyle);

            if (_penInput.IsPenAvailable)
            {
                EditorGUILayout.Space(5);

                // Pressure
                EditorGUILayout.LabelField($"Raw Pressure: {_penInput.RawPressure:F3}");
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), _penInput.RawPressure, $"Pressure: {_penInput.RawPressure:P0}");

                // Tilt
                EditorGUILayout.LabelField($"Tilt: {_penInput.Tilt}");

                // Button states
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Button States:", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField($"Barrel Button: {(_penInput.IsBarrelButtonPressed ? "Pressed" : "Released")}");
                EditorGUILayout.LabelField($"Eraser Tip: {(_penInput.IsEraserTip ? "Active" : "Inactive")}");
                EditorGUILayout.LabelField($"Is Drawing: {(_penInput.IsDrawing ? "Yes" : "No")}");
            }
            else
            {
                EditorGUILayout.HelpBox("Connect a pen/stylus tablet to see live input data.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();

            // Force repaint for live updates
            Repaint();
        }

        private void DrawPressureTest()
        {
            _showPressureTest = EditorGUILayout.Foldout(_showPressureTest, "Pressure Settings Test", true, EditorStyles.foldoutHeader);
            if (!_showPressureTest) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Simulate pressure to test settings:", EditorStyles.miniBoldLabel);

            float testPressure = EditorGUILayout.Slider("Test Pressure", 0.5f, 0f, 1f);

            EditorGUILayout.Space(5);

            // Calculate effective values
            int baseSize = 20;
            float baseOpacity = 1f;

            int effectiveSize = _penInput.GetEffectiveSize(baseSize, testPressure);
            float effectiveOpacity = _penInput.GetEffectiveOpacity(baseOpacity, testPressure);

            EditorGUILayout.LabelField($"Base Size: {baseSize} → Effective: {effectiveSize}");
            EditorGUILayout.LabelField($"Base Opacity: {baseOpacity:P0} → Effective: {effectiveOpacity:P0}");

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Settings:", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"Use Pressure for Size: {_penInput.UsePressureForSize}");
            EditorGUILayout.LabelField($"Use Pressure for Opacity: {_penInput.UsePressureForOpacity}");
            EditorGUILayout.LabelField($"Barrel Button Action: {_penInput.CurrentBarrelAction}");

            EditorGUILayout.EndVertical();
        }
    }
}
