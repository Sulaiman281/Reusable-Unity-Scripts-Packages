using UnityEngine;
using UnityEngine.EventSystems;
using WitShells.DesignPatterns;

namespace WitShells.MapView
{
    public interface IPlacable
    {
        PlacableData Data { get; }
        GameObject GameObject { get; }
        void UpdateCoordinates(Coordinates newCoordinates, float newZoomLevel);
        void UpdateScale(float currentZoomLevel, float maxZoomLevel);
    }

    public abstract class PlacableBase<TData> : Draggable, IPlacableData<TData>, IPlacable, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Data")]
        [SerializeField] protected PlacableData placableData;
        protected TData customData;

        public PlacableData Data => placableData;
        public TData CustomData => customData;
        public GameObject GameObject => this.gameObject;

        private float _holdTime;

        public virtual void Initialize(PlacableData data, TData customData)
        {
            placableData = data;
            this.customData = customData;
            UpdateFromData();
            CanDrag = MapSettings.Instance.CanDragMarkers;
        }

        private void OnEnable()
        {
            MapSettings.OnDragSettingsChanged += OnDragSettingsChanged;
            OnDragPositionUpdated.AddListener(OnPositionChanged);
        }

        private void OnDisable()
        {
            MapSettings.OnDragSettingsChanged -= OnDragSettingsChanged;
            OnDragPositionUpdated.RemoveListener(OnPositionChanged);
        }

        private void OnDragSettingsChanged(bool canDrag)
        {
            CanDrag = canDrag;
        }

        public abstract void UpdateFromData();
        public abstract void OnPositionChanged(Vector3 newPosition);

        public void UpdateCoordinates(Coordinates newCoordinates, float newZoomLevel)
        {
            placableData.Coordinates = newCoordinates;
            placableData.ZoomLevel = newZoomLevel;
        }

        public void UpdateScale(float currentZoomLevel, float maxZoomLevel)
        {
            var delta = currentZoomLevel - placableData.ZoomLevel;
            var scale = Vector3.one + new Vector3(delta, delta, delta);
            // clamp to min .01 and max 30
            scale = Vector3.Max(scale, Vector3.one * 0.01f);
            scale = Vector3.Min(scale, Vector3.one * 30f);
            transform.localScale = scale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!MapSettings.Instance.CanSelectMarkers) return;
            Select();
            _holdTime = Time.time;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!MapSettings.Instance.CanSelectMarkers) return;
            Release(Time.time - _holdTime);
        }

        public abstract void Select();
        public abstract void Release(float time);
    }

    public class Placable : PlacableBase<object>
    {
        public override void UpdateFromData()
        {
            // No additional implementation needed for generic Placable
        }

        public override void Select()
        {
            // Implement selection logic here
        }

        public override void Release(float time)
        {
            // Implement release logic here
        }

        public override void OnPositionChanged(Vector3 newPosition)
        {
            WitLogger.Log("Updating placable coordinates based on new position." +
                          $"World Position: {newPosition}");
            var mapViewLayout = FindFirstObjectByType<MapViewLayout>();
            if (mapViewLayout != null)
            {
                if (mapViewLayout.TryGetTileAndNormalizedFromWorldPosition(newPosition, out Coordinates coordinate, out float normalizedX, out float normalizedY))
                {
                    WitLogger.Log($"New Tile: {coordinate}, NormalizedX: {normalizedX}, NormalizedY: {normalizedY}");
                    placableData.Coordinates = coordinate;
                    placableData.NormalizedX = normalizedX;
                    placableData.NormalizedY = normalizedY;
                }
            }
        }
    }
}