using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using WitShells.DesignPatterns;
using WitShells.DesignPatterns.Core;

namespace WitShells.MapView
{
    [Serializable]
    public struct Coordinates
    {
        public double Latitude;
        public double Longitude;

        public override string ToString()
        {
            return $"{Latitude}, {Longitude}";
        }
    }

    public class MapViewLayout : MonoBehaviour, IPointerClickHandler, IDragHandler, IScrollHandler
    {

        [Header("References")]
        [SerializeField] private Canvas parentCanvas;

        [Header("Location")]
        [SerializeField] private string locationName;
        [SerializeField] private Coordinates fromCoordinates;
        [SerializeField] private Coordinates toCoordinates;
        [SerializeField] private int zoomLevel = 15;
        [SerializeField] private bool showLabels => MapSettings.Instance.showLabels;

        [Header("Prefab")]
        [SerializeField] private TileView tilePrefab;

        [Header("Settings")]
        [SerializeField] private int BoundsOffset = 5;
        [SerializeField] private Vector3 TopLeftLimit;
        [SerializeField] private Vector3 BottomRightLimit;

        [Header("Drag Settings")]
        [SerializeField] private float dragSensitivity = 1f;
        [SerializeField] private bool invertDrag = false;
        [SerializeField] private float dragTimeOut = .25f;
        [SerializeField] private float inertiaDamping = 0.9f;

        [Header("Zoom Settings")]
        [SerializeField] private int minZoomLevel = 12;
        [SerializeField] private int maxZoomLevel = 20;
        [SerializeField] private float zoomSensitivity = .1f;
        [SerializeField] private float currentZoomLevel;

        [SerializeField] private float zoomVelocity = 0f;


        [Header("Runtime")]
        [SerializeField] private Coordinates SelectedCoordinates;
        public TileView TopLeftTile;
        public TileView BottomRightTile;
        public TileView TopRightTile;
        public TileView BottomLeftTile;
        public TileView CenterTile;

        public Vector2Int CenterCoordiante;
        private bool _hasClicked = false;
        [SerializeField] private Vector2Int gridSize;

        public Vector2Int TargetClickedZoomTile => Utils.LatLonToTile(SelectedCoordinates.Latitude, SelectedCoordinates.Longitude, zoomLevel);

        private ObjectPool<TileView> tilePool;
        public ObjectPool<TileView> Pool
        {
            get
            {
                tilePool ??= new ObjectPool<TileView>(() =>
                {
                    var obj = Instantiate(tilePrefab, transform);
                    obj.gameObject.SetActive(false);
                    return obj;
                });
                return tilePool;
            }
        }

        private TileView[,] tiles;

        private Dictionary<int, Transform> zoomLayers = new Dictionary<int, Transform>();
        public UnityEvent<Vector3> MoveTileToDirection = new();
        private bool isFixedLayout = false;

        private bool _hasDragStarted = false;
        private Vector2 _lastDragPosition;
        private float _lastDragStartTime = 0f;
        private Vector3 _velocity = Vector3.zero;

        void Start()
        {
            currentZoomLevel = zoomLevel;
            GenerateLayout();

            // Subscribe to both single-tile and batch tile events for compatibility.
            // Batch updates are preferred for performance when available.
            MapTileManager.Instance.OnTileFetched.AddListener(OnTileIsFetched);
            MapTileManager.Instance.OnTilesFetched?.AddListener(OnTilesIsFetchedBatch);
        }

        void OnDestroy()
        {
            if (MapTileManager.Instance != null)
            {
                MapTileManager.Instance.OnTileFetched.RemoveListener(OnTileIsFetched);
                if (MapTileManager.Instance.OnTilesFetched != null)
                    MapTileManager.Instance.OnTilesFetched.RemoveListener(OnTilesIsFetchedBatch);
            }
        }


