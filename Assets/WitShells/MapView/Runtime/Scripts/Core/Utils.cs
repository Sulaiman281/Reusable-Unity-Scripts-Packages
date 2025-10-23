using System;
using System.Collections.Generic;
using UnityEngine;
using static WitShells.MapView.MapViewLayout;

namespace WitShells.MapView
{
    public static class Utils
    {
        public static string GoogleSatellite = "http://www.google.cn/maps/vt?lyrs=s@189&gl=cn&x={x}&y={y}&z={z}";
        public static string GoogleSatelliteWithLabels = "https://mt1.google.com/vt/lyrs=y&x={x}&y={y}&z={z}";

        public static void LatLonToTileXY(double lat, double lon, float zoom, out int x, out int y)
        {
            double latRad = lat * Mathf.Deg2Rad;
            int n = 1 << (int)zoom;
            x = (int)((lon + 180.0) / 360.0 * n);
            y = (int)((1.0 - Mathf.Log((float)(Mathf.Tan((float)latRad) + 1.0 / Mathf.Cos((float)latRad))) / Mathf.PI) / 2.0 * n);
        }

        public static (double, double) TileNormalizedToLatLon(int x, int y, int zoom, float normX, float normY)
        {
            double n = Mathf.Pow(2, zoom);
            double fx = x + normX;
            double fy = y + normY;
            double lon_deg = fx / n * 360.0 - 180.0;
            double lat_rad = Math.Atan(Math.Sinh(Math.PI * (1 - 2.0 * fy / n)));
            double lat_deg = lat_rad * Mathf.Rad2Deg;
            return (lat_deg, lon_deg);
        }

        public static Vector2Int LatLonToTile(double lat, double lon, int zoom)
        {
            int n = 1 << zoom;
            int x = (int)((lon + 180.0) / 360.0 * n);
            int y = (int)((1.0 - Mathf.Log((float)Math.Tan(Mathf.Deg2Rad * (float)lat) +
                       1.0f / Mathf.Cos(Mathf.Deg2Rad * (float)lat)) / Mathf.PI) / 2.0 * n);
            return new Vector2Int(x, y);
        }

        // bytes to Texture
        public static Texture2D BytesToTexture(this byte[] bytes)
        {
            var texture = new Texture2D(2, 2);
            if (bytes == null || bytes.Length == 0 || !texture.LoadImage(bytes))
            {
                UnityEngine.Object.Destroy(texture);
                return null;
            }
            return texture;
        }

        public static string MakeTileUrl(bool withLabels, float lat, float lon, float z)
        {
            LatLonToTileXY(lat, lon, z, out int x, out int y);
            return MakeTileUrl(withLabels, x, y, z);
        }

        public static string MakeTileUrl(bool withLabels, int x, int y, float z)
        {
            string urlTemplate = withLabels ? GoogleSatelliteWithLabels : GoogleSatellite;
            return urlTemplate.Replace("{x}", x.ToString())
                              .Replace("{y}", y.ToString())
                              .Replace("{z}", z.ToString());
        }

        public static int ZoomToWorldRectSize(int zoom)
        {
            return (int)Mathf.Pow(2, 20 - zoom);
        }

        public static (int xMin, int xMax, int yMin, int yMax) TileRangeForBounds(
    double lat1, double lon1, double lat2, double lon2, int zoom)
        {
            double minLat = Math.Min(lat1, lat2);
            double maxLat = Math.Max(lat1, lat2);
            double minLon = Math.Min(lon1, lon2);
            double maxLon = Math.Max(lon1, lon2);

            Vector2Int t1 = LatLonToTile(maxLat, minLon, zoom); // top-left
            Vector2Int t2 = LatLonToTile(minLat, maxLon, zoom); // bottom-right

            int xMin = Math.Min(t1.x, t2.x);
            int xMax = Math.Max(t1.x, t2.x);
            int yMin = Math.Min(t1.y, t2.y);
            int yMax = Math.Max(t1.y, t2.y);

            return (xMin, xMax, yMin, yMax);
        }

        public static (int xMin, int xMax, int yMin, int yMax) TileRangeForBounds(
    Coordinates coord1, Coordinates coord2, int zoom)
        {
            return TileRangeForBounds(coord1.Latitude, coord1.Longitude, coord2.Latitude, coord2.Longitude, zoom);
        }

        public static int TileCountForBounds(Coordinates from, Coordinates to, int zoom)
        {
            var (xMin, xMax, yMin, yMax) = TileRangeForBounds(from, to, zoom);
            return (xMax - xMin + 1) * (yMax - yMin + 1);
        }

        public static int TotalVerticalBoundsTiles(Coordinates from, Coordinates to, int zoom)
        {
            var (_, _, yMin, yMax) = TileRangeForBounds(from, to, zoom);
            return yMax - yMin + 1;
        }

        public static int TotalHorizontalBoundsTiles(Coordinates from, Coordinates to, int zoom)
        {
            var (xMin, xMax, _, _) = TileRangeForBounds(from, to, zoom);
            return xMax - xMin + 1;
        }

        public static Vector2Int TileCenterForBounds(Coordinates from, Coordinates to, int zoom)
        {
            var (xMin, xMax, yMin, yMax) = TileRangeForBounds(from, to, zoom);
            int centerX = (xMin + xMax) / 2;
            int centerY = (yMin + yMax) / 2;
            return new Vector2Int(centerX, centerY);
        }

        // Convert tile XY back to lon/lat of tile's top-left corner (useful)
        public static (double lat, double lon) TileXYToLonLat(int x, int y, int z)
        {
            double n = Math.Pow(2.0, z);
            double lon = x / n * 360.0 - 180.0;
            double latRad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * y / n)));
            double lat = latRad * 180.0 / Math.PI;
            return (lat, lon);
        }

        public static IEnumerable<Vector2Int> GenerateSpiral(Vector2Int center, int totalTiles)
        {
            yield return center;

            // Directions in order: Right, Up, Left, Down
            Vector2Int[] directions = new Vector2Int[]
            {
                new(1, 0),   // Right
                new(0, 1),   // Up
                new(-1, 0),  // Left
                new(0, -1)   // Down
            };

            int stepSize = 1;   // how many tiles to move in current direction
            int dirIndex = 0;   // which direction to move
            int count = 1;      // already added the center

            Vector2Int currentPos = center;

            while (count < totalTiles)
            {
                // We increase step size every two direction changes
                for (int repeat = 0; repeat < 2; repeat++)
                {
                    for (int step = 0; step < stepSize; step++)
                    {
                        currentPos += directions[dirIndex];

                        yield return currentPos;
                        count++;

                        if (count >= totalTiles)
                            yield break;
                    }

                    // Change direction clockwise
                    dirIndex = (dirIndex + 1) % 4;
                }

                stepSize++; // Expand outward
            }
        }

        public static string BytesToString64(byte[] bytes)
        {
            return Convert.ToBase64String(bytes);
        }

        public static byte[] String64ToBytes(string base64)
        {
            return Convert.FromBase64String(base64);
        }

        public static Texture2D String64ToTexture(string base64)
        {
            var bytes = String64ToBytes(base64);
            return BytesToTexture(bytes);
        }
    }
}