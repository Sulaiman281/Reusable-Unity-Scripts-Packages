using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using WitShells.DesignPatterns;
using WitShells.DesignPatterns.Core;

namespace WitShells.MapView
{
    #region Data Structures

    [Serializable]
    public struct Coordinates
    {
        public double Latitude;
        public double Longitude;
        public override string ToString() => $"{Latitude}, {Longitude}";
    }

    [Serializable]
    public class PlacableItems
    {
        public List<PlacableData> Items;
        public PlacableItems() => Items = new List<PlacableData>();
        public void Register(PlacableData data) => Items.Add(data);
        public void Unregister(PlacableData data) => Items.Remove(data);
    }

    [Serializable]
    public class WorldObjectMarkers
    {
        [SerializeField] protected GameObject markerContainer;
        [SerializeField] private ObjectPool<Placable> placablePool;
        public Dictionary<PlacableData, Placable> Placables;
        public bool HasMarkers => Placables != null && Placables.Count > 0;

        public void Initialize()
        {
            if (markerContainer == null)
                markerContainer = new GameObject("WorldObjectMarkers");
            markerContainer.name = "WorldObjectMarkers";

            placablePool = new ObjectPool<Placable>(() =>
            {
                var obj = new GameObject("Placable").AddComponent<Placable>();
                obj.transform.SetParent(markerContainer.transform);
                obj.gameObject.SetActive(false);
                return obj;
            });

            Placables ??= new Dictionary<PlacableData, Placable>();
        }

        public Placable GetPlacable()
        {
            placablePool ??= new ObjectPool<Placable>(() =>
            {
                var obj = new GameObject("Placable").AddComponent<Placable>();
                obj.transform.SetParent(markerContainer.transform);
                obj.gameObject.SetActive(false);
                return obj;
            });

            var placable = placablePool.Get();
            placable.gameObject.SetActive(true);
            Placables ??= new Dictionary<PlacableData, Placable>();
            Placables[placable.Data] = placable;
            return placable;
        }

        public void ReleasePlacable(Placable placable)
        {
            if (placablePool == null || placable == null) return;
            placable.gameObject.SetActive(false);
            Placables.Remove(placable.Data);
            placablePool.Release(placable);
        }
    }

    #endregion

    public class MapViewLayout : MonoBehaviour, IPointerClickHandler, IDragHandler, IScrollHandler
    {
        #region Serialized Fields

        [Header("References")][SerializeField] private Canvas parentCanvas;

        [Header("Location")]
        [SerializeField] private string locationName;
        [SerializeField] private Coordinates fromCoordinates;
        [SerializeField] private Coordinates toCoordinates;
        [SerializeField] private int zoomLevel = 15;
        [SerializeField] private bool showLabels => MapSettings.Instance.showLabels;

        [Header("Prefab")][SerializeField] private TileView tilePrefab;

        [Header("Settings")]
        [SerializeField] private int BoundsOffset = 5;
        [SerializeField] private Vector3 TopLeftLimit;
        [SerializeField] private Vector3 BottomRightLimit;

        [Tooltip("Toggle between UI Navigation or Touch Input")]
        [SerializeField] private bool useTouchInput = true;

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

        [Header("Map Markers")]
        [SerializeField] private WorldObjectMarkers worldObjectMarkers;
        [SerializeField] private PlacableItems placableItems;

        [Header("Runtime")]
        [SerializeField] private Coordinates SelectedCoordinates;

        #endregion

        #region Runtime State

        public TileView TopLeftTile;
        public TileView BottomRightTile;
        public TileView TopRightTile;
        public TileView BottomLeftTile;
        public TileView CenterTile;

        public Vector2Int CenterCoordiante;
        private bool _hasClicked = false;
        [SerializeField] private Vector2Int gridSize;
        public Vector2Int TargetClickedZoomTile => Utils.LatLonToTile(SelectedCoordinates.Latitude, SelectedCoordinates.Longitude, zoomLevel);
        public bool CanInput { get; set; } = true;
        public PlacableItems PlacableItems => placableItems;

        private ObjectPool<TileView> tilePool;
        private TileView[,] tiles;
        private Dictionary<int, Transform> zoomLayers = new();
        public UnityEvent<Vector3> MoveTileToDirection = new();
        private bool isFixedLayout = false;

