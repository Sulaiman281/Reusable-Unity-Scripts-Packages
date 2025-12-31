// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.Events;

// namespace WitShells.CanvasDrawTool
// {
//     public struct PixelData : System.IEquatable<PixelData>
//     {
//         public Vector2Int position;
//         public Color color;

//         public override string ToString()
//         {
//             return $"Pixel at {position} with color {color}";
//         }

//         public bool Equals(PixelData other)
//         {
//             return position.Equals(other.position);
//         }

//         // IMPORTANT: Also override GetHashCode to only use position
//         public override int GetHashCode()
//         {
//             return position.GetHashCode();
//         }

//         // Override object.Equals for consistency
//         public override bool Equals(object obj)
//         {
//             if (obj is PixelData other)
//                 return Equals(other);
//             return false;
//         }
//     }

//     public static class DirectTextureDrawing
//     {
//         private static Vector2Int? _lastDrawnPixel = null;
//         private static Color _lastColor = Color.white;
//         private static int _lastBrushSize = 1;

//         public static UnityEvent<PixelData[]> OnPixelsDrawn = new UnityEvent<PixelData[]>();

//         public static void Reset()
//         {
//             _lastDrawnPixel = null;
//         }

//         public static void DrawOnTextureData(byte[] textureData, int width, int height,
//         Vector2 normalizedPosition, BrushSettings brushSettings, bool connectLine = false)
//         {
//             DrawOnTextureData(textureData, width, height, normalizedPosition
//             , brushSettings.BrushColor, Mathf.RoundToInt(brushSettings.BrushSize),
//              brushSettings.BrushType, connectLine);
//         }

//         /// <summary>
//         /// Draw directly on texture byte data for maximum performance and accuracy
//         /// </summary>
//         public static void DrawOnTextureData(
//             byte[] textureData,
//             int textureWidth,
//             int textureHeight,
//             Vector2 normalizedPosition,
//             Color color,
//             int brushSize,
//             BrushType brushType,
//             bool connectLine = false)
//         {
//             if (textureData == null || textureWidth <= 0 || textureHeight <= 0) return;

//             // Convert normalized position to pixel coordinates
//             int x = Mathf.Clamp(Mathf.RoundToInt(normalizedPosition.x * (textureWidth - 1)), 0, textureWidth - 1);
//             int y = Mathf.Clamp(Mathf.RoundToInt(normalizedPosition.y * (textureHeight - 1)), 0, textureHeight - 1);

//             Vector2Int currentPixel = new Vector2Int(x, y);
//             List<PixelData> drawnPixels = new List<PixelData>();

//             // Draw connected line if enabled and we have a previous point
//             if (connectLine && _lastDrawnPixel.HasValue &&
//                 (_lastColor != color || _lastBrushSize != brushSize || Vector2Int.Distance(_lastDrawnPixel.Value, currentPixel) > 1))
//             {
//                 var linePixels = DrawLineBetweenPixels(textureData, textureWidth, textureHeight,
//                     _lastDrawnPixel.Value, currentPixel, color, brushSize, brushType);
//                 drawnPixels.AddRange(linePixels);
//             }

//             // Draw the current pixel cluster
//             var clusterPixels = DrawPixelCluster(textureData, textureWidth, textureHeight, x, y, color, brushSize, brushType);
//             drawnPixels.AddRange(clusterPixels);

//             // Invoke events with the drawn pixels
//             InvokePixelEvents(drawnPixels);

//             // Update last drawn pixel info
//             _lastDrawnPixel = currentPixel;
//             _lastColor = color;
//             _lastBrushSize = brushSize;
//         }

//         private static List<PixelData> DrawPixelCluster(
//             byte[] textureData,
//             int textureWidth,
//             int textureHeight,
//             int centerX,
//             int centerY,
//             Color color,
//             int brushSize,
//             BrushType brushType)
//         {
//             List<PixelData> drawnPixels = new List<PixelData>();

//             // Calculate radius - eraser should be 2x the size
//             int radius = brushType == BrushType.Eraser ? brushSize * 3 : brushSize;

//             for (int dy = -radius; dy <= radius; dy++)
//             {
//                 for (int dx = -radius; dx <= radius; dx++)
//                 {
//                     int x = centerX + dx;
//                     int y = centerY + dy;

//                     // Check bounds
//                     if (x < 0 || x >= textureWidth || y < 0 || y >= textureHeight) continue;

//                     // Apply brush shape (circle)
//                     bool shouldDraw = dx * dx + dy * dy <= radius * radius;

//                     if (shouldDraw)
//                     {
//                         byte r, g, b, a;
//                         Color pixelColor;

//                         if (brushType == BrushType.Eraser)
//                         {
//                             // Eraser: make pixel transparent
//                             r = g = b = a = 0;
//                             pixelColor = Color.clear;
//                         }
//                         else
//                         {
//                             // Normal brush: use the provided color
//                             r = (byte)(color.r * 255);
//                             g = (byte)(color.g * 255);
//                             b = (byte)(color.b * 255);
//                             a = (byte)(color.a * 255);
//                             pixelColor = color;
//                         }

//                         SetPixelInTextureData(textureData, textureWidth, x, y, r, g, b, a);

