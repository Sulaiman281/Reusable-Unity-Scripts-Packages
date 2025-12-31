namespace WitShells.CanvasDrawTool
{
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.EventSystems;
    using UnityEngine.Events;
    using System;

    /// <summary>
    /// Main drawing canvas UI component.
    /// Manages the layer container and provides zoom/pan functionality.
    /// Each layer is now a separate RawImage GameObject for mobile performance.
    /// </summary>
    public class DrawCanvasUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Canvas Settings")]
        [SerializeField] private int _defaultWidth = 1024;
        [SerializeField] private int _defaultHeight = 1024;
        [SerializeField] private Color _backgroundColor = Color.white;

        [Header("References")]
        [SerializeField] private RectTransform _canvasRect;
        [SerializeField] private LayerManager _layerManager;
        [SerializeField] private DrawingEngine _drawingEngine;
        [SerializeField] private RawImage _backgroundImage; // For checkerboard

        [Header("Display Settings")]
        [SerializeField] private bool _showCheckerboard = true;
        [SerializeField] private Color _checkerColor1 = new Color(0.8f, 0.8f, 0.8f, 1f);
        [SerializeField] private Color _checkerColor2 = new Color(0.6f, 0.6f, 0.6f, 1f);
        [SerializeField] private int _checkerSize = 16;

        [Header("Zoom/Pan")]
        [SerializeField] private float _minZoom = 0.1f;
        [SerializeField] private float _maxZoom = 10f;
        [SerializeField] private float _zoomSpeed = 0.1f;
        [SerializeField] private float _currentZoom = 1f;
        [SerializeField] private Vector2 _panOffset = Vector2.zero;

        [Header("Events")]
        public UnityEvent OnCanvasCreated;
        public UnityEvent OnCanvasCleared;
        public UnityEvent<float> OnZoomChanged;
        public UnityEvent<Vector2> OnPanChanged;
        public UnityEvent OnPointerEnterCanvas;
        public UnityEvent OnPointerExitCanvas;

        private Texture2D _checkerboardTexture;
        private bool _isPointerOver;
        private int _canvasWidth;
        private int _canvasHeight;

        // Public accessors
        public int CanvasWidth => _canvasWidth;
        public int CanvasHeight => _canvasHeight;
        public float CurrentZoom => _currentZoom;
        public Vector2 PanOffset => _panOffset;
        public bool IsPointerOver => _isPointerOver;
        public LayerManager LayerManager => _layerManager;
        public DrawingEngine DrawingEngine => _drawingEngine;
        public RectTransform CanvasRect => _canvasRect;

        private void Awake()
        {
            if (_canvasRect == null)
                _canvasRect = GetComponent<RectTransform>();
        }

        private void Start()
        {
            // Create canvas with default size if not already created
            if (_layerManager == null || _layerManager.CompositeTexture == null)
            {
                CreateNewCanvas(_defaultWidth, _defaultHeight);
            }
        }

        /// <summary>
        /// Create a new canvas with specified dimensions.
        /// </summary>
        public void CreateNewCanvas(int width, int height, Color? backgroundColor = null)
        {
            _canvasWidth = width;
            _canvasHeight = height;

            Color bgColor = backgroundColor ?? _backgroundColor;

            // Create layer manager if needed
            if (_layerManager == null)
            {
                _layerManager = gameObject.AddComponent<LayerManager>();
            }

            // Set up canvas rect with center anchors and fixed size
            // This ensures coordinate conversion works correctly
            _canvasRect.anchorMin = new Vector2(0.5f, 0.5f);
            _canvasRect.anchorMax = new Vector2(0.5f, 0.5f);
            _canvasRect.pivot = new Vector2(0.5f, 0.5f);
            _canvasRect.sizeDelta = new Vector2(width, height);
            _canvasRect.anchoredPosition = Vector2.zero;

            // Set layer container to our rect
            _layerManager.SetLayerContainer(_canvasRect);

            // Initialize layer manager
            _layerManager.InitializeCanvas(width, height);

            // Create drawing engine if needed
            if (_drawingEngine == null)
            {
                _drawingEngine = gameObject.AddComponent<DrawingEngine>();
            }

            // Setup background layer
            LayerObject bgLayer = _layerManager.LayerCount > 0 ? _layerManager.Layers[0] : null;
            if (bgLayer != null)
            {
                bgLayer.LayerName = "Background";
                bgLayer.Clear(bgColor);
                bgLayer.ApplyChanges();
            }

            // Create a drawing layer
            _layerManager.CreateLayer("Layer 1");
            _layerManager.SetActiveLayer(1);

            // Generate checkerboard if enabled
            if (_showCheckerboard)
            {
                GenerateCheckerboard();
            }

            OnCanvasCreated?.Invoke();
        }

        /// <summary>
        /// Clear all layers and start fresh.
        /// </summary>
        public void ClearCanvas()
        {
            if (_layerManager != null)
            {
                // Clear all layers except background
                for (int i = _layerManager.LayerCount - 1; i > 0; i--)
                {
                    _layerManager.DeleteLayer(i);
                }

                // Clear background
                LayerObject bgLayer = _layerManager.LayerCount > 0 ? _layerManager.Layers[0] : null;
                if (bgLayer != null)
                {
                    bgLayer.Clear(_backgroundColor);
                    bgLayer.ApplyChanges();
                }

                // Create a new drawing layer
                _layerManager.CreateLayer("Layer 1");
                _layerManager.SetActiveLayer(1);
            }

            OnCanvasCleared?.Invoke();
        }

        /// <summary>
        /// Update the canvas display (refresh layer visuals).
        /// </summary>
        public void UpdateCanvasDisplay()
        {
            // With the new system, each layer updates itself
            // Just update composite for export purposes
            if (_layerManager != null)
            {
                _layerManager.UpdateComposite();
            }
        }

        /// <summary>
        /// Set zoom level.
        /// </summary>
        public void SetZoom(float zoom)
        {
            _currentZoom = Mathf.Clamp(zoom, _minZoom, _maxZoom);
            ApplyTransform();
            OnZoomChanged?.Invoke(_currentZoom);
        }

        /// <summary>
        /// Zoom in by step.
        /// </summary>
        public void ZoomIn()
        {
            SetZoom(_currentZoom + _zoomSpeed);
        }

        /// <summary>
        /// Zoom out by step.
        /// </summary>
        public void ZoomOut()
        {
            SetZoom(_currentZoom - _zoomSpeed);
        }

        /// <summary>
        /// Zoom to fit canvas in view.
        /// </summary>
        public void ZoomToFit()
        {
            if (_canvasRect == null) return;

            RectTransform parentRect = _canvasRect.parent as RectTransform;
            if (parentRect == null) return;

            float parentWidth = parentRect.rect.width;
            float parentHeight = parentRect.rect.height;

            float scaleX = parentWidth / _canvasWidth;
            float scaleY = parentHeight / _canvasHeight;

            SetZoom(Mathf.Min(scaleX, scaleY) * 0.9f);
            ResetPan();
        }

        /// <summary>
        /// Zoom to actual size (100%).
        /// </summary>
        public void ZoomToActualSize()
        {
            SetZoom(1f);
            ResetPan();
        }

        /// <summary>
        /// Set pan offset.
        /// </summary>
        public void SetPan(Vector2 offset)
        {
            _panOffset = offset;
            ApplyTransform();
            OnPanChanged?.Invoke(_panOffset);
        }

        /// <summary>
        /// Add to pan offset.
        /// </summary>
        public void Pan(Vector2 delta)
        {
            SetPan(_panOffset + delta);
        }

        /// <summary>
        /// Reset pan to center.
        /// </summary>
        public void ResetPan()
        {
            SetPan(Vector2.zero);
        }

        /// <summary>
        /// Apply zoom and pan transforms.
        /// </summary>
        private void ApplyTransform()
        {
            if (_canvasRect == null) return;

            _canvasRect.localScale = new Vector3(_currentZoom, _currentZoom, 1f);
            _canvasRect.anchoredPosition = _panOffset;
        }

        /// <summary>
        /// Generate checkerboard texture for transparency.
        /// </summary>
        private void GenerateCheckerboard()
        {
            if (_checkerboardTexture != null)
            {
                Destroy(_checkerboardTexture);
            }

            int texSize = _checkerSize * 2;
            _checkerboardTexture = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            _checkerboardTexture.filterMode = FilterMode.Point;
            _checkerboardTexture.wrapMode = TextureWrapMode.Repeat;

            for (int y = 0; y < texSize; y++)
            {
                for (int x = 0; x < texSize; x++)
                {
                    bool isEven = ((x / _checkerSize) + (y / _checkerSize)) % 2 == 0;
                    _checkerboardTexture.SetPixel(x, y, isEven ? _checkerColor1 : _checkerColor2);
                }
            }

            _checkerboardTexture.Apply();

            // Create background image if needed
            if (_backgroundImage == null)
            {
                GameObject bgObj = new GameObject("Checkerboard");
                bgObj.transform.SetParent(transform, false);
                bgObj.transform.SetAsFirstSibling();

                RectTransform bgRect = bgObj.AddComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;

                _backgroundImage = bgObj.AddComponent<RawImage>();
                _backgroundImage.raycastTarget = false;
            }

            _backgroundImage.texture = _checkerboardTexture;
            _backgroundImage.uvRect = new Rect(0, 0, (float)_canvasWidth / _checkerSize, (float)_canvasHeight / _checkerSize);
        }

        /// <summary>
        /// Convert screen position to canvas pixel coordinates.
        /// </summary>
        public bool ScreenToCanvasPixel(Vector2 screenPosition, out Vector2Int pixelPosition)
        {
            pixelPosition = Vector2Int.zero;

            if (_canvasRect == null) return false;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return false;

            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

            // Convert screen to local position in canvas rect
            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPosition, cam, out localPoint))
            {
                return false;
            }

            // Account for zoom and pan
            localPoint /= _currentZoom;

            // Convert to pixel coordinates (0 to width/height)
            float normalizedX = (localPoint.x / _canvasRect.rect.width) + 0.5f;
            float normalizedY = (localPoint.y / _canvasRect.rect.height) + 0.5f;

            int x = Mathf.FloorToInt(normalizedX * _canvasWidth);
            int y = Mathf.FloorToInt(normalizedY * _canvasHeight);

            // Check bounds
            if (x < 0 || x >= _canvasWidth || y < 0 || y >= _canvasHeight)
            {
                return false;
            }

            pixelPosition = new Vector2Int(x, y);
            return true;
        }

        /// <summary>
        /// Get color at screen position.
        /// </summary>
        public bool GetColorAtScreenPosition(Vector2 screenPosition, out Color color)
        {
            color = Color.clear;

            Vector2Int pixelPos;
            if (!ScreenToCanvasPixel(screenPosition, out pixelPos)) return false;

            if (_layerManager != null && _layerManager.CompositeTexture != null)
            {
                color = _layerManager.CompositeTexture.GetPixel(pixelPos.x, pixelPos.y);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Pick color from current active layer.
        /// </summary>
        public bool PickColorFromActiveLayer(Vector2 screenPosition, out Color color)
        {
            color = Color.clear;

            Vector2Int pixelPos;
            if (!ScreenToCanvasPixel(screenPosition, out pixelPos)) return false;

            LayerObject activeLayer = _layerManager?.ActiveLayer;
            if (activeLayer != null)
            {
                color = activeLayer.GetPixel(pixelPos.x, pixelPos.y);
                return true;
            }

            return false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isPointerOver = true;
            OnPointerEnterCanvas?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isPointerOver = false;
            OnPointerExitCanvas?.Invoke();
        }

        private void OnDestroy()
        {
            if (_checkerboardTexture != null)
            {
                Destroy(_checkerboardTexture);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _defaultWidth = Mathf.Max(1, _defaultWidth);
            _defaultHeight = Mathf.Max(1, _defaultHeight);
            _checkerSize = Mathf.Max(4, _checkerSize);
            _minZoom = Mathf.Max(0.01f, _minZoom);
            _maxZoom = Mathf.Max(_minZoom, _maxZoom);
        }
#endif
    }
}