        private bool _hasDragStarted = false;
        private Vector2 _lastDragPosition;
        private float _lastDragStartTime = 0f;
        private Vector3 _velocity = Vector3.zero;

        // Touch helpers
        private bool _isPinching = false;
        private float _prevTouchDistance = 0f;
        private Vector2 _prevTouchCenter = Vector2.zero;

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

        #endregion

        #region Lifecycle

        void Start()
        {
            InitializeMapSettings(MapSettings.Instance.MapFile);
            if (!EnhancedTouchSupport.enabled) EnhancedTouchSupport.Enable();

            currentZoomLevel = zoomLevel;
            GenerateLayout();

            MapTileManager.Instance.OnTileFetched.AddListener(OnTileIsFetched);
            MapTileManager.Instance.OnTilesFetched?.AddListener(OnTilesIsFetchedBatch);

            worldObjectMarkers.Initialize();
            placableItems ??= new PlacableItems();
            HandleMarkerUpdate();
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
            HandleTouchInputes();
            ApplyVelocityMovement();
            HandleMarkerUpdate();
            HandleZoomUpdate();
        }

        void FixedUpdate()
        {
            if (isFixedLayout) return;
            if (TopLeftTile == null || BottomRightTile == null || tiles == null) return;
            HandleCycling();
        }

        #endregion

        #region Initialization

        public void InitializeMapSettings(MapFile settings)
        {
            fromCoordinates = settings.TopLeft;
            toCoordinates = settings.BottomRight;
            minZoomLevel = settings.MinZoom;
            maxZoomLevel = settings.MaxZoom;
            locationName = settings.MapName;
        }

        #endregion

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

        #region Touch Input

        private void HandleTouchInputes()
        {
            if (!CanInput || !useTouchInput) return;
            if (Touchscreen.current == null)
            {
                useTouchInput = false;
                return;
            }
            if (!EnhancedTouchSupport.enabled) EnhancedTouchSupport.Enable();

            var touches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
            int touchCount = touches.Count;
            if (touchCount == 0) { _isPinching = false; return; }

            if (touchCount == 1)
            {
                var t = touches[0];
                var phase = t.phase;
                if (phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    _hasDragStarted = true;
                    _lastDragPosition = t.screenPosition;
                    _lastDragStartTime = Time.time;
                }
                else if (phase == UnityEngine.InputSystem.TouchPhase.Moved || phase == UnityEngine.InputSystem.TouchPhase.Stationary)
                {
                    if (!_hasDragStarted)
                    {
                        _hasDragStarted = true;
                        _lastDragPosition = t.screenPosition;
                        _lastDragStartTime = Time.time;
                    }
                    Vector2 delta = t.screenPosition - _lastDragPosition;
                    var direction = invertDrag ? -1 : 1;
                    var movement = delta * dragSensitivity * direction;
                    _velocity = new Vector3(movement.x, movement.y, 0f) / Mathf.Max(Time.deltaTime, 0.0001f);
                    _lastDragPosition = t.screenPosition;
                }
                else if (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    _hasDragStarted = false;
                }
                _isPinching = false;
                return;
            }

            if (touchCount >= 2)
            {
                var t0 = touches[0];
                var t1 = touches[1];
                Vector2 prevCenter = (t0.screenPosition - t0.delta + t1.screenPosition - t1.delta) * 0.5f;
                Vector2 curCenter = (t0.screenPosition + t1.screenPosition) * 0.5f;
                Vector2 centerDelta = curCenter - prevCenter;
                var direction = invertDrag ? -1 : 1;
                var panMovement = centerDelta * dragSensitivity * direction;
                _velocity = new Vector3(panMovement.x, panMovement.y, 0f) / Mathf.Max(Time.deltaTime, 0.0001f);

                float prevDist = (t0.screenPosition - t0.delta - (t1.screenPosition - t1.delta)).magnitude;
                float currDist = (t0.screenPosition - t1.screenPosition).magnitude;
                float pinchDelta = currDist - prevDist;
                float targetZoomVelocity = pinchDelta * zoomSensitivity * 0.01f;
                zoomVelocity = Mathf.Lerp(zoomVelocity, targetZoomVelocity, Time.deltaTime * 10f);

                _isPinching = true;
                _prevTouchDistance = currDist;
                _prevTouchCenter = curCenter;
            }
        }

