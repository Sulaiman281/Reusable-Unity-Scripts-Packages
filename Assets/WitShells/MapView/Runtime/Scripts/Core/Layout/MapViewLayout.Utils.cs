using UnityEngine;

namespace WitShells.MapView
{
    public partial class MapViewLayout
    {
        #region Utilities

        public bool IsPlacableInBoundsAndContains(PlacableData data, out IPlacable placable)
        {
            placable = null;
            if (worldObjectMarkers == null || !worldObjectMarkers.HasMarkers)
                return false;

            if (HasWorldPositionInMapView(data, out var position))
            {
                return position.x >= TopLeftLimit.x && position.x <= BottomRightLimit.x &&
                       position.y <= TopLeftLimit.y && position.y >= BottomRightLimit.y;
            }

            if (worldObjectMarkers.HasPlacableByData(data, out placable))
                return true;

            return false;
        }

        public Vector3 GetPositionForTile(Vector2Int coordinate)
        {
            if (CenterTile == null)
            {
                var pos = coordinate - CenterCoordiante;
                return new Vector3(pos.x * 256, pos.y * -256, 0);
            }
            var position = coordinate - CenterTile.Coordinate;
            var basePosition = new Vector3(position.x * 256, position.y * -256, 0);
            return basePosition + CenterTile.transform.localPosition;
        }

        private bool IsLocationBoundsLessThenScreen()
        {
            var (col, row) = TileCount();
            var totalCols = Utils.TotalHorizontalBoundsTiles(fromCoordinates, toCoordinates, zoomLevel);
            var totalRows = Utils.TotalVerticalBoundsTiles(fromCoordinates, toCoordinates, zoomLevel);
            return totalCols < col && totalRows < row;
        }

        private Transform ZoomLayer()
        {
            if (!zoomLayers.ContainsKey(zoomLevel))
            {
                var obj = new GameObject($"Zoom_{zoomLevel}");
                obj.transform.SetParent(transform);
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;
                var rectTransform = obj.AddComponent<RectTransform>();
                var size = RectSize();
                rectTransform.sizeDelta = new Vector2(size.Width, size.Height);
                obj.transform.localScale = Vector3.one;
                zoomLayers[zoomLevel] = obj.transform;
            }
            return zoomLayers[zoomLevel];
        }

        public (float Width, float Height) RectSize()
        {
            var rectSize = transform as RectTransform;
            return (rectSize.rect.width, rectSize.rect.height);
        }

        public (int Col, int Row) TileCount()
        {
            var (width, height) = RectSize();
            return ((int)(width / 256f), (int)(height / 256f));
        }

        public TileView GetTileAtCoordinate(Vector2Int coordinate)
        {
            var startingTile = coordinate - TopLeftTile.Coordinate;
            if (startingTile.x < 0 || startingTile.y < 0 ||
                startingTile.x >= tiles.GetLength(0) || startingTile.y >= tiles.GetLength(1))
                return null;
            return tiles[startingTile.x, startingTile.y];
        }

        public bool HasWorldPositionInMapView(PlacableData data, out Vector3 position) =>
            HasWorldPositionInMapView(data.Coordinates, out position);

        public bool HasWorldPositionInMapView(Coordinates coordinates, out Vector3 position)
        {
            (int tileX, int tileY, float normX, float normY) =
                Utils.LatLonToTileNormalized(coordinates.Latitude, coordinates.Longitude, zoomLevel);
            normX = Mathf.Clamp01(normX);
            normY = Mathf.Clamp01(normY);
            return HasWorldPositionInMapView(new Vector2Int(tileX, tileY), normX, normY, out position);
        }

        private bool HasWorldPositionInMapView(Vector2Int tileCoordinate, float normX, float normY, out Vector3 position)
        {
            var tile = GetTileAtCoordinate(tileCoordinate);
            if (tile == null)
            {
                position = Vector3.zero;
                return false;
            }
            Utils.GetLocalPositionFromNormalizedInTile(tile.RectTransform, normX, normY, transform, out position);
            position = transform.TransformPoint(position);
            return true;
        }

