namespace WitShells.CanvasDrawTool
{
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.InputSystem.EnhancedTouch;
    using UnityEngine.Events;
    using ETouch = UnityEngine.InputSystem.EnhancedTouch;

    /// <summary>
    /// Handles all input for the drawing tool using New Input System.
    /// Supports mouse, touch, and provides base for pen input.
    /// </summary>
    public class DrawingInput : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private bool _enableTouch = true;
        [SerializeField] private bool _enableMouse = true;
        [SerializeField] private bool _debugMode = false;

        [Header("Canvas Reference")]
        [SerializeField] private RectTransform _canvasRect;
        [SerializeField] private Camera _uiCamera;

        [Header("Events")]
        public UnityEvent<Vector2, float> OnDrawStart;
        public UnityEvent<Vector2, float> OnDrawMove;
        public UnityEvent<Vector2, float> OnDrawEnd;
        public UnityEvent<float> OnZoom;
        public UnityEvent<Vector2> OnPan;

        protected bool _isDrawing;
        protected Vector2 _lastPosition;
        protected float _currentPressure = 1f;

        private Mouse _mouse;
        private bool _isPanning;
        private Vector2 _panStartPosition;

        public bool IsDrawing => _isDrawing;
        public Vector2 LastPosition => _lastPosition;
        public float CurrentPressure => _currentPressure;

        protected virtual void OnEnable()
        {
            if (_enableTouch)
            {
                EnhancedTouchSupport.Enable();
                ETouch.Touch.onFingerDown += OnFingerDown;
                ETouch.Touch.onFingerMove += OnFingerMove;
                ETouch.Touch.onFingerUp += OnFingerUp;
            }
        }

        protected virtual void OnDisable()
        {
            if (_enableTouch && EnhancedTouchSupport.enabled)
            {
                ETouch.Touch.onFingerDown -= OnFingerDown;
                ETouch.Touch.onFingerMove -= OnFingerMove;
                ETouch.Touch.onFingerUp -= OnFingerUp;
            }
        }

        protected virtual void Awake()
        {
            _mouse = Mouse.current;

            // Don't auto-assign camera - let it be set based on canvas render mode
        }

        protected virtual void Update()
        {
            if (!_enableMouse || _mouse == null) return;

            HandleMouseInput();
            HandleScrollZoom();
        }

        private void HandleMouseInput()
        {
            Vector2 screenPosition = _mouse.position.ReadValue();
            Vector2 canvasPosition;

            if (!ScreenToCanvas(screenPosition, out canvasPosition)) return;

            // Left mouse button for drawing
            if (_mouse.leftButton.wasPressedThisFrame)
            {
                StartDraw(canvasPosition, 1f);
            }
            else if (_mouse.leftButton.isPressed && _isDrawing)
            {
                ContinueDraw(canvasPosition, 1f);
            }
            else if (_mouse.leftButton.wasReleasedThisFrame && _isDrawing)
            {
                EndDraw(canvasPosition, 1f);
            }

            // Middle mouse button for panning
            if (_mouse.middleButton.wasPressedThisFrame)
            {
                _isPanning = true;
                _panStartPosition = screenPosition;
            }
            else if (_mouse.middleButton.isPressed && _isPanning)
            {
                Vector2 delta = screenPosition - _panStartPosition;
                _panStartPosition = screenPosition;
                OnPan?.Invoke(delta);
            }
            else if (_mouse.middleButton.wasReleasedThisFrame)
            {
                _isPanning = false;
            }
        }

