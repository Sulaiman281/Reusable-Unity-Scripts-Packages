namespace WitShells.CanvasDrawTool.Editor
{
    using UnityEngine;
    using UnityEditor;
    using WitShells.CanvasDrawTool;  // Import runtime types

    /// <summary>
    /// Custom inspector for DrawingEngine with tool and brush controls.
    /// Uses DrawToolSettings for brush and color operations.
    /// </summary>
    [CustomEditor(typeof(DrawingEngine))]
    public class DrawingEngineEditor : Editor
    {
        private DrawingEngine _engine;
        private bool _showQuickTools = true;
        private bool _showBrushPresets = true;

        private void OnEnable()
        {
            _engine = (DrawingEngine)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (!Application.isPlaying) return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            DrawQuickTools();
            DrawBrushPresets();
            DrawCurrentBrushInfo();
        }

        private void DrawQuickTools()
        {
            _showQuickTools = EditorGUILayout.Foldout(_showQuickTools, "Quick Tool Selection", true, EditorStyles.foldoutHeader);
            if (!_showQuickTools) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            foreach (DrawingEngine.DrawTool tool in System.Enum.GetValues(typeof(DrawingEngine.DrawTool)))
            {
                GUIStyle style = new GUIStyle(GUI.skin.button);
                if (_engine.CurrentTool == tool)
                {
                    style.fontStyle = FontStyle.Bold;
                    style.normal.textColor = Color.green;
                }

                if (GUILayout.Button(tool.ToString(), style))
                {
                    _engine.SetTool(tool);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawBrushPresets()
        {
            _showBrushPresets = EditorGUILayout.Foldout(_showBrushPresets, "Brush Presets", true, EditorStyles.foldoutHeader);
            if (!_showBrushPresets) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Apply presets via settings if available
            DrawToolSettings settings = _engine.Settings;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Default")) ApplyPreset(settings, Brush.Default);
            if (GUILayout.Button("Soft")) ApplyPreset(settings, Brush.Soft);
            if (GUILayout.Button("Airbrush")) ApplyPreset(settings, Brush.Airbrush);
            if (GUILayout.Button("Pencil")) ApplyPreset(settings, Brush.Pencil);
            if (GUILayout.Button("Eraser")) ApplyPreset(settings, Brush.Eraser);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Color selection from settings
            if (settings != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Primary Color", GUILayout.Width(100));
                Color newColor = EditorGUILayout.ColorField(settings.PrimaryColor);
                if (newColor != settings.PrimaryColor)
                {
                    settings.PrimaryColor = newColor;
                }
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Swap Primary/Secondary"))
                {
                    settings.SwapColors();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No DrawToolSettings assigned to engine.", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void ApplyPreset(DrawToolSettings settings, Brush preset)
        {
            if (settings != null)
            {
                settings.ApplyBrush(preset);
                Debug.Log($"Applied preset: {preset.Name}");
            }
            else
            {
                // Fallback to engine's SetBrush for backwards compatibility
                _engine.SetBrush(preset);
            }
        }

        private void DrawCurrentBrushInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Current Brush Info", EditorStyles.boldLabel);

            DrawToolSettings settings = _engine.Settings;
            if (settings != null)
            {
                EditorGUILayout.LabelField($"Type: {settings.BrushType}");
                EditorGUILayout.LabelField($"Size: {settings.BrushSize}");
                EditorGUILayout.LabelField($"Opacity: {settings.BrushOpacity:P0}");
                EditorGUILayout.LabelField($"Hardness: {settings.BrushHardness:P0}");
                EditorGUILayout.LabelField($"Spacing: {settings.BrushSpacing:F2}");
            }
            else
            {
                EditorGUILayout.LabelField("No settings assigned");
            }

            EditorGUILayout.EndVertical();
        }
    }
}
