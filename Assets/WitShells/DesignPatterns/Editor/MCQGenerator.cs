using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace WitShells.DesignPatterns.Editor
{
    public class MCQGenerator
    {
        [MenuItem("GameObject/WitShells/Tools/MCQ Layout Generator", false, 10)]
        public static void CreateMCQLayout(MenuCommand menuCommand)
        {
            GameObject selectedPanel = Selection.activeGameObject;
            
            if (selectedPanel == null || selectedPanel.GetComponent<RectTransform>() == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a UI Panel with RectTransform component first!", "OK");
                return;
            }

            // Show configuration window
            MCQGeneratorWindow.ShowWindow(selectedPanel);
        }
        
        [MenuItem("GameObject/WitShells/Tools/MCQ Layout Generator", true)]
        public static bool ValidateCreateMCQLayout()
        {
            GameObject selectedObject = Selection.activeGameObject;
            return selectedObject != null && selectedObject.GetComponent<RectTransform>() != null;
        }
    }

    public class MCQGeneratorWindow : EditorWindow
    {
        public static void ShowWindow(GameObject targetPanel)
        {
            MCQGeneratorWindow window = GetWindow<MCQGeneratorWindow>("MCQ Layout Generator");
            window.selectedPanel = targetPanel;
            window.Show();
        }

        public enum MCQLayout
        {
            Vertical,
            Horizontal,
            Grid2x2,
            Grid1x4,
            Card,
            Modern
        }

        private GameObject selectedPanel;
        private Canvas parentCanvas;
        private CanvasScaler canvasScaler;
        private string titleText = "Quiz Title";
        private string descriptionText = "Enter your question description here...";
        private int optionsCount = 4;
        private MCQLayout selectedLayout = MCQLayout.Vertical;
        private bool includeImages = true;
        private bool useTextMeshPro = true;
        private bool useBackgroundImages = true;
        private Color primaryColor = new Color(0.2f, 0.6f, 1f, 1f);
        private Color secondaryColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        private Color backgroundTint = Color.white;
        private Vector2 scrollPosition;
        
        // Canvas Resolution Settings
        private bool autoDetectResolution = true;
        private Vector2 targetResolution = new Vector2(1920, 1080);
        private float scaleFactor = 1f;

        private void OnGUI()
        {
            if (selectedPanel == null)
            {
                EditorGUILayout.HelpBox("No panel selected. Please close this window and right-click on a UI Panel.", MessageType.Warning);
                return;
            }
            
            UpdateCanvasInfo();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("MCQ Layout Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Panel Info
            EditorGUILayout.LabelField("Target Panel", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("UI Panel", selectedPanel, typeof(GameObject), true);
            EditorGUI.EndDisabledGroup();
            
            if (parentCanvas != null)
            {
                EditorGUILayout.LabelField($"Canvas: {parentCanvas.name}");
                EditorGUILayout.LabelField($"Render Mode: {parentCanvas.renderMode}");
                if (canvasScaler != null)
                {
                    EditorGUILayout.LabelField($"Reference Resolution: {canvasScaler.referenceResolution}");
                    EditorGUILayout.LabelField($"Scale Factor: {scaleFactor:F2}");
                }
            }

            EditorGUILayout.Space();

            // Content Settings
            EditorGUILayout.LabelField("Content Settings", EditorStyles.boldLabel);
            titleText = EditorGUILayout.TextField("Title", titleText);
            descriptionText = EditorGUILayout.TextArea(descriptionText, GUILayout.Height(60));
            optionsCount = EditorGUILayout.IntSlider("Options Count", optionsCount, 2, 8);

            EditorGUILayout.Space();

            // Resolution Settings
            EditorGUILayout.LabelField("Resolution Settings", EditorStyles.boldLabel);
            autoDetectResolution = EditorGUILayout.Toggle("Auto Detect Resolution", autoDetectResolution);
            if (!autoDetectResolution)
            {
                targetResolution = EditorGUILayout.Vector2Field("Target Resolution", targetResolution);
            }

            EditorGUILayout.Space();

            // Visual Settings
            EditorGUILayout.LabelField("Visual Settings", EditorStyles.boldLabel);
            selectedLayout = (MCQLayout)EditorGUILayout.EnumPopup("Layout Type", selectedLayout);
            includeImages = EditorGUILayout.Toggle("Include Images", includeImages);
            useBackgroundImages = EditorGUILayout.Toggle("Use Background Images", useBackgroundImages);
            useTextMeshPro = EditorGUILayout.Toggle("Use TextMeshPro", useTextMeshPro);

            EditorGUILayout.Space();

            // Color Settings
            EditorGUILayout.LabelField("Color Settings", EditorStyles.boldLabel);
            primaryColor = EditorGUILayout.ColorField("Primary Color", primaryColor);
            secondaryColor = EditorGUILayout.ColorField("Secondary Color", secondaryColor);
            if (useBackgroundImages)
            {
                backgroundTint = EditorGUILayout.ColorField("Background Tint", backgroundTint);
            }

            EditorGUILayout.Space();

            // Generate Button
            GUI.enabled = selectedPanel != null && selectedPanel.GetComponent<RectTransform>() != null;
            if (GUILayout.Button("Generate MCQ Layout", GUILayout.Height(40)))
            {
                GenerateMCQLayout();
            }
            GUI.enabled = true;

            EditorGUILayout.Space();

            // Help Box
            EditorGUILayout.HelpBox(
                "Canvas-Aware MCQ Generator:\n" +
                "• Automatically detects Canvas resolution settings\n" +
                "• Generates responsive layouts with proper anchoring\n" +
                "• Supports background images for each component\n" +
                "• Right-click any UI Panel to access this tool",
                MessageType.Info);

            EditorGUILayout.EndScrollView();
        }

        private void GenerateMCQLayout()
        {
            if (selectedPanel == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a UI Panel first!", "OK");
                return;
            }
            
            if (selectedPanel.GetComponent<RectTransform>() == null)
            {
                EditorUtility.DisplayDialog("Error", "Selected GameObject must have a RectTransform component!", "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(selectedPanel, "Generate MCQ Layout");

            // Clear existing children
            while (selectedPanel.transform.childCount > 0)
            {
                DestroyImmediate(selectedPanel.transform.GetChild(0).gameObject);
            }

            RectTransform panelRect = selectedPanel.GetComponent<RectTransform>();
            
            // Ensure UpdateCanvasInfo is called before generation
            UpdateCanvasInfo();
            
            // Create main container
            GameObject mainContainer = CreateUIElement("MCQ_Container", selectedPanel);
            SetupRectTransform(mainContainer.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero);

            // Create title
            CreateTitle(mainContainer);

            // Create description
            CreateDescription(mainContainer);

            // Create options based on layout
            CreateOptions(mainContainer);

            // Create buttons
            CreateButtons(mainContainer);

            EditorUtility.SetDirty(selectedPanel);
            
            EditorUtility.DisplayDialog("Success", 
                "MCQ Layout generated successfully!\n\n" +
                "UI components have been created and are ready to use.\n" +
                "You can now add your own scripts to handle interactions.", "OK");
        }

        private void CreateTitle(GameObject parent)
        {
            GameObject titleGO = CreateUIElement("Title", parent);
            RectTransform titleRect = titleGO.GetComponent<RectTransform>();
            
            // Calculate responsive dimensions based on canvas scale
            float scaledHeight = Mathf.Max(60f, 80f * GetCanvasScale());
            SetupRectTransform(titleRect, new Vector2(0, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), 
                new Vector2(0, scaledHeight), new Vector2(0, -10f));

            // Add background image if enabled
            Image titleBackground = titleGO.GetComponent<Image>();
            if (useBackgroundImages)
            {
                titleBackground.color = backgroundTint;
                titleBackground.type = Image.Type.Sliced;
                // You can assign a sprite reference here or let users assign it later
            }
            else
            {
                titleBackground.color = primaryColor * 0.3f; // Subtle background
            }

            // Create text container for proper layering
            GameObject textContainer = CreateUIElement("TitleText", titleGO);
            RectTransform textRect = textContainer.GetComponent<RectTransform>();
            SetupRectTransform(textRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero);
            DestroyImmediate(textContainer.GetComponent<Image>()); // Remove default image

            if (useTextMeshPro)
            {
                TextMeshProUGUI titleText = textContainer.AddComponent<TextMeshProUGUI>();
                titleText.text = this.titleText;
                titleText.fontSize = Mathf.Max(18f, 24f * GetCanvasScale());
                titleText.fontStyle = FontStyles.Bold;
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.color = primaryColor;
            }
            else
            {
                Text titleText = textContainer.AddComponent<Text>();
                titleText.text = this.titleText;
                titleText.fontSize = Mathf.RoundToInt(Mathf.Max(14f, 20f * GetCanvasScale()));
                titleText.fontStyle = FontStyle.Bold;
                titleText.alignment = TextAnchor.MiddleCenter;
                titleText.color = primaryColor;
                titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }

        private void CreateDescription(GameObject parent)
        {
            GameObject descriptionGO = CreateUIElement("Description", parent);
            RectTransform descriptionRect = descriptionGO.GetComponent<RectTransform>();
            
            // Calculate responsive dimensions
            float scaledHeight = Mathf.Max(80f, 100f * GetCanvasScale());
            float topOffset = -(70f + 20f) * GetCanvasScale(); // Title height + spacing
            
            SetupRectTransform(descriptionRect, new Vector2(0, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), 
                new Vector2(0, scaledHeight), new Vector2(0, topOffset));

            // Add background image if enabled
            Image descriptionBackground = descriptionGO.GetComponent<Image>();
            if (useBackgroundImages)
            {
                descriptionBackground.color = backgroundTint;
                descriptionBackground.type = Image.Type.Sliced;
            }
            else
            {
                descriptionBackground.color = secondaryColor * 0.5f;
            }

            // Create text container
            GameObject textContainer = CreateUIElement("DescriptionText", descriptionGO);
            RectTransform textRect = textContainer.GetComponent<RectTransform>();
            SetupRectTransform(textRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new RectOffset(15, 15, 10, 10));
            DestroyImmediate(textContainer.GetComponent<Image>());

            if (useTextMeshPro)
            {
                TextMeshProUGUI descriptionText = textContainer.AddComponent<TextMeshProUGUI>();
                descriptionText.text = this.descriptionText;
                descriptionText.fontSize = Mathf.Max(14f, 18f * GetCanvasScale());
                descriptionText.alignment = TextAlignmentOptions.Center;
                descriptionText.color = Color.black;
                descriptionText.textWrappingMode = TextWrappingModes.PreserveWhitespace;
            }
            else
            {
                Text descriptionText = textContainer.AddComponent<Text>();
                descriptionText.text = this.descriptionText;
                descriptionText.fontSize = Mathf.RoundToInt(Mathf.Max(12f, 16f * GetCanvasScale()));
                descriptionText.alignment = TextAnchor.MiddleCenter;
                descriptionText.color = Color.black;
                descriptionText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }

        private void CreateOptions(GameObject parent)
        {
            GameObject optionsContainer = CreateUIElement("Options_Container", parent);
            RectTransform optionsRect = optionsContainer.GetComponent<RectTransform>();
            
            // Position below description with proper Canvas scaling
            float canvasScale = GetCanvasScale();
            float topOffset = -(70f + 100f + 30f) * canvasScale; // Title + Description + spacing
            float containerHeight = 300f * canvasScale; // Responsive height
            
            SetupRectTransform(optionsRect, new Vector2(0, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), 
                new Vector2(0, containerHeight), new Vector2(0, topOffset));

            // Add background for options container if enabled
            Image containerBackground = optionsContainer.GetComponent<Image>();
            if (useBackgroundImages)
            {
                containerBackground.color = new Color(backgroundTint.r, backgroundTint.g, backgroundTint.b, 0.1f);
            }
            else
            {
                containerBackground.color = Color.clear;
            }

            // Add layout group based on selected layout
            switch (selectedLayout)
            {
                case MCQLayout.Vertical:
                    CreateVerticalLayout(optionsContainer);
                    break;
                case MCQLayout.Horizontal:
                    CreateHorizontalLayout(optionsContainer);
                    break;
                case MCQLayout.Grid2x2:
                    CreateGridLayout(optionsContainer, 2);
                    break;
                case MCQLayout.Grid1x4:
                    CreateGridLayout(optionsContainer, 4);
                    break;
                case MCQLayout.Card:
                    CreateCardLayout(optionsContainer);
                    break;
                case MCQLayout.Modern:
                    CreateModernLayout(optionsContainer);
                    break;
            }
        }

        private void CreateVerticalLayout(GameObject container)
        {
            float canvasScale = GetCanvasScale();
            
            VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = Mathf.RoundToInt(10f * canvasScale);
            vlg.padding = new RectOffset(
                Mathf.RoundToInt(20f * canvasScale), 
                Mathf.RoundToInt(20f * canvasScale), 
                Mathf.RoundToInt(10f * canvasScale), 
                Mathf.RoundToInt(10f * canvasScale)
            );
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            ContentSizeFitter csf = container.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateOptionButtons(container, false);
        }

        private void CreateHorizontalLayout(GameObject container)
        {
            float canvasScale = GetCanvasScale();
            
            HorizontalLayoutGroup hlg = container.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = Mathf.RoundToInt(10f * canvasScale);
            hlg.padding = new RectOffset(
                Mathf.RoundToInt(20f * canvasScale), 
                Mathf.RoundToInt(20f * canvasScale), 
                Mathf.RoundToInt(10f * canvasScale), 
                Mathf.RoundToInt(10f * canvasScale)
            );
            hlg.childControlHeight = true;
            hlg.childControlWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth = true;

            CreateOptionButtons(container, true);
        }

        private void CreateGridLayout(GameObject container, int columns)
        {
            float canvasScale = GetCanvasScale();
            
            GridLayoutGroup glg = container.AddComponent<GridLayoutGroup>();
            glg.cellSize = GetScaledSize(new Vector2(200, 80));
            glg.spacing = GetScaledSize(new Vector2(10, 10));
            glg.padding = new RectOffset(
                Mathf.RoundToInt(20f * canvasScale), 
                Mathf.RoundToInt(20f * canvasScale), 
                Mathf.RoundToInt(10f * canvasScale), 
                Mathf.RoundToInt(10f * canvasScale)
            );
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = columns;

            CreateOptionButtons(container, true);
        }

        private void CreateCardLayout(GameObject container)
        {
            float canvasScale = GetCanvasScale();
            
            VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = Mathf.RoundToInt(15f * canvasScale);
            vlg.padding = new RectOffset(
                Mathf.RoundToInt(30f * canvasScale), 
                Mathf.RoundToInt(30f * canvasScale), 
                Mathf.RoundToInt(20f * canvasScale), 
                Mathf.RoundToInt(20f * canvasScale)
            );
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;

            CreateOptionButtons(container, false, true);
        }

        private void CreateModernLayout(GameObject container)
        {
            float canvasScale = GetCanvasScale();
            
            VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = Mathf.RoundToInt(12f * canvasScale);
            vlg.padding = new RectOffset(
                Mathf.RoundToInt(25f * canvasScale), 
                Mathf.RoundToInt(25f * canvasScale), 
                Mathf.RoundToInt(15f * canvasScale), 
                Mathf.RoundToInt(15f * canvasScale)
            );
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;

            CreateOptionButtons(container, false, false, true);
        }

        private void CreateOptionButtons(GameObject container, bool horizontal = false, bool cardStyle = false, bool modernStyle = false)
        {
            if (container == null)
            {
                Debug.LogError("Container is null in CreateOptionButtons");
                return;
            }
            
            float canvasScale = GetCanvasScale();
            
            for (int i = 0; i < optionsCount; i++)
            {
                GameObject optionGO = CreateUIElement($"Option_{i + 1}", container);
                if (optionGO == null)
                {
                    Debug.LogError($"Failed to create option {i + 1}");
                    continue;
                }
                
                RectTransform optionRect = optionGO.GetComponent<RectTransform>();

                // Add button component
                Button button = optionGO.AddComponent<Button>();
                Image buttonImage = optionGO.GetComponent<Image>();
                
                // Configure background based on style and background image settings
                if (useBackgroundImages)
                {
                    buttonImage.color = backgroundTint;
                    buttonImage.type = Image.Type.Sliced;
                    // Sprite can be assigned later by user
                }
                else if (cardStyle)
                {
                    buttonImage.color = Color.white;
                }
                else if (modernStyle)
                {
                    buttonImage.color = secondaryColor;
                }
                else
                {
                    buttonImage.color = secondaryColor;
                }
                
                // Setup button colors
                ColorBlock colors = button.colors;
                if (useBackgroundImages)
                {
                    colors.normalColor = backgroundTint;
                    colors.highlightedColor = backgroundTint * 0.9f;
                    colors.pressedColor = primaryColor * 0.8f;
                }
                else if (cardStyle)
                {
                    colors.normalColor = Color.white;
                    colors.highlightedColor = primaryColor * 0.8f;
                    colors.pressedColor = primaryColor;
                }
                else
                {
                    colors.normalColor = secondaryColor;
                    colors.highlightedColor = primaryColor * 0.8f;
                    colors.pressedColor = primaryColor;
                }
                button.colors = colors;

                // Add visual effects
                if (cardStyle || useBackgroundImages)
                {
                    Shadow shadow = optionGO.AddComponent<Shadow>();
                    shadow.effectColor = new Color(0, 0, 0, 0.3f);
                    shadow.effectDistance = new Vector2(2 * canvasScale, -2 * canvasScale);
                }

                // Set responsive size
                if (!horizontal && !cardStyle && !modernStyle)
                {
                    LayoutElement le = optionGO.AddComponent<LayoutElement>();
                    le.minHeight = Mathf.Max(40f, 50f * canvasScale);
                    le.preferredHeight = Mathf.Max(50f, 60f * canvasScale);
                }

                // Create content container
                GameObject contentContainer = CreateUIElement("Content", optionGO);
                if (contentContainer == null)
                {
                    Debug.LogError($"Failed to create content container for option {i + 1}");
                    continue;
                }
                
                RectTransform contentRect = contentContainer.GetComponent<RectTransform>();
                SetupRectTransform(contentRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero);
                DestroyImmediate(contentContainer.GetComponent<Image>()); // Remove default image for content

                // Setup layout
                int spacing = Mathf.RoundToInt((horizontal ? 10f : 15f) * canvasScale);
                RectOffset padding = new RectOffset(
                    Mathf.RoundToInt((horizontal ? 15f : 20f) * canvasScale),
                    Mathf.RoundToInt((horizontal ? 15f : 20f) * canvasScale),
                    Mathf.RoundToInt((horizontal ? 10f : 15f) * canvasScale),
                    Mathf.RoundToInt((horizontal ? 10f : 15f) * canvasScale)
                );

                HorizontalLayoutGroup hlg = contentContainer.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = spacing;
                hlg.padding = padding;
                hlg.childAlignment = horizontal ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;

                // Add image if requested
                if (includeImages)
                {
                    GameObject imageGO = CreateUIElement($"Option_{i + 1}_Image", contentContainer);
                    if (imageGO != null)
                    {
                        Image image = imageGO.GetComponent<Image>();
                        if (image != null)
                        {
                            image.color = primaryColor;
                        }
                        
                        float imageSize = Mathf.Max(30f, 40f * canvasScale);
                        LayoutElement imageLE = imageGO.AddComponent<LayoutElement>();
                        imageLE.minWidth = imageSize;
                        imageLE.preferredWidth = imageSize;
                        imageLE.minHeight = imageSize;
                        imageLE.preferredHeight = imageSize;
                    }
                }

                // Add text
                GameObject textGO = CreateUIElement($"Option_{i + 1}_Text", contentContainer);
                if (textGO == null)
                {
                    Debug.LogError($"Failed to create text for option {i + 1}");
                    continue;
                }
                
                DestroyImmediate(textGO.GetComponent<Image>()); // Remove default image for text
                
                if (useTextMeshPro)
                {
                    TextMeshProUGUI optionText = textGO.AddComponent<TextMeshProUGUI>();
                    optionText.text = $"Option {i + 1}";
                    optionText.fontSize = Mathf.Max(12f, 16f * canvasScale);
                    optionText.alignment = horizontal ? TextAlignmentOptions.Center : TextAlignmentOptions.Left;
                    optionText.color = Color.black;
                }
                else
                {
                    Text optionText = textGO.AddComponent<Text>();
                    optionText.text = $"Option {i + 1}";
                    optionText.fontSize = Mathf.RoundToInt(Mathf.Max(10f, 14f * canvasScale));
                    optionText.alignment = horizontal ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
                    optionText.color = Color.black;
                    optionText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }

                LayoutElement textLE = textGO.AddComponent<LayoutElement>();
                textLE.flexibleWidth = 1;
            }
        }

        private void CreateButtons(GameObject parent)
        {
            GameObject buttonsContainer = CreateUIElement("Buttons_Container", parent);
            RectTransform buttonsRect = buttonsContainer.GetComponent<RectTransform>();
            
            // Position at bottom with proper Canvas scaling
            float canvasScale = GetCanvasScale();
            float buttonHeight = Mathf.Max(50f, 60f * canvasScale);
            float bottomOffset = 20f * canvasScale;
            
            SetupRectTransform(buttonsRect, new Vector2(0, 0), new Vector2(1f, 0), new Vector2(0.5f, 0), 
                new Vector2(0, buttonHeight), new Vector2(0, bottomOffset));

            // Remove default image for container
            DestroyImmediate(buttonsContainer.GetComponent<Image>());

            HorizontalLayoutGroup hlg = buttonsContainer.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = Mathf.RoundToInt(20f * canvasScale);
            hlg.padding = new RectOffset(
                Mathf.RoundToInt(50f * canvasScale), 
                Mathf.RoundToInt(50f * canvasScale), 
                Mathf.RoundToInt(10f * canvasScale), 
                Mathf.RoundToInt(10f * canvasScale)
            );
            hlg.childControlHeight = true;
            hlg.childControlWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth = true;

            // Skip button
            CreateActionButton(buttonsContainer, "Skip", Color.gray);

            // Submit button
            CreateActionButton(buttonsContainer, "Submit", primaryColor);
        }

        private void CreateActionButton(GameObject parent, string buttonText, Color buttonColor)
        {
            float canvasScale = GetCanvasScale();
            
            GameObject buttonGO = CreateUIElement($"{buttonText}_Button", parent);
            Button button = buttonGO.AddComponent<Button>();
            Image buttonImage = buttonGO.GetComponent<Image>();
            
            // Configure background
            if (useBackgroundImages)
            {
                buttonImage.color = backgroundTint;
                buttonImage.type = Image.Type.Sliced;
            }
            else
            {
                buttonImage.color = buttonColor;
            }

            ColorBlock colors = button.colors;
            if (useBackgroundImages)
            {
                colors.normalColor = backgroundTint;
                colors.highlightedColor = backgroundTint * 0.8f;
                colors.pressedColor = buttonColor;
            }
            else
            {
                colors.normalColor = buttonColor;
                colors.highlightedColor = buttonColor * 0.8f;
                colors.pressedColor = buttonColor * 1.2f;
            }
            button.colors = colors;

            // Add shadow effect for enhanced buttons
            if (useBackgroundImages)
            {
                Shadow shadow = buttonGO.AddComponent<Shadow>();
                shadow.effectColor = new Color(0, 0, 0, 0.4f);
                shadow.effectDistance = new Vector2(3 * canvasScale, -3 * canvasScale);
            }

            GameObject textGO = CreateUIElement("Text", buttonGO);
            RectTransform textRect = textGO.GetComponent<RectTransform>();
            SetupRectTransform(textRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero);
            DestroyImmediate(textGO.GetComponent<Image>()); // Remove default image for text

            if (useTextMeshPro)
            {
                TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
                text.text = buttonText;
                text.fontSize = Mathf.Max(12f, 16f * canvasScale);
                text.fontStyle = FontStyles.Bold;
                text.alignment = TextAlignmentOptions.Center;
                text.color = Color.white;
            }
            else
            {
                Text text = textGO.AddComponent<Text>();
                text.text = buttonText;
                text.fontSize = Mathf.RoundToInt(Mathf.Max(10f, 14f * canvasScale));
                text.fontStyle = FontStyle.Bold;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            LayoutElement le = buttonGO.AddComponent<LayoutElement>();
            le.minWidth = Mathf.Max(80f, 100f * canvasScale);
            le.preferredWidth = Mathf.Max(100f, 120f * canvasScale);
        }

        private GameObject CreateUIElement(string name, GameObject parent)
        {
            if (parent == null)
            {
                Debug.LogError($"Cannot create UI element '{name}': parent is null");
                return null;
            }
            
            GameObject go = new GameObject(name);
            try
            {
                go.transform.SetParent(parent.transform, false);
                RectTransform rect = go.AddComponent<RectTransform>();
                go.AddComponent<CanvasRenderer>();
                go.AddComponent<Image>();
                
                // Ensure the RectTransform has valid default values
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;
                
                return go;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create UI element '{name}': {e.Message}");
                if (go != null)
                    DestroyImmediate(go);
                return null;
            }
        }

        private void SetupRectTransform(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = Vector2.zero;
        }

        // Overloaded method for positioned elements
        private void SetupRectTransform(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, 
            Vector2 sizeDelta, Vector2 anchoredPosition, RectOffset padding = null)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
            
            if (padding != null)
            {
                rect.offsetMin = new Vector2(padding.left, padding.bottom);
                rect.offsetMax = new Vector2(-padding.right, -padding.top);
            }
        }

        private float GetCanvasScale()
        {
            // Ensure we always return a valid scale factor
            if (canvasScaler != null && scaleFactor > 0)
            {
                return Mathf.Max(0.1f, scaleFactor); // Prevent zero or negative scale
            }
            return 1f;
        }

        private Vector2 GetScaledSize(Vector2 baseSize)
        {
            float scale = GetCanvasScale();
            return new Vector2(baseSize.x * scale, baseSize.y * scale);
        }

        private void UpdateCanvasInfo()
        {
            if (selectedPanel == null) return;
            
            parentCanvas = selectedPanel.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                canvasScaler = parentCanvas.GetComponent<CanvasScaler>();
                if (canvasScaler != null && autoDetectResolution)
                {
                    targetResolution = canvasScaler.referenceResolution;
                    // Calculate scale factor manually since scaleFactor might not be accurate in editor
                    float screenScale = Mathf.Min(
                        Screen.width / targetResolution.x, 
                        Screen.height / targetResolution.y
                    );
                    scaleFactor = Mathf.Max(0.1f, screenScale); // Ensure minimum scale
                }
                else
                {
                    scaleFactor = 1f;
                }
            }
            else
            {
                scaleFactor = 1f;
            }
        }
    }
}