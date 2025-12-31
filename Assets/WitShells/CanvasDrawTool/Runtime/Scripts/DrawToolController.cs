namespace WitShells.CanvasDrawTool
{
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.Events;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Main controller that wires all drawing tool components together.
    /// This is the primary entry point for the drawing tool.
    /// Uses DrawToolSettings ScriptableObject as single source of truth for all settings.
    /// </summary>
    public class DrawToolController : MonoBehaviour
    {
        [Header("Settings (Single Source of Truth)")]
        [SerializeField] private DrawToolSettings _settings;

        [Header("Core Components")]
        [SerializeField] private DrawCanvasUI _canvasUI;
        [SerializeField] private LayerManager _layerManager;
        [SerializeField] private DrawingEngine _drawingEngine;

        [Header("Input")]
        [SerializeField] private DrawingInput _drawingInput;
        [SerializeField] private PenInput _penInput;
        [SerializeField] private bool _preferPenInput = true;

        [Header("UI Panels")]
        [SerializeField] private ColorPickerUI _colorPicker;
        [SerializeField] private LayerPanelUI _layerPanel;
        [SerializeField] private BrushSettingsUI _brushPanel;

        [Header("Image Transform")]
        [SerializeField] private ImageSelectionManager _imageSelectionManager;
        [SerializeField] private bool _maintainAspectRatioOnImport = true;

        [Header("Events")]
        public UnityEvent<DrawToolSettings.DrawingTool> OnToolChanged;
        public UnityEvent<Color> OnPrimaryColorChanged;
        public UnityEvent<Color> OnSecondaryColorChanged;
        public UnityEvent OnBrushChanged;
        public UnityEvent OnDrawStart;
        public UnityEvent OnDrawEnd;
        public UnityEvent<string> OnCanvasSaved;
        public UnityEvent<string> OnImageImported;

        private bool _isDrawing;
        private Vector2Int _lastPixelPosition;
        private Vector2Int _shapeStartPosition;
        private bool _useSecondaryColor;
        private bool _eventsInitialized = false;

        // Public accessors
        public DrawToolSettings Settings => _settings;
        public DrawCanvasUI CanvasUI => _canvasUI;
        public LayerManager LayerManager => _layerManager;
        public DrawingEngine DrawingEngine => _drawingEngine;
        public DrawingInput DrawingInput => _drawingInput;
        public PenInput PenInput => _penInput;
        public ColorPickerUI ColorPicker => _colorPicker;
        public LayerPanelUI LayerPanel => _layerPanel;
        public ImageSelectionManager ImageSelectionManager => _imageSelectionManager;
        public bool IsDrawing => _isDrawing;

        // Convenience accessors that forward to settings
        public DrawToolSettings.DrawingTool CurrentTool => _settings != null ? _settings.CurrentTool : DrawToolSettings.DrawingTool.Brush;
        public Color PrimaryColor => _settings != null ? _settings.PrimaryColor : Color.black;
        public Color SecondaryColor => _settings != null ? _settings.SecondaryColor : Color.white;

        private void Awake()
        {
            ValidateSettings();
            InitializeComponents();
        }

        private void Start()
        {
            SetupEventListeners();
            SetupSettingsCallbacks();
            CreateDefaultCanvas();
        }

        private void OnDestroy()
        {
            UnsubscribeFromSettings();
        }

        /// <summary>
        /// Ensure settings exist - create runtime instance if needed.
        /// </summary>
        private void ValidateSettings()
        {
            if (_settings == null)
            {
                _settings = ScriptableObject.CreateInstance<DrawToolSettings>();
                Debug.LogWarning("[DrawToolController] No DrawToolSettings assigned. Created runtime instance.");
            }
        }

        /// <summary>
        /// Initialize the controller. Call this after wiring references at runtime.
        /// </summary>
        public void Initialize()
        {
            ValidateSettings();
            InitializeComponents();
            SetupEventListeners();
            SetupSettingsCallbacks();
        }

        /// <summary>
        /// Initialize all component references.
        /// </summary>
        private void InitializeComponents()
        {
            // Auto-find components if not assigned
            if (_canvasUI == null) _canvasUI = GetComponentInChildren<DrawCanvasUI>();
            if (_layerManager == null) _layerManager = GetComponentInChildren<LayerManager>();
            if (_drawingEngine == null) _drawingEngine = GetComponentInChildren<DrawingEngine>();
            if (_drawingInput == null) _drawingInput = GetComponentInChildren<DrawingInput>();
            if (_penInput == null) _penInput = GetComponentInChildren<PenInput>();
            if (_colorPicker == null) _colorPicker = GetComponentInChildren<ColorPickerUI>();
            if (_layerPanel == null) _layerPanel = GetComponentInChildren<LayerPanelUI>();
            if (_brushPanel == null) _brushPanel = GetComponentInChildren<BrushSettingsUI>();

            // Create missing components
            if (_layerManager == null && _canvasUI != null)
            {
                _layerManager = _canvasUI.gameObject.AddComponent<LayerManager>();
            }

            if (_drawingEngine == null && _layerManager != null)
            {
                _drawingEngine = _layerManager.gameObject.AddComponent<DrawingEngine>();
            }

            // Pass settings reference to drawing engine
            if (_drawingEngine != null)
            {
                _drawingEngine.SetSettings(_settings);
            }

            // Initialize image selection manager
            InitializeImageSelectionManager();
        }

        /// <summary>
        /// Initialize the image selection manager for transform mode.
        /// </summary>
        private void InitializeImageSelectionManager()
        {
            if (_imageSelectionManager == null)
            {
                _imageSelectionManager = gameObject.GetComponentInChildren<ImageSelectionManager>();
                if (_imageSelectionManager == null)
                {
                    _imageSelectionManager = gameObject.AddComponent<ImageSelectionManager>();
                }
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = FindAnyObjectByType<Canvas>();

            if (canvas != null && _layerManager != null)
            {
                _imageSelectionManager.Initialize(canvas, _layerManager);
                _imageSelectionManager.MaintainAspectRatio = _maintainAspectRatioOnImport;
            }
        }

        /// <summary>
        /// Set up event listeners for input and UI.
        /// </summary>
        private void SetupEventListeners()
        {
            if (_eventsInitialized) return;
            _eventsInitialized = true;

            // Drawing input events
            DrawingInput activeInput = GetActiveInput();
            if (activeInput != null)
            {
                activeInput.OnDrawStart.AddListener(HandleDrawStart);
                activeInput.OnDrawMove.AddListener(HandleDrawMove);
                activeInput.OnDrawEnd.AddListener(HandleDrawEnd);
                activeInput.OnZoom.AddListener(HandleZoom);
                activeInput.OnPan.AddListener(HandlePan);
            }

            // Color picker events
            if (_colorPicker != null)
            {
                _colorPicker.OnColorChanged.AddListener(SetPrimaryColor);
            }

            // Pen-specific events
            if (_penInput != null)
            {
                _penInput.OnEraserTipActive.AddListener(() => SetTool(DrawToolSettings.DrawingTool.Eraser));
                _penInput.OnPenTipActive.AddListener(() => SetTool(DrawToolSettings.DrawingTool.Brush));
                _penInput.OnBarrelButtonPressed.AddListener(HandleBarrelButton);
            }
        }

        /// <summary>
        /// Subscribe to settings change events.
        /// </summary>
        private void SetupSettingsCallbacks()
        {
            if (_settings == null) return;

            _settings.OnToolChanged += HandleToolChanged;
            _settings.OnPrimaryColorChanged += HandlePrimaryColorChanged;
            _settings.OnSecondaryColorChanged += HandleSecondaryColorChanged;
            _settings.OnBrushTypeChanged += HandleBrushTypeChanged;
            _settings.OnBrushSizeChanged += HandleBrushSizeChanged;
            _settings.OnBrushOpacityChanged += HandleBrushOpacityChanged;
            _settings.OnSettingsChanged += HandleSettingsChanged;
        }

        /// <summary>
        /// Unsubscribe from settings events.
        /// </summary>
        private void UnsubscribeFromSettings()
        {
            if (_settings == null) return;

            _settings.OnToolChanged -= HandleToolChanged;
            _settings.OnPrimaryColorChanged -= HandlePrimaryColorChanged;
            _settings.OnSecondaryColorChanged -= HandleSecondaryColorChanged;
            _settings.OnBrushTypeChanged -= HandleBrushTypeChanged;
            _settings.OnBrushSizeChanged -= HandleBrushSizeChanged;
            _settings.OnBrushOpacityChanged -= HandleBrushOpacityChanged;
            _settings.OnSettingsChanged -= HandleSettingsChanged;
        }

        /// <summary>
        /// Create default canvas.
        /// </summary>
        private void CreateDefaultCanvas()
        {
            if (_canvasUI != null && _settings != null)
            {
                _canvasUI.CreateNewCanvas(
                    _settings.DefaultCanvasWidth,
                    _settings.DefaultCanvasHeight,
                    _settings.DefaultBackgroundColor
                );

                // Ensure DrawingInput has correct canvas rect
                if (_drawingInput != null && _canvasUI.CanvasRect != null)
                {
                    _drawingInput.SetCanvasRect(_canvasUI.CanvasRect);
                }
                if (_penInput != null && _canvasUI.CanvasRect != null)
                {
                    _penInput.SetCanvasRect(_canvasUI.CanvasRect);
                }
            }
        }

        /// <summary>
        /// Get the currently active input handler.
        /// </summary>
        private DrawingInput GetActiveInput()
        {
            if (_preferPenInput && _penInput != null && _penInput.IsPenAvailable)
            {
                return _penInput;
            }
            return _drawingInput;
        }

        /// <summary>
        /// Convert normalized canvas position (0-1) to pixel coordinates.
        /// </summary>
        private Vector2Int NormalizedToPixel(Vector2 normalized)
        {
            if (_layerManager == null) return Vector2Int.zero;
            return new Vector2Int(
                Mathf.Clamp(Mathf.RoundToInt(normalized.x * _layerManager.CanvasWidth), 0, _layerManager.CanvasWidth - 1),
                Mathf.Clamp(Mathf.RoundToInt(normalized.y * _layerManager.CanvasHeight), 0, _layerManager.CanvasHeight - 1)
            );
        }

        // ==================== SETTINGS CHANGE HANDLERS ====================

        private void HandleToolChanged(DrawToolSettings.DrawingTool tool)
        {
            // Update image selection manager transform mode
            if (_imageSelectionManager != null)
            {
                _imageSelectionManager.IsTransformMode = (tool == DrawToolSettings.DrawingTool.Transform);
            }

            OnToolChanged?.Invoke(tool);
        }

        private void HandlePrimaryColorChanged(Color color)
        {
            if (_colorPicker != null)
            {
                _colorPicker.SetColor(color);
            }
            OnPrimaryColorChanged?.Invoke(color);
        }

        private void HandleSecondaryColorChanged(Color color)
        {
            OnSecondaryColorChanged?.Invoke(color);
        }

        private void HandleBrushTypeChanged(Brush.BrushType type)
        {
            OnBrushChanged?.Invoke();
        }

        private void HandleBrushSizeChanged(int size)
        {
            OnBrushChanged?.Invoke();
        }

        private void HandleBrushOpacityChanged(float opacity)
        {
            OnBrushChanged?.Invoke();
        }

        private void HandleSettingsChanged()
        {
            // Notify drawing engine of settings change
            if (_drawingEngine != null)
            {
                _drawingEngine.RefreshFromSettings();
            }
        }

        // ==================== DRAWING HANDLERS ====================

        /// <summary>
        /// Handle draw start event.
        /// </summary>
        private void HandleDrawStart(Vector2 canvasPosition, float pressure)
        {
            if (_layerManager == null || _layerManager.ActiveLayer == null) return;
            if (_layerManager.ActiveLayer.IsLocked) return;
            if (_settings == null) return;

            Vector2Int pixelPos = NormalizedToPixel(canvasPosition);
            Vector2 pixelPosFloat = new Vector2(pixelPos.x, pixelPos.y);
            if (!IsInBounds(pixelPos)) return;

            _isDrawing = true;
            _lastPixelPosition = pixelPos;
            _shapeStartPosition = pixelPos;

            // Check for pen modifiers
            if (_penInput != null && _penInput.ShouldUseEyedropper())
            {
                PickColor(pixelPos);
                return;
            }

            Color drawColor = GetDrawColor();

            switch (_settings.CurrentTool)
            {
                case DrawToolSettings.DrawingTool.Brush:
                    _drawingEngine.SetTool(DrawingEngine.DrawTool.Brush);
                    _drawingEngine.BeginDraw(pixelPosFloat, pressure);
                    break;

                case DrawToolSettings.DrawingTool.Eraser:
                    _drawingEngine.SetTool(DrawingEngine.DrawTool.Eraser);
                    _drawingEngine.BeginDraw(pixelPosFloat, pressure);
                    break;

                case DrawToolSettings.DrawingTool.Eyedropper:
                    PickColor(pixelPos);
                    break;

                case DrawToolSettings.DrawingTool.Fill:
                    _drawingEngine.FloodFill(_layerManager.ActiveLayer, pixelPos, drawColor);
                    break;

                case DrawToolSettings.DrawingTool.Line:
                case DrawToolSettings.DrawingTool.Rectangle:
                case DrawToolSettings.DrawingTool.Ellipse:
                    // Store start position, actual drawing happens on end
                    break;

                case DrawToolSettings.DrawingTool.Transform:
                    // Transform mode - handled by ImageSelectionManager
                    _isDrawing = false;
                    return;
            }

            UpdateCanvas();
            OnDrawStart?.Invoke();
        }

        /// <summary>
        /// Handle draw move event.
        /// </summary>
        private void HandleDrawMove(Vector2 canvasPosition, float pressure)
        {
            if (!_isDrawing) return;
            if (_settings == null) return;

            Vector2Int pixelPos = NormalizedToPixel(canvasPosition);
            Vector2 pixelPosFloat = new Vector2(pixelPos.x, pixelPos.y);
            if (!IsInBounds(pixelPos)) return;

            if (pixelPos == _lastPixelPosition) return;

            if (_penInput != null && _penInput.ShouldUseEyedropper())
            {
                return;
            }

            switch (_settings.CurrentTool)
            {
                case DrawToolSettings.DrawingTool.Brush:
                case DrawToolSettings.DrawingTool.Eraser:
                    _drawingEngine.ContinueDraw(pixelPosFloat, pressure);
                    break;

                case DrawToolSettings.DrawingTool.Line:
                case DrawToolSettings.DrawingTool.Rectangle:
                case DrawToolSettings.DrawingTool.Ellipse:
                    // Preview could be implemented here
                    break;
            }

            _lastPixelPosition = pixelPos;
            UpdateCanvas();
        }

        /// <summary>
        /// Handle draw end event.
        /// </summary>
        private void HandleDrawEnd(Vector2 canvasPosition, float pressure)
        {
            if (!_isDrawing) return;
            if (_settings == null) return;

            Vector2Int pixelPos = NormalizedToPixel(canvasPosition);
            Vector2 pixelPosFloat = new Vector2(pixelPos.x, pixelPos.y);
            Color drawColor = GetDrawColor();
            int brushSize = _settings.GetEffectiveSize(pressure);

            switch (_settings.CurrentTool)
            {
                case DrawToolSettings.DrawingTool.Brush:
                case DrawToolSettings.DrawingTool.Eraser:
                    _drawingEngine.EndDraw(pixelPosFloat, pressure);
                    break;

                case DrawToolSettings.DrawingTool.Line:
                    _drawingEngine.DrawLine(_layerManager.ActiveLayer, _shapeStartPosition, pixelPos, drawColor, brushSize);
                    break;

                case DrawToolSettings.DrawingTool.Rectangle:
                    Vector2Int rectMin = new Vector2Int(Mathf.Min(_shapeStartPosition.x, pixelPos.x), Mathf.Min(_shapeStartPosition.y, pixelPos.y));
                    Vector2Int rectMax = new Vector2Int(Mathf.Max(_shapeStartPosition.x, pixelPos.x), Mathf.Max(_shapeStartPosition.y, pixelPos.y));
                    _drawingEngine.DrawRectangle(_layerManager.ActiveLayer, rectMin, rectMax, drawColor, false, brushSize);
                    break;

                case DrawToolSettings.DrawingTool.Ellipse:
                    Vector2Int center = new Vector2Int((_shapeStartPosition.x + pixelPos.x) / 2, (_shapeStartPosition.y + pixelPos.y) / 2);
                    int radiusX = Mathf.Abs(pixelPos.x - _shapeStartPosition.x) / 2;
                    int radiusY = Mathf.Abs(pixelPos.y - _shapeStartPosition.y) / 2;
                    _drawingEngine.DrawEllipse(_layerManager.ActiveLayer, center, radiusX, radiusY, drawColor, false);
                    break;
            }

            _isDrawing = false;
            UpdateCanvas();
            OnDrawEnd?.Invoke();
        }

        /// <summary>
        /// Handle zoom event.
        /// </summary>
        private void HandleZoom(float delta)
        {
            if (_canvasUI == null) return;

            if (delta > 0)
                _canvasUI.ZoomIn();
            else
                _canvasUI.ZoomOut();
        }

        /// <summary>
        /// Handle pan event.
        /// </summary>
        private void HandlePan(Vector2 delta)
        {
            if (_canvasUI == null) return;
            _canvasUI.Pan(delta);
        }

        /// <summary>
        /// Handle pen barrel button.
        /// </summary>
        private void HandleBarrelButton()
        {
            if (_penInput == null) return;

            switch (_penInput.CurrentBarrelAction)
            {
                case PenInput.BarrelButtonAction.Eraser:
                    break;
                case PenInput.BarrelButtonAction.Eyedropper:
                    break;
                case PenInput.BarrelButtonAction.Pan:
                    break;
            }
        }

        /// <summary>
        /// Get the current drawing color.
        /// </summary>
        private Color GetDrawColor()
        {
            if (_settings == null) return Color.black;

            if (_penInput != null && _penInput.ShouldUseEraser())
            {
                return Color.clear;
            }

            return _useSecondaryColor ? _settings.SecondaryColor : _settings.PrimaryColor;
        }

        /// <summary>
        /// Pick color from canvas at position.
        /// </summary>
        private void PickColor(Vector2Int pixelPos)
        {
            if (_layerManager == null || _settings == null) return;

            Color pickedColor = _layerManager.CompositeTexture.GetPixel(pixelPos.x, pixelPos.y);
            _settings.PrimaryColor = pickedColor;
        }

        /// <summary>
        /// Check if position is within canvas bounds.
        /// </summary>
        private bool IsInBounds(Vector2Int pos)
        {
            if (_layerManager == null) return false;

            return pos.x >= 0 && pos.x < _layerManager.CanvasWidth &&
                   pos.y >= 0 && pos.y < _layerManager.CanvasHeight;
        }

        /// <summary>
        /// Update canvas display.
        /// </summary>
        private void UpdateCanvas()
        {
            if (_canvasUI != null)
            {
                _canvasUI.UpdateCanvasDisplay();
            }
        }

        // ==================== PUBLIC API ====================

        /// <summary>
        /// Set the current drawing tool.
        /// </summary>
        public void SetTool(DrawToolSettings.DrawingTool tool)
        {
            if (_settings != null)
            {
                _settings.CurrentTool = tool;
            }
        }

        /// <summary>
        /// Set the primary drawing color.
        /// </summary>
        public void SetPrimaryColor(Color color)
        {
            if (_settings != null)
            {
                _settings.PrimaryColor = color;
            }
        }

        /// <summary>
        /// Set the secondary drawing color.
        /// </summary>
        public void SetSecondaryColor(Color color)
        {
            if (_settings != null)
            {
                _settings.SecondaryColor = color;
            }
        }

        /// <summary>
        /// Swap primary and secondary colors.
        /// </summary>
        public void SwapColors()
        {
            if (_settings != null)
            {
                _settings.SwapColors();
            }
        }

        /// <summary>
        /// Set brush type.
        /// </summary>
        public void SetBrushType(Brush.BrushType type)
        {
            if (_settings != null)
            {
                _settings.BrushType = type;
            }
        }

        /// <summary>
        /// Set brush size.
        /// </summary>
        public void SetBrushSize(int size)
        {
            if (_settings != null)
            {
                _settings.BrushSize = size;
            }
        }

        /// <summary>
        /// Set brush opacity.
        /// </summary>
        public void SetBrushOpacity(float opacity)
        {
            if (_settings != null)
            {
                _settings.BrushOpacity = opacity;
            }
        }

        /// <summary>
        /// Set brush hardness.
        /// </summary>
        public void SetBrushHardness(float hardness)
        {
            if (_settings != null)
            {
                _settings.BrushHardness = hardness;
            }
        }

        /// <summary>
        /// Set brush spacing.
        /// </summary>
        public void SetBrushSpacing(float spacing)
        {
            if (_settings != null)
            {
                _settings.BrushSpacing = spacing;
            }
        }

        /// <summary>
        /// Apply a brush preset.
        /// </summary>
        public void ApplyBrushPreset(DrawToolSettings.BrushPreset preset)
        {
            if (_settings != null)
            {
                _settings.ApplyPreset(preset);
            }
        }

        /// <summary>
        /// Create a new canvas.
        /// </summary>
        public void NewCanvas(int width, int height, Color backgroundColor)
        {
            if (_canvasUI != null)
            {
                _canvasUI.CreateNewCanvas(width, height, backgroundColor);
            }
        }

        /// <summary>
        /// Clear the current canvas.
        /// </summary>
        public void ClearCanvas()
        {
            if (_canvasUI != null)
            {
                _canvasUI.ClearCanvas();
            }
        }

        /// <summary>
        /// Import an image to the active layer.
        /// </summary>
        public void ImportImage(string filePath)
        {
            if (_layerManager == null || _layerManager.ActiveLayer == null) return;

            if (File.Exists(filePath))
            {
                byte[] data = File.ReadAllBytes(filePath);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(data);

                _layerManager.ActiveLayer.ImportImage(texture);
                UpdateCanvas();

                Destroy(texture);

                OnImageImported?.Invoke(filePath);
            }
        }

        /// <summary>
        /// Import an image to a new layer.
        /// </summary>
        public void ImportImageToNewLayer(string filePath, string layerName = null)
        {
            if (_layerManager == null) return;

            string name = layerName ?? Path.GetFileNameWithoutExtension(filePath);
            _layerManager.CreateLayer(name);
            int layerIndex = _layerManager.LayerCount - 1;

            _layerManager.SetActiveLayer(layerIndex);
            ImportImage(filePath);
        }

        /// <summary>
        /// Import an image as a new layer that maintains aspect ratio and can be transformed.
        /// The image layer preserves its original size and can be moved/scaled/rotated.
        /// </summary>
        public LayerObject ImportImageAsTransformableLayer(string filePath, string layerName = null)
        {
            if (_layerManager == null) return null;

            if (!File.Exists(filePath)) return null;

            byte[] data = File.ReadAllBytes(filePath);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(data);

            string name = layerName ?? Path.GetFileNameWithoutExtension(filePath);
            LayerObject layer = _layerManager.CreateLayerFromImage(texture, name);

            Destroy(texture);

            // Switch to transform tool and select the layer
            SetTool(DrawToolSettings.DrawingTool.Transform);
            if (_imageSelectionManager != null)
            {
                _imageSelectionManager.SelectLayer(layer);
            }

            OnImageImported?.Invoke(filePath);
            return layer;
        }

        /// <summary>
        /// Import an image from a Texture2D as a transformable layer.
        /// </summary>
        public LayerObject ImportImageAsTransformableLayer(Texture2D texture, string layerName = null)
        {
            if (_layerManager == null || texture == null) return null;

            string name = layerName ?? texture.name ?? "Imported Image";
            LayerObject layer = _layerManager.CreateLayerFromImage(texture, name);

            // Switch to transform tool and select the layer
            SetTool(DrawToolSettings.DrawingTool.Transform);
            if (_imageSelectionManager != null)
            {
                _imageSelectionManager.SelectLayer(layer);
            }

            return layer;
        }

        /// <summary>
        /// Select a layer for transform operations.
        /// </summary>
        public void SelectLayerForTransform(LayerObject layer)
        {
            if (_imageSelectionManager != null && layer != null)
            {
                SetTool(DrawToolSettings.DrawingTool.Transform);
                _imageSelectionManager.SelectLayer(layer);
            }
        }

        /// <summary>
        /// Clear current layer selection.
        /// </summary>
        public void ClearLayerSelection()
        {
            if (_imageSelectionManager != null)
            {
                _imageSelectionManager.ClearSelection();
            }
        }

        /// <summary>
        /// Reset selected layer to original size.
        /// </summary>
        public void ResetSelectedLayerSize()
        {
            if (_imageSelectionManager != null)
            {
                _imageSelectionManager.ResetSelectedToOriginalSize();
            }
        }

        /// <summary>
        /// Fit selected layer to canvas bounds.
        /// </summary>
        public void FitSelectedLayerToCanvas()
        {
            if (_imageSelectionManager != null && _canvasUI != null && _canvasUI.CanvasRect != null)
            {
                _imageSelectionManager.FitSelectedToCanvas(_canvasUI.CanvasRect);
            }
        }

        /// <summary>
        /// Set whether imported images maintain aspect ratio when scaling.
        /// </summary>
        public void SetMaintainAspectRatio(bool maintain)
        {
            _maintainAspectRatioOnImport = maintain;
            if (_imageSelectionManager != null)
            {
                _imageSelectionManager.MaintainAspectRatio = maintain;
            }
        }

        /// <summary>
        /// Export the canvas as PNG.
        /// </summary>
        public void ExportAsPNG(string filePath)
        {
            if (_layerManager != null)
            {
                byte[] pngData = _layerManager.ExportAsPNG();
                File.WriteAllBytes(filePath, pngData);
                OnCanvasSaved?.Invoke(filePath);
            }
        }

        /// <summary>
        /// Export the canvas as JPG.
        /// </summary>
        public void ExportAsJPG(string filePath, int quality = 90)
        {
            if (_layerManager != null)
            {
                byte[] jpgData = _layerManager.ExportAsJPG(quality);
                File.WriteAllBytes(filePath, jpgData);
                OnCanvasSaved?.Invoke(filePath);
            }
        }

        /// <summary>
        /// Undo last operation (placeholder - not yet implemented).
        /// </summary>
        public void Undo()
        {
            Debug.LogWarning("Undo is not yet implemented");
        }

        /// <summary>
        /// Redo last undone operation (placeholder - not yet implemented).
        /// </summary>
        public void Redo()
        {
            Debug.LogWarning("Redo is not yet implemented");
        }

        /// <summary>
        /// Zoom to fit canvas in view.
        /// </summary>
        public void ZoomToFit()
        {
            if (_canvasUI != null)
            {
                _canvasUI.ZoomToFit();
            }
        }

        /// <summary>
        /// Zoom to actual size (100%).
        /// </summary>
        public void ZoomToActualSize()
        {
            if (_canvasUI != null)
            {
                _canvasUI.ZoomToActualSize();
            }
        }
    }

    /// <summary>
    /// Brush settings UI panel that reads/writes to DrawToolSettings.
    /// </summary>
    public class BrushSettingsUI : MonoBehaviour
    {
        [Header("Settings Reference")]
        [SerializeField] private DrawToolSettings _settings;
        [SerializeField] private DrawToolController _controller;

        [Header("UI Elements")]
        [SerializeField] private Slider _sizeSlider;
        [SerializeField] private Slider _opacitySlider;
        [SerializeField] private Slider _hardnessSlider;
        [SerializeField] private Slider _spacingSlider;
        [SerializeField] private TMPro.TMP_Dropdown _brushTypeDropdown;
        [SerializeField] private TMPro.TMP_Text _sizeLabel;
        [SerializeField] private TMPro.TMP_Text _opacityLabel;
        [SerializeField] private Toggle _pressureSizeToggle;
        [SerializeField] private Toggle _pressureOpacityToggle;

        [Header("Brush Presets")]
        [SerializeField] private Transform _presetContainer;
        [SerializeField] private GameObject _presetButtonPrefab;

        private bool _isUpdatingUI = false;

        private void OnEnable()
        {
            SubscribeToSettings();
        }

        private void OnDisable()
        {
            UnsubscribeFromSettings();
        }

        private void Start()
        {
            // Try to get settings from controller if not assigned
            if (_settings == null && _controller != null)
            {
                _settings = _controller.Settings;
            }

            SetupUI();
            CreatePresetButtons();
            SyncUIFromSettings();
        }

        private void SubscribeToSettings()
        {
            if (_settings == null) return;

            _settings.OnBrushTypeChanged += OnSettingsBrushTypeChanged;
            _settings.OnBrushSizeChanged += OnSettingsBrushSizeChanged;
            _settings.OnBrushOpacityChanged += OnSettingsBrushOpacityChanged;
            _settings.OnBrushHardnessChanged += OnSettingsBrushHardnessChanged;
            _settings.OnSettingsChanged += OnSettingsChanged;
        }

        private void UnsubscribeFromSettings()
        {
            if (_settings == null) return;

            _settings.OnBrushTypeChanged -= OnSettingsBrushTypeChanged;
            _settings.OnBrushSizeChanged -= OnSettingsBrushSizeChanged;
            _settings.OnBrushOpacityChanged -= OnSettingsBrushOpacityChanged;
            _settings.OnBrushHardnessChanged -= OnSettingsBrushHardnessChanged;
            _settings.OnSettingsChanged -= OnSettingsChanged;
        }

        private void SetupUI()
        {
            if (_sizeSlider != null)
            {
                _sizeSlider.minValue = 1;
                _sizeSlider.maxValue = 200;
                _sizeSlider.onValueChanged.AddListener(OnSizeSliderChanged);
            }

            if (_opacitySlider != null)
            {
                _opacitySlider.minValue = 0;
                _opacitySlider.maxValue = 1;
                _opacitySlider.onValueChanged.AddListener(OnOpacitySliderChanged);
            }

            if (_hardnessSlider != null)
            {
                _hardnessSlider.minValue = 0;
                _hardnessSlider.maxValue = 1;
                _hardnessSlider.onValueChanged.AddListener(OnHardnessSliderChanged);
            }

            if (_spacingSlider != null)
            {
                _spacingSlider.minValue = 0.01f;
                _spacingSlider.maxValue = 2f;
                _spacingSlider.onValueChanged.AddListener(OnSpacingSliderChanged);
            }

            if (_brushTypeDropdown != null)
            {
                _brushTypeDropdown.ClearOptions();
                var options = new List<TMPro.TMP_Dropdown.OptionData>();
                foreach (Brush.BrushType type in System.Enum.GetValues(typeof(Brush.BrushType)))
                {
                    options.Add(new TMPro.TMP_Dropdown.OptionData(type.ToString()));
                }
                _brushTypeDropdown.AddOptions(options);
                _brushTypeDropdown.onValueChanged.AddListener(OnBrushTypeDropdownChanged);
            }

            if (_pressureSizeToggle != null)
            {
                _pressureSizeToggle.onValueChanged.AddListener(OnPressureSizeToggleChanged);
            }

            if (_pressureOpacityToggle != null)
            {
                _pressureOpacityToggle.onValueChanged.AddListener(OnPressureOpacityToggleChanged);
            }
        }

        private void CreatePresetButtons()
        {
            if (_presetContainer == null || _presetButtonPrefab == null || _settings == null) return;

            // Clear existing
            foreach (Transform child in _presetContainer)
            {
                Destroy(child.gameObject);
            }

            // Create buttons for each preset
            foreach (var preset in _settings.BrushPresets)
            {
                GameObject btn = Instantiate(_presetButtonPrefab, _presetContainer);
                Button button = btn.GetComponent<Button>();
                if (button != null)
                {
                    var capturedPreset = preset;
                    button.onClick.AddListener(() => SelectPreset(capturedPreset));
                }

                TMPro.TMP_Text label = btn.GetComponentInChildren<TMPro.TMP_Text>();
                if (label != null)
                {
                    label.text = preset.Name;
                }
            }
        }

        private void SelectPreset(DrawToolSettings.BrushPreset preset)
        {
            if (_settings != null)
            {
                _settings.ApplyPreset(preset);
            }
        }

        /// <summary>
        /// Sync all UI elements from current settings.
        /// </summary>
        public void SyncUIFromSettings()
        {
            if (_settings == null) return;

            _isUpdatingUI = true;

            if (_sizeSlider != null) _sizeSlider.SetValueWithoutNotify(_settings.BrushSize);
            if (_opacitySlider != null) _opacitySlider.SetValueWithoutNotify(_settings.BrushOpacity);
            if (_hardnessSlider != null) _hardnessSlider.SetValueWithoutNotify(_settings.BrushHardness);
            if (_spacingSlider != null) _spacingSlider.SetValueWithoutNotify(_settings.BrushSpacing);
            if (_brushTypeDropdown != null) _brushTypeDropdown.SetValueWithoutNotify((int)_settings.BrushType);
            if (_pressureSizeToggle != null) _pressureSizeToggle.SetIsOnWithoutNotify(_settings.UsePressureForSize);
            if (_pressureOpacityToggle != null) _pressureOpacityToggle.SetIsOnWithoutNotify(_settings.UsePressureForOpacity);

            UpdateLabels();

            _isUpdatingUI = false;
        }

        private void UpdateLabels()
        {
            if (_sizeLabel != null && _settings != null)
            {
                _sizeLabel.text = $"Size: {_settings.BrushSize}px";
            }

            if (_opacityLabel != null && _settings != null)
            {
                _opacityLabel.text = $"Opacity: {Mathf.RoundToInt(_settings.BrushOpacity * 100)}%";
            }
        }

        // ==================== UI CALLBACKS ====================

        private void OnSizeSliderChanged(float value)
        {
            if (_isUpdatingUI || _settings == null) return;
            _settings.BrushSize = Mathf.RoundToInt(value);
            UpdateLabels();
        }

        private void OnOpacitySliderChanged(float value)
        {
            if (_isUpdatingUI || _settings == null) return;
            _settings.BrushOpacity = value;
            UpdateLabels();
        }

        private void OnHardnessSliderChanged(float value)
        {
            if (_isUpdatingUI || _settings == null) return;
            _settings.BrushHardness = value;
        }

        private void OnSpacingSliderChanged(float value)
        {
            if (_isUpdatingUI || _settings == null) return;
            _settings.BrushSpacing = value;
        }

        private void OnBrushTypeDropdownChanged(int index)
        {
            if (_isUpdatingUI || _settings == null) return;
            _settings.BrushType = (Brush.BrushType)index;
        }

        private void OnPressureSizeToggleChanged(bool enabled)
        {
            if (_isUpdatingUI || _settings == null) return;
            _settings.UsePressureForSize = enabled;
        }

        private void OnPressureOpacityToggleChanged(bool enabled)
        {
            if (_isUpdatingUI || _settings == null) return;
            _settings.UsePressureForOpacity = enabled;
        }

        // ==================== SETTINGS CALLBACKS ====================

        private void OnSettingsBrushTypeChanged(Brush.BrushType type)
        {
            if (_brushTypeDropdown != null)
            {
                _isUpdatingUI = true;
                _brushTypeDropdown.SetValueWithoutNotify((int)type);
                _isUpdatingUI = false;
            }
        }

        private void OnSettingsBrushSizeChanged(int size)
        {
            if (_sizeSlider != null)
            {
                _isUpdatingUI = true;
                _sizeSlider.SetValueWithoutNotify(size);
                _isUpdatingUI = false;
            }
            UpdateLabels();
        }

        private void OnSettingsBrushOpacityChanged(float opacity)
        {
            if (_opacitySlider != null)
            {
                _isUpdatingUI = true;
                _opacitySlider.SetValueWithoutNotify(opacity);
                _isUpdatingUI = false;
            }
            UpdateLabels();
        }

        private void OnSettingsBrushHardnessChanged(float hardness)
        {
            if (_hardnessSlider != null)
            {
                _isUpdatingUI = true;
                _hardnessSlider.SetValueWithoutNotify(hardness);
                _isUpdatingUI = false;
            }
        }

        private void OnSettingsChanged()
        {
            SyncUIFromSettings();
        }
    }
}