        void Update()
        {
            if (_velocity.magnitude > 0.01f)
            {
                // Compute the movement that would be applied this frame
                Vector3 movement = _velocity * Time.unscaledDeltaTime;

                // If we have corner tiles available, ensure movement won't immediately push the layout
                // past the allowed bounds. For any axis that would exceed bounds, stop movement on that axis.
                if (TopLeftTile != null && BottomRightTile != null)
                {
                    // Horizontal movement: positive X = move right, negative X = move left
                    if (movement.x > 0f)
                    {
                        // moving right -> would trigger MoveRightColumnToLeft when exceeding right bound
                        if (!CanMoveRightColumnToLeft())
                        {
                            movement.x = 0f;
                            _velocity.x = 0f;
                        }
                    }
                    else if (movement.x < 0f)
                    {
                        // moving left -> would trigger MoveLeftColumnToRight when exceeding left bound
                        if (!CanMoveLeftColumnToRight())
                        {
                            movement.x = 0f;
                            _velocity.x = 0f;
                        }
                    }

                    // Vertical movement: positive Y = move up, negative Y = move down
                    if (movement.y > 0f)
                    {
                        // moving up -> would trigger MoveTopRowToBottom when exceeding top bound
                        if (!CanMoveTopRowToBottom())
                        {
                            movement.y = 0f;
                            _velocity.y = 0f;
                        }
                    }
                    else if (movement.y < 0f)
                    {
                        // moving down -> would trigger MoveBottomRowToTop when exceeding bottom bound
                        if (!CanMoveBottomRowToTop())
                        {
                            movement.y = 0f;
                            _velocity.y = 0f;
                        }
                    }
                }

                // Apply movement if any axis is still allowed
                if (movement.sqrMagnitude > 0.000001f)
                {
                    MoveTileToDirection.Invoke(movement);
                }

                // Framerate-independent damping (inertiaDamping ~ 0.9 means ~10% decay per 60fps frame)
                float decay = Mathf.Pow(inertiaDamping, Time.unscaledDeltaTime * 60f);
                _velocity *= decay;
            }

            HandleZoomUpdate();
        }

        private void OnTileIsFetched(Vector2Int coordinate, Tile tile)
        {
            // Backwards-compatible single-tile handler
            if (tile == null) return;
            TileView tv = GetTileAtCoordinate(coordinate);
            if (tv == null) return;
            tv.SetData(tile);
        }

        private void OnTilesIsFetchedBatch(List<Tile> fetchedTiles)
        {
            if (fetchedTiles == null || fetchedTiles.Count == 0) return;
            if (this.tiles == null) return;

            foreach (var tile in fetchedTiles)
            {
                if (tile == null) continue;
                var coord = new Vector2Int(tile.TileX, tile.TileY);
                var tv = GetTileAtCoordinate(coord);
                if (tv == null) continue;
                tv.SetData(tile);
            }
        }

        private void HandleZoomUpdate()
        {
            currentZoomLevel = Mathf.Clamp(currentZoomLevel + zoomVelocity, minZoomLevel, maxZoomLevel + .9f);

            float decay = Mathf.Pow(inertiaDamping, Time.unscaledDeltaTime * 60f);
            zoomVelocity *= decay;

            var zoom = (int)currentZoomLevel;

            if (Mathf.Abs(zoomVelocity) <= 0.01f)
            {
                if (zoom != zoomLevel)
                {
                    SetZoomUpdate(zoom);
                }
            }

            var zoomDelta = currentZoomLevel - zoomLevel;
            ZoomLayer().localScale = Vector3.one * (1 + zoomDelta);
        }

        void FixedUpdate()
        {
            if (isFixedLayout) return;

            if (TopLeftTile == null || BottomRightTile == null || tiles == null) return;

            // Left-Right cycling using 2D array
            if (TopLeftTile.transform.localPosition.x < TopLeftLimit.x - 128)
            {
                // Move left column to right side if allowed by bounds
                if (CanMoveLeftColumnToRight())
                {
                    MoveLeftColumnToRight();
                    UpdateCorners();
                }
                else
                {
                    // Hit horizontal right bound - stop horizontal motion
                    _velocity.x = 0f;
                }
                return;
            }

            if (BottomRightTile.transform.localPosition.x > BottomRightLimit.x + 128)
            {
                // Move right column to left side if allowed by bounds
                if (CanMoveRightColumnToLeft())
                {
                    MoveRightColumnToLeft();
                    UpdateCorners();
                }
                else
                {
                    // Hit horizontal left bound - stop horizontal motion
                    _velocity.x = 0f;
                }
                return;
            }

            // Top-Bottom cycling using 2D array
            if (TopLeftTile.transform.localPosition.y > TopLeftLimit.y + 128)
            {
                // Move top row to bottom if allowed by bounds
                if (CanMoveTopRowToBottom())
                {
                    MoveTopRowToBottom();
                    UpdateCorners();
                }
                else
                {
                    // Hit vertical top bound - stop vertical motion
                    _velocity.y = 0f;
                }
                return;
            }

            if (BottomRightTile.transform.localPosition.y < BottomRightLimit.y - 128)
            {
                // Move bottom row to top if allowed by bounds
                if (CanMoveBottomRowToTop())
                {
                    MoveBottomRowToTop();
                    UpdateCorners();
                }
                else
                {
                    // Hit vertical bottom bound - stop vertical motion
                    _velocity.y = 0f;
                }
                return;
            }
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

        [ContextMenu("Generate Layout")]
        public void GenerateLayout()
        {
            // clear existing tiles
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
                // ignore
                WitLogger.LogWarning($"Failed to generate map layout: {ex.Message}");
            }
        }

