namespace WitShells.CanvasDrawTool.Editor
{
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEditor;
    using WitShells.CanvasDrawTool;  // Import runtime types

    /// <summary>
    /// Editor utility for quick setup of Canvas Draw Tool on a Canvas.
    /// </summary>
    public static class CanvasDrawToolSetup
    {
        private const string MENU_PATH = "GameObject/WitShells/Setup Canvas Draw Tool";
        private const string CONTEXT_MENU_PATH = "CONTEXT/Canvas/Setup Draw Tool";

        [MenuItem(MENU_PATH, false, 10)]
        public static void SetupDrawToolOnCanvas()
        {
            GameObject selected = Selection.activeGameObject;

            if (selected == null)
            {
                EditorUtility.DisplayDialog("Canvas Draw Tool Setup",
                    "Please select a Canvas GameObject first.", "OK");
                return;
            }

            Canvas canvas = selected.GetComponent<Canvas>();
            if (canvas == null)
            {
                // Check if parent has canvas
                canvas = selected.GetComponentInParent<Canvas>();
                if (canvas == null)
                {
                    EditorUtility.DisplayDialog("Canvas Draw Tool Setup",
                        "Selected GameObject must be a Canvas or be under a Canvas.", "OK");
                    return;
                }
            }

            SetupDrawTool(canvas);
        }

        [MenuItem(MENU_PATH, true)]
        public static bool ValidateSetupDrawToolOnCanvas()
        {
            if (Selection.activeGameObject == null) return false;

            Canvas canvas = Selection.activeGameObject.GetComponent<Canvas>();
            if (canvas == null)
                canvas = Selection.activeGameObject.GetComponentInParent<Canvas>();

            return canvas != null;
        }

        [MenuItem(CONTEXT_MENU_PATH)]
        public static void SetupDrawToolFromContext(MenuCommand command)
        {
            Canvas canvas = command.context as Canvas;
            if (canvas != null)
            {
                SetupDrawTool(canvas);
            }
        }

        /// <summary>
        /// Main setup method that creates all necessary components.
        /// Updated for LayerObject-based system with individual RawImage per layer.
        /// </summary>
        public static void SetupDrawTool(Canvas canvas)
        {
            Undo.SetCurrentGroupName("Setup Canvas Draw Tool");
            int group = Undo.GetCurrentGroup();

            // Create root DrawingTool GameObject
            GameObject drawingToolRoot = new GameObject("DrawingTool");
            Undo.RegisterCreatedObjectUndo(drawingToolRoot, "Create DrawingTool");
            drawingToolRoot.transform.SetParent(canvas.transform, false);

            // Setup RectTransform to fill canvas
            RectTransform rootRect = drawingToolRoot.AddComponent<RectTransform>();
            SetupFullStretch(rootRect);

            // Create the layer container (where layer RawImages will be spawned)
            // Use center anchors with fixed size - actual size will be set at runtime
            GameObject layerContainer = new GameObject("LayerContainer");
            Undo.RegisterCreatedObjectUndo(layerContainer, "Create LayerContainer");
            layerContainer.transform.SetParent(drawingToolRoot.transform, false);
            RectTransform layerContainerRect = layerContainer.AddComponent<RectTransform>();
            layerContainerRect.anchorMin = new Vector2(0.5f, 0.5f);
            layerContainerRect.anchorMax = new Vector2(0.5f, 0.5f);
            layerContainerRect.pivot = new Vector2(0.5f, 0.5f);
            layerContainerRect.sizeDelta = new Vector2(1024, 1024); // Default size, will be updated at runtime
            layerContainerRect.anchoredPosition = Vector2.zero;

            // Add Outline for visibility to the layer container
            Outline outline = layerContainer.AddComponent<Outline>();
            outline.effectColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            outline.effectDistance = new Vector2(2, -2);

            // Add LayerManager
            LayerManager layerManager = drawingToolRoot.AddComponent<LayerManager>();

            // Add DrawingEngine
            DrawingEngine drawingEngine = drawingToolRoot.AddComponent<DrawingEngine>();

            // Add DrawCanvasUI to layer container
            DrawCanvasUI drawCanvasUI = layerContainer.AddComponent<DrawCanvasUI>();

            // Add Input handlers
            DrawingInput drawingInput = drawingToolRoot.AddComponent<DrawingInput>();
            PenInput penInput = drawingToolRoot.AddComponent<PenInput>();

            // Add main controller
            DrawToolController controller = drawingToolRoot.AddComponent<DrawToolController>();

            // Create DrawToolSettings asset if needed or find existing
            DrawToolSettings settings = CreateOrFindSettings();

            // Wire up references using SerializedObject
            SerializedObject controllerSO = new SerializedObject(controller);
            controllerSO.FindProperty("_settings").objectReferenceValue = settings;
            controllerSO.FindProperty("_canvasUI").objectReferenceValue = drawCanvasUI;
            controllerSO.FindProperty("_layerManager").objectReferenceValue = layerManager;
            controllerSO.FindProperty("_drawingEngine").objectReferenceValue = drawingEngine;
            controllerSO.FindProperty("_drawingInput").objectReferenceValue = drawingInput;
            controllerSO.FindProperty("_penInput").objectReferenceValue = penInput;
            controllerSO.ApplyModifiedProperties();

            // Wire up DrawCanvasUI (no longer has _canvasImage, uses _canvasRect and _backgroundImage)
            SerializedObject canvasUISO = new SerializedObject(drawCanvasUI);
            canvasUISO.FindProperty("_canvasRect").objectReferenceValue = layerContainerRect;
            canvasUISO.FindProperty("_layerManager").objectReferenceValue = layerManager;
            canvasUISO.FindProperty("_drawingEngine").objectReferenceValue = drawingEngine;
            canvasUISO.ApplyModifiedProperties();

            // Wire up LayerManager - set layer container
            SerializedObject layerManagerSO = new SerializedObject(layerManager);
            layerManagerSO.FindProperty("_layerContainer").objectReferenceValue = layerContainerRect;
            layerManagerSO.ApplyModifiedProperties();

            // Wire up DrawingEngine
            SerializedObject engineSO = new SerializedObject(drawingEngine);
            engineSO.FindProperty("_layerManager").objectReferenceValue = layerManager;
            engineSO.FindProperty("_settings").objectReferenceValue = settings;
            engineSO.ApplyModifiedProperties();

            // Wire up DrawingInput
            SerializedObject inputSO = new SerializedObject(drawingInput);
            inputSO.FindProperty("_canvasRect").objectReferenceValue = layerContainerRect;
            // Camera is auto-detected at runtime based on canvas render mode
            // Only set for non-overlay modes
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                Camera uiCamera = canvas.worldCamera ?? Camera.main;
                inputSO.FindProperty("_uiCamera").objectReferenceValue = uiCamera;
            }
            inputSO.ApplyModifiedProperties();

