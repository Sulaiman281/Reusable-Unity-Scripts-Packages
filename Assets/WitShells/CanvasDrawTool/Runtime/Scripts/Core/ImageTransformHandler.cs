namespace WitShells.CanvasDrawTool
{
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.EventSystems;
    using System;

    /// <summary>
    /// Handles transform operations (move, scale, rotate) for imported images.
    /// Maintains aspect ratio during scaling. Provides visual handles at corners and edges.
    /// </summary>
    public class ImageTransformHandler : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Handle Settings")]
        [SerializeField] private float _handleSize = 20f;
        [SerializeField] private Color _handleColor = new Color(0.2f, 0.6f, 1f, 1f);
        [SerializeField] private Color _handleHoverColor = new Color(0.4f, 0.8f, 1f, 1f);
        [SerializeField] private Color _borderColor = new Color(0.2f, 0.6f, 1f, 0.8f);
        [SerializeField] private float _borderWidth = 2f;

        [Header("Behavior")]
        [SerializeField] private bool _maintainAspectRatio = true;
        [SerializeField] private float _minScale = 0.1f;
        [SerializeField] private float _maxScale = 10f;
        [SerializeField] private bool _allowRotation = true;
        #endregion

        #region Private Fields
        private LayerObject _targetLayer;
        private RectTransform _targetRect;
        private Canvas _canvas;
        private RectTransform _canvasRect;

        // UI Elements
        private GameObject _handlesContainer;
        private RectTransform _handlesRect;
        private Image _borderImage;
        
        // Corner handles (for scale with aspect ratio)
        private HandleControl _handleTL; // Top-Left
        private HandleControl _handleTR; // Top-Right
        private HandleControl _handleBL; // Bottom-Left
        private HandleControl _handleBR; // Bottom-Right

        // Edge handles (for scale without aspect ratio - disabled when maintainAspectRatio is true)
        private HandleControl _handleT;  // Top
        private HandleControl _handleB;  // Bottom
        private HandleControl _handleL;  // Left
        private HandleControl _handleR;  // Right

        // Rotation handle
        private HandleControl _handleRotate;

        // State
        private bool _isSelected;
        private bool _isDragging;
        private bool _isScaling;
        private bool _isRotating;
        private Vector2 _dragStartPosition;
        private Vector2 _originalPosition;
        private Vector2 _originalSize;
        private Vector2 _originalScale;
        private float _originalRotation;
        private float _originalAspectRatio;
        private HandleType _activeHandle;

        // Center drag area
        private EventTrigger _centerDragArea;
        #endregion

        #region Enums
        private enum HandleType
        {
            None,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight,
            Top,
            Bottom,
            Left,
            Right,
            Rotate,
            Move
        }
        #endregion

        #region Events
        public event Action OnTransformStarted;
        public event Action OnTransformChanged;
        public event Action OnTransformEnded;
        public event Action OnSelectionChanged;
        #endregion

        #region Properties
        public bool IsSelected => _isSelected;
        public bool MaintainAspectRatio
        {
            get => _maintainAspectRatio;
            set
            {
                _maintainAspectRatio = value;
                UpdateEdgeHandlesVisibility();
            }
        }
        public LayerObject TargetLayer => _targetLayer;
        #endregion

        #region Public Methods
        /// <summary>
        /// Attach this handler to a LayerObject.
        /// </summary>
        public void AttachTo(LayerObject layer, Canvas canvas)
        {
            if (layer == null || canvas == null) return;

            _targetLayer = layer;
            _targetRect = layer.RectTransform;
            _canvas = canvas;
            _canvasRect = canvas.GetComponent<RectTransform>();

            // Calculate original aspect ratio
            _originalAspectRatio = _targetRect.sizeDelta.x / Mathf.Max(_targetRect.sizeDelta.y, 0.001f);

            CreateHandles();
            Deselect();
        }

        /// <summary>
        /// Select this layer for transform operations.
        /// </summary>
        public void Select()
        {
            if (_handlesContainer == null) return;

            _isSelected = true;
            _handlesContainer.SetActive(true);
            UpdateHandlePositions();
            OnSelectionChanged?.Invoke();
        }

        /// <summary>
        /// Deselect and hide handles.
        /// </summary>
        public void Deselect()
        {
            _isSelected = false;
            if (_handlesContainer != null)
                _handlesContainer.SetActive(false);
            OnSelectionChanged?.Invoke();
        }

        /// <summary>
        /// Toggle selection state.
        /// </summary>
        public void ToggleSelection()
        {
            if (_isSelected)
                Deselect();
            else
                Select();
        }

        /// <summary>
        /// Update handle positions to match target.
        /// </summary>
        public void UpdateHandlePositions()
        {
            if (!_isSelected || _handlesRect == null || _targetRect == null) return;

            // Match container to target
            _handlesRect.anchoredPosition = _targetRect.anchoredPosition;
            _handlesRect.sizeDelta = _targetRect.sizeDelta * _targetRect.localScale;
            _handlesRect.localRotation = _targetRect.localRotation;

            // Update border size
            if (_borderImage != null)
            {
                var borderRect = _borderImage.rectTransform;
                borderRect.sizeDelta = _handlesRect.sizeDelta + new Vector2(_borderWidth * 2, _borderWidth * 2);
            }
        }

        /// <summary>
        /// Reset to original size (1:1 with imported image).
        /// </summary>
        public void ResetToOriginalSize()
        {
            if (_targetRect == null) return;

            _targetRect.localScale = Vector3.one;
            UpdateHandlePositions();
            OnTransformChanged?.Invoke();
        }

        /// <summary>
        /// Fit to canvas while maintaining aspect ratio.
        /// </summary>
        public void FitToCanvas(RectTransform canvasBounds)
        {
            if (_targetRect == null || canvasBounds == null) return;

            Vector2 canvasSize = canvasBounds.sizeDelta;
            Vector2 imageSize = _targetRect.sizeDelta;

            float scaleX = canvasSize.x / imageSize.x;
            float scaleY = canvasSize.y / imageSize.y;
            float scale = Mathf.Min(scaleX, scaleY);

            _targetRect.localScale = new Vector3(scale, scale, 1f);
            _targetRect.anchoredPosition = Vector2.zero;

            UpdateHandlePositions();
            OnTransformChanged?.Invoke();
        }

        /// <summary>
        /// Clean up handles.
        /// </summary>
        public void Dispose()
        {
            if (_handlesContainer != null)
            {
                Destroy(_handlesContainer);
            }
        }
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            if (!_isSelected) return;

            // Update handle positions if target moved externally
            if (!_isDragging && !_isScaling && !_isRotating)
            {
                UpdateHandlePositions();
            }
        }

        private void OnDestroy()
        {
            Dispose();
        }
        #endregion

        #region Handle Creation
        private void CreateHandles()
        {
            if (_handlesContainer != null)
            {
                Destroy(_handlesContainer);
            }

            // Create container
            _handlesContainer = new GameObject("TransformHandles");
            _handlesContainer.transform.SetParent(_targetRect.parent, false);
            
            _handlesRect = _handlesContainer.AddComponent<RectTransform>();
            _handlesRect.anchorMin = new Vector2(0.5f, 0.5f);
            _handlesRect.anchorMax = new Vector2(0.5f, 0.5f);
            _handlesRect.pivot = new Vector2(0.5f, 0.5f);

            // Create border
            CreateBorder();

            // Create center drag area
            CreateCenterDragArea();

            // Create corner handles
            _handleTL = CreateHandle("HandleTL", new Vector2(0, 1), HandleType.TopLeft);
            _handleTR = CreateHandle("HandleTR", new Vector2(1, 1), HandleType.TopRight);
            _handleBL = CreateHandle("HandleBL", new Vector2(0, 0), HandleType.BottomLeft);
            _handleBR = CreateHandle("HandleBR", new Vector2(1, 0), HandleType.BottomRight);

            // Create edge handles
            _handleT = CreateHandle("HandleT", new Vector2(0.5f, 1), HandleType.Top);
            _handleB = CreateHandle("HandleB", new Vector2(0.5f, 0), HandleType.Bottom);
            _handleL = CreateHandle("HandleL", new Vector2(0, 0.5f), HandleType.Left);
            _handleR = CreateHandle("HandleR", new Vector2(1, 0.5f), HandleType.Right);

            // Create rotation handle (above top center)
            if (_allowRotation)
            {
                _handleRotate = CreateRotationHandle();
            }

            UpdateEdgeHandlesVisibility();
        }

        private void CreateBorder()
        {
            GameObject borderObj = new GameObject("Border");
            borderObj.transform.SetParent(_handlesRect, false);

            var borderRect = borderObj.AddComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0.5f, 0.5f);
            borderRect.anchorMax = new Vector2(0.5f, 0.5f);
            borderRect.pivot = new Vector2(0.5f, 0.5f);
            borderRect.anchoredPosition = Vector2.zero;

            _borderImage = borderObj.AddComponent<Image>();
            _borderImage.color = _borderColor;
            _borderImage.raycastTarget = false;

            // Create inner transparent area
            GameObject innerObj = new GameObject("Inner");
            innerObj.transform.SetParent(borderRect, false);

            var innerRect = innerObj.AddComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(_borderWidth, _borderWidth);
            innerRect.offsetMax = new Vector2(-_borderWidth, -_borderWidth);

            var innerImage = innerObj.AddComponent<Image>();
            innerImage.color = Color.clear;
            innerImage.raycastTarget = false;

            // Use mask to create border effect
            var mask = borderObj.AddComponent<Mask>();
            mask.showMaskGraphic = true;
        }

        private void CreateCenterDragArea()
        {
            GameObject dragObj = new GameObject("DragArea");
            dragObj.transform.SetParent(_handlesRect, false);

            var dragRect = dragObj.AddComponent<RectTransform>();
            dragRect.anchorMin = Vector2.zero;
            dragRect.anchorMax = Vector2.one;
            dragRect.offsetMin = new Vector2(_handleSize / 2, _handleSize / 2);
            dragRect.offsetMax = new Vector2(-_handleSize / 2, -_handleSize / 2);

            var dragImage = dragObj.AddComponent<Image>();
            dragImage.color = new Color(1, 1, 1, 0.01f); // Nearly transparent but raycast-able
            dragImage.raycastTarget = true;

            _centerDragArea = dragObj.AddComponent<EventTrigger>();
            AddDragEvents(_centerDragArea, HandleType.Move);
        }

        private HandleControl CreateHandle(string name, Vector2 anchor, HandleType handleType)
        {
            GameObject handleObj = new GameObject(name);
            handleObj.transform.SetParent(_handlesRect, false);

            var rect = handleObj.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(_handleSize, _handleSize);
            rect.anchoredPosition = Vector2.zero;

            var image = handleObj.AddComponent<Image>();
            image.color = _handleColor;
            image.raycastTarget = true;

            // Add event trigger
            var trigger = handleObj.AddComponent<EventTrigger>();
            AddDragEvents(trigger, handleType);

            // Add hover effects
            AddHoverEvents(trigger, image);

            return new HandleControl
            {
                GameObject = handleObj,
                RectTransform = rect,
                Image = image,
                HandleType = handleType
            };
        }

        private HandleControl CreateRotationHandle()
        {
            GameObject handleObj = new GameObject("HandleRotate");
            handleObj.transform.SetParent(_handlesRect, false);

            var rect = handleObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(_handleSize, _handleSize);
            rect.anchoredPosition = new Vector2(0, _handleSize * 2);

            var image = handleObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.8f, 0.2f, 1f); // Green for rotation
            image.raycastTarget = true;

            // Create line connecting to top
            GameObject lineObj = new GameObject("RotateLine");
            lineObj.transform.SetParent(_handlesRect, false);

            var lineRect = lineObj.AddComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0.5f, 1f);
            lineRect.anchorMax = new Vector2(0.5f, 1f);
            lineRect.pivot = new Vector2(0.5f, 0f);
            lineRect.sizeDelta = new Vector2(2, _handleSize * 2);
            lineRect.anchoredPosition = Vector2.zero;

            var lineImage = lineObj.AddComponent<Image>();
            lineImage.color = new Color(0.2f, 0.8f, 0.2f, 0.8f);
            lineImage.raycastTarget = false;

            var trigger = handleObj.AddComponent<EventTrigger>();
            AddDragEvents(trigger, HandleType.Rotate);
            AddHoverEvents(trigger, image);

            return new HandleControl
            {
                GameObject = handleObj,
                RectTransform = rect,
                Image = image,
                HandleType = HandleType.Rotate
            };
        }

        private void AddDragEvents(EventTrigger trigger, HandleType handleType)
        {
            // Begin Drag
            var beginEntry = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
            beginEntry.callback.AddListener((data) => OnBeginDrag((PointerEventData)data, handleType));
            trigger.triggers.Add(beginEntry);

            // Drag
            var dragEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            dragEntry.callback.AddListener((data) => OnDrag((PointerEventData)data, handleType));
            trigger.triggers.Add(dragEntry);

            // End Drag
            var endEntry = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
            endEntry.callback.AddListener((data) => OnEndDrag((PointerEventData)data, handleType));
            trigger.triggers.Add(endEntry);
        }

        private void AddHoverEvents(EventTrigger trigger, Image image)
        {
            var originalColor = image.color;

            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener((data) => image.color = _handleHoverColor);
            trigger.triggers.Add(enterEntry);

            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener((data) => image.color = originalColor);
            trigger.triggers.Add(exitEntry);
        }

        private void UpdateEdgeHandlesVisibility()
        {
            bool showEdgeHandles = !_maintainAspectRatio;

            if (_handleT != null) _handleT.GameObject.SetActive(showEdgeHandles);
            if (_handleB != null) _handleB.GameObject.SetActive(showEdgeHandles);
            if (_handleL != null) _handleL.GameObject.SetActive(showEdgeHandles);
            if (_handleR != null) _handleR.GameObject.SetActive(showEdgeHandles);
        }
        #endregion

        #region Drag Handlers
        private void OnBeginDrag(PointerEventData eventData, HandleType handleType)
        {
            _activeHandle = handleType;
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, eventData.position, eventData.pressEventCamera, out _dragStartPosition);

            _originalPosition = _targetRect.anchoredPosition;
            _originalSize = _targetRect.sizeDelta;
            _originalScale = _targetRect.localScale;
            _originalRotation = _targetRect.localEulerAngles.z;

            switch (handleType)
            {
                case HandleType.Move:
                    _isDragging = true;
                    break;
                case HandleType.Rotate:
                    _isRotating = true;
                    break;
                default:
                    _isScaling = true;
                    break;
            }

            OnTransformStarted?.Invoke();
        }

        private void OnDrag(PointerEventData eventData, HandleType handleType)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, eventData.position, eventData.pressEventCamera, out localPoint);

            Vector2 delta = localPoint - _dragStartPosition;

            switch (handleType)
            {
                case HandleType.Move:
                    HandleMove(delta);
                    break;
                case HandleType.Rotate:
                    HandleRotation(localPoint);
                    break;
                default:
                    HandleScale(handleType, delta, localPoint);
                    break;
            }

            UpdateHandlePositions();
            OnTransformChanged?.Invoke();
        }

        private void OnEndDrag(PointerEventData eventData, HandleType handleType)
        {
            _isDragging = false;
            _isScaling = false;
            _isRotating = false;
            _activeHandle = HandleType.None;

            // Sync changes to LayerObject
            if (_targetLayer != null)
            {
                _targetLayer.Position = _targetRect.anchoredPosition;
                _targetLayer.Scale = _targetRect.localScale;
                _targetLayer.Rotation = _targetRect.localEulerAngles.z;
            }

            OnTransformEnded?.Invoke();
        }

        private void HandleMove(Vector2 delta)
        {
            _targetRect.anchoredPosition = _originalPosition + delta;
        }

        private void HandleRotation(Vector2 currentPoint)
        {
            Vector2 center = _targetRect.anchoredPosition;
            Vector2 startDir = (_dragStartPosition - center).normalized;
            Vector2 currentDir = (currentPoint - center).normalized;

            float angle = Vector2.SignedAngle(startDir, currentDir);
            _targetRect.localRotation = Quaternion.Euler(0, 0, _originalRotation + angle);
        }

        private void HandleScale(HandleType handleType, Vector2 delta, Vector2 localPoint)
        {
            if (_maintainAspectRatio)
            {
                HandleScaleWithAspectRatio(handleType, delta, localPoint);
            }
            else
            {
                HandleScaleWithoutAspectRatio(handleType, delta);
            }
        }

        private void HandleScaleWithAspectRatio(HandleType handleType, Vector2 delta, Vector2 localPoint)
        {
            Vector2 center = _targetRect.anchoredPosition;
            float originalDist = Vector2.Distance(_dragStartPosition, center);
            float currentDist = Vector2.Distance(localPoint, center);

            if (originalDist < 0.001f) return;

            float scaleMultiplier = currentDist / originalDist;
            float newScaleX = _originalScale.x * scaleMultiplier;
            float newScaleY = _originalScale.y * scaleMultiplier;

            // Clamp scale
            newScaleX = Mathf.Clamp(newScaleX, _minScale, _maxScale);
            newScaleY = Mathf.Clamp(newScaleY, _minScale, _maxScale);

            // Ensure aspect ratio is maintained
            float currentAspect = Mathf.Abs(newScaleX / Mathf.Max(Mathf.Abs(newScaleY), 0.001f));
            float originalAspect = Mathf.Abs(_originalScale.x / Mathf.Max(Mathf.Abs(_originalScale.y), 0.001f));

            if (Mathf.Abs(currentAspect - originalAspect) > 0.001f)
            {
                // Maintain original aspect
                newScaleY = newScaleX * Mathf.Sign(_originalScale.y) / (originalAspect * Mathf.Sign(_originalScale.x));
            }

            _targetRect.localScale = new Vector3(newScaleX, newScaleY, 1f);
        }

        private void HandleScaleWithoutAspectRatio(HandleType handleType, Vector2 delta)
        {
            Vector2 newScale = _originalScale;
            Vector2 newPosition = _originalPosition;

            // Account for rotation
            float rotRad = _originalRotation * Mathf.Deg2Rad;
            Vector2 rotatedDelta = new Vector2(
                delta.x * Mathf.Cos(rotRad) + delta.y * Mathf.Sin(rotRad),
                -delta.x * Mathf.Sin(rotRad) + delta.y * Mathf.Cos(rotRad)
            );

            Vector2 baseSize = _originalSize;
            float scaleFactorX = rotatedDelta.x / Mathf.Max(baseSize.x, 1f);
            float scaleFactorY = rotatedDelta.y / Mathf.Max(baseSize.y, 1f);

            switch (handleType)
            {
                case HandleType.TopLeft:
                    newScale.x = _originalScale.x - scaleFactorX;
                    newScale.y = _originalScale.y + scaleFactorY;
                    break;
                case HandleType.TopRight:
                    newScale.x = _originalScale.x + scaleFactorX;
                    newScale.y = _originalScale.y + scaleFactorY;
                    break;
                case HandleType.BottomLeft:
                    newScale.x = _originalScale.x - scaleFactorX;
                    newScale.y = _originalScale.y - scaleFactorY;
                    break;
                case HandleType.BottomRight:
                    newScale.x = _originalScale.x + scaleFactorX;
                    newScale.y = _originalScale.y - scaleFactorY;
                    break;
                case HandleType.Top:
                    newScale.y = _originalScale.y + scaleFactorY;
                    break;
                case HandleType.Bottom:
                    newScale.y = _originalScale.y - scaleFactorY;
                    break;
                case HandleType.Left:
                    newScale.x = _originalScale.x - scaleFactorX;
                    break;
                case HandleType.Right:
                    newScale.x = _originalScale.x + scaleFactorX;
                    break;
            }

            // Clamp scale
            newScale.x = Mathf.Clamp(newScale.x, _minScale, _maxScale);
            newScale.y = Mathf.Clamp(newScale.y, _minScale, _maxScale);

            _targetRect.localScale = new Vector3(newScale.x, newScale.y, 1f);
        }
        #endregion

        #region Helper Class
        private class HandleControl
        {
            public GameObject GameObject;
            public RectTransform RectTransform;
            public Image Image;
            public HandleType HandleType;
        }
        #endregion
    }
}
