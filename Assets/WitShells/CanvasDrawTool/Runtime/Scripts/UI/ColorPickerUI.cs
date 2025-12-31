namespace WitShells.CanvasDrawTool
{
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.Events;
    using UnityEngine.EventSystems;

    /// <summary>
    /// Color picker UI with hue bar, saturation/value square, and alpha slider.
    /// Supports color history and direct input.
    /// </summary>
    public class ColorPickerUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RawImage _svSquare;
        [SerializeField] private RawImage _hueBar;
        [SerializeField] private Slider _alphaSlider;
        [SerializeField] private Image _currentColorPreview;
        [SerializeField] private Image _previousColorPreview;

        [Header("Cursor References")]
        [SerializeField] private RectTransform _svCursor;
        [SerializeField] private RectTransform _hueCursor;

        [Header("Input Fields")]
        [SerializeField] private TMPro.TMP_InputField _hexInput;
        [SerializeField] private Slider _rSlider;
        [SerializeField] private Slider _gSlider;
        [SerializeField] private Slider _bSlider;

        [Header("Color History")]
        [SerializeField] private Transform _historyContainer;
        [SerializeField] private GameObject _historyColorPrefab;
        [SerializeField] private int _maxHistoryColors = 12;

        [Header("Events")]
        public UnityEvent<Color> OnColorChanged;
        public UnityEvent<Color> OnColorConfirmed;

        private Texture2D _svTexture;
        private Texture2D _hueTexture;

        private float _hue;
        private float _saturation;
        private float _value;
        private float _alpha = 1f;

        private Color _currentColor = Color.black;
        private Color _previousColor = Color.black;
        private System.Collections.Generic.List<Color> _colorHistory = new System.Collections.Generic.List<Color>();

        // Public accessors
        public Color CurrentColor => _currentColor;
        public Color PreviousColor => _previousColor;
        public float Hue => _hue;
        public float Saturation => _saturation;
        public float Value => _value;
        public float Alpha => _alpha;

        private void Awake()
        {
            GenerateTextures();
            SetupEventListeners();
        }

        private void Start()
        {
            SetColor(Color.white);
        }

        /// <summary>
        /// Generate the SV square and Hue bar textures.
        /// </summary>
        private void GenerateTextures()
        {
            // Generate SV square texture (256x256)
            _svTexture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            _svTexture.filterMode = FilterMode.Bilinear;
            _svTexture.wrapMode = TextureWrapMode.Clamp;

            // Generate Hue bar texture
            _hueTexture = new Texture2D(1, 256, TextureFormat.RGBA32, false);
            _hueTexture.filterMode = FilterMode.Bilinear;
            _hueTexture.wrapMode = TextureWrapMode.Clamp;

            // Fill hue texture
            for (int y = 0; y < 256; y++)
            {
                float h = y / 255f;
                Color hueColor = Color.HSVToRGB(h, 1f, 1f);
                _hueTexture.SetPixel(0, y, hueColor);
            }
            _hueTexture.Apply();

            if (_hueBar != null)
            {
                _hueBar.texture = _hueTexture;
            }

            UpdateSVTexture();
        }

        /// <summary>
        /// Update the SV square texture based on current hue.
        /// </summary>
        private void UpdateSVTexture()
        {
            for (int y = 0; y < 256; y++)
            {
                for (int x = 0; x < 256; x++)
                {
                    float s = x / 255f;
                    float v = y / 255f;
                    Color color = Color.HSVToRGB(_hue, s, v);
                    _svTexture.SetPixel(x, y, color);
                }
            }
            _svTexture.Apply();

            if (_svSquare != null)
            {
                _svSquare.texture = _svTexture;
            }
        }

        /// <summary>
        /// Set up event listeners for UI elements.
        /// </summary>
        private void SetupEventListeners()
        {
            // Alpha slider
            if (_alphaSlider != null)
            {
                _alphaSlider.onValueChanged.AddListener(OnAlphaChanged);
            }

            // RGB sliders
            if (_rSlider != null) _rSlider.onValueChanged.AddListener(_ => OnRGBSliderChanged());
            if (_gSlider != null) _gSlider.onValueChanged.AddListener(_ => OnRGBSliderChanged());
            if (_bSlider != null) _bSlider.onValueChanged.AddListener(_ => OnRGBSliderChanged());

            // Hex input
            if (_hexInput != null)
            {
                _hexInput.onEndEdit.AddListener(OnHexInput);
            }

            // Add drag handlers to SV square
            if (_svSquare != null)
            {
                AddDragHandler(_svSquare.gameObject, OnSVDrag);
            }

            // Add drag handlers to Hue bar
            if (_hueBar != null)
            {
                AddDragHandler(_hueBar.gameObject, OnHueDrag);
            }
        }

        /// <summary>
        /// Add drag handlers to a UI element.
        /// </summary>
        private void AddDragHandler(GameObject obj, System.Action<PointerEventData> onDrag)
        {
            EventTrigger trigger = obj.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = obj.AddComponent<EventTrigger>();
            }

            // Pointer down
            var pointerDownEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            pointerDownEntry.callback.AddListener((data) => onDrag((PointerEventData)data));
            trigger.triggers.Add(pointerDownEntry);

            // Drag
            var dragEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            dragEntry.callback.AddListener((data) => onDrag((PointerEventData)data));
            trigger.triggers.Add(dragEntry);
        }

        /// <summary>
        /// Handle SV square drag.
        /// </summary>
        private void OnSVDrag(PointerEventData eventData)
        {
            if (_svSquare == null) return;

            RectTransform rectTransform = _svSquare.rectTransform;
            Vector2 localPoint;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                // Normalize to 0-1
                float normalizedX = (localPoint.x / rectTransform.rect.width) + 0.5f;
                float normalizedY = (localPoint.y / rectTransform.rect.height) + 0.5f;

                _saturation = Mathf.Clamp01(normalizedX);
                _value = Mathf.Clamp01(normalizedY);

                UpdateColorFromHSV();
                UpdateCursors();
            }
        }

        /// <summary>
        /// Handle Hue bar drag.
        /// </summary>
        private void OnHueDrag(PointerEventData eventData)
        {
            if (_hueBar == null) return;

            RectTransform rectTransform = _hueBar.rectTransform;
            Vector2 localPoint;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                float normalizedY = (localPoint.y / rectTransform.rect.height) + 0.5f;
                _hue = Mathf.Clamp01(normalizedY);

                UpdateSVTexture();
                UpdateColorFromHSV();
                UpdateCursors();
            }
        }

        /// <summary>
        /// Handle alpha slider change.
        /// </summary>
        private void OnAlphaChanged(float value)
        {
            _alpha = value;
            UpdateColorFromHSV();
        }

        /// <summary>
        /// Handle RGB slider changes.
        /// </summary>
        private void OnRGBSliderChanged()
        {
            if (_rSlider == null || _gSlider == null || _bSlider == null) return;

            Color newColor = new Color(_rSlider.value, _gSlider.value, _bSlider.value, _alpha);
            SetColorInternal(newColor, updateSliders: false);
        }

        /// <summary>
        /// Handle hex input.
        /// </summary>
        private void OnHexInput(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return;

            // Remove # if present
            if (hex.StartsWith("#"))
            {
                hex = hex.Substring(1);
            }

            Color newColor;
            if (ColorUtility.TryParseHtmlString("#" + hex, out newColor))
            {
                newColor.a = _alpha;
                SetColorInternal(newColor, updateHex: false);
            }
        }

        /// <summary>
        /// Update color from current HSV values.
        /// </summary>
        private void UpdateColorFromHSV()
        {
            _currentColor = Color.HSVToRGB(_hue, _saturation, _value);
            _currentColor.a = _alpha;

            UpdateUI();
            OnColorChanged?.Invoke(_currentColor);
        }

        /// <summary>
        /// Set color internally with control over which UI elements to update.
        /// </summary>
        private void SetColorInternal(Color color, bool updateSliders = true, bool updateHex = true)
        {
            _currentColor = color;
            _alpha = color.a;

            Color.RGBToHSV(color, out _hue, out _saturation, out _value);

            UpdateSVTexture();
            UpdateCursors();

            if (updateSliders) UpdateRGBSliders();
            if (updateHex) UpdateHexInput();

            UpdateColorPreview();
            UpdateAlphaSlider();

            OnColorChanged?.Invoke(_currentColor);
        }

        /// <summary>
        /// Set the current color.
        /// </summary>
        public void SetColor(Color color)
        {
            _previousColor = _currentColor;
            SetColorInternal(color);

            if (_previousColorPreview != null)
            {
                _previousColorPreview.color = _previousColor;
            }
        }

        /// <summary>
        /// Confirm the current color selection.
        /// </summary>
        public void ConfirmColor()
        {
            AddToHistory(_currentColor);
            OnColorConfirmed?.Invoke(_currentColor);
        }

        /// <summary>
        /// Revert to previous color.
        /// </summary>
        public void RevertToPrevious()
        {
            SetColor(_previousColor);
        }

        /// <summary>
        /// Update all UI elements.
        /// </summary>
        private void UpdateUI()
        {
            UpdateColorPreview();
            UpdateCursors();
            UpdateRGBSliders();
            UpdateAlphaSlider();
            UpdateHexInput();
        }

        /// <summary>
        /// Update color preview images.
        /// </summary>
        private void UpdateColorPreview()
        {
            if (_currentColorPreview != null)
            {
                _currentColorPreview.color = _currentColor;
            }
        }

        /// <summary>
        /// Update cursor positions.
        /// </summary>
        private void UpdateCursors()
        {
            // SV cursor
            if (_svCursor != null && _svSquare != null)
            {
                RectTransform svRect = _svSquare.rectTransform;
                float x = (_saturation - 0.5f) * svRect.rect.width;
                float y = (_value - 0.5f) * svRect.rect.height;
                _svCursor.anchoredPosition = new Vector2(x, y);
            }

            // Hue cursor
            if (_hueCursor != null && _hueBar != null)
            {
                RectTransform hueRect = _hueBar.rectTransform;
                float y = (_hue - 0.5f) * hueRect.rect.height;
                _hueCursor.anchoredPosition = new Vector2(0, y);
            }
        }

        /// <summary>
        /// Update RGB sliders.
        /// </summary>
        private void UpdateRGBSliders()
        {
            if (_rSlider != null) _rSlider.SetValueWithoutNotify(_currentColor.r);
            if (_gSlider != null) _gSlider.SetValueWithoutNotify(_currentColor.g);
            if (_bSlider != null) _bSlider.SetValueWithoutNotify(_currentColor.b);
        }

        /// <summary>
        /// Update alpha slider.
        /// </summary>
        private void UpdateAlphaSlider()
        {
            if (_alphaSlider != null)
            {
                _alphaSlider.SetValueWithoutNotify(_alpha);
            }
        }

        /// <summary>
        /// Update hex input field.
        /// </summary>
        private void UpdateHexInput()
        {
            if (_hexInput != null)
            {
                _hexInput.SetTextWithoutNotify(ColorUtility.ToHtmlStringRGB(_currentColor));
            }
        }

        /// <summary>
        /// Add color to history.
        /// </summary>
        public void AddToHistory(Color color)
        {
            // Remove if already exists
            _colorHistory.Remove(color);

            // Add to front
            _colorHistory.Insert(0, color);

            // Trim to max
            while (_colorHistory.Count > _maxHistoryColors)
            {
                _colorHistory.RemoveAt(_colorHistory.Count - 1);
            }

            UpdateHistoryUI();
        }

        /// <summary>
        /// Update history UI.
        /// </summary>
        private void UpdateHistoryUI()
        {
            if (_historyContainer == null || _historyColorPrefab == null) return;

            // Clear existing
            foreach (Transform child in _historyContainer)
            {
                Destroy(child.gameObject);
            }

            // Create new
            foreach (Color historyColor in _colorHistory)
            {
                GameObject colorObj = Instantiate(_historyColorPrefab, _historyContainer);
                Image image = colorObj.GetComponent<Image>();
                if (image != null)
                {
                    image.color = historyColor;
                }

                Button button = colorObj.GetComponent<Button>();
                if (button != null)
                {
                    Color capturedColor = historyColor;
                    button.onClick.AddListener(() => SetColor(capturedColor));
                }
            }
        }

        /// <summary>
        /// Set color from HSV values.
        /// </summary>
        public void SetHSV(float h, float s, float v)
        {
            _hue = Mathf.Clamp01(h);
            _saturation = Mathf.Clamp01(s);
            _value = Mathf.Clamp01(v);

            UpdateSVTexture();
            UpdateColorFromHSV();
            UpdateCursors();
        }

        /// <summary>
        /// Set only the alpha value.
        /// </summary>
        public void SetAlpha(float alpha)
        {
            _alpha = Mathf.Clamp01(alpha);
            UpdateColorFromHSV();
            UpdateAlphaSlider();
        }

        private void OnDestroy()
        {
            if (_svTexture != null) Destroy(_svTexture);
            if (_hueTexture != null) Destroy(_hueTexture);
        }
    }
}