        private void SetZoomUpdate(int value)
        {
            // clear current zoom layer
            foreach (Transform child in ZoomLayer())
            {
                if (child.TryGetComponent<TileView>(out var tile))
                {
                    tile.gameObject.SetActive(false);
                    MoveTileToDirection.RemoveListener(tile.MoveTo);
                    Pool.Release(tile);
                }
            }
            ZoomLayer().gameObject.SetActive(false);

            var (lat, lon) = _hasClicked ? (SelectedCoordinates.Latitude, SelectedCoordinates.Longitude) :
                Utils.TileXYToLonLat(CenterCoordiante.x, CenterCoordiante.y, zoomLevel);

            // increase the zoom
            zoomLevel = Mathf.Clamp(value, minZoomLevel, maxZoomLevel);
            ZoomLayer().gameObject.SetActive(true);
            ZoomLayer().localScale = Vector3.one;

            CenterCoordiante = Utils.LatLonToTile(lat, lon, zoomLevel);

            CenterTile = null;
            if (IsLocationBoundsLessThenScreen())
            {
                GenerateAllTiles();
                isFixedLayout = true;
            }
            else
            {
                GenerateScreenFillingTiles();
                isFixedLayout = false;
            }
        }

        private void GenerateAllTiles()
        {
            (int xMin, int xMax, int yMin, int yMax) = Utils.TileRangeForBounds(fromCoordinates, toCoordinates, zoomLevel);

            this.CenterCoordiante = Utils.TileCenterForBounds(fromCoordinates, toCoordinates, zoomLevel);

            gridSize = new Vector2Int(xMax - xMin + 1, yMax - yMin + 1);

            tiles = new TileView[gridSize.x, gridSize.y];

            List<Vector2Int> allTiles = new List<Vector2Int>();

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

            // set limits
            TopLeftLimit = GetPositionForTile(new Vector2Int(xMin, yMin));
            BottomRightLimit = GetPositionForTile(new Vector2Int(xMax, yMax));

        }

        private void AddTile(Vector2Int coordinate, out TileView tile)
        {
            tile = Pool.Get();
            tile.transform.SetParent(ZoomLayer());
            tile.name = $"Tile_{coordinate.x}_{coordinate.y}";
            tile.gameObject.SetActive(true);
            tile.UpdateCoordinate(coordinate, zoomLevel, showLabels);

            // Register Events
            MoveTileToDirection.AddListener(tile.MoveTo);
            // tile.OnMoved += OnTileMoved;

            tile.transform.localScale = Vector3.one;

            var position = GetPositionForTile(coordinate);
            tile.transform.localPosition = position;
        }

        public Vector3 GetPositionForTile(Vector2Int coordinate)
        {
            if (CenterTile == null)
            {
                var pos = coordinate - CenterCoordiante;
                return new Vector3(pos.x * 256, pos.y * -1 * 256, 0);
            }
            var position = coordinate - CenterTile.Coordinate;
            var basePosition = new Vector3(position.x * 256, position.y * -1 * 256, 0);
            return basePosition + CenterTile.transform.localPosition;
        }

        private bool IsLocationBoundsLessThenScreen()
        {
            var (col, row) = TileCount();
            var totalCols = Utils.TotalHorizontalBoundsTiles(fromCoordinates, toCoordinates, zoomLevel);
            var totalRows = Utils.TotalVerticalBoundsTiles(fromCoordinates, toCoordinates, zoomLevel);
            return totalCols < col && totalRows < row;
        }

