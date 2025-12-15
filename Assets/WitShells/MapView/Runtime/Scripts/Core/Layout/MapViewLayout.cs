using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
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
        [Header("References")]
        [SerializeField] protected PlacablePrefabCatalog placablePrefabCatalog;

        [SerializeField] protected GameObject markerContainer;
        private Dictionary<string, ObjectPool<GameObject>> _placablePool;
        public Dictionary<string, IPlacable> Placables;
        public bool HasMarkers => Placables != null && Placables.Count > 0;

        public void Initialize(MapViewLayout mapViewLayout)
        {
            _placablePool = new Dictionary<string, ObjectPool<GameObject>>();
            Placables ??= new Dictionary<string, IPlacable>();

            if (markerContainer == null)
                markerContainer = new GameObject("WorldObjectMarkers");
            else
            {
                // check existing children and add to pool
                foreach (Transform child in markerContainer.transform)
                {
                    if (child.TryGetComponent<MonoBehaviour>(out var mb) && mb is IPlacable placable)
                    {
                        Placables[placable.Data.Id] = placable;
                        mapViewLayout.PlacableItems.Register(placable.Data);
                        _placablePool.TryAdd(placable.Data.Key, new ObjectPool<GameObject>(() => placable.GameObject));
                    }
                }
            }
            markerContainer.name = "WorldObjectMarkers";


        }

        public IPlacable GetPlacable(PlacableData data)
        {
            if (string.IsNullOrEmpty(data.Key))
            {
                throw new ArgumentException("PlacableData Key cannot be null or empty.");
            }

            if (!_placablePool.TryGetValue(data.Key, out var pool))
            {
                pool = new ObjectPool<GameObject>(() =>
                {
                    var prefab = placablePrefabCatalog.GetPrefabByKey(data.Key);
                    if (prefab == null)
                    {
                        throw new ArgumentException($"No prefab found for key: {data.Key}");
                    }
                    var obj = UnityEngine.Object.Instantiate(prefab, markerContainer.transform);
                    obj.SetActive(false);
                    return obj;
                });
                _placablePool[data.Key] = pool;
            }

            var placable = pool.Get();
            placable.SetActive(true);
            Spawn(placable, data, out var placableComponent);
            return placableComponent;
        }

        private void Spawn(GameObject obj, PlacableData data, out IPlacable placable)
        {
            placable = obj.GetComponent<IPlacable>();
            if (placable == null)
            {
                throw new InvalidOperationException($"Prefab for key '{data.Key}' must have a component implementing IPlacable.");
            }

            Placables[data.Id] = placable;

            placable.Initialize(data, data.Payload);
            placable.AddedToMapView();
        }

        public bool HasPlacableByData(PlacableData data, out IPlacable placable)
        {
            if (Placables != null && Placables.TryGetValue(data.Id, out placable))
                return true;
            placable = null;
            return false;
        }

        public void ReleasePlacable(IPlacable placable)
        {
            if (placable == null)
            {
                WitLogger.LogWarning("ReleasePlacable: placable is null");
                return;
            }

            var data = placable.Data;
            if (data == null)
            {
                WitLogger.LogWarning("ReleasePlacable: placable.Data is null; deactivating without pooling");
                placable.GameObject.SetActive(false);
                return;
            }

            if (string.IsNullOrEmpty(data.Key))
            {
                WitLogger.LogWarning($"ReleasePlacable: invalid Key for placable Id={data.Id}; deactivating without pooling");
                Placables?.Remove(data.Id);
                placable.RemovedFromMapView();
                placable.GameObject.SetActive(false);
                return;
            }

            if (_placablePool == null || !_placablePool.TryGetValue(data.Key, out var pool))
            {
                WitLogger.LogWarning($"ReleasePlacable: no pool found for Key={data.Key}; deactivating without pooling");
                Placables?.Remove(data.Id);
                placable.RemovedFromMapView();
                placable.GameObject.SetActive(false);
                return;
            }

            // Normal release path
            Placables?.Remove(data.Id);
            placable.RemovedFromMapView();
            placable.GameObject.SetActive(false);
            pool.Release(placable.GameObject);
        }
    }

    #endregion

    public partial class MapViewLayout : MonoBehaviour, IPointerClickHandler, IDragHandler, IScrollHandler
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

        [Header("Events")]
        public UnityEvent<Coordinates> OnLocationSelected;
        public UnityEvent<Vector3> OnPositionSelected;

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
        public bool CanInput { get; set; } = true;
        public PlacableItems PlacableItems => placableItems;

        public int ZoomLevel => Mathf.RoundToInt(currentZoomLevel);

        private ObjectPool<TileView> tilePool;
        private TileView[,] tiles;
        private Dictionary<int, Transform> zoomLayers = new();
        public UnityEvent<Vector3> MoveTileToDirection = new();
        private bool isFixedLayout = false;

        private bool _hasDragStarted = false;
        private Vector2 _lastDragPosition;
        private float _lastDragStartTime = 0f;
        private Vector3 _velocity = Vector3.zero;

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

            worldObjectMarkers.Initialize(this);
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
            if (_velocity.magnitude > 0.01f)
            {
                HandleMarkerUpdate();
            }
            HandleZoomUpdate();
        }

        void FixedUpdate()
        {
            if (isFixedLayout) return;
            if (TopLeftTile == null || BottomRightTile == null || tiles == null) return;
            HandleCycling();
        }

        #endregion

        #region Queries

        /// <summary>
        /// Sets the map center to the given geographic coordinate at the current zoom.
        /// Optionally clamps the input to the configured geographic bounds and regenerates tiles immediately.
        /// </summary>
        /// <param name="coordinates">Target latitude/longitude.</param>
        /// <param name="clampToBounds">Clamp the coordinate to [TopLeft, BottomRight] bounds.</param>
        /// <param name="instantLoad">If true, clears and rebuilds tiles around the new center immediately.</param>
        public void SetCenterCoordinate(Coordinates coordinates, bool clampToBounds = true, bool instantLoad = true)
        {
            // Clamp to configured geographic bounds if requested
            if (clampToBounds)
            {
                double minLat = Math.Min(fromCoordinates.Latitude, toCoordinates.Latitude);
                double maxLat = Math.Max(fromCoordinates.Latitude, toCoordinates.Latitude);
                double minLon = Math.Min(fromCoordinates.Longitude, toCoordinates.Longitude);
                double maxLon = Math.Max(fromCoordinates.Longitude, toCoordinates.Longitude);

                coordinates.Latitude = Mathf.Clamp((float)coordinates.Latitude, (float)minLat, (float)maxLat);
                coordinates.Longitude = Mathf.Clamp((float)coordinates.Longitude, (float)minLon, (float)maxLon);
            }

            SelectedCoordinates = coordinates;

            // Convert to tile center at current zoom level
            var targetTile = Utils.LatLonToTile(coordinates.Latitude, coordinates.Longitude, zoomLevel);
            CenterCoordiante = targetTile;

            if (!instantLoad)
                return;

            // Clear existing tiles and rebuild around new center
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
                    // Bounds area is smaller than the view; load all tiles within bounds
                    GenerateAllTiles();
                }
                else
                {
                    // Rebuild a screen-filling grid centered on the requested tile
                    GenerateScreenFillingTiles();
                }

                // Update markers to reflect new tile positions
                HandleMarkerUpdate();
            }
            catch (Exception ex)
            {
                WitLogger.LogWarning($"SetCenterCoordinate failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Convenience overload accepting raw latitude/longitude.
        /// </summary>
        public void SetCenterCoordinate(double latitude, double longitude, bool clampToBounds = true, bool instantLoad = true)
            => SetCenterCoordinate(new Coordinates { Latitude = latitude, Longitude = longitude }, clampToBounds, instantLoad);

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