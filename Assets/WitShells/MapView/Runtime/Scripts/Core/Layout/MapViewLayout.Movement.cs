using System.Collections.Generic;
using UnityEngine;

namespace WitShells.MapView
{
    public partial class MapViewLayout
    {
        #region Velocity & Movement

        private void ApplyVelocityMovement()
        {
            if (_velocity.magnitude <= 0.01f) return;

            Vector3 movement = _velocity * Time.unscaledDeltaTime;

            if (TopLeftTile != null && BottomRightTile != null)
            {
                if (movement.x > 0f && !CanMoveRightColumnToLeft()) { movement.x = 0f; _velocity.x = 0f; }
                else if (movement.x < 0f && !CanMoveLeftColumnToRight()) { movement.x = 0f; _velocity.x = 0f; }

                if (movement.y > 0f && !CanMoveTopRowToBottom()) { movement.y = 0f; _velocity.y = 0f; }
                else if (movement.y < 0f && !CanMoveBottomRowToTop()) { movement.y = 0f; _velocity.y = 0f; }
            }

            if (movement.sqrMagnitude > 0.000001f)
                MoveTileToDirection.Invoke(movement);

            float decay = Mathf.Pow(inertiaDamping, Time.unscaledDeltaTime * 60f);
            _velocity *= decay;
        }

        #endregion

        #region Bounds Cycling

        private void HandleCycling()
        {
            if (TopLeftTile.transform.localPosition.x < TopLeftLimit.x - 128)
            {
                if (CanMoveLeftColumnToRight())
                {
                    MoveLeftColumnToRight();
                    UpdateCorners();
                }
                else _velocity.x = 0f;
                return;
            }

            if (BottomRightTile.transform.localPosition.x > BottomRightLimit.x + 128)
            {
                if (CanMoveRightColumnToLeft())
                {
                    MoveRightColumnToLeft();
                    UpdateCorners();
                }
                else _velocity.x = 0f;
                return;
            }

            if (TopLeftTile.transform.localPosition.y > TopLeftLimit.y + 128)
            {
                if (CanMoveTopRowToBottom())
                {
                    MoveTopRowToBottom();
                    UpdateCorners();
                }
                else _velocity.y = 0f;
                return;
            }

            if (BottomRightTile.transform.localPosition.y < BottomRightLimit.y - 128)
            {
                if (CanMoveBottomRowToTop())
                {
                    MoveBottomRowToTop();
                    UpdateCorners();
                }
                else _velocity.y = 0f;
                return;
            }
        }

        private void MoveLeftColumnToRight()
        {
            TileView[] leftColumn = new TileView[gridSize.y];
            List<Vector2Int> fetch = new();

            for (int y = 0; y < gridSize.y; y++)
                leftColumn[y] = tiles[0, y];

            for (int x = 0; x < gridSize.x - 1; x++)
                for (int y = 0; y < gridSize.y; y++)
                    tiles[x, y] = tiles[x + 1, y];

            for (int y = 0; y < gridSize.y; y++)
            {
                var tile = leftColumn[y];
                var newCoord = new Vector2Int(tile.Coordinate.x + gridSize.x, tile.Coordinate.y);
                tile.UpdateCoordinate(newCoord, zoomLevel, showLabels);
                tile.transform.localPosition = GetPositionForTile(newCoord);
                tiles[gridSize.x - 1, y] = tile;

                if (MapTileManager.Instance.HasCachedTile(newCoord, out var data, showLabels))
                    tile.SetData(data);
                else fetch.Add(newCoord);
            }
            MapTileManager.Instance.StartStreamFetch(fetch, zoomLevel, showLabels);
        }

        private void MoveRightColumnToLeft()
        {
            TileView[] rightColumn = new TileView[gridSize.y];
            List<Vector2Int> fetch = new();

            for (int y = 0; y < gridSize.y; y++)
                rightColumn[y] = tiles[gridSize.x - 1, y];

            for (int x = gridSize.x - 1; x > 0; x--)
                for (int y = 0; y < gridSize.y; y++)
                    tiles[x, y] = tiles[x - 1, y];

            for (int y = 0; y < gridSize.y; y++)
            {
                var tile = rightColumn[y];
                var newCoord = new Vector2Int(tile.Coordinate.x - gridSize.x, tile.Coordinate.y);
                tile.UpdateCoordinate(newCoord, zoomLevel, showLabels);
                tile.transform.localPosition = GetPositionForTile(newCoord);
                tiles[0, y] = tile;

                if (MapTileManager.Instance.HasCachedTile(newCoord, out var data, showLabels))
                    tile.SetData(data);
                else fetch.Add(newCoord);
            }
            MapTileManager.Instance.StartStreamFetch(fetch, zoomLevel, showLabels);
        }

