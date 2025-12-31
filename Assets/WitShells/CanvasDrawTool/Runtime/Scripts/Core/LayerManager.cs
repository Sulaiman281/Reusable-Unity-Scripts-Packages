namespace WitShells.CanvasDrawTool
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.Events;

    /// <summary>
    /// Manages all layers as separate RawImage GameObjects for high performance on mobile.
    /// Each layer is a LayerObject with its own texture and transform capabilities.
    /// </summary>
    public class LayerManager : MonoBehaviour
    {
        [Header("Canvas Settings")]
        [SerializeField] private int _canvasWidth = 512;
        [SerializeField] private int _canvasHeight = 512;
        [SerializeField] private Color _backgroundColor = Color.white;

        [Header("Layer Container")]
        [SerializeField] private RectTransform _layerContainer;

        [Header("Events")]
        public UnityEvent<LayerObject> OnLayerCreated;
        public UnityEvent<LayerObject> OnLayerDeleted;
        public UnityEvent<int> OnActiveLayerChanged;
        public UnityEvent OnLayersReordered;
        public UnityEvent OnCanvasUpdated;

        private List<LayerObject> _layers = new List<LayerObject>();
        private int _activeLayerIndex = 0;
        private Texture2D _compositeTexture;
        private Color[] _compositePixels;

        public int CanvasWidth => _canvasWidth;
        public int CanvasHeight => _canvasHeight;
        public IReadOnlyList<LayerObject> Layers => _layers;
        public int LayerCount => _layers.Count;
        public int ActiveLayerIndex => _activeLayerIndex;
        public LayerObject ActiveLayer => _layers.Count > 0 && _activeLayerIndex < _layers.Count ? _layers[_activeLayerIndex] : null;
        public Texture2D CompositeTexture => _compositeTexture;
        public RectTransform LayerContainer => _layerContainer;

        private void Awake()
        {
            // Don't auto-initialize in Awake - wait for explicit call
        }

        /// <summary>
        /// Initialize or resize the canvas.
        /// </summary>
        public void InitializeCanvas(int width, int height, bool clearLayers = true)
        {
            _canvasWidth = width;
            _canvasHeight = height;

            // Create layer container if needed
            if (_layerContainer == null)
            {
                CreateLayerContainer();
            }

            // Create composite texture for export
            if (_compositeTexture != null)
            {
                Destroy(_compositeTexture);
            }

            _compositeTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            _compositePixels = new Color[width * height];

            if (clearLayers)
            {
                ClearAllLayers();
                // Create background layer
                CreateLayer("Background", _backgroundColor);
            }
        }

        /// <summary>
        /// Create the layer container if not already created.
        /// </summary>
        private void CreateLayerContainer()
        {
            GameObject containerObj = new GameObject("LayerContainer");
            containerObj.transform.SetParent(transform, false);

            _layerContainer = containerObj.AddComponent<RectTransform>();
            _layerContainer.anchorMin = Vector2.zero;
            _layerContainer.anchorMax = Vector2.one;
            _layerContainer.offsetMin = Vector2.zero;
            _layerContainer.offsetMax = Vector2.zero;
            _layerContainer.pivot = new Vector2(0.5f, 0.5f);
        }

        /// <summary>
        /// Set the layer container reference.
        /// </summary>
        public void SetLayerContainer(RectTransform container)
        {
            _layerContainer = container;
        }

        /// <summary>
        /// Create a new layer at the top.
        /// </summary>
        public LayerObject CreateLayer(string name = null, Color? fillColor = null)
        {
            if (_layerContainer == null)
            {
                CreateLayerContainer();
            }

            string layerName = name ?? $"Layer {_layers.Count + 1}";

            GameObject layerObj = new GameObject(layerName);
            layerObj.transform.SetParent(_layerContainer, false);

            LayerObject layer = layerObj.AddComponent<LayerObject>();
            layer.Initialize(_canvasWidth, _canvasHeight, fillColor);
            layer.LayerName = layerName;

            // Set layer RectTransform to fill container
            RectTransform rt = layer.RectTransform;
            rt.pivot = new Vector2(0.5f, 0.5f);

            // Set layer to fill container exactly (anchors at corners, no offset)
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _layers.Add(layer);
            _activeLayerIndex = _layers.Count - 1;

            // Update sibling order (lower index = behind)
            UpdateLayerOrder();

            OnLayerCreated?.Invoke(layer);
            OnActiveLayerChanged?.Invoke(_activeLayerIndex);

            return layer;
        }

        /// <summary>
        /// Create a new layer from an imported image as a separate RawImage.
        /// </summary>
        public LayerObject CreateLayerFromImage(Texture2D image, string name = null)
        {
            if (_layerContainer == null)
            {
                CreateLayerContainer();
            }

            string layerName = name ?? image.name ?? $"Image Layer {_layers.Count + 1}";

            GameObject layerObj = new GameObject(layerName);
            layerObj.transform.SetParent(_layerContainer, false);

            LayerObject layer = layerObj.AddComponent<LayerObject>();
            layer.InitializeFromTexture(image, true);
            layer.LayerName = layerName;

            // Position in center
            RectTransform rt = layer.RectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            _layers.Add(layer);
            _activeLayerIndex = _layers.Count - 1;

            UpdateLayerOrder();

            OnLayerCreated?.Invoke(layer);
            OnActiveLayerChanged?.Invoke(_activeLayerIndex);

            return layer;
        }

        /// <summary>
        /// Delete a layer by index.
        /// </summary>
        public void DeleteLayer(int index)
        {
            if (index < 0 || index >= _layers.Count) return;
            if (_layers.Count <= 1) return; // Keep at least one layer

            var layer = _layers[index];
            _layers.RemoveAt(index);

            OnLayerDeleted?.Invoke(layer);

            layer.Dispose();

            // Adjust active layer index
            if (_activeLayerIndex >= _layers.Count)
            {
                _activeLayerIndex = _layers.Count - 1;
            }
            else if (_activeLayerIndex > index)
            {
                _activeLayerIndex--;
            }

            UpdateLayerOrder();
            OnActiveLayerChanged?.Invoke(_activeLayerIndex);
        }

        /// <summary>
        /// Delete the currently active layer.
        /// </summary>
        public void DeleteActiveLayer()
        {
            DeleteLayer(_activeLayerIndex);
        }

        /// <summary>
        /// Set the active layer by index.
        /// </summary>
        public void SetActiveLayer(int index)
        {
            if (index < 0 || index >= _layers.Count) return;
            _activeLayerIndex = index;
            OnActiveLayerChanged?.Invoke(_activeLayerIndex);
        }

        /// <summary>
        /// Move a layer up in the stack (towards front).
        /// </summary>
        public void MoveLayerUp(int index)
        {
            if (index < 0 || index >= _layers.Count - 1) return;

            var temp = _layers[index];
            _layers[index] = _layers[index + 1];
            _layers[index + 1] = temp;

            if (_activeLayerIndex == index)
                _activeLayerIndex = index + 1;
            else if (_activeLayerIndex == index + 1)
                _activeLayerIndex = index;

            UpdateLayerOrder();
            OnLayersReordered?.Invoke();
        }

        /// <summary>
        /// Move a layer down in the stack (towards back).
        /// </summary>
        public void MoveLayerDown(int index)
        {
            if (index <= 0 || index >= _layers.Count) return;

            var temp = _layers[index];
            _layers[index] = _layers[index - 1];
            _layers[index - 1] = temp;

            if (_activeLayerIndex == index)
                _activeLayerIndex = index - 1;
            else if (_activeLayerIndex == index - 1)
                _activeLayerIndex = index;

            UpdateLayerOrder();
            OnLayersReordered?.Invoke();
        }

        /// <summary>
        /// Update the sibling order of all layers to match list order.
        /// </summary>
        private void UpdateLayerOrder()
        {
            for (int i = 0; i < _layers.Count; i++)
            {
                if (_layers[i] != null && _layers[i].transform != null)
                {
                    _layers[i].transform.SetSiblingIndex(i);
                }
            }
        }

        /// <summary>
        /// Duplicate a layer.
        /// </summary>
        public LayerObject DuplicateLayer(int index)
        {
            if (index < 0 || index >= _layers.Count) return null;

            var source = _layers[index];
            var duplicate = source.Duplicate(_layerContainer);

            _layers.Insert(index + 1, duplicate);
            _activeLayerIndex = index + 1;

            UpdateLayerOrder();

            OnLayerCreated?.Invoke(duplicate);
            OnActiveLayerChanged?.Invoke(_activeLayerIndex);

            return duplicate;
        }

        /// <summary>
        /// Merge a layer with the one below it.
        /// </summary>
        public void MergeDown(int index)
        {
            if (index <= 0 || index >= _layers.Count) return;

            var topLayer = _layers[index];
            var bottomLayer = _layers[index - 1];

            if (bottomLayer.IsLocked) return;

            // Merge top into bottom (pixel by pixel)
            Color[] topPixels = topLayer.GetPixels();
            int width = Mathf.Min(topLayer.TextureWidth, bottomLayer.TextureWidth);
            int height = Mathf.Min(topLayer.TextureHeight, bottomLayer.TextureHeight);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color topColor = topPixels[y * topLayer.TextureWidth + x];
                    if (topColor.a > 0)
                    {
                        bottomLayer.DrawPixel(x, y, topColor, topLayer.Opacity);
                    }
                }
            }
            bottomLayer.ApplyChanges();

            // Remove top layer
            _layers.RemoveAt(index);
            OnLayerDeleted?.Invoke(topLayer);
            topLayer.Dispose();

            if (_activeLayerIndex >= index)
            {
                _activeLayerIndex = Mathf.Max(0, _activeLayerIndex - 1);
            }

            UpdateLayerOrder();
            OnActiveLayerChanged?.Invoke(_activeLayerIndex);
        }

        /// <summary>
        /// Merge all visible layers into one.
        /// </summary>
        public LayerObject MergeVisible()
        {
            // Get all visible layers
            var visibleLayers = new List<LayerObject>();
            foreach (var layer in _layers)
            {
                if (layer.IsVisible)
                {
                    visibleLayers.Add(layer);
                }
            }

            if (visibleLayers.Count <= 1) return null;

            // Create composite of visible layers
            UpdateComposite();

            // Create new merged layer
            var mergedLayer = CreateLayer("Merged", Color.clear);
            mergedLayer.SetPixels(_compositePixels);
            mergedLayer.ApplyChanges();

            // Remove old visible layers (except the merged one we just created)
            for (int i = _layers.Count - 1; i >= 0; i--)
            {
                if (_layers[i] != mergedLayer && visibleLayers.Contains(_layers[i]))
                {
                    var layer = _layers[i];
                    _layers.RemoveAt(i);
                    OnLayerDeleted?.Invoke(layer);
                    layer.Dispose();
                }
            }

            _activeLayerIndex = _layers.Count - 1;
            UpdateLayerOrder();
            OnActiveLayerChanged?.Invoke(_activeLayerIndex);

            return mergedLayer;
        }

        /// <summary>
        /// Flatten all visible layers into one.
        /// </summary>
        public LayerObject Flatten()
        {
            // Create a new layer with the composite
            UpdateComposite();

            // Store old layers
            var oldLayers = new List<LayerObject>(_layers);

            // Clear list (don't dispose yet)
            _layers.Clear();

            // Create new flattened layer
            var mergedLayer = CreateLayer("Flattened", Color.clear);
            mergedLayer.SetPixels(_compositePixels);
            mergedLayer.ApplyChanges();

            // Now dispose old layers
            foreach (var layer in oldLayers)
            {
                layer.Dispose();
            }

            _activeLayerIndex = 0;
            UpdateLayerOrder();

            OnActiveLayerChanged?.Invoke(_activeLayerIndex);

            return mergedLayer;
        }

        /// <summary>
        /// Clear all layers.
        /// </summary>
        public void ClearAllLayers()
        {
            foreach (var layer in _layers)
            {
                if (layer != null)
                {
                    layer.Dispose();
                }
            }
            _layers.Clear();
            _activeLayerIndex = 0;
        }

        /// <summary>
        /// Update the composite texture from all layers.
        /// WARNING: This is expensive! Only call when needed (export, layer visibility change, etc.)
        /// During drawing, the individual layer RawImages display directly - no composite needed.
        /// </summary>
        public void UpdateComposite()
        {
            if (_compositePixels == null || _compositePixels.Length != _canvasWidth * _canvasHeight)
            {
                _compositePixels = new Color[_canvasWidth * _canvasHeight];
            }

            // Start with background color
            Color bg = _backgroundColor;
            for (int i = 0; i < _compositePixels.Length; i++)
            {
                _compositePixels[i] = bg;
            }

            // Blend each visible layer (layers at lower indices are behind)
            foreach (var layer in _layers)
            {
                if (layer == null || !layer.IsVisible) continue;

                layer.ApplyChanges();
                Color[] layerPixels = layer.GetPixelsReadOnly();  // Use read-only for performance

                int width = Mathf.Min(_canvasWidth, layer.TextureWidth);
                int height = Mathf.Min(_canvasHeight, layer.TextureHeight);
                float layerOpacity = layer.Opacity;

                for (int y = 0; y < height; y++)
                {
                    int compositeRowStart = y * _canvasWidth;
                    int layerRowStart = y * layer.TextureWidth;

                    for (int x = 0; x < width; x++)
                    {
                        int compositeIndex = compositeRowStart + x;
                        int layerIndex = layerRowStart + x;

                        Color fgColor = layerPixels[layerIndex];
                        float alpha = fgColor.a * layerOpacity;

                        if (alpha > 0.001f)
                        {
                            Color bgColor = _compositePixels[compositeIndex];
                            _compositePixels[compositeIndex] = new Color(
                                bgColor.r + (fgColor.r - bgColor.r) * alpha,
                                bgColor.g + (fgColor.g - bgColor.g) * alpha,
                                bgColor.b + (fgColor.b - bgColor.b) * alpha,
                                Mathf.Min(1f, bgColor.a + alpha * (1f - bgColor.a))
                            );
                        }
                    }
                }
            }

            if (_compositeTexture != null)
            {
                _compositeTexture.SetPixels(_compositePixels);
                _compositeTexture.Apply(false);  // false = don't rebuild mipmaps
            }

            OnCanvasUpdated?.Invoke();
        }

        /// <summary>
        /// Export the composite texture as PNG bytes.
        /// </summary>
        public byte[] ExportAsPNG()
        {
            UpdateComposite();
            return _compositeTexture.EncodeToPNG();
        }

        /// <summary>
        /// Export the composite texture as JPG bytes.
        /// </summary>
        public byte[] ExportAsJPG(int quality = 75)
        {
            UpdateComposite();
            return _compositeTexture.EncodeToJPG(quality);
        }

        /// <summary>
        /// Get a copy of the composite texture.
        /// </summary>
        public Texture2D GetCompositeCopy()
        {
            UpdateComposite();
            var copy = new Texture2D(_canvasWidth, _canvasHeight, TextureFormat.RGBA32, false);
            copy.SetPixels(_compositeTexture.GetPixels());
            copy.Apply();
            return copy;
        }

        /// <summary>
        /// Get layer by index.
        /// </summary>
        public LayerObject GetLayer(int index)
        {
            if (index < 0 || index >= _layers.Count) return null;
            return _layers[index];
        }

        /// <summary>
        /// Find layer index by reference.
        /// </summary>
        public int GetLayerIndex(LayerObject layer)
        {
            return _layers.IndexOf(layer);
        }

        #region Layer Transform Operations

        /// <summary>
        /// Move the active layer by delta.
        /// </summary>
        public void MoveActiveLayer(Vector2 delta)
        {
            ActiveLayer?.Move(delta);
        }

        /// <summary>
        /// Scale the active layer.
        /// </summary>
        public void ScaleActiveLayer(Vector2 scale)
        {
            ActiveLayer?.SetScale(scale);
        }

        /// <summary>
        /// Rotate the active layer.
        /// </summary>
        public void RotateActiveLayer(float degrees)
        {
            ActiveLayer?.SetRotation(degrees);
        }

        /// <summary>
        /// Set crop on active layer.
        /// </summary>
        public void CropActiveLayer(RectInt rect)
        {
            ActiveLayer?.SetCrop(rect);
        }

        /// <summary>
        /// Reset crop on active layer.
        /// </summary>
        public void ResetActiveLayerCrop()
        {
            ActiveLayer?.ResetCrop();
        }

        /// <summary>
        /// Resize active layer.
        /// </summary>
        public void ResizeActiveLayer(int width, int height)
        {
            ActiveLayer?.Resize(width, height);
        }

        #endregion

        private void OnDestroy()
        {
            ClearAllLayers();
            if (_compositeTexture != null)
            {
                Destroy(_compositeTexture);
            }
        }
    }
}
