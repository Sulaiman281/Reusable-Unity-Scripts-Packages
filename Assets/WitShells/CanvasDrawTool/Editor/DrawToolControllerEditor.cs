namespace WitShells.CanvasDrawTool.Editor
{
    using UnityEngine;
    using UnityEditor;
    using System.IO;
    using WitShells.CanvasDrawTool;  // Import runtime types

    /// <summary>
    /// Custom inspector for DrawToolController with full testing capabilities.
    /// Uses DrawToolSettings for all brush and color operations.
    /// </summary>
    [CustomEditor(typeof(DrawToolController))]
    public class DrawToolControllerEditor : Editor
    {
        private DrawToolController _controller;
        private bool _showToolSettings = true;
        private bool _showBrushSettings = true;
        private bool _showColorSettings = true;
        private bool _showLayerControls = true;
        private bool _showCanvasControls = true;
        private bool _showExportControls = true;
        private bool _showTestDrawing = true;

        // Test drawing state
        private Vector2Int _testDrawStart = new Vector2Int(100, 100);
        private Vector2Int _testDrawEnd = new Vector2Int(200, 200);

        // Canvas settings
        private int _newCanvasWidth = 512;
        private int _newCanvasHeight = 512;
        private Color _newCanvasColor = Color.white;

        // Export settings
        private string _exportPath = "";
        private int _jpgQuality = 90;

        private void OnEnable()
        {
            _controller = (DrawToolController)target;
            _exportPath = Application.dataPath + "/ExportedDrawing.png";
        }

        public override void OnInspectorGUI()
        {
            // Draw default inspector
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.LabelField("Testing Controls", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Use these controls to test the drawing tool without UI components.", MessageType.Info);

            // Draw debug status in both edit and play mode
            DrawDebugStatus();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to test drawing functionality.", MessageType.Warning);
                return;
            }

            DrawToolSelectionSection();
            DrawBrushSettings();
            DrawColorSettings();
            DrawLayerControls();
            DrawCanvasControls();
            DrawTestDrawing();
            DrawExportControls();
        }

        private void DrawToolSelectionSection()
        {
            _showToolSettings = EditorGUILayout.Foldout(_showToolSettings, "Tool Selection", true, EditorStyles.foldoutHeader);
            if (!_showToolSettings) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DrawToolSettings settings = _controller.Settings;
            if (settings == null)
            {
                EditorGUILayout.HelpBox("No DrawToolSettings assigned!", MessageType.Error);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Brush", GetToolButtonStyle(DrawToolSettings.DrawingTool.Brush)))
                settings.CurrentTool = DrawToolSettings.DrawingTool.Brush;
            if (GUILayout.Button("Eraser", GetToolButtonStyle(DrawToolSettings.DrawingTool.Eraser)))
                settings.CurrentTool = DrawToolSettings.DrawingTool.Eraser;
            if (GUILayout.Button("Eyedropper", GetToolButtonStyle(DrawToolSettings.DrawingTool.Eyedropper)))
                settings.CurrentTool = DrawToolSettings.DrawingTool.Eyedropper;
            if (GUILayout.Button("Fill", GetToolButtonStyle(DrawToolSettings.DrawingTool.Fill)))
                settings.CurrentTool = DrawToolSettings.DrawingTool.Fill;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Line", GetToolButtonStyle(DrawToolSettings.DrawingTool.Line)))
                settings.CurrentTool = DrawToolSettings.DrawingTool.Line;
            if (GUILayout.Button("Rectangle", GetToolButtonStyle(DrawToolSettings.DrawingTool.Rectangle)))
                settings.CurrentTool = DrawToolSettings.DrawingTool.Rectangle;
            if (GUILayout.Button("Ellipse", GetToolButtonStyle(DrawToolSettings.DrawingTool.Ellipse)))
                settings.CurrentTool = DrawToolSettings.DrawingTool.Ellipse;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"Current Tool: {settings.CurrentTool}", EditorStyles.boldLabel);

            EditorGUILayout.EndVertical();
        }

        private GUIStyle GetToolButtonStyle(DrawToolSettings.DrawingTool tool)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button);
            DrawToolSettings settings = _controller.Settings;
            if (settings != null && settings.CurrentTool == tool)
            {
                style.normal.textColor = Color.green;
                style.fontStyle = FontStyle.Bold;
            }
            return style;
        }

        private void DrawBrushSettings()
        {
            _showBrushSettings = EditorGUILayout.Foldout(_showBrushSettings, "Brush Settings", true, EditorStyles.foldoutHeader);
            if (!_showBrushSettings) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DrawToolSettings settings = _controller.Settings;
            if (settings == null)
            {
                EditorGUILayout.HelpBox("No DrawToolSettings assigned!", MessageType.Error);
                EditorGUILayout.EndVertical();
                return;
            }

            // Read from settings
            Brush.BrushType brushType = (Brush.BrushType)EditorGUILayout.EnumPopup("Brush Type", settings.BrushType);
            if (brushType != settings.BrushType) settings.BrushType = brushType;

            int brushSize = EditorGUILayout.IntSlider("Size", settings.BrushSize, 1, 100);
            if (brushSize != settings.BrushSize) settings.BrushSize = brushSize;

            float opacity = EditorGUILayout.Slider("Opacity", settings.BrushOpacity, 0f, 1f);
            if (!Mathf.Approximately(opacity, settings.BrushOpacity)) settings.BrushOpacity = opacity;

            float hardness = EditorGUILayout.Slider("Hardness", settings.BrushHardness, 0f, 1f);
            if (!Mathf.Approximately(hardness, settings.BrushHardness)) settings.BrushHardness = hardness;

            float spacing = EditorGUILayout.Slider("Spacing", settings.BrushSpacing, 0.01f, 2f);
            if (!Mathf.Approximately(spacing, settings.BrushSpacing)) settings.BrushSpacing = spacing;

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Quick Presets", EditorStyles.miniBoldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Default")) ApplyPreset(Brush.Default);
            if (GUILayout.Button("Soft")) ApplyPreset(Brush.Soft);
            if (GUILayout.Button("Airbrush")) ApplyPreset(Brush.Airbrush);
            if (GUILayout.Button("Pencil")) ApplyPreset(Brush.Pencil);
            if (GUILayout.Button("Eraser")) ApplyPreset(Brush.Eraser);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void ApplyPreset(Brush preset)
        {
            DrawToolSettings settings = _controller.Settings;
            if (settings != null)
            {
                settings.ApplyBrush(preset);
                Debug.Log($"Applied preset: {preset.Name}");
            }
        }

        private void DrawColorSettings()
        {
            _showColorSettings = EditorGUILayout.Foldout(_showColorSettings, "Color Settings", true, EditorStyles.foldoutHeader);
            if (!_showColorSettings) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DrawToolSettings settings = _controller.Settings;
            if (settings == null)
            {
                EditorGUILayout.HelpBox("No DrawToolSettings assigned!", MessageType.Error);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Primary Color", GUILayout.Width(100));
            Color primary = EditorGUILayout.ColorField(settings.PrimaryColor);
            if (primary != settings.PrimaryColor)
            {
                settings.PrimaryColor = primary;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Secondary Color", GUILayout.Width(100));
            Color secondary = EditorGUILayout.ColorField(settings.SecondaryColor);
            if (secondary != settings.SecondaryColor)
            {
                settings.SecondaryColor = secondary;
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Swap Colors"))
            {
                settings.SwapColors();
            }

            // Quick color buttons
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Quick Colors", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            if (ColorButton(Color.black)) settings.PrimaryColor = Color.black;
            if (ColorButton(Color.white)) settings.PrimaryColor = Color.white;
            if (ColorButton(Color.red)) settings.PrimaryColor = Color.red;
            if (ColorButton(Color.green)) settings.PrimaryColor = Color.green;
            if (ColorButton(Color.blue)) settings.PrimaryColor = Color.blue;
            if (ColorButton(Color.yellow)) settings.PrimaryColor = Color.yellow;
            if (ColorButton(Color.cyan)) settings.PrimaryColor = Color.cyan;
            if (ColorButton(Color.magenta)) settings.PrimaryColor = Color.magenta;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private bool ColorButton(Color color)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button);
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            style.normal.background = tex;
            return GUILayout.Button("", style, GUILayout.Width(25), GUILayout.Height(25));
        }

        private void DrawLayerControls()
        {
            _showLayerControls = EditorGUILayout.Foldout(_showLayerControls, "Layer Controls", true, EditorStyles.foldoutHeader);
            if (!_showLayerControls) return;

            LayerManager layerManager = _controller.LayerManager;
            if (layerManager == null)
            {
                EditorGUILayout.HelpBox("LayerManager not assigned.", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField($"Total Layers: {layerManager.LayerCount}");
            EditorGUILayout.LabelField($"Active Layer: {layerManager.ActiveLayerIndex}");

            if (layerManager.ActiveLayer != null)
            {
                EditorGUILayout.LabelField($"Active Layer Name: {layerManager.ActiveLayer.LayerName}");
            }

            EditorGUILayout.Space(5);

            // Layer list
            EditorGUILayout.LabelField("Layers:", EditorStyles.miniBoldLabel);
            for (int i = layerManager.LayerCount - 1; i >= 0; i--)
            {
                var layer = layerManager.Layers[i];
                if (layer == null) continue;

                EditorGUILayout.BeginHorizontal();

                // Active indicator
                string activeIndicator = (i == layerManager.ActiveLayerIndex) ? "► " : "   ";
                EditorGUILayout.LabelField(activeIndicator + layer.LayerName, GUILayout.Width(150));

                // Visibility toggle
                bool wasVisible = layer.IsVisible;
                layer.IsVisible = EditorGUILayout.Toggle(layer.IsVisible, GUILayout.Width(20));
                if (wasVisible != layer.IsVisible) layerManager.UpdateComposite();

                // Lock toggle
                layer.IsLocked = EditorGUILayout.Toggle(layer.IsLocked, GUILayout.Width(20));

                // Select button
                if (GUILayout.Button("Select", GUILayout.Width(50)))
                {
                    layerManager.SetActiveLayer(i);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(5);

            // Layer operations
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Layer"))
            {
                layerManager.CreateLayer($"Layer {layerManager.LayerCount + 1}");
                Debug.Log("Layer added");
            }
            if (GUILayout.Button("Delete Layer"))
            {
                if (layerManager.LayerCount > 1)
                {
                    layerManager.DeleteActiveLayer();
                    Debug.Log("Layer deleted");
                }
            }
            if (GUILayout.Button("Duplicate"))
            {
                layerManager.DuplicateLayer(layerManager.ActiveLayerIndex);
                Debug.Log("Layer duplicated");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Move Up"))
            {
                layerManager.MoveLayerUp(layerManager.ActiveLayerIndex);
            }
            if (GUILayout.Button("Move Down"))
            {
                layerManager.MoveLayerDown(layerManager.ActiveLayerIndex);
            }
            if (GUILayout.Button("Merge Down"))
            {
                layerManager.MergeDown(layerManager.ActiveLayerIndex);
            }
            if (GUILayout.Button("Flatten"))
            {
                layerManager.Flatten();
                Debug.Log("Layers flattened");
            }
            EditorGUILayout.EndHorizontal();

            // Clear active layer
            if (GUILayout.Button("Clear Active Layer"))
            {
                layerManager.ActiveLayer?.Clear(Color.clear);
                layerManager.ActiveLayer?.ApplyChanges();
                layerManager.UpdateComposite();
                Debug.Log("Active layer cleared");
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawCanvasControls()
        {
            _showCanvasControls = EditorGUILayout.Foldout(_showCanvasControls, "Canvas Controls", true, EditorStyles.foldoutHeader);
            if (!_showCanvasControls) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            LayerManager layerManager = _controller.LayerManager;
            if (layerManager != null)
            {
                EditorGUILayout.LabelField($"Canvas Size: {layerManager.CanvasWidth} x {layerManager.CanvasHeight}");
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Create New Canvas", EditorStyles.miniBoldLabel);

            _newCanvasWidth = EditorGUILayout.IntField("Width", _newCanvasWidth);
            _newCanvasHeight = EditorGUILayout.IntField("Height", _newCanvasHeight);
            _newCanvasColor = EditorGUILayout.ColorField("Background", _newCanvasColor);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("256x256")) { _newCanvasWidth = 256; _newCanvasHeight = 256; }
            if (GUILayout.Button("512x512")) { _newCanvasWidth = 512; _newCanvasHeight = 512; }
            if (GUILayout.Button("1024x1024")) { _newCanvasWidth = 1024; _newCanvasHeight = 1024; }
            if (GUILayout.Button("2048x2048")) { _newCanvasWidth = 2048; _newCanvasHeight = 2048; }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Create New Canvas"))
            {
                _controller.NewCanvas(_newCanvasWidth, _newCanvasHeight, _newCanvasColor);
                Debug.Log($"New canvas created: {_newCanvasWidth}x{_newCanvasHeight}");
            }

            if (GUILayout.Button("Clear Canvas"))
            {
                _controller.ClearCanvas();
                Debug.Log("Canvas cleared");
            }

            // Zoom controls
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Zoom", EditorStyles.miniBoldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Fit")) _controller.ZoomToFit();
            if (GUILayout.Button("100%")) _controller.ZoomToActualSize();
            if (GUILayout.Button("Zoom In")) _controller.CanvasUI?.ZoomIn();
            if (GUILayout.Button("Zoom Out")) _controller.CanvasUI?.ZoomOut();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawTestDrawing()
        {
            _showTestDrawing = EditorGUILayout.Foldout(_showTestDrawing, "Test Drawing (Manual)", true, EditorStyles.foldoutHeader);
            if (!_showTestDrawing) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.HelpBox("Use these controls to draw directly on the canvas for testing.", MessageType.Info);

            _testDrawStart = EditorGUILayout.Vector2IntField("Start Position", _testDrawStart);
            _testDrawEnd = EditorGUILayout.Vector2IntField("End Position", _testDrawEnd);

            DrawToolSettings settings = _controller.Settings;
            if (settings == null)
            {
                EditorGUILayout.HelpBox("No DrawToolSettings assigned!", MessageType.Error);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.Space(5);

            // Direct brush stamp
            if (GUILayout.Button("Draw Brush Stamp at Start"))
            {
                var engine = _controller.DrawingEngine;
                if (engine != null)
                {
                    engine.BeginDraw(_testDrawStart, 1f);
                    engine.EndDraw(_testDrawStart, 1f);
                    Debug.Log($"Brush stamp drawn at {_testDrawStart}");
                }
            }

            // Draw line
            if (GUILayout.Button("Draw Line (Start to End)"))
            {
                var engine = _controller.DrawingEngine;
                var layerManager = _controller.LayerManager;
                if (engine != null && layerManager?.ActiveLayer != null)
                {
                    engine.DrawLine(layerManager.ActiveLayer, _testDrawStart, _testDrawEnd, settings.PrimaryColor, settings.BrushSize);
                    Debug.Log($"Line drawn from {_testDrawStart} to {_testDrawEnd}");
                }
            }

            // Draw rectangle
            if (GUILayout.Button("Draw Rectangle"))
            {
                var engine = _controller.DrawingEngine;
                var layerManager = _controller.LayerManager;
                if (engine != null && layerManager?.ActiveLayer != null)
                {
                    Vector2Int min = new Vector2Int(
                        Mathf.Min(_testDrawStart.x, _testDrawEnd.x),
                        Mathf.Min(_testDrawStart.y, _testDrawEnd.y));
                    Vector2Int max = new Vector2Int(
                        Mathf.Max(_testDrawStart.x, _testDrawEnd.x),
                        Mathf.Max(_testDrawStart.y, _testDrawEnd.y));
                    engine.DrawRectangle(layerManager.ActiveLayer, min, max, settings.PrimaryColor, false, settings.BrushSize);
                    Debug.Log($"Rectangle drawn from {min} to {max}");
                }
            }

            // Draw filled rectangle
            if (GUILayout.Button("Draw Filled Rectangle"))
            {
                var engine = _controller.DrawingEngine;
                var layerManager = _controller.LayerManager;
                if (engine != null && layerManager?.ActiveLayer != null)
                {
                    Vector2Int min = new Vector2Int(
                        Mathf.Min(_testDrawStart.x, _testDrawEnd.x),
                        Mathf.Min(_testDrawStart.y, _testDrawEnd.y));
                    Vector2Int max = new Vector2Int(
                        Mathf.Max(_testDrawStart.x, _testDrawEnd.x),
                        Mathf.Max(_testDrawStart.y, _testDrawEnd.y));
                    engine.DrawRectangle(layerManager.ActiveLayer, min, max, settings.PrimaryColor, true, settings.BrushSize);
                    Debug.Log($"Filled rectangle drawn from {min} to {max}");
                }
            }

            // Draw ellipse
            if (GUILayout.Button("Draw Ellipse"))
            {
                var engine = _controller.DrawingEngine;
                var layerManager = _controller.LayerManager;
                if (engine != null && layerManager?.ActiveLayer != null)
                {
                    Vector2Int center = new Vector2Int(
                        (_testDrawStart.x + _testDrawEnd.x) / 2,
                        (_testDrawStart.y + _testDrawEnd.y) / 2);
                    int radiusX = Mathf.Abs(_testDrawEnd.x - _testDrawStart.x) / 2;
                    int radiusY = Mathf.Abs(_testDrawEnd.y - _testDrawStart.y) / 2;
                    engine.DrawEllipse(layerManager.ActiveLayer, center, radiusX, radiusY, settings.PrimaryColor, false);
                    Debug.Log($"Ellipse drawn at center {center}, radii ({radiusX}, {radiusY})");
                }
            }

            // Flood fill
            if (GUILayout.Button("Flood Fill at Start"))
            {
                var engine = _controller.DrawingEngine;
                var layerManager = _controller.LayerManager;
                if (engine != null && layerManager?.ActiveLayer != null)
                {
                    engine.FloodFill(layerManager.ActiveLayer, _testDrawStart, settings.PrimaryColor);
                    Debug.Log($"Flood fill at {_testDrawStart} with color {settings.PrimaryColor}");
                }
            }

            // Draw stroke (simulated)
            if (GUILayout.Button("Draw Stroke (Simulated Mouse Drag)"))
            {
                var engine = _controller.DrawingEngine;
                if (engine != null)
                {
                    engine.BeginDraw(_testDrawStart, 1f);
                    engine.ContinueDraw(_testDrawEnd, 1f);
                    engine.EndDraw(_testDrawEnd, 1f);
                    Debug.Log($"Stroke drawn from {_testDrawStart} to {_testDrawEnd}");
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawExportControls()
        {
            _showExportControls = EditorGUILayout.Foldout(_showExportControls, "Export Controls", true, EditorStyles.foldoutHeader);
            if (!_showExportControls) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            _exportPath = EditorGUILayout.TextField("Export Path", _exportPath);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string path = EditorUtility.SaveFilePanel("Export Drawing", Application.dataPath, "Drawing", "png");
                if (!string.IsNullOrEmpty(path))
                    _exportPath = path;
            }
            EditorGUILayout.EndHorizontal();

            _jpgQuality = EditorGUILayout.IntSlider("JPG Quality", _jpgQuality, 1, 100);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export as PNG"))
            {
                string path = _exportPath;
                if (!path.EndsWith(".png")) path = Path.ChangeExtension(path, ".png");
                _controller.ExportAsPNG(path);
                AssetDatabase.Refresh();
                Debug.Log($"Exported to: {path}");
                EditorUtility.DisplayDialog("Export Complete", $"Drawing exported to:\n{path}", "OK");
            }
            if (GUILayout.Button("Export as JPG"))
            {
                string path = _exportPath;
                if (!path.EndsWith(".jpg")) path = Path.ChangeExtension(path, ".jpg");
                _controller.ExportAsJPG(path, _jpgQuality);
                AssetDatabase.Refresh();
                Debug.Log($"Exported to: {path}");
                EditorUtility.DisplayDialog("Export Complete", $"Drawing exported to:\n{path}", "OK");
            }
            EditorGUILayout.EndHorizontal();

            // Import
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Import Image", EditorStyles.miniBoldLabel);

            if (GUILayout.Button("Import Image to Active Layer"))
            {
                string path = EditorUtility.OpenFilePanel("Import Image", Application.dataPath, "png,jpg,jpeg");
                if (!string.IsNullOrEmpty(path))
                {
                    _controller.ImportImage(path);
                    Debug.Log($"Image imported from: {path}");
                }
            }

            if (GUILayout.Button("Import Image to New Layer"))
            {
                string path = EditorUtility.OpenFilePanel("Import Image", Application.dataPath, "png,jpg,jpeg");
                if (!string.IsNullOrEmpty(path))
                {
                    _controller.ImportImageToNewLayer(path);
                    Debug.Log($"Image imported to new layer from: {path}");
                }
            }

            if (GUILayout.Button("Import as Transformable Layer"))
            {
                string path = EditorUtility.OpenFilePanel("Import Image", Application.dataPath, "png,jpg,jpeg");
                if (!string.IsNullOrEmpty(path))
                {
                    _controller.ImportImageAsTransformableLayer(path);
                    Debug.Log($"Image imported as transformable layer from: {path}");
                }
            }

            // Transform controls
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Transform Controls", EditorStyles.miniBoldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Size"))
            {
                _controller.ResetSelectedLayerSize();
            }
            if (GUILayout.Button("Fit to Canvas"))
            {
                _controller.FitSelectedLayerToCanvas();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Clear Selection"))
            {
                _controller.ClearLayerSelection();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDebugStatus()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Component Status", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Check all required components
            bool hasSettings = _controller.Settings != null;
            bool hasCanvasUI = _controller.CanvasUI != null;
            bool hasLayerManager = _controller.LayerManager != null;
            bool hasDrawingEngine = _controller.DrawingEngine != null;
            bool hasDrawingInput = _controller.DrawingInput != null;
            bool hasPenInput = _controller.PenInput != null;

            DrawStatusLine("Settings", hasSettings);
            DrawStatusLine("Canvas UI", hasCanvasUI);
            DrawStatusLine("Layer Manager", hasLayerManager);
            DrawStatusLine("Drawing Engine", hasDrawingEngine);
            DrawStatusLine("Drawing Input", hasDrawingInput);
            DrawStatusLine("Pen Input (Optional)", hasPenInput);

            // Check input references
            if (hasDrawingInput)
            {
                var input = _controller.DrawingInput;
                var inputSO = new SerializedObject(input);
                var canvasRectProp = inputSO.FindProperty("_canvasRect");
                bool hasCanvasRect = canvasRectProp?.objectReferenceValue != null;
                DrawStatusLine("  - Canvas Rect", hasCanvasRect);
            }

            // Runtime status (only in play mode)
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Runtime Status", EditorStyles.miniBoldLabel);

                if (hasLayerManager)
                {
                    var lm = _controller.LayerManager;
                    EditorGUILayout.LabelField($"  Canvas Size: {lm.CanvasWidth} x {lm.CanvasHeight}");
                    EditorGUILayout.LabelField($"  Layer Count: {lm.LayerCount}");
                    EditorGUILayout.LabelField($"  Active Layer: {lm.ActiveLayerIndex}");
                    DrawStatusLine("  Composite Texture", lm.CompositeTexture != null);
                }

                if (hasDrawingInput)
                {
                    EditorGUILayout.LabelField($"  Is Drawing: {_controller.DrawingInput.IsDrawing}");
                    EditorGUILayout.LabelField($"  Last Position: {_controller.DrawingInput.LastPosition}");
                }

                if (hasSettings)
                {
                    EditorGUILayout.LabelField($"  Current Tool: {_controller.Settings.CurrentTool}");
                    EditorGUILayout.LabelField($"  Brush Type: {_controller.Settings.BrushType}");
                    EditorGUILayout.LabelField($"  Brush Size: {_controller.Settings.BrushSize}");
                }
            }

            // Show setup button if missing components
            if (!hasSettings || !hasCanvasUI || !hasLayerManager || !hasDrawingEngine || !hasDrawingInput)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("Some components are missing. Run setup to configure.", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawStatusLine(string label, bool isOk)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(150));
            GUIStyle style = new GUIStyle(EditorStyles.label);
            style.normal.textColor = isOk ? Color.green : Color.red;
            EditorGUILayout.LabelField(isOk ? "✓ OK" : "✗ Missing", style);
            EditorGUILayout.EndHorizontal();
        }
    }
}
