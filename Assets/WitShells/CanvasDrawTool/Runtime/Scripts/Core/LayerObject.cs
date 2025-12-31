namespace WitShells.CanvasDrawTool
{
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.Events;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Represents a single layer as a RawImage GameObject for high performance on mobile.
    /// Uses FastTextureBuffer for optimized pixel operations.
    /// Each layer can be independently positioned, scaled, rotated and cropped.
    /// </summary>
    public class LayerObject : MonoBehaviour
    {
        [Header("Layer Settings")]
        [SerializeField] private string _layerName = "Layer";
        [SerializeField] private bool _isVisible = true;
        [SerializeField] private bool _isLocked = false;
        [SerializeField] private float _opacity = 1f;
        [SerializeField] private BlendMode _blendMode = BlendMode.Normal;

        [Header("Transform")]
        [SerializeField] private Vector2 _position = Vector2.zero;
        [SerializeField] private Vector2 _scale = Vector2.one;
        [SerializeField] private float _rotation = 0f;
        [SerializeField] private Vector2 _pivot = new Vector2(0.5f, 0.5f);

        [Header("Crop")]
        [SerializeField] private bool _isCropped = false;
        [SerializeField] private RectInt _cropRect;

        [Header("References")]
        [SerializeField] private RawImage _rawImage;
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private CanvasGroup _canvasGroup;

        private Texture2D _texture;
        private FastTextureBuffer _buffer;  // High-performance byte buffer
        private Color[] _pixels;  // Keep for backward compatibility
        private int _textureWidth;
        private int _textureHeight;
        private bool _isDirty;

        // Events
        public UnityEvent OnLayerChanged;
        public UnityEvent OnTransformChanged;
        public UnityEvent OnVisibilityChanged;

        public enum BlendMode
        {
            Normal,
            Multiply,
            Screen,
            Overlay,
            Add,
            Subtract
        }

        // Properties
        public string LayerName
        {
            get => _layerName;
            set { _layerName = value; gameObject.name = value; OnLayerChanged?.Invoke(); }
        }

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                if (_rawImage) _rawImage.enabled = value;
                OnVisibilityChanged?.Invoke();
            }
        }

        public bool IsLocked
        {
            get => _isLocked;
            set { _isLocked = value; OnLayerChanged?.Invoke(); }
        }

        public float Opacity
        {
            get => _opacity;
            set
            {
                _opacity = Mathf.Clamp01(value);
                if (_canvasGroup) _canvasGroup.alpha = _opacity;
                else if (_rawImage)
                {
                    Color c = _rawImage.color;
                    c.a = _opacity;
                    _rawImage.color = c;
                }
                OnLayerChanged?.Invoke();
            }
        }

        public BlendMode Blend
        {
            get => _blendMode;
            set { _blendMode = value; OnLayerChanged?.Invoke(); }
        }

        public Vector2 Position
        {
            get => _position;
            set { _position = value; ApplyTransform(); }
        }

        public Vector2 Scale
        {
            get => _scale;
            set { _scale = value; ApplyTransform(); }
        }

        public float Rotation
        {
            get => _rotation;
            set { _rotation = value; ApplyTransform(); }
        }

        public Vector2 Pivot
        {
            get => _pivot;
            set { _pivot = value; ApplyTransform(); }
        }

        public Texture2D Texture => _texture;
        public RawImage RawImage => _rawImage;
        public RectTransform RectTransform => _rectTransform;
        public int TextureWidth => _textureWidth;
        public int TextureHeight => _textureHeight;
        public bool IsDirty => _isDirty || (_buffer != null && _buffer.IsDirty);
        public bool IsCropped => _isCropped;
        public RectInt CropRect => _cropRect;
        public FastTextureBuffer Buffer => _buffer;  // Direct access for high-perf operations

        /// <summary>
        /// Initialize the layer with a new texture.
        /// </summary>
        public void Initialize(int width, int height, Color? fillColor = null)
        {
            _textureWidth = width;
            _textureHeight = height;

            // Setup components
            SetupComponents();

            // Create texture
            CreateTexture(width, height, fillColor ?? Color.clear);

            // Set initial size
            _rectTransform.sizeDelta = new Vector2(width, height);
            ApplyTransform();
        }

        /// <summary>
        /// Initialize from an existing texture/image.
        /// </summary>
        public void InitializeFromTexture(Texture2D source, bool preserveSize = true)
        {
            SetupComponents();

            _textureWidth = source.width;
            _textureHeight = source.height;

            // Create our own texture copy
            _texture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            _pixels = source.GetPixels();
            _texture.SetPixels(_pixels);
            _texture.Apply();

            _rawImage.texture = _texture;

            if (preserveSize)
            {
                _rectTransform.sizeDelta = new Vector2(source.width, source.height);
            }

            ApplyTransform();
        }

        private void SetupComponents()
        {
            // Ensure RectTransform exists (required for UI)
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null)
                _rectTransform = gameObject.AddComponent<RectTransform>();

            // Ensure RawImage exists for rendering
            _rawImage = GetComponent<RawImage>();
            if (_rawImage == null)
                _rawImage = gameObject.AddComponent<RawImage>();

            // Ensure CanvasGroup exists for opacity control
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            _rawImage.raycastTarget = false; // Don't block raycasts
            
            // Only set alpha if canvasGroup is valid
            if (_canvasGroup != null)
                _canvasGroup.alpha = _opacity;
        }

        private void CreateTexture(int width, int height, Color fillColor)
        {
            if (_texture != null)
            {
                Destroy(_texture);
            }

            _texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            // Create high-performance buffer
            Color32 fillColor32 = fillColor;
            _buffer = new FastTextureBuffer(width, height, fillColor32);

            // Keep Color[] for backward compatibility
            _pixels = new Color[width * height];
            for (int i = 0; i < _pixels.Length; i++)
            {
                _pixels[i] = fillColor;
            }

            // Apply buffer to texture
            _buffer.ApplyToTexture(_texture);

            _rawImage.texture = _texture;
        }

        /// <summary>
        /// Apply position, scale, rotation transforms.
        /// </summary>
        public void ApplyTransform()
        {
            if (_rectTransform == null) return;

            _rectTransform.anchoredPosition = _position;
            _rectTransform.localScale = new Vector3(_scale.x, _scale.y, 1f);
            _rectTransform.localRotation = Quaternion.Euler(0, 0, _rotation);
            _rectTransform.pivot = _pivot;

            OnTransformChanged?.Invoke();
        }

        /// <summary>
        /// Move the layer by delta.
        /// </summary>
        public void Move(Vector2 delta)
        {
            Position += delta;
        }

        /// <summary>
        /// Set absolute position.
        /// </summary>
        public void SetPosition(Vector2 position)
        {
            Position = position;
        }

        /// <summary>
        /// Scale the layer uniformly.
        /// </summary>
        public void SetScale(float scale)
        {
            Scale = new Vector2(scale, scale);
        }

        /// <summary>
        /// Scale the layer non-uniformly.
        /// </summary>
        public void SetScale(Vector2 scale)
        {
            Scale = scale;
        }

        /// <summary>
        /// Rotate the layer.
        /// </summary>
        public void SetRotation(float degrees)
        {
            Rotation = degrees;
        }

        /// <summary>
        /// Set crop rectangle.
        /// </summary>
        public void SetCrop(RectInt rect)
        {
            _cropRect = rect;
            _isCropped = true;

            // Apply UV crop via RawImage uvRect
            if (_rawImage != null && _textureWidth > 0 && _textureHeight > 0)
            {
                float x = (float)rect.x / _textureWidth;
                float y = (float)rect.y / _textureHeight;
                float w = (float)rect.width / _textureWidth;
                float h = (float)rect.height / _textureHeight;
                _rawImage.uvRect = new Rect(x, y, w, h);

                // Adjust display size
                _rectTransform.sizeDelta = new Vector2(rect.width * _scale.x, rect.height * _scale.y);
            }

            OnLayerChanged?.Invoke();
        }

        /// <summary>
        /// Reset crop to full texture.
        /// </summary>
        public void ResetCrop()
        {
            _isCropped = false;
            _cropRect = new RectInt(0, 0, _textureWidth, _textureHeight);

            if (_rawImage != null)
            {
                _rawImage.uvRect = new Rect(0, 0, 1, 1);
                _rectTransform.sizeDelta = new Vector2(_textureWidth * _scale.x, _textureHeight * _scale.y);
            }

            OnLayerChanged?.Invoke();
        }

        /// <summary>
        /// Resize the layer texture (destructive - resamples content).
        /// </summary>
        public void Resize(int newWidth, int newHeight)
        {
            if (newWidth <= 0 || newHeight <= 0) return;

            Texture2D resized = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear
            };

            // Simple bilinear resize
            Color[] newPixels = new Color[newWidth * newHeight];
            for (int y = 0; y < newHeight; y++)
            {
                for (int x = 0; x < newWidth; x++)
                {
                    float u = (float)x / (newWidth - 1);
                    float v = (float)y / (newHeight - 1);
                    newPixels[y * newWidth + x] = _texture.GetPixelBilinear(u, v);
                }
            }

            resized.SetPixels(newPixels);
            resized.Apply();

            // Replace old texture
            Destroy(_texture);
            _texture = resized;
            _pixels = newPixels;
            _textureWidth = newWidth;
            _textureHeight = newHeight;

            _rawImage.texture = _texture;
            _rectTransform.sizeDelta = new Vector2(newWidth, newHeight);

            ResetCrop();
        }

        #region Pixel Operations

        /// <summary>
        /// Set a pixel color (uses fast buffer).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPixel(int x, int y, Color color)
        {
            if (_isLocked) return;
            if (x < 0 || x >= _textureWidth || y < 0 || y >= _textureHeight) return;

            // Use fast buffer
            if (_buffer != null)
            {
                _buffer.SetPixel(x, y, color);
            }
            
            // Keep Color[] in sync for compatibility
            int index = y * _textureWidth + x;
            _pixels[index] = color;
            _isDirty = true;
        }

        /// <summary>
        /// Get pixel color (uses fast buffer).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color GetPixel(int x, int y)
        {
            if (x < 0 || x >= _textureWidth || y < 0 || y >= _textureHeight) return Color.clear;
            
            // Use fast buffer if available
            if (_buffer != null)
            {
                Color32 c32 = _buffer.GetPixel(x, y);
                return new Color(c32.r / 255f, c32.g / 255f, c32.b / 255f, c32.a / 255f);
            }
            
            return _pixels[y * _textureWidth + x];
        }

        /// <summary>
        /// Draw a pixel with blending (optimized).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawPixel(int x, int y, Color brushColor, float brushOpacity)
        {
            if (_isLocked) return;
            if (x < 0 || x >= _textureWidth || y < 0 || y >= _textureHeight) return;

            // Use fast buffer for best performance
            if (_buffer != null)
            {
                _buffer.DrawPixelBlend(x, y, (Color32)brushColor, brushOpacity);
                _isDirty = true;
                return;
            }

            // Fallback to Color array
            Color existingColor = GetPixel(x, y);
            Color blendedColor = BlendColors(existingColor, brushColor, brushOpacity);
            SetPixel(x, y, blendedColor);
        }

        /// <summary>
        /// Erase a pixel (optimized).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ErasePixel(int x, int y, float eraseStrength)
        {
            if (_isLocked) return;
            if (x < 0 || x >= _textureWidth || y < 0 || y >= _textureHeight) return;

            // Use fast buffer for best performance
            if (_buffer != null)
            {
                _buffer.ErasePixel(x, y, eraseStrength);
                _isDirty = true;
                return;
            }

            // Fallback
            Color existingColor = GetPixel(x, y);
            existingColor.a = Mathf.Max(0, existingColor.a - eraseStrength);
            SetPixel(x, y, existingColor);
        }

        /// <summary>
        /// Draw brush circle (high-performance).
        /// </summary>
        public void DrawBrushCircle(int centerX, int centerY, int radius, Color32 color, float opacity, float hardness)
        {
            if (_isLocked || _buffer == null) return;
            _buffer.DrawCircle(centerX, centerY, radius, color, opacity, hardness);
            _isDirty = true;
        }

        /// <summary>
        /// Erase brush circle (high-performance).
        /// </summary>
        public void EraseBrushCircle(int centerX, int centerY, int radius, float opacity, float hardness)
        {
            if (_isLocked || _buffer == null) return;
            _buffer.EraseCircle(centerX, centerY, radius, opacity, hardness);
            _isDirty = true;
        }

        /// <summary>
        /// Draw line between two points (high-performance using Bresenham).
        /// </summary>
        public void DrawBrushLine(int x0, int y0, int x1, int y1, Color32 color, int thickness, float opacity, float hardness)
        {
            if (_isLocked || _buffer == null) return;
            _buffer.DrawLine(x0, y0, x1, y1, color, thickness, opacity, hardness);
            _isDirty = true;
        }

        /// <summary>
        /// Erase line between two points (high-performance).
        /// </summary>
        public void EraseBrushLine(int x0, int y0, int x1, int y1, int thickness, float opacity, float hardness)
        {
            if (_isLocked || _buffer == null) return;
            _buffer.EraseLine(x0, y0, x1, y1, thickness, opacity, hardness);
            _isDirty = true;
        }

        /// <summary>
        /// Clear the layer with a color.
        /// </summary>
        public void Clear(Color color)
        {
            if (_isLocked) return;

            if (_buffer != null)
            {
                _buffer.Clear(color);
            }

            for (int i = 0; i < _pixels.Length; i++)
            {
                _pixels[i] = color;
            }
            _isDirty = true;
        }

        /// <summary>
        /// Apply pending pixel changes to texture (optimized).
        /// </summary>
        public void ApplyChanges()
        {
            if (_texture == null) return;

            // Use fast buffer apply (uses LoadRawTextureData)
            if (_buffer != null && _buffer.IsDirty)
            {
                _buffer.ApplyToTexture(_texture);
                _isDirty = false;
            }
            else if (_isDirty)
            {
                // Fallback to Color array
                _texture.SetPixels(_pixels);
                _texture.Apply(false);  // false = no mipmaps
                _isDirty = false;
            }
        }

        /// <summary>
        /// Get all pixels (creates copy - use sparingly).
        /// </summary>
        public Color[] GetPixels()
        {
            // Sync pixels from buffer if needed
            if (_buffer != null)
            {
                SyncPixelsFromBuffer();
            }
            return (Color[])_pixels.Clone();
        }

        /// <summary>
        /// Get pixels without cloning (read-only reference - use carefully).
        /// </summary>
        public Color[] GetPixelsReadOnly()
        {
            if (_buffer != null)
            {
                SyncPixelsFromBuffer();
            }
            return _pixels;
        }

        private void SyncPixelsFromBuffer()
        {
            if (_buffer == null) return;
            
            byte[] data = _buffer.Data;
            int pixelCount = _textureWidth * _textureHeight;
            
            for (int i = 0; i < pixelCount; i++)
            {
                int byteIndex = i * 4;
                _pixels[i] = new Color(
                    data[byteIndex] / 255f,
                    data[byteIndex + 1] / 255f,
                    data[byteIndex + 2] / 255f,
                    data[byteIndex + 3] / 255f
                );
            }
        }

        /// <summary>
        /// Set all pixels.
        /// </summary>
        public void SetPixels(Color[] pixels)
        {
            if (_isLocked) return;
            if (pixels.Length != _pixels.Length) return;

            pixels.CopyTo(_pixels, 0);
            _isDirty = true;
        }

        private Color BlendColors(Color background, Color foreground, float opacity)
        {
            float alpha = foreground.a * opacity;
            if (alpha <= 0) return background;

            switch (_blendMode)
            {
                case BlendMode.Normal:
                    return new Color(
                        Mathf.Lerp(background.r, foreground.r, alpha),
                        Mathf.Lerp(background.g, foreground.g, alpha),
                        Mathf.Lerp(background.b, foreground.b, alpha),
                        Mathf.Clamp01(background.a + alpha * (1 - background.a))
                    );

                case BlendMode.Multiply:
                    return new Color(
                        Mathf.Lerp(background.r, background.r * foreground.r, alpha),
                        Mathf.Lerp(background.g, background.g * foreground.g, alpha),
                        Mathf.Lerp(background.b, background.b * foreground.b, alpha),
                        Mathf.Clamp01(background.a + alpha * (1 - background.a))
                    );

                case BlendMode.Screen:
                    return new Color(
                        Mathf.Lerp(background.r, 1 - (1 - background.r) * (1 - foreground.r), alpha),
                        Mathf.Lerp(background.g, 1 - (1 - background.g) * (1 - foreground.g), alpha),
                        Mathf.Lerp(background.b, 1 - (1 - background.b) * (1 - foreground.b), alpha),
                        Mathf.Clamp01(background.a + alpha * (1 - background.a))
                    );

                case BlendMode.Add:
                    return new Color(
                        Mathf.Clamp01(background.r + foreground.r * alpha),
                        Mathf.Clamp01(background.g + foreground.g * alpha),
                        Mathf.Clamp01(background.b + foreground.b * alpha),
                        Mathf.Clamp01(background.a + alpha * (1 - background.a))
                    );

                default:
                    return Color.Lerp(background, foreground, alpha);
            }
        }

        #endregion

        /// <summary>
        /// Import an image into this layer.
        /// </summary>
        public void ImportImage(Texture2D image, int offsetX = 0, int offsetY = 0)
        {
            if (_isLocked) return;

            Color[] imagePixels = image.GetPixels();
            int imgWidth = image.width;
            int imgHeight = image.height;

            for (int y = 0; y < imgHeight; y++)
            {
                for (int x = 0; x < imgWidth; x++)
                {
                    int destX = offsetX + x;
                    int destY = offsetY + y;

                    if (destX >= 0 && destX < _textureWidth && destY >= 0 && destY < _textureHeight)
                    {
                        Color srcColor = imagePixels[y * imgWidth + x];
                        if (srcColor.a > 0)
                        {
                            DrawPixel(destX, destY, srcColor, 1f);
                        }
                    }
                }
            }

            _isDirty = true;
        }

        /// <summary>
        /// Export this layer as PNG bytes.
        /// </summary>
        public byte[] ExportAsPNG()
        {
            ApplyChanges();
            return _texture.EncodeToPNG();
        }

        /// <summary>
        /// Export this layer as JPG bytes.
        /// </summary>
        public byte[] ExportAsJPG(int quality = 75)
        {
            ApplyChanges();
            return _texture.EncodeToJPG(quality);
        }

        /// <summary>
        /// Create a copy of this layer.
        /// </summary>
        public LayerObject Duplicate(Transform parent)
        {
            GameObject newObj = new GameObject($"{_layerName} Copy");
            newObj.transform.SetParent(parent, false);

            LayerObject copy = newObj.AddComponent<LayerObject>();
            copy.Initialize(_textureWidth, _textureHeight);
            copy.SetPixels(_pixels);
            copy.LayerName = $"{_layerName} Copy";
            copy.Opacity = _opacity;
            copy.Blend = _blendMode;
            copy.Position = _position;
            copy.Scale = _scale;
            copy.Rotation = _rotation;
            copy.ApplyChanges();

            return copy;
        }

        private void OnDestroy()
        {
            if (_texture != null)
            {
                Destroy(_texture);
                _texture = null;
            }
        }

        /// <summary>
        /// Dispose and cleanup.
        /// </summary>
        public void Dispose()
        {
            if (_texture != null)
            {
                Destroy(_texture);
                _texture = null;
            }
            Destroy(gameObject);
        }
    }
}
