namespace WitShells.CanvasDrawTool
{
    using UnityEngine;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Handles drawing operations on LayerObject.
    /// Uses DrawToolSettings ScriptableObject as single source of truth for brush settings.
    /// OPTIMIZED: Uses FastTextureBuffer for high-performance Android drawing.
    /// </summary>
    public class DrawingEngine : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LayerManager _layerManager;
        [SerializeField] private DrawToolSettings _settings;

        [Header("Tool Settings")]
        [SerializeField] private DrawTool _currentTool = DrawTool.Brush;

        private Vector2Int _lastDrawPosition;
        private bool _isDrawing;
        private bool _needsApply;  // Track if we need to apply changes

        public enum DrawTool
        {
            Brush,
            Eraser,
            Eyedropper,
            Fill,
            Line,
            Rectangle,
            Ellipse
        }

        public LayerManager LayerManager => _layerManager;
        public DrawToolSettings Settings => _settings;
        public DrawTool CurrentTool => _currentTool;

        // Convenience accessors from settings
        public Color PrimaryColor => _settings != null ? _settings.PrimaryColor : Color.black;
        public Color SecondaryColor => _settings != null ? _settings.SecondaryColor : Color.white;

        // Performance: Apply texture at fixed rate
        private float _lastApplyTime;
        private const float APPLY_INTERVAL = 0.033f;  // ~30fps max texture updates

        private void Awake()
        {
            if (_layerManager == null)
                _layerManager = GetComponent<LayerManager>();
        }

        private void Update()
        {
            // Performance: Apply pending changes at fixed rate during drawing
            // NOTE: We only call ApplyChanges(), NOT UpdateComposite() - that's too expensive
            if (_isDrawing && _needsApply && Time.time - _lastApplyTime >= APPLY_INTERVAL)
            {
                var layer = _layerManager?.ActiveLayer;
                if (layer != null)
                {
                    layer.ApplyChanges();
                    // DO NOT call UpdateComposite() here - it's extremely expensive!
                    // The RawImage already displays the layer texture directly.
                    _lastApplyTime = Time.time;
                }
            }
        }