            // Wire up PenInput
            SerializedObject penSO = new SerializedObject(penInput);
            penSO.FindProperty("_canvasRect").objectReferenceValue = layerContainerRect;
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                Camera uiCamera = canvas.worldCamera ?? Camera.main;
                penSO.FindProperty("_uiCamera").objectReferenceValue = uiCamera;
            }
            penSO.ApplyModifiedProperties();

            Undo.CollapseUndoOperations(group);

            // Select the new object
            Selection.activeGameObject = drawingToolRoot;

            Debug.Log($"<color=green>Canvas Draw Tool setup complete!</color> Root object: {drawingToolRoot.name}");
            EditorUtility.DisplayDialog("Canvas Draw Tool Setup",
                "Setup complete!\n\n" +
                "• DrawingTool root created\n" +
                "• LayerContainer for RawImage layers created\n" +
                "• All components attached and wired\n\n" +
                "Enter Play mode to test. Use the Inspector to configure settings.\n\n" +
                "NEW: Each layer is now a separate RawImage for mobile performance!",
                "OK");
        }

        private static GameObject CreateDrawingCanvas(Transform parent)
        {
            // This is kept for backwards compatibility but no longer used by SetupDrawTool
            GameObject canvasObj = new GameObject("DrawCanvas");
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create DrawCanvas");
            canvasObj.transform.SetParent(parent, false);

            // RectTransform - fill parent with padding
            RectTransform rect = canvasObj.AddComponent<RectTransform>();
            SetupFullStretch(rect);
            rect.offsetMin = new Vector2(10, 10);
            rect.offsetMax = new Vector2(-10, -10);

            // RawImage for displaying the drawing
            RawImage rawImage = canvasObj.AddComponent<RawImage>();
            rawImage.color = Color.white;
            rawImage.raycastTarget = true;

            // Add outline for visibility
            Outline outline = canvasObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            outline.effectDistance = new Vector2(2, -2);

            return canvasObj;
        }

        private static void SetupFullStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        /// <summary>
        /// Create or find DrawToolSettings asset.
        /// </summary>
        private static DrawToolSettings CreateOrFindSettings()
        {
            // Try to find existing settings
            string[] guids = AssetDatabase.FindAssets("t:DrawToolSettings");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                DrawToolSettings existing = AssetDatabase.LoadAssetAtPath<DrawToolSettings>(path);
                if (existing != null)
                {
                    Debug.Log($"Using existing DrawToolSettings: {path}");
                    return existing;
                }
            }

            // Create new settings
            DrawToolSettings settings = ScriptableObject.CreateInstance<DrawToolSettings>();
            settings.CreateDefaultPresets();
            
            // Ensure Resources folder exists
            string resourcesPath = "Assets/Resources";
            if (!AssetDatabase.IsValidFolder(resourcesPath))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            string assetPath = resourcesPath + "/DrawToolSettings.asset";
            AssetDatabase.CreateAsset(settings, assetPath);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"Created new DrawToolSettings at: {assetPath}");
            return settings;
        }

        /// <summary>
        /// Create a new Canvas with Draw Tool already set up.
        /// </summary>
        [MenuItem("GameObject/WitShells/Create Drawing Canvas", false, 11)]
        public static void CreateNewDrawingCanvas()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("DrawingCanvas");
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Drawing Canvas");

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();

            // Setup draw tool on this canvas
            SetupDrawTool(canvas);
        }
    }
}
