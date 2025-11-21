using System;
using System.Collections.Generic;
using UnityEngine;

namespace WitShells.MapView
{
    public partial class MapViewLayout
    {
        #region Labels Toggle

        [ContextMenu("Toggle Geo Tags")]
        public void ToggleGeoTags()
        {
            MapSettings.Instance.showLabels = !MapSettings.Instance.showLabels;
            foreach (Transform zoomLayer in zoomLayers.Values)
            {
                foreach (Transform child in zoomLayer)
                {
                    if (child.TryGetComponent<TileView>(out var tile))
                        tile.ChangeLabelMode(showLabels);
                }
            }
        }

        #endregion

        #region Tile Generation

        [ContextMenu("Generate Layout")]
        public void GenerateLayout()
        {
            foreach (Transform zoomLayer in zoomLayers.Values)
            {
                foreach (Transform child in zoomLayer)
                {
                    if (child.TryGetComponent<TileView>(out var tile))
                    {
                        tile.gameObject.SetActive(false);
                        MoveTileToDirection.RemoveListener(tile.MoveTo);
                        Pool.Release(tile);
                    }
                }
            }
            CenterTile = null;

            try
            {
                if (IsLocationBoundsLessThenScreen())
                {
                    GenerateAllTiles();
                    isFixedLayout = true;
                }
                else
                {
                    CenterCoordiante = Utils.TileCenterForBounds(fromCoordinates, toCoordinates, zoomLevel);
                    GenerateScreenFillingTiles();
                    isFixedLayout = false;
                }
            }
            catch (Exception ex)
            {
                DesignPatterns.WitLogger.LogWarning($"Failed to generate map layout: {ex.Message}");
            }
        }

        private void GenerateAllTiles()
        {
            (int xMin, int xMax, int yMin, int yMax) = Utils.TileRangeForBounds(fromCoordinates, toCoordinates, zoomLevel);
            CenterCoordiante = Utils.TileCenterForBounds(fromCoordinates, toCoordinates, zoomLevel);
            gridSize = new Vector2Int(xMax - xMin + 1, yMax - yMin + 1);
            tiles = new TileView[gridSize.x, gridSize.y];

            List<Vector2Int> allTiles = new();

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    var coordinate = new Vector2Int(x + xMin, y + yMin);
                    AddTile(coordinate, out var tile);
                    tiles[x, y] = tile;
                    allTiles.Add(coordinate);
                }
            }

            MapTileManager.Instance.StartStreamFetch(allTiles, zoomLevel, showLabels);
            UpdateCorners();
            TopLeftLimit = GetPositionForTile(new Vector2Int(xMin, yMin));
            BottomRightLimit = GetPositionForTile(new Vector2Int(xMax, yMax));
        }

        private void GenerateScreenFillingTiles()
        {
            var centerTile = CenterCoordiante;
            CenterCoordiante = centerTile;

            var count = TileCount();
            int startX = centerTile.x - count.Col / 2;
            int endX = centerTile.x + count.Col / 2;
            int startY = centerTile.y - count.Row / 2;
            int endY = centerTile.y + count.Row / 2;

            startX -= BoundsOffset;
            endX += BoundsOffset;
            startY -= BoundsOffset;
            endY += BoundsOffset;

            gridSize = new Vector2Int(endX - startX + 1, endY - startY + 1);
            tiles = new TileView[gridSize.x, gridSize.y];

            TopLeftLimit = GetPositionForTile(new Vector2Int(startX, startY));
            BottomRightLimit = GetPositionForTile(new Vector2Int(endX, endY));

            List<Vector2Int> allTiles = new();
            int arrayX = 0;

            for (int x = startX; x <= endX; x++)
            {
                int arrayY = 0;
                for (int y = startY; y <= endY; y++)
                {
                    var coordinate = new Vector2Int(x, y);
                    AddTile(coordinate, out var tile);
                    tiles[arrayX, arrayY] = tile;
                    arrayY++;
                    allTiles.Add(coordinate);
                }
                arrayX++;
            }

            MapTileManager.Instance.StartStreamFetch(allTiles, zoomLevel, showLabels);
            UpdateCorners();
        }

        private void AddTile(Vector2Int coordinate, out TileView tile)
        {
            tile = Pool.Get();
            tile.transform.SetParent(ZoomLayer());
            tile.name = $"Tile_{coordinate.x}_{coordinate.y}";
            tile.gameObject.SetActive(true);
            tile.UpdateCoordinate(coordinate, zoomLevel, showLabels);

            MoveTileToDirection.AddListener(tile.MoveTo);
            tile.transform.localScale = Vector3.one;
            tile.transform.localPosition = GetPositionForTile(coordinate);
        }

        private void UpdateCorners()
        {
            TopLeftTile = tiles[0, 0];
            BottomRightTile = tiles[gridSize.x - 1, gridSize.y - 1];
            TopRightTile = tiles[gridSize.x - 1, 0];
            BottomLeftTile = tiles[0, gridSize.y - 1];
            CenterTile = tiles[gridSize.x / 2, gridSize.y / 2];
            CenterCoordiante = CenterTile.Coordinate;
        }

        #endregion

        #region Tile Fetch Handlers

        private void OnTileIsFetched(Vector2Int coordinate, Tile tile)
        {
            if (tile == null) return;
            TileView tv = GetTileAtCoordinate(coordinate);
            if (tv == null) return;
            tv.SetData(tile);
        }

        private void OnTilesIsFetchedBatch(List<Tile> fetchedTiles)
        {
            if (fetchedTiles == null || fetchedTiles.Count == 0 || tiles == null) return;
            foreach (var tile in fetchedTiles)
            {
                if (tile == null) continue;
                var coord = new Vector2Int(tile.TileX, tile.TileY);
                var tv = GetTileAtCoordinate(coord);
                if (tv == null) continue;
                tv.SetData(tile);
            }
        }

        #endregion
    }
}