        /// <summary>
        /// Set the settings ScriptableObject reference.
        /// </summary>
        public void SetSettings(DrawToolSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Refresh internal state from settings (called when settings change).
        /// </summary>
        public void RefreshFromSettings()
        {
            // Nothing to cache - we read directly from settings now
        }

        /// <summary>
        /// Set the current tool.
        /// </summary>
        public void SetTool(DrawTool tool)
        {
            _currentTool = tool;
        }

        /// <summary>
        /// Start drawing at a position.
        /// </summary>
        public void BeginDraw(Vector2 canvasPosition, float pressure = 1f)
        {
            _isDrawing = true;
            _lastDrawPosition = Vector2Int.RoundToInt(canvasPosition);
            _lastApplyTime = Time.time;
            _needsApply = false;

            var layer = _layerManager.ActiveLayer;
            if (layer == null || layer.IsLocked) return;

            switch (_currentTool)
            {
                case DrawTool.Brush:
                    DrawBrushStampFast(layer, _lastDrawPosition, pressure);
                    _needsApply = true;
                    break;

                case DrawTool.Eraser:
                    EraseBrushStampFast(layer, _lastDrawPosition, pressure);
                    _needsApply = true;
                    break;

                case DrawTool.Eyedropper:
                    PickColor(canvasPosition);
                    break;

                case DrawTool.Fill:
                    FloodFill(layer, _lastDrawPosition, PrimaryColor);
                    break;
            }
        }

        /// <summary>
        /// Continue drawing to a new position.
        /// </summary>
        public void ContinueDraw(Vector2 canvasPosition, float pressure = 1f)
        {
            if (!_isDrawing) return;

            var layer = _layerManager.ActiveLayer;
            if (layer == null || layer.IsLocked) return;

            Vector2Int currentPos = Vector2Int.RoundToInt(canvasPosition);

            // Skip if same position
            if (currentPos == _lastDrawPosition) return;

            switch (_currentTool)
            {
                case DrawTool.Brush:
                    DrawBrushLineFast(layer, _lastDrawPosition, currentPos, pressure);
                    _needsApply = true;
                    break;

                case DrawTool.Eraser:
                    EraseBrushLineFast(layer, _lastDrawPosition, currentPos, pressure);
                    _needsApply = true;
                    break;

                case DrawTool.Eyedropper:
                    PickColor(canvasPosition);
                    break;
            }

            _lastDrawPosition = currentPos;
        }

        /// <summary>
        /// End drawing.
        /// </summary>
        public void EndDraw(Vector2 canvasPosition, float pressure = 1f)
        {
            if (!_isDrawing) return;

            var layer = _layerManager.ActiveLayer;
            if (layer != null && _needsApply)
            {
                layer.ApplyChanges();
                // Only update composite for export/preview purposes, not during drawing
                // The individual layer's RawImage already shows the drawing
            }

            _isDrawing = false;
            _needsApply = false;
        }

        /// <summary>
        /// Draw a single brush stamp at a position (OPTIMIZED).
        /// Uses FastTextureBuffer for high-performance Android drawing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DrawBrushStampFast(LayerObject layer, Vector2Int position, float pressure)
        {
            if (_settings == null || layer.Buffer == null) return;

            int size = _settings.GetEffectiveSize(pressure);
            float opacity = _settings.GetEffectiveOpacity(pressure);
            float hardness = _settings.BrushHardness;
            Color32 brushColor = _settings.PrimaryColor;

            // Use optimized LayerObject method
            layer.DrawBrushCircle(position.x, position.y, size / 2, brushColor, opacity, hardness);
        }

        /// <summary>
        /// Erase with brush stamp at a position (OPTIMIZED).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EraseBrushStampFast(LayerObject layer, Vector2Int position, float pressure)
        {
            if (_settings == null || layer.Buffer == null) return;

            int size = _settings.GetEffectiveSize(pressure);
            float opacity = _settings.GetEffectiveOpacity(pressure);
            float hardness = _settings.BrushHardness;

            // Use optimized LayerObject method
            layer.EraseBrushCircle(position.x, position.y, size / 2, opacity, hardness);
        }

        /// <summary>
        /// Draw brush stamps along a line (OPTIMIZED - using Bresenham).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DrawBrushLineFast(LayerObject layer, Vector2Int start, Vector2Int end, float pressure)
        {
            if (_settings == null || layer.Buffer == null) return;

            int size = _settings.GetEffectiveSize(pressure);
            float opacity = _settings.GetEffectiveOpacity(pressure);
            float hardness = _settings.BrushHardness;
            Color32 brushColor = _settings.PrimaryColor;

            // Use optimized LayerObject method with Bresenham line
            layer.DrawBrushLine(start.x, start.y, end.x, end.y, brushColor, size, opacity, hardness);
        }

        /// <summary>
        /// Erase along a line (OPTIMIZED - using Bresenham).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EraseBrushLineFast(LayerObject layer, Vector2Int start, Vector2Int end, float pressure)
        {
            if (_settings == null || layer.Buffer == null) return;

            int size = _settings.GetEffectiveSize(pressure);
            float opacity = _settings.GetEffectiveOpacity(pressure);
            float hardness = _settings.BrushHardness;

            // Use optimized LayerObject method
            layer.EraseBrushLine(start.x, start.y, end.x, end.y, size, opacity, hardness);
        }

        // ===== LEGACY METHODS (kept for backward compatibility) =====

        /// <summary>
        /// Draw a single brush stamp at a position (legacy).
        /// </summary>
        private void DrawBrushStamp(LayerObject layer, Vector2 position, float pressure)
        {
            if (_settings == null) return;

            int size = _settings.GetEffectiveSize(pressure);
            float opacity = _settings.GetEffectiveOpacity(pressure);
            int radius = size / 2;
            if (radius < 1) radius = 1;

            int centerX = Mathf.RoundToInt(position.x);
            int centerY = Mathf.RoundToInt(position.y);

            Brush.BrushType brushType = _settings.BrushType;
            float hardness = _settings.BrushHardness;
            float jitter = _settings.BrushJitter;
            Color brushColor = _settings.PrimaryColor;

            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    int px = centerX + x;
                    int py = centerY + y;

                    float distance;
                    if (brushType == Brush.BrushType.Square)
                    {
                        distance = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) / (float)radius;
                    }
                    else
                    {
                        distance = Mathf.Sqrt(x * x + y * y) / radius;
                    }

                    float alpha = GetAlphaAtDistance(distance, brushType, hardness);
                    if (alpha > 0)
                    {
                        // Apply jitter
                        if (jitter > 0)
                        {
                            px += Mathf.RoundToInt(Random.Range(-1f, 1f) * jitter * size);
                            py += Mathf.RoundToInt(Random.Range(-1f, 1f) * jitter * size);
                        }

                        Color color = brushColor;
                        color.a = alpha;
                        layer.DrawPixel(px, py, color, opacity);
                    }
                }
            }

            layer.ApplyChanges();
        }

        /// <summary>
        /// Erase with brush stamp at a position (legacy).
        /// </summary>
        private void EraseBrushStamp(LayerObject layer, Vector2 position, float pressure)
        {
            if (_settings == null) return;

            int size = _settings.GetEffectiveSize(pressure);
            float opacity = _settings.GetEffectiveOpacity(pressure);
            int radius = size / 2;
            if (radius < 1) radius = 1;

            int centerX = Mathf.RoundToInt(position.x);
            int centerY = Mathf.RoundToInt(position.y);

            Brush.BrushType brushType = _settings.BrushType;
            float hardness = _settings.BrushHardness;

            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    int px = centerX + x;
                    int py = centerY + y;

                    float distance = Mathf.Sqrt(x * x + y * y) / radius;
                    float alpha = GetAlphaAtDistance(distance, brushType, hardness);

                    if (alpha > 0)
                    {
                        layer.ErasePixel(px, py, alpha * opacity);
                    }
                }
            }
        }

        /// <summary>
        /// Draw brush stamps along a line from start to end (legacy).
        /// </summary>
        private void DrawBrushLine(LayerObject layer, Vector2 start, Vector2 end, float pressure)
        {
            if (_settings == null) return;

            float spacing = _settings.BrushSpacing;
            int size = _settings.GetEffectiveSize(pressure);
            float step = Mathf.Max(1, size * spacing);
            float distance = Vector2.Distance(start, end);
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / step));

            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector2 pos = Vector2.Lerp(start, end, t);
                DrawBrushStamp(layer, pos, pressure);
            }
        }

        /// <summary>
        /// Erase along a line from start to end (legacy).
        /// </summary>
        private void EraseBrushLine(LayerObject layer, Vector2 start, Vector2 end, float pressure)
        {
            if (_settings == null) return;

            float spacing = _settings.BrushSpacing;
            int size = _settings.GetEffectiveSize(pressure);
            float step = Mathf.Max(1, size * spacing);
            float distance = Vector2.Distance(start, end);
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / step));

            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector2 pos = Vector2.Lerp(start, end, t);
                EraseBrushStamp(layer, pos, pressure);
            }
        }

        /// <summary>
        /// Get brush alpha at a given distance from center (0-1).
        /// </summary>
        private float GetAlphaAtDistance(float normalizedDistance, Brush.BrushType type, float hardness)
        {
            switch (type)
            {
                case Brush.BrushType.Round:
                case Brush.BrushType.Pencil:
                    if (hardness >= 1f)
                    {
                        return normalizedDistance <= 1f ? 1f : 0f;
                    }
                    else
                    {
                        float edge = 1f - hardness;
                        if (normalizedDistance <= 1f - edge)
                            return 1f;
                        else if (normalizedDistance <= 1f)
                            return 1f - (normalizedDistance - (1f - edge)) / edge;
                        else
                            return 0f;
                    }

                case Brush.BrushType.Soft:
                    return Mathf.Clamp01(1f - normalizedDistance) * hardness;

                case Brush.BrushType.Airbrush:
                    float falloff = 1f - normalizedDistance * normalizedDistance;
                    return Mathf.Clamp01(falloff * hardness * 0.5f);

                case Brush.BrushType.Square:
                    return normalizedDistance <= 1f ? 1f : 0f;

                case Brush.BrushType.Marker:
                    return normalizedDistance <= 0.8f ? 0.7f : (normalizedDistance <= 1f ? 0.3f : 0f);

                default:
                    return normalizedDistance <= 1f ? 1f : 0f;
            }
        }

        /// <summary>
        /// Pick color from position.
        /// </summary>
        private void PickColor(Vector2 position)
        {
            if (_layerManager == null || _settings == null) return;

            int x = Mathf.RoundToInt(position.x);
            int y = Mathf.RoundToInt(position.y);

            if (x >= 0 && x < _layerManager.CanvasWidth && y >= 0 && y < _layerManager.CanvasHeight)
            {
                Color pickedColor = _layerManager.CompositeTexture.GetPixel(x, y);
                _settings.PrimaryColor = pickedColor;
            }
        }

        /// <summary>
        /// Flood fill from a starting point.
        /// </summary>
        public void FloodFill(LayerObject layer, Vector2Int startPos, Color fillColor)
        {
            if (layer == null || layer.IsLocked) return;

            Texture2D texture = layer.Texture;
            if (texture == null) return;

            int width = texture.width;
            int height = texture.height;

            if (startPos.x < 0 || startPos.x >= width || startPos.y < 0 || startPos.y >= height)
                return;

            Color targetColor = texture.GetPixel(startPos.x, startPos.y);

            // Don't fill if same color
            if (ColorsMatch(targetColor, fillColor, 0.01f))
                return;

            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            queue.Enqueue(startPos);
            visited.Add(startPos);

            Color[] pixels = texture.GetPixels();

            while (queue.Count > 0)
            {
                Vector2Int pos = queue.Dequeue();

                int index = pos.y * width + pos.x;
                if (ColorsMatch(GetPixelColor(pixels, index), targetColor, 0.1f))
                {
                    pixels[index] = fillColor;

                    // Check neighbors
                    Vector2Int[] neighbors = new Vector2Int[]
                    {
                        new Vector2Int(pos.x + 1, pos.y),
                        new Vector2Int(pos.x - 1, pos.y),
                        new Vector2Int(pos.x, pos.y + 1),
                        new Vector2Int(pos.x, pos.y - 1)
                    };

                    foreach (var neighbor in neighbors)
                    {
                        if (neighbor.x >= 0 && neighbor.x < width &&
                            neighbor.y >= 0 && neighbor.y < height &&
                            !visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            layer.ApplyChanges();
            _layerManager.UpdateComposite();
        }

        private Color GetPixelColor(Color[] pixels, int index)
        {
            if (index >= 0 && index < pixels.Length)
                return pixels[index];
            return Color.clear;
        }

        private bool ColorsMatch(Color a, Color b, float tolerance)
        {
            return Mathf.Abs(a.r - b.r) <= tolerance &&
                   Mathf.Abs(a.g - b.g) <= tolerance &&
                   Mathf.Abs(a.b - b.b) <= tolerance &&
                   Mathf.Abs(a.a - b.a) <= tolerance;
        }

        /// <summary>
        /// Draw a line between two points.
        /// </summary>
        public void DrawLine(LayerObject layer, Vector2Int start, Vector2Int end, Color color, int thickness)
        {
            if (layer == null || layer.IsLocked) return;

            int dx = Mathf.Abs(end.x - start.x);
            int dy = Mathf.Abs(end.y - start.y);
            int sx = start.x < end.x ? 1 : -1;
            int sy = start.y < end.y ? 1 : -1;
            int err = dx - dy;

            int x = start.x;
            int y = start.y;

            while (true)
            {
                DrawThickPoint(layer, x, y, color, thickness);

                if (x == end.x && y == end.y) break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }

            layer.ApplyChanges();
            _layerManager.UpdateComposite();
        }

        /// <summary>
        /// Draw a rectangle.
        /// </summary>
        public void DrawRectangle(LayerObject layer, Vector2Int min, Vector2Int max, Color color, bool filled, int thickness)
        {
            if (layer == null || layer.IsLocked) return;

            if (filled)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    for (int x = min.x; x <= max.x; x++)
                    {
                        layer.DrawPixel(x, y, color, 1f);
                    }
                }
            }
            else
            {
                // Draw four lines
                DrawLine(layer, min, new Vector2Int(max.x, min.y), color, thickness);
                DrawLine(layer, new Vector2Int(max.x, min.y), max, color, thickness);
                DrawLine(layer, max, new Vector2Int(min.x, max.y), color, thickness);
                DrawLine(layer, new Vector2Int(min.x, max.y), min, color, thickness);
            }

            layer.ApplyChanges();
            _layerManager.UpdateComposite();
        }

        /// <summary>
        /// Draw an ellipse.
        /// </summary>
        public void DrawEllipse(LayerObject layer, Vector2Int center, int radiusX, int radiusY, Color color, bool filled)
        {
            if (layer == null || layer.IsLocked) return;

            if (filled)
            {
                for (int y = -radiusY; y <= radiusY; y++)
                {
                    for (int x = -radiusX; x <= radiusX; x++)
                    {
                        float normalizedX = (float)x / radiusX;
                        float normalizedY = (float)y / radiusY;
                        if (normalizedX * normalizedX + normalizedY * normalizedY <= 1f)
                        {
                            layer.DrawPixel(center.x + x, center.y + y, color, 1f);
                        }
                    }
                }
            }
            else
            {
                // Bresenham ellipse outline
                int x = 0;
                int y = radiusY;
                int rxSq = radiusX * radiusX;
                int rySq = radiusY * radiusY;
                int rxSq2 = 2 * rxSq;
                int rySq2 = 2 * rySq;
                int p;
                int px = 0;
                int py = rxSq2 * y;

                DrawEllipsePoints(layer, center, x, y, color);

                // Region 1
                p = Mathf.RoundToInt(rySq - (rxSq * radiusY) + (0.25f * rxSq));
                while (px < py)
                {
                    x++;
                    px += rySq2;
                    if (p < 0)
                    {
                        p += rySq + px;
                    }
                    else
                    {
                        y--;
                        py -= rxSq2;
                        p += rySq + px - py;
                    }
                    DrawEllipsePoints(layer, center, x, y, color);
                }

                // Region 2
                p = Mathf.RoundToInt(rySq * (x + 0.5f) * (x + 0.5f) + rxSq * (y - 1) * (y - 1) - rxSq * rySq);
                while (y > 0)
                {
                    y--;
                    py -= rxSq2;
                    if (p > 0)
                    {
                        p += rxSq - py;
                    }
                    else
                    {
                        x++;
                        px += rySq2;
                        p += rxSq - py + px;
                    }
                    DrawEllipsePoints(layer, center, x, y, color);
                }
            }

            layer.ApplyChanges();
            _layerManager.UpdateComposite();
        }

        private void DrawEllipsePoints(LayerObject layer, Vector2Int center, int x, int y, Color color)
        {
            layer.DrawPixel(center.x + x, center.y + y, color, 1f);
            layer.DrawPixel(center.x - x, center.y + y, color, 1f);
            layer.DrawPixel(center.x + x, center.y - y, color, 1f);
            layer.DrawPixel(center.x - x, center.y - y, color, 1f);
        }

        private void DrawThickPoint(LayerObject layer, int cx, int cy, Color color, int thickness)
        {
            int radius = thickness / 2;
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y <= radius * radius)
                    {
                        layer.DrawPixel(cx + x, cy + y, color, 1f);
                    }
                }
            }
        }

        // Legacy method compatibility
        public void SetBrush(Brush brush)
        {
            // No longer used - settings are read directly from DrawToolSettings
            if (_settings != null && brush != null)
            {
                _settings.ApplyBrush(brush);
            }
        }
    }
}