        private void GenerateScreenFillingTiles()
        {
            Vector2Int centerTile = CenterCoordiante;

            this.CenterCoordiante = centerTile;

            var gridSize = TileCount();

            int startX = centerTile.x - gridSize.Col / 2;
            int endX = centerTile.x + gridSize.Col / 2;
            int startY = centerTile.y - gridSize.Row / 2;
            int endY = centerTile.y + gridSize.Row / 2;

            startX -= BoundsOffset;
            endX += BoundsOffset;
            startY -= BoundsOffset;
            endY += BoundsOffset;

            this.gridSize = new Vector2Int(endX - startX + 1, endY - startY + 1);

            // Initialize the 2D array for dynamic layout too
            tiles = new TileView[this.gridSize.x, this.gridSize.y];


            TopLeftLimit = GetPositionForTile(new Vector2Int(startX, startY));
            BottomRightLimit = GetPositionForTile(new Vector2Int(endX, endY));

            List<Vector2Int> allTiles = new List<Vector2Int>();

            int arrayX = 0;
            WitLogger.Log($"Generating tiles for area: {startX}, {startY} to {endX}, {endY}");
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
            WitLogger.Log($"Generated {arrayX} x {tiles.GetLength(1)} tiles.");

            MapTileManager.Instance.StartStreamFetch(allTiles, zoomLevel, showLabels);

            UpdateCorners();
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
            if (startingTile.x < 0 || startingTile.y < 0 || startingTile.x >= tiles.GetLength(0) || startingTile.y >= tiles.GetLength(1))
            {
                return null;
            }
            return tiles[startingTile.x, startingTile.y];
        }


        #region Input Handlers

        public void OnDrag(PointerEventData eventData)
        {
            if (isFixedLayout) return;

            if (!_hasDragStarted)
            {
                _hasDragStarted = true;
                _lastDragPosition = eventData.position;
                _lastDragStartTime = Time.time;
                return;
            }

            if (Time.time - _lastDragStartTime > dragTimeOut)
            {
                _hasDragStarted = false;
                return;
            }

            Vector2 delta = eventData.position - _lastDragPosition;

            var direction = invertDrag ? -1 : 1;
            var movement = delta * dragSensitivity * direction;
            _velocity = new Vector3(movement.x, movement.y, 0) / Time.deltaTime;
        }

        public void OnScroll(PointerEventData eventData)
        {
            WitLogger.Log($"Scroll delta: {eventData.scrollDelta} - position {eventData.position}");

            float scrollDelta = eventData.scrollDelta.y;
            float targetVelocity = scrollDelta * zoomSensitivity;
            zoomVelocity = Mathf.Lerp(zoomVelocity, targetVelocity, Time.deltaTime * 10f);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isFixedLayout) return;

            var cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;

            // Convert screen position to local position
            RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform, eventData.position, cam, out Vector2 localPoint);


            // Calculate the tile position
            int tileX = Mathf.FloorToInt((localPoint.x + (gridSize.x * 256f / 2)) / 256f);
            int tileY = Mathf.FloorToInt((-localPoint.y + (gridSize.y * 256f / 2)) / 256f);