        /// <summary>
        /// Reverse of HasWorldPositionInMapView: given a world position, returns the tile's coordinate
        /// (tile X/Y in the current grid) and the normalized position inside that tile (0..1).
        /// Returns false if the world position is outside the currently generated tiles or tiles not available.
        /// </summary>
        public bool TryGetTileAndNormalizedFromWorldPosition(Vector3 worldPosition, out Coordinates tileCoordinate, out float normX, out float normY)
        {
            tileCoordinate = default;
            normX = normY = 0f;

            if (tiles == null || tiles.Length == 0 || gridSize.x <= 0 || gridSize.y <= 0)
                return false;

            // Calculate tile index relative to the current TopLeftTile to account for grid drift/cycling
            if (TopLeftTile == null) return false;

            // Use ZoomLayer to handle scaling and correct local space
            var zoomLayer = ZoomLayer();
            var localPos = zoomLayer.InverseTransformPoint(worldPosition);

            var topLeftRT = TopLeftTile.RectTransform;
            var width = topLeftRT.rect.width;
            var height = topLeftRT.rect.height;
            var pivot = topLeftRT.pivot;

            // Calculate the visual top-left corner of the grid in local space
            float gridOriginX = TopLeftTile.transform.localPosition.x - (width * pivot.x);
            float gridOriginY = TopLeftTile.transform.localPosition.y + (height * (1f - pivot.y));

            int tileX = Mathf.FloorToInt((localPos.x - gridOriginX) / width);
            int tileY = Mathf.FloorToInt((gridOriginY - localPos.y) / height);

            if (tileX < 0 || tileX >= gridSize.x || tileY < 0 || tileY >= gridSize.y)
                return false;

            var tile = tiles[tileX, tileY];
            if (tile == null) return false;

            // Calculate normalized position manually to avoid clamping (which Utils.GetNormalizedPositionInTile does)
            // This ensures that if we are slightly off the tile (due to precision), we get the correct continuous coordinate
            var localInTile = tile.RectTransform.InverseTransformPoint(worldPosition);
            var rect = tile.RectTransform.rect;

            float px = localInTile.x + rect.width * tile.RectTransform.pivot.x;
            float py = localInTile.y + rect.height * tile.RectTransform.pivot.y;

            normX = px / rect.width;
            // invert Y so normalized Y matches tile coordinate system (0 = top)
            normY = 1f - (py / rect.height);

            var (lat, lon) = Utils.TileNormalizedToLatLon(tile.Coordinate.x, tile.Coordinate.y, zoomLevel, normX, normY);

            tileCoordinate = new Coordinates { Latitude = lat, Longitude = lon };
            return true;
        }
        #endregion

        #region Cache

        public void SaveCurrentCenterTileToCache()
        {
            string cacheValue = $"{locationName}_{zoomLevel}_{CenterCoordiante.x}_{CenterCoordiante.y}";
            PlayerPrefs.SetString("MapView_LastCenterTile", cacheValue);
            PlayerPrefs.Save();
        }

        public bool HasCachedCenterTile(out Vector2Int coordinate, out int zoom)
        {
            coordinate = Vector2Int.zero;
            zoom = zoomLevel;

            if (!PlayerPrefs.HasKey("MapView_LastCenterTile")) return false;
            string cacheValue = PlayerPrefs.GetString("MapView_LastCenterTile");
            string[] parts = cacheValue.Split('_');
            if (parts.Length != 4) return false;

            if (!string.Equals(parts[0], locationName)) return false;
            if (!int.TryParse(parts[1], out zoom)) return false;
            if (!int.TryParse(parts[2], out int tileX)) return false;
            if (!int.TryParse(parts[3], out int tileY)) return false;

            coordinate = new Vector2Int(tileX, tileY);
            return true;
        }

        #endregion
    }
}