        private void HandleScrollZoom()
        {
            float scroll = _mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                OnZoom?.Invoke(scroll > 0 ? 1.1f : 0.9f);
            }
        }

        #region Touch Input

        private void OnFingerDown(Finger finger)
        {
            if (finger.index != 0) return; // Only primary finger

            Vector2 canvasPosition;
            if (ScreenToCanvas(finger.screenPosition, out canvasPosition))
            {
                float pressure = GetTouchPressure(finger);
                StartDraw(canvasPosition, pressure);
            }
        }

        private void OnFingerMove(Finger finger)
        {
            if (finger.index != 0 || !_isDrawing) return;

            Vector2 canvasPosition;
            if (ScreenToCanvas(finger.screenPosition, out canvasPosition))
            {
                float pressure = GetTouchPressure(finger);
                ContinueDraw(canvasPosition, pressure);
            }
        }

        private void OnFingerUp(Finger finger)
        {
            if (finger.index != 0 || !_isDrawing) return;

            Vector2 canvasPosition;
            if (ScreenToCanvas(finger.screenPosition, out canvasPosition))
            {
                float pressure = GetTouchPressure(finger);
                EndDraw(canvasPosition, pressure);
            }
        }

        protected virtual float GetTouchPressure(Finger finger)
        {
            // Base touch doesn't have pressure, return 1
            return 1f;
        }

        #endregion

        #region Draw Methods

        protected virtual void StartDraw(Vector2 canvasPosition, float pressure)
        {
            _isDrawing = true;
            _lastPosition = canvasPosition;
            _currentPressure = pressure;

            OnDrawStart?.Invoke(canvasPosition, pressure);
        }

        protected virtual void ContinueDraw(Vector2 canvasPosition, float pressure)
        {
            _lastPosition = canvasPosition;
            _currentPressure = pressure;

            OnDrawMove?.Invoke(canvasPosition, pressure);
        }

        protected virtual void EndDraw(Vector2 canvasPosition, float pressure)
        {
            _isDrawing = false;
            _currentPressure = pressure;

            OnDrawEnd?.Invoke(canvasPosition, pressure);
        }

        #endregion

        #region Coordinate Conversion

        /// <summary>
        /// Convert screen position to canvas coordinates.
        /// Returns normalized coordinates (0-1) relative to canvas bounds.
        /// </summary>
        public bool ScreenToCanvas(Vector2 screenPosition, out Vector2 canvasPosition)
        {
            canvasPosition = Vector2.zero;

            if (_canvasRect == null) return false;

            // Auto-detect correct camera based on canvas render mode
            Camera cam = _uiCamera;
            Canvas parentCanvas = _canvasRect.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    cam = null; // Overlay mode uses null camera
                }
                else if (cam == null)
                {
                    cam = parentCanvas.worldCamera ?? Camera.main;
                }
            }

            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPosition, cam, out localPoint))
            {
                // Convert from rect local to normalized coordinates (0-1)
                // The localPoint is relative to the pivot, so we need to account for that
                Rect rect = _canvasRect.rect;
                
                // Normalize: localPoint ranges from rect.xMin to rect.xMax
                // We want to map this to 0-1
                float normalizedX = (localPoint.x - rect.xMin) / rect.width;
                float normalizedY = (localPoint.y - rect.yMin) / rect.height;
                
                canvasPosition = new Vector2(normalizedX, normalizedY);

                // Check if within bounds (0-1)
                if (canvasPosition.x >= 0 && canvasPosition.x <= 1 &&
                    canvasPosition.y >= 0 && canvasPosition.y <= 1)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Convert normalized canvas position to pixel coordinates.
        /// </summary>
        public Vector2Int NormalizedToPixel(Vector2 normalized, int width, int height)
        {
            return new Vector2Int(
                Mathf.Clamp(Mathf.RoundToInt(normalized.x * width), 0, width - 1),
                Mathf.Clamp(Mathf.RoundToInt(normalized.y * height), 0, height - 1)
            );
        }

        #endregion

        /// <summary>
        /// Set the canvas rect transform for coordinate conversion.
        /// </summary>
        public void SetCanvasRect(RectTransform rect)
        {
            _canvasRect = rect;
        }

        /// <summary>
        /// Set the UI camera for coordinate conversion.
        /// </summary>
        public void SetUICamera(Camera cam)
        {
            _uiCamera = cam;
        }
    }
}
