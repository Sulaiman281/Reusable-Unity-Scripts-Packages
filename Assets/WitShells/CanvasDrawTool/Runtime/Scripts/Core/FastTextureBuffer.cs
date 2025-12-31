namespace WitShells.CanvasDrawTool
{
    using UnityEngine;
    using System.Runtime.CompilerServices;
    using Unity.Collections;

    /// <summary>
    /// High-performance texture buffer using byte array for Android optimization.
    /// Based on DirectTextureDrawing approach for maximum performance.
    /// Uses direct byte manipulation instead of Color struct for speed. (Android optimized)
    /// </summary>
    public class FastTextureBuffer
    {
        private byte[] _data;
        private int _width;
        private int _height;
        private bool _isDirty;

        // Dirty region tracking for partial updates
        private int _dirtyMinX;
        private int _dirtyMinY;
        private int _dirtyMaxX;
        private int _dirtyMaxY;
        private bool _hasDirtyRegion;

        // Reusable lookup tables for performance
        private static readonly byte[] _alphaLUT = new byte[256];
        private static readonly float[] _floatToByteMultiplier = new float[256];
        private static bool _lutInitialized = false;

        public int Width => _width;
        public int Height => _height;
        public bool IsDirty => _isDirty;
        public byte[] Data => _data;

        static FastTextureBuffer()
        {
            InitializeLUT();
        }

        private static void InitializeLUT()
        {
            if (_lutInitialized) return;

            for (int i = 0; i < 256; i++)
            {
                _alphaLUT[i] = (byte)i;
                _floatToByteMultiplier[i] = i / 255f;
            }
            _lutInitialized = true;
        }

        public FastTextureBuffer(int width, int height)
        {
            _width = width;
            _height = height;
            _data = new byte[width * height * 4]; // RGBA32
            ResetDirtyRegion();
        }

        public FastTextureBuffer(int width, int height, Color32 fillColor)
        {
            _width = width;
            _height = height;
            _data = new byte[width * height * 4];

            // Fast fill using memset-like approach
            int pixelCount = width * height;
            for (int i = 0; i < pixelCount; i++)
            {
                int index = i * 4;
                _data[index] = fillColor.r;
                _data[index + 1] = fillColor.g;
                _data[index + 2] = fillColor.b;
                _data[index + 3] = fillColor.a;
            }

            ResetDirtyRegion();
        }

        /// <summary>
        /// Initialize from existing texture.
        /// </summary>
        public void LoadFromTexture(Texture2D texture)
        {
            if (texture.width != _width || texture.height != _height)
            {
                _width = texture.width;
                _height = texture.height;
                _data = new byte[_width * _height * 4];
            }

            // Get raw texture data for maximum performance
            var rawData = texture.GetRawTextureData<byte>();
            if (rawData.Length == _data.Length)
            {
                rawData.CopyTo(_data);
            }
            else
            {
                // Fallback for different formats
                Color32[] pixels = texture.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                {
                    int index = i * 4;
                    _data[index] = pixels[i].r;
                    _data[index + 1] = pixels[i].g;
                    _data[index + 2] = pixels[i].b;
                    _data[index + 3] = pixels[i].a;
                }
            }

            ResetDirtyRegion();
        }

        /// <summary>
        /// Apply buffer to texture (optimized partial update if possible).
        /// </summary>
        public void ApplyToTexture(Texture2D texture)
        {
            if (!_isDirty) return;

            // Use LoadRawTextureData for maximum performance
            texture.LoadRawTextureData(_data);
            texture.Apply(false); // false = don't rebuild mipmaps

            _isDirty = false;
            ResetDirtyRegion();
        }

        /// <summary>
        /// Clear entire buffer with color.
        /// </summary>
        public void Clear(Color32 color)
        {
            int pixelCount = _width * _height;
            for (int i = 0; i < pixelCount; i++)
            {
                int index = i * 4;
                _data[index] = color.r;
                _data[index + 1] = color.g;
                _data[index + 2] = color.b;
                _data[index + 3] = color.a;
            }

            _isDirty = true;
            _hasDirtyRegion = false; // Full update needed
        }

        /// <summary>
        /// Set pixel directly (fastest, no blending).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPixelDirect(int x, int y, byte r, byte g, byte b, byte a)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height) return;

            int index = (y * _width + x) * 4;
            _data[index] = r;
            _data[index + 1] = g;
            _data[index + 2] = b;
            _data[index + 3] = a;

            ExpandDirtyRegion(x, y);
            _isDirty = true;
        }

        /// <summary>
        /// Set pixel with Color32 (no blending, very fast).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPixel(int x, int y, Color32 color)
        {
            SetPixelDirect(x, y, color.r, color.g, color.b, color.a);
        }

        /// <summary>
        /// Get pixel as Color32 (fast).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color32 GetPixel(int x, int y)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height)
                return new Color32(0, 0, 0, 0);

            int index = (y * _width + x) * 4;
            return new Color32(_data[index], _data[index + 1], _data[index + 2], _data[index + 3]);
        }

        /// <summary>
        /// Draw pixel with alpha blending (optimized).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawPixelBlend(int x, int y, byte r, byte g, byte b, byte brushAlpha, float opacity)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height) return;

            int index = (y * _width + x) * 4;

            // Calculate effective alpha
            int alpha = (int)(brushAlpha * opacity);
            if (alpha <= 0) return;
            if (alpha > 255) alpha = 255;

            // Get existing pixel
            byte existR = _data[index];
            byte existG = _data[index + 1];
            byte existB = _data[index + 2];
            byte existA = _data[index + 3];

            // Fast alpha blend using integer math (much faster than float)
            int invAlpha = 255 - alpha;

            _data[index] = (byte)((r * alpha + existR * invAlpha) >> 8);
            _data[index + 1] = (byte)((g * alpha + existG * invAlpha) >> 8);
            _data[index + 2] = (byte)((b * alpha + existB * invAlpha) >> 8);
            _data[index + 3] = (byte)Mathf.Min(255, existA + ((alpha * (255 - existA)) >> 8));

            ExpandDirtyRegion(x, y);
            _isDirty = true;
        }

        /// <summary>
        /// Draw pixel with Color32 and opacity blending.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawPixelBlend(int x, int y, Color32 color, float opacity)
        {
            DrawPixelBlend(x, y, color.r, color.g, color.b, color.a, opacity);
        }

        /// <summary>
        /// Erase pixel (reduce alpha).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ErasePixel(int x, int y, float eraseStrength)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height) return;

            int index = (y * _width + x) * 4;
            int currentAlpha = _data[index + 3];
            int reduction = (int)(eraseStrength * 255);
            _data[index + 3] = (byte)Mathf.Max(0, currentAlpha - reduction);

            ExpandDirtyRegion(x, y);
            _isDirty = true;
        }

        /// <summary>
        /// Draw a filled circle (brush stamp) - highly optimized.
        /// </summary>
        public void DrawCircle(int centerX, int centerY, int radius, Color32 color, float opacity, float hardness)
        {
            if (radius <= 0) return;

            int radiusSq = radius * radius;
            int minX = Mathf.Max(0, centerX - radius);
            int maxX = Mathf.Min(_width - 1, centerX + radius);
            int minY = Mathf.Max(0, centerY - radius);
            int maxY = Mathf.Min(_height - 1, centerY + radius);

            // Pre-calculate for hard brush
            bool isHardBrush = hardness >= 0.99f;

            for (int y = minY; y <= maxY; y++)
            {
                int dy = y - centerY;
                int dySq = dy * dy;
                int rowIndex = y * _width * 4;

                for (int x = minX; x <= maxX; x++)
                {
                    int dx = x - centerX;
                    int distSq = dx * dx + dySq;

                    if (distSq <= radiusSq)
                    {
                        int index = rowIndex + x * 4;
                        byte brushAlpha;

                        if (isHardBrush)
                        {
                            brushAlpha = color.a;
                        }
                        else
                        {
                            // Soft falloff
                            float dist = Mathf.Sqrt(distSq);
                            float normalizedDist = dist / radius;
                            float alphaMultiplier = 1f - (normalizedDist * (1f - hardness));
                            brushAlpha = (byte)(color.a * Mathf.Clamp01(alphaMultiplier));
                        }

                        // Inline blend for speed
                        int alpha = (int)(brushAlpha * opacity);
                        if (alpha > 0)
                        {
                            if (alpha > 255) alpha = 255;
                            int invAlpha = 255 - alpha;

                            _data[index] = (byte)((color.r * alpha + _data[index] * invAlpha) >> 8);
                            _data[index + 1] = (byte)((color.g * alpha + _data[index + 1] * invAlpha) >> 8);
                            _data[index + 2] = (byte)((color.b * alpha + _data[index + 2] * invAlpha) >> 8);
                            _data[index + 3] = (byte)Mathf.Min(255, _data[index + 3] + ((alpha * (255 - _data[index + 3])) >> 8));
                        }
                    }
                }
            }

            ExpandDirtyRegion(minX, minY);
            ExpandDirtyRegion(maxX, maxY);
            _isDirty = true;
        }

        /// <summary>
        /// Erase a circle area.
        /// </summary>
        public void EraseCircle(int centerX, int centerY, int radius, float opacity, float hardness)
        {
            if (radius <= 0) return;

            int radiusSq = radius * radius;
            int minX = Mathf.Max(0, centerX - radius);
            int maxX = Mathf.Min(_width - 1, centerX + radius);
            int minY = Mathf.Max(0, centerY - radius);
            int maxY = Mathf.Min(_height - 1, centerY + radius);

            bool isHardBrush = hardness >= 0.99f;

            for (int y = minY; y <= maxY; y++)
            {
                int dy = y - centerY;
                int dySq = dy * dy;
                int rowIndex = y * _width * 4;

                for (int x = minX; x <= maxX; x++)
                {
                    int dx = x - centerX;
                    int distSq = dx * dx + dySq;

                    if (distSq <= radiusSq)
                    {
                        int index = rowIndex + x * 4;
                        float eraseAmount;

                        if (isHardBrush)
                        {
                            eraseAmount = opacity;
                        }
                        else
                        {
                            float dist = Mathf.Sqrt(distSq);
                            float normalizedDist = dist / radius;
                            eraseAmount = opacity * (1f - (normalizedDist * (1f - hardness)));
                        }

                        int reduction = (int)(eraseAmount * 255);
                        _data[index + 3] = (byte)Mathf.Max(0, _data[index + 3] - reduction);
                    }
                }
            }

            ExpandDirtyRegion(minX, minY);
            ExpandDirtyRegion(maxX, maxY);
            _isDirty = true;
        }

        /// <summary>
        /// Draw a line using Bresenham's algorithm (your optimized approach).
        /// </summary>
        public void DrawLine(int x0, int y0, int x1, int y1, Color32 color, int thickness, float opacity, float hardness)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            int x = x0;
            int y = y0;

            // Use spacing based on thickness to avoid overdraw
            int stepCount = 0;
            int spacing = Mathf.Max(1, thickness / 4);

            while (true)
            {
                if (stepCount % spacing == 0)
                {
                    DrawCircle(x, y, thickness / 2, color, opacity, hardness);
                }
                stepCount++;

                if (x == x1 && y == y1) break;

                int err2 = err * 2;
                if (err2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }
                if (err2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }
        }

        /// <summary>
        /// Erase along a line.
        /// </summary>
        public void EraseLine(int x0, int y0, int x1, int y1, int thickness, float opacity, float hardness)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            int x = x0;
            int y = y0;

            int stepCount = 0;
            int spacing = Mathf.Max(1, thickness / 4);

            while (true)
            {
                if (stepCount % spacing == 0)
                {
                    EraseCircle(x, y, thickness / 2, opacity, hardness);
                }
                stepCount++;

                if (x == x1 && y == y1) break;

                int err2 = err * 2;
                if (err2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }
                if (err2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }
        }

        /// <summary>
        /// Copy data from another buffer.
        /// </summary>
        public void CopyFrom(FastTextureBuffer source)
        {
            if (source._width != _width || source._height != _height) return;
            System.Buffer.BlockCopy(source._data, 0, _data, 0, _data.Length);
            _isDirty = true;
        }

        /// <summary>
        /// Get a copy of the data for undo.
        /// </summary>
        public byte[] GetDataCopy()
        {
            byte[] copy = new byte[_data.Length];
            System.Buffer.BlockCopy(_data, 0, copy, 0, _data.Length);
            return copy;
        }

        /// <summary>
        /// Restore from data copy.
        /// </summary>
        public void RestoreFromData(byte[] data)
        {
            if (data.Length != _data.Length) return;
            System.Buffer.BlockCopy(data, 0, _data, 0, _data.Length);
            _isDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExpandDirtyRegion(int x, int y)
        {
            if (!_hasDirtyRegion)
            {
                _dirtyMinX = _dirtyMaxX = x;
                _dirtyMinY = _dirtyMaxY = y;
                _hasDirtyRegion = true;
            }
            else
            {
                if (x < _dirtyMinX) _dirtyMinX = x;
                if (x > _dirtyMaxX) _dirtyMaxX = x;
                if (y < _dirtyMinY) _dirtyMinY = y;
                if (y > _dirtyMaxY) _dirtyMaxY = y;
            }
        }

        private void ResetDirtyRegion()
        {
            _hasDirtyRegion = false;
            _dirtyMinX = _width;
            _dirtyMinY = _height;
            _dirtyMaxX = 0;
            _dirtyMaxY = 0;
        }
    }
}