            // Ensure we're within bounds
            if (tileX >= 0 && tileX < gridSize.x && tileY >= 0 && tileY < gridSize.y)
            {
                var clickedTile = tiles[tileX, tileY];

                // Get the tile's actual position and size (256x256 pixels)
                Vector3 tileWorldPos = clickedTile.transform.localPosition;
                float tileSize = 256f; // Standard tile size

                // Calculate the tile's bounds in local space
                float tileLeft = tileWorldPos.x - tileSize / 2f;
                float tileRight = tileWorldPos.x + tileSize / 2f;
                float tileTop = tileWorldPos.y + tileSize / 2f;
                float tileBottom = tileWorldPos.y - tileSize / 2f;

                // Calculate normalized position within the tile (0,0 to 1,1)
                float normX = (localPoint.x - tileLeft) / tileSize;
                float normY = (tileTop - localPoint.y) / tileSize; // Invert Y since Unity Y-up vs tile Y-down

                // Clamp to [0,1] to handle any floating point precision issues
                normX = Mathf.Clamp01(normX);
                normY = Mathf.Clamp01(normY);

                WitLogger.Log($"Clicked On: {localPoint} - Tile bounds: ({tileLeft},{tileBottom}) to ({tileRight},{tileTop})");
                WitLogger.Log($"Normalized position: ({normX:F4}, {normY:F4})");
                WitLogger.Log($"Clicked on tile: {clickedTile.name} at coordinate {clickedTile.Coordinate}");

                var (lat, lon) = Utils.TileNormalizedToLatLon(clickedTile.Coordinate.x, clickedTile.Coordinate.y, zoomLevel, normX, normY);
                SelectedCoordinates = new Coordinates { Latitude = lat, Longitude = lon };

                _hasClicked = true;

                GUIUtility.systemCopyBuffer = SelectedCoordinates.ToString();
                WitLogger.Log($"Selected Coordinates: {SelectedCoordinates} (copied to clipboard)");
            }
        }

        #endregion

        private void MoveLeftColumnToRight()
        {
            // Store the left column tiles
            TileView[] leftColumn = new TileView[gridSize.y];
            List<Vector2Int> allTiles = new List<Vector2Int>();
            for (int y = 0; y < gridSize.y; y++)
            {
                leftColumn[y] = tiles[0, y];
            }

            // Shift all columns to the left
            for (int x = 0; x < gridSize.x - 1; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    tiles[x, y] = tiles[x + 1, y];
                }
            }

            // Place left column on the right side with new coordinates
            for (int y = 0; y < gridSize.y; y++)
            {
                var tile = leftColumn[y];
                var newCoord = new Vector2Int(tile.Coordinate.x + gridSize.x, tile.Coordinate.y);

                tile.UpdateCoordinate(newCoord, zoomLevel, showLabels);
                tile.transform.localPosition = GetPositionForTile(newCoord);
                tiles[gridSize.x - 1, y] = tile;

                if (MapTileManager.Instance.HasCachedTile(newCoord, out var data, showLabels))
                {
                    tile.SetData(data);
                }
                else
                {
                    allTiles.Add(newCoord);
                }
            }
            MapTileManager.Instance.StartStreamFetch(allTiles, zoomLevel, showLabels);
        }

        private void MoveRightColumnToLeft()
        {
            // Store the right column tiles
            TileView[] rightColumn = new TileView[gridSize.y];
            List<Vector2Int> allTiles = new List<Vector2Int>();

            for (int y = 0; y < gridSize.y; y++)
            {
                rightColumn[y] = tiles[gridSize.x - 1, y];
            }

            // Shift all columns to the right
            for (int x = gridSize.x - 1; x > 0; x--)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    tiles[x, y] = tiles[x - 1, y];
                }
            }

            // Place right column on the left side with new coordinates
            for (int y = 0; y < gridSize.y; y++)
            {
                var tile = rightColumn[y];
                var newCoord = new Vector2Int(tile.Coordinate.x - gridSize.x, tile.Coordinate.y);
                // tile.Coordinate = newCoord;
                tile.UpdateCoordinate(newCoord, zoomLevel, showLabels);
                tile.transform.localPosition = GetPositionForTile(newCoord);
                tiles[0, y] = tile;

                if (MapTileManager.Instance.HasCachedTile(newCoord, out var data, showLabels))
                {
                    tile.SetData(data);
                }
                else
                {
                    allTiles.Add(newCoord);
                }
            }
            MapTileManager.Instance.StartStreamFetch(allTiles, zoomLevel, showLabels);
        }

        private void MoveTopRowToBottom()
        {
            // Store the top row tiles
            TileView[] topRow = new TileView[gridSize.x];
            List<Vector2Int> allTiles = new List<Vector2Int>();

            for (int x = 0; x < gridSize.x; x++)
            {
                topRow[x] = tiles[x, 0];
            }

            // Shift all rows up
            for (int y = 0; y < gridSize.y - 1; y++)
            {
                for (int x = 0; x < gridSize.x; x++)
                {
                    tiles[x, y] = tiles[x, y + 1];
                }
            }

            // Place top row at the bottom with new coordinates
            for (int x = 0; x < gridSize.x; x++)
            {
                var tile = topRow[x];
                var newCoord = new Vector2Int(tile.Coordinate.x, tile.Coordinate.y + gridSize.y);
                // tile.Coordinate = newCoord;
                tile.UpdateCoordinate(newCoord, zoomLevel, showLabels);
                tile.transform.localPosition = GetPositionForTile(newCoord);
                tiles[x, gridSize.y - 1] = tile;

                if (MapTileManager.Instance.HasCachedTile(newCoord, out var data, showLabels))
                {
                    tile.SetData(data);
                }
                else
                {
                    allTiles.Add(newCoord);
                }
            }

            MapTileManager.Instance.StartStreamFetch(allTiles, zoomLevel, showLabels);
        }

        private void MoveBottomRowToTop()
        {
            // Store the bottom row tiles
            TileView[] bottomRow = new TileView[gridSize.x];
            List<Vector2Int> allTiles = new List<Vector2Int>();

            for (int x = 0; x < gridSize.x; x++)
            {
                bottomRow[x] = tiles[x, gridSize.y - 1];
            }

            // Shift all rows down
            for (int y = gridSize.y - 1; y > 0; y--)
            {
                for (int x = 0; x < gridSize.x; x++)
                {
                    tiles[x, y] = tiles[x, y - 1];
                }
            }

            // Place bottom row at the top with new coordinates
            for (int x = 0; x < gridSize.x; x++)
            {
                var tile = bottomRow[x];
                var newCoord = new Vector2Int(tile.Coordinate.x, tile.Coordinate.y - gridSize.y);
                // tile.Coordinate = newCoord;
                tile.UpdateCoordinate(newCoord, zoomLevel, showLabels);
                tile.transform.localPosition = GetPositionForTile(newCoord);
                tiles[x, 0] = tile;

                if (MapTileManager.Instance.HasCachedTile(newCoord, out var data, showLabels))
                {
                    tile.SetData(data);
                }
                else
                {
                    allTiles.Add(newCoord);
                }
            }

            MapTileManager.Instance.StartStreamFetch(allTiles, zoomLevel, showLabels);
        }

        // --- Boundary checks ---
        private (int xMin, int xMax, int yMin, int yMax) GetBoundsForCurrentZoom()
        {
            return Utils.TileRangeForBounds(fromCoordinates, toCoordinates, zoomLevel);
        }

        private bool CanMoveLeftColumnToRight()
        {
            var (xMin, xMax, yMin, yMax) = GetBoundsForCurrentZoom();
            // The left column tiles will be moved by +gridSize.x; ensure the new X does not exceed xMax
            for (int y = 0; y < gridSize.y; y++)
            {
                var tile = tiles[0, y];
                if (tile == null) continue;
                int newX = tile.Coordinate.x + gridSize.x;
                if (newX > xMax) return false;
            }
            return true;
        }

        private bool CanMoveRightColumnToLeft()
        {
            var (xMin, xMax, yMin, yMax) = GetBoundsForCurrentZoom();
            for (int y = 0; y < gridSize.y; y++)
            {
                var tile = tiles[gridSize.x - 1, y];
                if (tile == null) continue;
                int newX = tile.Coordinate.x - gridSize.x;
                if (newX < xMin) return false;
            }
            return true;
        }

        private bool CanMoveTopRowToBottom()
        {
            var (xMin, xMax, yMin, yMax) = GetBoundsForCurrentZoom();
            for (int x = 0; x < gridSize.x; x++)
            {
                var tile = tiles[x, 0];
                if (tile == null) continue;
                int newY = tile.Coordinate.y + gridSize.y;
                if (newY > yMax) return false;
            }
            return true;
        }

        private bool CanMoveBottomRowToTop()
        {
            var (xMin, xMax, yMin, yMax) = GetBoundsForCurrentZoom();
            for (int x = 0; x < gridSize.x; x++)
            {
                var tile = tiles[x, gridSize.y - 1];
                if (tile == null) continue;
                int newY = tile.Coordinate.y - gridSize.y;
                if (newY < yMin) return false;
            }
            return true;
        }

        #region Editor Methods

#if UNITY_EDITOR

        [Header("WitLogger Downloader")]
        [SerializeField] private DownloaderTiles downloaderTiles;

        [ContextMenu("Download Map File")]
        public void DownloadMapFile()
        {
            var mapFile = new MapFile
            {
                MapName = locationName,
                TopLeft = fromCoordinates,
                BottomRight = toCoordinates,
                MinZoom = minZoomLevel,
                MaxZoom = maxZoomLevel
            };

            downloaderTiles = new DownloaderTiles(mapFile);
            downloaderTiles.DownloadMapFile();
        }

#endif

        #endregion
    }
}