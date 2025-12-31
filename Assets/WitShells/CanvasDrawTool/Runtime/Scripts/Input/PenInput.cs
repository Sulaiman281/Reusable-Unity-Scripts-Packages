namespace WitShells.CanvasDrawTool
{
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.Events;

    /// <summary>
    /// Specialized input handler for pen/stylus tablets.
    /// Supports pressure sensitivity, tilt, and pen-specific features.
    /// Works with Wacom, Surface Pen, Apple Pencil (via Unity's Pen device).
    /// </summary>
    public class PenInput : DrawingInput
    {
        [Header("Pen Settings")]
        [Tooltip("Enable pressure sensitivity for opacity")]
        [SerializeField] private bool _usePressureForOpacity = true;

        [Tooltip("Enable pressure sensitivity for brush size")]
        [SerializeField] private bool _usePressureForSize = false;

        [Tooltip("Minimum pressure threshold to start drawing")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _pressureThreshold = 0.01f;

        [Tooltip("Pressure curve for more control")]
        [SerializeField] private AnimationCurve _pressureCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Tooltip("Invert pressure (1 - pressure)")]
        [SerializeField] private bool _invertPressure = false;

        [Header("Tilt Settings")]
        [Tooltip("Use pen tilt for brush angle")]
        [SerializeField] private bool _useTilt = false;

        [Tooltip("Tilt sensitivity multiplier")]
        [Range(0.1f, 5f)]
        [SerializeField] private float _tiltSensitivity = 1f;

        [Header("Barrel Button")]
        [Tooltip("What action the barrel button performs")]
        [SerializeField] private BarrelButtonAction _barrelButtonAction = BarrelButtonAction.Eraser;

        [Header("Events")]
        public UnityEvent<float> OnPressureChanged;
        public UnityEvent<Vector2> OnTiltChanged;
        public UnityEvent OnBarrelButtonPressed;
        public UnityEvent OnBarrelButtonReleased;
        public UnityEvent OnEraserTipActive;
        public UnityEvent OnPenTipActive;

        public enum BarrelButtonAction
        {
            None,
            Eraser,
            Eyedropper,
            RightClick,
            Pan
        }

        private Pen _pen;
        private bool _isPenAvailable;
        private bool _isBarrelButtonPressed;
        private bool _isEraserTip;
        private float _lastPressure;
        private Vector2 _lastTilt;

        // Public accessors
        public bool IsPenAvailable => _isPenAvailable;
        public bool UsePressureForOpacity
        {
            get => _usePressureForOpacity;
            set => _usePressureForOpacity = value;
        }
        public bool UsePressureForSize
        {
            get => _usePressureForSize;
            set => _usePressureForSize = value;
        }
        public bool IsBarrelButtonPressed => _isBarrelButtonPressed;
        public bool IsEraserTip => _isEraserTip;
        public float RawPressure => _lastPressure;
        public Vector2 Tilt => _lastTilt;
        public BarrelButtonAction CurrentBarrelAction => _barrelButtonAction;

        protected override void Awake()
        {
            base.Awake();
            _pen = Pen.current;
            _isPenAvailable = _pen != null;

            // Set up default pressure curve if not assigned
            if (_pressureCurve == null || _pressureCurve.length == 0)
            {
                _pressureCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            }
        }

        protected override void Update()
        {
            // Update pen reference (in case it connects/disconnects)
            if (Pen.current != _pen)
            {
                _pen = Pen.current;
                _isPenAvailable = _pen != null;
            }

            if (_isPenAvailable)
            {
                HandlePenInput();
            }
            else
            {
                // Fall back to base mouse/touch handling
                base.Update();
            }
        }

        private void HandlePenInput()
        {
            Vector2 screenPosition = _pen.position.ReadValue();
            float rawPressure = _pen.pressure.ReadValue();
            Vector2 tilt = _pen.tilt.ReadValue();

            // Process pressure
            float processedPressure = ProcessPressure(rawPressure);

            // Update tilt
            if (_useTilt)
            {
                _lastTilt = tilt * _tiltSensitivity;
                OnTiltChanged?.Invoke(_lastTilt);
            }

            // Check for pressure change
            if (Mathf.Abs(rawPressure - _lastPressure) > 0.001f)
            {
                _lastPressure = rawPressure;
                OnPressureChanged?.Invoke(processedPressure);
            }

            // Handle barrel button
            HandleBarrelButton();

            // Handle eraser tip detection (if supported)
            HandleEraserTip();

            // Convert screen to canvas coordinates
            Vector2 canvasPosition;
            if (!ScreenToCanvas(screenPosition, out canvasPosition)) return;

            // Pen tip pressed (pressure above threshold)
            if (_pen.tip.wasPressedThisFrame || (rawPressure > _pressureThreshold && !_isDrawing))
            {
                if (rawPressure >= _pressureThreshold)
                {
                    StartDraw(canvasPosition, processedPressure);
                }
            }
            else if (_pen.tip.isPressed && _isDrawing)
            {
                if (rawPressure >= _pressureThreshold)
                {
                    ContinueDraw(canvasPosition, processedPressure);
                }
                else
                {
                    // Pressure dropped below threshold
                    EndDraw(canvasPosition, processedPressure);
                }
            }
            else if (_pen.tip.wasReleasedThisFrame && _isDrawing)
            {
                EndDraw(canvasPosition, processedPressure);
            }
        }

        private void HandleBarrelButton()
        {
            // Check for barrel button (first button on pen)
            if (_pen.firstBarrelButton.wasPressedThisFrame)
            {
                _isBarrelButtonPressed = true;
                OnBarrelButtonPressed?.Invoke();
            }
            else if (_pen.firstBarrelButton.wasReleasedThisFrame)
            {
                _isBarrelButtonPressed = false;
                OnBarrelButtonReleased?.Invoke();
            }

            // Also check second barrel button
            if (_pen.secondBarrelButton.wasPressedThisFrame)
            {
                _isBarrelButtonPressed = true;
                OnBarrelButtonPressed?.Invoke();
            }
            else if (_pen.secondBarrelButton.wasReleasedThisFrame)
            {
                _isBarrelButtonPressed = false;
                OnBarrelButtonReleased?.Invoke();
            }
        }

        private void HandleEraserTip()
        {
            // Check if eraser end is being used (if pen supports it)
            bool eraserActive = _pen.eraser.isPressed;

            if (eraserActive && !_isEraserTip)
            {
                _isEraserTip = true;
                OnEraserTipActive?.Invoke();
            }
            else if (!eraserActive && _isEraserTip)
            {
                _isEraserTip = false;
                OnPenTipActive?.Invoke();
            }
        }

        /// <summary>
        /// Process raw pressure through curve and settings.
        /// </summary>
        private float ProcessPressure(float rawPressure)
        {
            // Apply pressure curve
            float pressure = _pressureCurve.Evaluate(rawPressure);

            // Invert if needed
            if (_invertPressure)
            {
                pressure = 1f - pressure;
            }

            return Mathf.Clamp01(pressure);
        }

        /// <summary>
        /// Get the effective opacity based on pressure settings.
        /// </summary>
        public float GetEffectiveOpacity(float baseOpacity, float pressure)
        {
            if (!_usePressureForOpacity) return baseOpacity;
            return baseOpacity * pressure;
        }

        /// <summary>
        /// Get the effective size based on pressure settings.
        /// </summary>
        public int GetEffectiveSize(int baseSize, float pressure)
        {
            if (!_usePressureForSize) return baseSize;
            return Mathf.Max(1, Mathf.RoundToInt(baseSize * pressure));
        }

        /// <summary>
        /// Set the pressure curve.
        /// </summary>
        public void SetPressureCurve(AnimationCurve curve)
        {
            _pressureCurve = curve;
        }

        /// <summary>
        /// Set the barrel button action.
        /// </summary>
        public void SetBarrelButtonAction(BarrelButtonAction action)
        {
            _barrelButtonAction = action;
        }

        /// <summary>
        /// Check if we should use eraser (either eraser tip or barrel button set to eraser).
        /// </summary>
        public bool ShouldUseEraser()
        {
            return _isEraserTip || (_isBarrelButtonPressed && _barrelButtonAction == BarrelButtonAction.Eraser);
        }

        /// <summary>
        /// Check if we should use eyedropper (barrel button set to eyedropper).
        /// </summary>
        public bool ShouldUseEyedropper()
        {
            return _isBarrelButtonPressed && _barrelButtonAction == BarrelButtonAction.Eyedropper;
        }

        protected override float GetTouchPressure(UnityEngine.InputSystem.EnhancedTouch.Finger finger)
        {
            // If we have a pen, try to get pressure from it
            if (_isPenAvailable && _pen.pressure.ReadValue() > 0)
            {
                return ProcessPressure(_pen.pressure.ReadValue());
            }

            // Apple Pencil on iPad will come through as touch with pressure
            var touch = finger.currentTouch;
            if (touch.pressure > 0)
            {
                return ProcessPressure(touch.pressure);
            }

            return 1f;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure pressure curve is valid
            if (_pressureCurve == null || _pressureCurve.length == 0)
            {
                _pressureCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            }
        }
#endif
    }
}
