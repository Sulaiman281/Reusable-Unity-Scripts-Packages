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
                    Spawn(obj, data, out var placableComponent);
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
            var placableComponent = obj.GetComponent<MonoBehaviour>() as IPlacable;
            Placables[data.Id] = placableComponent;

            placable = placableComponent;

            try
            {
                var placableData = obj.GetComponent<MonoBehaviour>() as IPlacableData<object>;
                if (placableData != null)
                {
                    object customData = null;
                    if (!string.IsNullOrEmpty(data.Payload))
                    {
                        var settings = new Newtonsoft.Json.JsonSerializerSettings
                        {
                            TypeNameHandling = Newtonsoft.Json.TypeNameHandling.All
                        };
                        customData = Newtonsoft.Json.JsonConvert.DeserializeObject<object>(data.Payload, settings);
                    }
                    placableData.Initialize(data, customData);
                }
            }
            catch
            {
                WitLogger.LogWarning($"Failed to deserialize custom data for Placable with Key: {data.Key}");
            }

            placableComponent.AddedToMapView();
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
            if (_placablePool == null || placable == null) return;
            placable.GameObject.SetActive(false);
            Placables.Remove(placable.Data.Id);
            placable.RemovedFromMapView();
            _placablePool[placable.Data.Key].Release(placable.GameObject);
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
        /// Returns the geographic coordinates (lat, lon) at the exact center of the map view.
        /// Uses the current CenterTile and current zoom to compute the center point (norm 0.5, 0.5).
        /// </summary>
        public Coordinates GetCenterCoordinates()
        {
            int z = Mathf.RoundToInt(currentZoomLevel);

            // Prefer CenterTile when available
            if (CenterTile != null)
            {
                var (lat, lon) = Utils.TileNormalizedToLatLon(CenterTile.Coordinate.x, CenterTile.Coordinate.y, z, 0.5f, 0.5f);
                return new Coordinates { Latitude = lat, Longitude = lon };
            }

            // Fallback to CenterCoordiante if CenterTile not assigned yet
            if (CenterCoordiante != Vector2Int.zero)
            {
                var (lat, lon) = Utils.TileNormalizedToLatLon(CenterCoordiante.x, CenterCoordiante.y, z, 0.5f, 0.5f);
                return new Coordinates { Latitude = lat, Longitude = lon };
            }

            // If neither available, estimate from configured bounds mid-point
            var midLat = (fromCoordinates.Latitude + toCoordinates.Latitude) * 0.5;
            var midLon = (fromCoordinates.Longitude + toCoordinates.Longitude) * 0.5;
            return new Coordinates { Latitude = midLat, Longitude = midLon };
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