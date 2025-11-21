using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace WitShells.DesignPatterns
{
    /// <summary>
    /// Generic draggable component that works for UI (RectTransform) and world objects.
    /// Attach to any GameObject. For UI make sure Canvas has a GraphicRaycaster.
    /// For world objects use a PhysicsRaycaster on the camera and a collider on the object.
    /// </summary>
    public class Draggable : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        [Tooltip("Smoothing factor for following the pointer. Higher = snappier.")]
        [Range(1f, 50f)] public float smoothing = 20f;

        [Tooltip("If true and object has a Rigidbody, MovePosition will be used for physics-safe movement.")]
        public bool useRigidbodyMove = false;

        [Header("Runtime")]
        public UnityEvent<Vector3> OnDragPositionUpdated;

        private RectTransform _rectTransform;
        private Transform _cachedTransform;
        private Rigidbody _rigidbody;

        // runtime offsets so the object doesn't jump to pointer
        private Vector3 _worldOffset = Vector3.zero;
        private Vector2 _uiOffset = Vector2.zero;

        public bool CanDrag { get; set; } = true;

        private bool _isDragging;
        public bool IsDragging => _isDragging;

        private Camera _activeCamera;

        void Awake()
        {
            _cachedTransform = transform;
            _rectTransform = GetComponent<RectTransform>();
            _rigidbody = GetComponent<Rigidbody>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!CanDrag) return;
            _isDragging = true;
            _activeCamera = eventData.pressEventCamera ?? Camera.main;

            if (_rectTransform != null)
            {
                // UI element: compute local point in parent RectTransform space and store offset
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rectTransform.parent as RectTransform,
                    eventData.position,
                    _activeCamera,
                    out Vector2 localPointerPos);

                // anchoredPosition is relative to parent
                _uiOffset = _rectTransform.anchoredPosition - localPointerPos;
            }
            else
            {
                // World object: compute world point and offset using camera
                if (_activeCamera != null)
                {
                    // distance from camera to object
                    float distance = Vector3.Dot(_cachedTransform.position - _activeCamera.transform.position, _activeCamera.transform.forward);
                    Vector3 screenPoint = new Vector3(eventData.position.x, eventData.position.y, Mathf.Max(0.01f, distance));
                    Vector3 worldPoint = _activeCamera.ScreenToWorldPoint(screenPoint);
                    _worldOffset = _cachedTransform.position - worldPoint;
                }
                else
                {
                    _worldOffset = Vector3.zero;
                }
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!CanDrag) return;
            if (!_isDragging) return;
            _activeCamera = eventData.pressEventCamera ?? Camera.main;

            if (_rectTransform != null)
            {
                // UI dragging (RectTransform anchoredPosition)
                RectTransform parentRT = _rectTransform.parent as RectTransform;
                if (parentRT == null) return;

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRT, eventData.position, _activeCamera, out Vector2 localPointerPos))
                {
                    Vector2 target = localPointerPos + _uiOffset;
                    Vector2 current = _rectTransform.anchoredPosition;
                    Vector2 next = Vector2.Lerp(current, target, Mathf.Clamp01(Time.deltaTime * smoothing));
                    _rectTransform.anchoredPosition = next;
                }
            }
            else
            {
                // World object dragging
                if (_activeCamera == null)
                {
                    // fallback: move in XY by screen delta scaled
                    Vector3 delta = new Vector3(eventData.delta.x, eventData.delta.y, 0f) * (Time.deltaTime * 0.5f);
                    Vector3 targetFallback = _cachedTransform.position + delta;
                    ApplyWorldMove(targetFallback);
                    return;
                }

                float distance = Vector3.Dot(_cachedTransform.position - _activeCamera.transform.position, _activeCamera.transform.forward);
                Vector3 screenPoint = new Vector3(eventData.position.x, eventData.position.y, Mathf.Max(0.01f, distance));
                Vector3 worldPoint = _activeCamera.ScreenToWorldPoint(screenPoint);
                Vector3 target = worldPoint + _worldOffset;
                // smooth move
                Vector3 next = Vector3.Lerp(_cachedTransform.position, target, Mathf.Clamp01(Time.deltaTime * smoothing));
                ApplyWorldMove(next);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!CanDrag) return;
            _isDragging = false;
            OnDragPositionUpdated?.Invoke(_cachedTransform.position);
        }

        private void ApplyWorldMove(Vector3 target)
        {
            if (useRigidbodyMove && _rigidbody != null)
            {
                // physics-safe move
                _rigidbody.MovePosition(target);
            }
            else
            {
                _cachedTransform.position = target;
            }
        }
    }
}