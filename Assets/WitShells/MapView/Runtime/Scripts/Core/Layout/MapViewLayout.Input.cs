using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

namespace WitShells.MapView
{
    public partial class MapViewLayout
    {
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
            if (touchCount == 0) { return; }

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
            }
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

                OnLocationSelected?.Invoke(SelectedCoordinates);
                Utils.GetLocalPositionFromNormalizedInTile(clickedTile.RectTransform, normX, normY, transform, out var worldPos);
                worldPos = transform.TransformPoint(worldPos);
                OnPositionSelected?.Invoke(worldPos);

#if UNITY_EDITOR
                GUIUtility.systemCopyBuffer = SelectedCoordinates.ToString();
#endif
                DesignPatterns.WitLogger.Log($"Selected Coordinates: {SelectedCoordinates} (copied) {normX}, {normY}");
            }
        }

        #endregion
    }
}