        private void MoveTopRowToBottom()
        {
            TileView[] topRow = new TileView[gridSize.x];
            List<Vector2Int> fetch = new();

            for (int x = 0; x < gridSize.x; x++)
                topRow[x] = tiles[x, 0];

            for (int y = 0; y < gridSize.y - 1; y++)
                for (int x = 0; x < gridSize.x; x++)
                    tiles[x, y] = tiles[x, y + 1];

            for (int x = 0; x < gridSize.x; x++)
            {
                var tile = topRow[x];
                var newCoord = new Vector2Int(tile.Coordinate.x, tile.Coordinate.y + gridSize.y);
                tile.UpdateCoordinate(newCoord, zoomLevel, showLabels);
                tile.transform.localPosition = GetPositionForTile(newCoord);
                tiles[x, gridSize.y - 1] = tile;

                if (MapTileManager.Instance.HasCachedTile(newCoord, out var data, showLabels))
                    tile.SetData(data);
                else fetch.Add(newCoord);
            }
            MapTileManager.Instance.StartStreamFetch(fetch, zoomLevel, showLabels);
        }

        private void MoveBottomRowToTop()
        {
            TileView[] bottomRow = new TileView[gridSize.x];
            List<Vector2Int> fetch = new();

            for (int x = 0; x < gridSize.x; x++)
                bottomRow[x] = tiles[x, gridSize.y - 1];

            for (int y = gridSize.y - 1; y > 0; y--)
                for (int x = 0; x < gridSize.x; x++)
                    tiles[x, y] = tiles[x, y - 1];

            for (int x = 0; x < gridSize.x; x++)
            {
                var tile = bottomRow[x];
                var newCoord = new Vector2Int(tile.Coordinate.x, tile.Coordinate.y - gridSize.y);
                tile.UpdateCoordinate(newCoord, zoomLevel, showLabels);
                tile.transform.localPosition = GetPositionForTile(newCoord);
                tiles[x, 0] = tile;

                if (MapTileManager.Instance.HasCachedTile(newCoord, out var data, showLabels))
                    tile.SetData(data);
                else fetch.Add(newCoord);
            }
            MapTileManager.Instance.StartStreamFetch(fetch, zoomLevel, showLabels);
        }

        private (int xMin, int xMax, int yMin, int yMax) GetBoundsForCurrentZoom() =>
            Utils.TileRangeForBounds(fromCoordinates, toCoordinates, zoomLevel);

        private bool CanMoveLeftColumnToRight()
        {
            var (xMin, xMax, _, _) = GetBoundsForCurrentZoom();
            for (int y = 0; y < gridSize.y; y++)
            {
                var tile = tiles[0, y];
                if (tile == null) continue;
                if (tile.Coordinate.x + gridSize.x > xMax) return false;
            }
            return true;
        }

        private bool CanMoveRightColumnToLeft()
        {
            var (xMin, _, _, _) = GetBoundsForCurrentZoom();
            for (int y = 0; y < gridSize.y; y++)
            {
                var tile = tiles[gridSize.x - 1, y];
                if (tile == null) continue;
                if (tile.Coordinate.x - gridSize.x < xMin) return false;
            }
            return true;
        }

        private bool CanMoveTopRowToBottom()
        {
            var (_, _, _, yMax) = GetBoundsForCurrentZoom();
            for (int x = 0; x < gridSize.x; x++)
            {
                var tile = tiles[x, 0];
                if (tile == null) continue;
                if (tile.Coordinate.y + gridSize.y > yMax) return false;
            }
            return true;
        }

        private bool CanMoveBottomRowToTop()
        {
            var (_, _, yMin, _) = GetBoundsForCurrentZoom();
            for (int x = 0; x < gridSize.x; x++)
            {
                var tile = tiles[x, gridSize.y - 1];
                if (tile == null) continue;
                if (tile.Coordinate.y - gridSize.y < yMin) return false;
            }
            return true;
        }

        #endregion
    }
}