        #endregion

        #region Zoom

        private void HandleZoomUpdate()
        {
            currentZoomLevel = Mathf.Clamp(currentZoomLevel + zoomVelocity, minZoomLevel, maxZoomLevel + .9f);
            float decay = Mathf.Pow(inertiaDamping, Time.unscaledDeltaTime * 60f);
            zoomVelocity *= decay;

            var zoom = (int)currentZoomLevel;
            if (Mathf.Abs(zoomVelocity) <= 0.01f && zoom != zoomLevel)
                SetZoomUpdate(zoom);

            var zoomDelta = currentZoomLevel - zoomLevel;
            ZoomLayer().localScale = Vector3.one * (1 + zoomDelta);
        }

        private void SetZoomUpdate(int value)
        {
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

            var (lat, lon) = _hasClicked
                ? (SelectedCoordinates.Latitude, SelectedCoordinates.Longitude)
                : Utils.TileXYToLonLat(CenterCoordiante.x, CenterCoordiante.y, zoomLevel);

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

        #endregion

        #region Marker Management

        private void HandleMarkerUpdate()
        {
            if (worldObjectMarkers == null || placableItems?.Items == null || placableItems.Items.Count == 0) return;

            foreach (var data in placableItems.Items)
            {
                bool exists = worldObjectMarkers.Placables != null && worldObjectMarkers.Placables.ContainsKey(data);
                var placable = exists ? worldObjectMarkers.Placables[data] : null;

                if (!HasWorldPositionInMapView(data, out var position))
                {
                    if (exists) worldObjectMarkers.ReleasePlacable(placable);
                    continue;
                }

                if (exists)
                {
                    placable.transform.position = position;
                    placable.UpdateScale(currentZoomLevel, maxZoomLevel);
                }
                else
                {
                    placable = worldObjectMarkers.GetPlacable();
                    placable.UpdateCoordinates(data.Coordinates, data.ZoomLevel);
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
                WitLogger.LogWarning($"Failed to generate map layout: {ex.Message}");
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

        #region Utilities

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

        #endregion

        #region Input Handlers (Pointer / Mouse when touch disabled)

        public void OnDrag(PointerEventData eventData)
        {
            if (!CanInput || useTouchInput || isFixedLayout) return;

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
            _velocity = new Vector3(movement.x, movement.y, 0) / Mathf.Max(Time.deltaTime, 0.0001f);
            _lastDragPosition = eventData.position;
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (!CanInput || useTouchInput) return;
            float scrollDelta = eventData.scrollDelta.y;
            float targetVelocity = scrollDelta * zoomSensitivity;
            zoomVelocity = Mathf.Lerp(zoomVelocity, targetVelocity, Time.deltaTime * 10f);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isFixedLayout) return;

            var cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform, eventData.position, cam, out Vector2 localPoint);

            int tileX = Mathf.FloorToInt((localPoint.x + (gridSize.x * 256f / 2)) / 256f);
            int tileY = Mathf.FloorToInt((-localPoint.y + (gridSize.y * 256f / 2)) / 256f);

            if (tileX >= 0 && tileX < gridSize.x && tileY >= 0 && tileY < gridSize.y)
            {
                var clickedTile = tiles[tileX, tileY];
                Utils.GetNormalizedPositionInTile(clickedTile.RectTransform, localPoint, transform, out float normX, out float normY);
                var (lat, lon) = Utils.TileNormalizedToLatLon(clickedTile.Coordinate.x, clickedTile.Coordinate.y, zoomLevel, normX, normY);
                SelectedCoordinates = new Coordinates { Latitude = lat, Longitude = lon };
                _hasClicked = true;
                GUIUtility.systemCopyBuffer = SelectedCoordinates.ToString();
                WitLogger.Log($"Selected Coordinates: {SelectedCoordinates} (copied) {normX}, {normY}");
            }
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

        #region Editor Methods

#if UNITY_EDITOR
        [Header("WitLogger Downloader")][SerializeField] private DownloaderTiles downloaderTiles;

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