//                         // Add to drawn pixels list
//                         drawnPixels.Add(new PixelData
//                         {
//                             position = new Vector2Int(x, y),
//                             color = pixelColor
//                         });
//                     }
//                 }
//             }

//             return drawnPixels;
//         }

//         private static List<PixelData> DrawLineBetweenPixels(
//             byte[] textureData,
//             int textureWidth,
//             int textureHeight,
//             Vector2Int start,
//             Vector2Int end,
//             Color color,
//             int brushSize,
//             BrushType brushType)
//         {
//             List<PixelData> drawnPixels = new List<PixelData>();

//             // Bresenham's line algorithm
//             int dx = Mathf.Abs(end.x - start.x);
//             int dy = Mathf.Abs(end.y - start.y);
//             int sx = start.x < end.x ? 1 : -1;
//             int sy = start.y < end.y ? 1 : -1;
//             int err = dx - dy;

//             int x = start.x;
//             int y = start.y;

//             while (true)
//             {
//                 // Draw pixel cluster at current position with the correct brush type
//                 var clusterPixels = DrawPixelCluster(textureData, textureWidth, textureHeight, x, y, color, brushSize, brushType);
//                 drawnPixels.AddRange(clusterPixels);

//                 if (x == end.x && y == end.y) break;

//                 int err2 = err * 2;
//                 if (err2 > -dy)
//                 {
//                     err -= dy;
//                     x += sx;
//                 }
//                 if (err2 < dx)
//                 {
//                     err += dx;
//                     y += sy;
//                 }
//             }

//             return drawnPixels;
//         }

//         private static void InvokePixelEvents(List<PixelData> drawnPixels)
//         {
//             if (drawnPixels == null || drawnPixels.Count == 0) return;

//             // Invoke event for multiple pixels
//             OnPixelsDrawn?.Invoke(drawnPixels.ToArray());
//         }

//         public static void SetPixelInTextureData(byte[] textureData, int textureWidth, int x, int y, byte r, byte g, byte b, byte a)
//         {
//             int index = (y * textureWidth + x) * 4; // RGBA32 format
//             if (index < 0 || index + 3 >= textureData.Length) return;

//             textureData[index] = r;
//             textureData[index + 1] = g;
//             textureData[index + 2] = b;
//             textureData[index + 3] = a;
//         }

//         /// <summary>
//         /// Get current texture data as copy for undo operations
//         /// </summary>
//         public static byte[] GetTextureDataCopy(byte[] sourceData)
//         {
//             if (sourceData == null) return null;

//             byte[] copy = new byte[sourceData.Length];
//             System.Array.Copy(sourceData, copy, sourceData.Length);
//             return copy;
//         }

//         /// <summary>
//         /// Apply an array of pixels to texture data
//         /// </summary>
//         /// <param name="textureData">The texture byte array to modify</param>
//         /// <param name="textureWidth">Width of the texture</param>
//         /// <param name="textureHeight">Height of the texture</param>
//         /// <param name="pixels">Array of pixels to apply</param>
//         public static void ApplyPixelsToTextureData(byte[] textureData, int textureWidth, int textureHeight, PixelData[] pixels)
//         {
//             if (textureData == null || pixels == null || textureWidth <= 0 || textureHeight <= 0) return;

//             foreach (var pixel in pixels)
//             {
//                 // Check bounds
//                 if (pixel.position.x < 0 || pixel.position.x >= textureWidth ||
//                     pixel.position.y < 0 || pixel.position.y >= textureHeight) continue;

//                 // Convert color to bytes
//                 byte r = (byte)(pixel.color.r * 255);
//                 byte g = (byte)(pixel.color.g * 255);
//                 byte b = (byte)(pixel.color.b * 255);
//                 byte a = (byte)(pixel.color.a * 255);

//                 // Apply pixel to texture data
//                 SetPixelInTextureData(textureData, textureWidth, pixel.position.x, pixel.position.y, r, g, b, a);
//             }
//         }

//         /// <summary>
//         /// Apply a single pixel to texture data
//         /// </summary>
//         /// <param name="textureData">The texture byte array to modify</param>
//         /// <param name="textureWidth">Width of the texture</param>
//         /// <param name="textureHeight">Height of the texture</param>
//         /// <param name="pixel">Single pixel to apply</param>
//         public static void ApplyPixelToTextureData(byte[] textureData, int textureWidth, int textureHeight, PixelData pixel)
//         {
//             if (textureData == null || textureWidth <= 0 || textureHeight <= 0) return;

//             // Check bounds
//             if (pixel.position.x < 0 || pixel.position.x >= textureWidth ||
//                 pixel.position.y < 0 || pixel.position.y >= textureHeight) return;

//             // Convert color to bytes
//             byte r = (byte)(pixel.color.r * 255);
//             byte g = (byte)(pixel.color.g * 255);
//             byte b = (byte)(pixel.color.b * 255);
//             byte a = (byte)(pixel.color.a * 255);

//             // Apply pixel to texture data
//             SetPixelInTextureData(textureData, textureWidth, pixel.position.x, pixel.position.y, r, g, b, a);
//         }
//     }
// }
