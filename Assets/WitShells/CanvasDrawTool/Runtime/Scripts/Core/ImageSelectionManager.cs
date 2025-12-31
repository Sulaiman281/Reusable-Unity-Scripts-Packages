namespace WitShells.CanvasDrawTool
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.InputSystem;
    using System.Collections.Generic;
    using System;
    using System.Linq;

    /// <summary>
    /// Manages selection of imported images and their transform handlers.
    /// Handles click-to-select, deselection, and transform mode toggling.
    /// </summary>
    public class ImageSelectionManager : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Settings")]
        [SerializeField] private bool _maintainAspectRatio = true;
        [SerializeField] private bool _allowRotation = true;

        [Header("Input Actions (New Input System)")]
        [SerializeField] private InputActionReference _deselectAction;
        [SerializeField] private InputActionReference _deleteAction;

        [Header("Handle Appearance")]
        [SerializeField] private float _handleSize = 20f;
        [SerializeField] private Color _handleColor = new Color(0.2f, 0.6f, 1f, 1f);
        [SerializeField] private Color _borderColor = new Color(0.2f, 0.6f, 1f, 0.8f);

        [Header("References")]
        [SerializeField] private Canvas _canvas;
        [SerializeField] private LayerManager _layerManager;
        #endregion

        #region Private Fields
        private Dictionary<LayerObject, ImageTransformHandler> _handlers = new Dictionary<LayerObject, ImageTransformHandler>();
        private LayerObject _selectedLayer;
        private ImageTransformHandler _selectedHandler;
        private bool _isTransformMode;

        // Runtime input actions (created if no reference assigned)
        private InputAction _runtimeDeselectAction;
        private InputAction _runtimeDeleteAction;
        #endregion

        #region Events
        public event Action<LayerObject> OnLayerSelected;
        public event Action OnSelectionCleared;
        public event Action<LayerObject> OnLayerTransformed;
        #endregion

        #region Properties
        public LayerObject SelectedLayer => _selectedLayer;
        public bool HasSelection => _selectedLayer != null;
        public bool IsTransformMode
        {
            get => _isTransformMode;
            set
            {
                _isTransformMode = value;
                if (!value)
                {
                    ClearSelection();
                }
            }
        }
        public bool MaintainAspectRatio
        {
            get => _maintainAspectRatio;
            set
            {
                _maintainAspectRatio = value;
                foreach (var handler in _handlers.Values)
                {
                    handler.MaintainAspectRatio = value;
                }
            }
        }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_canvas == null)
                _canvas = GetComponentInParent<Canvas>();

            // Create runtime input actions if not assigned via inspector
            SetupInputActions();
        }

        private void OnEnable()
        {
            // Enable input actions
            EnableInputActions();
        }

        private void OnDisable()
        {
            // Disable input actions
            DisableInputActions();
        }

        private void Start()
        {
            if (_layerManager != null)
            {
                // Subscribe to layer events (UnityEvents use AddListener)
                _layerManager.OnLayerCreated.AddListener(OnLayerCreated);
                _layerManager.OnLayerDeleted.AddListener(OnLayerDeleted);

                // Create handlers for existing layers
                foreach (var layer in _layerManager.Layers)
                {
                    CreateHandlerForLayer(layer);
                }
            }
        }

        private void Update()
        {
            HandleInput();
        }

        private void OnDestroy()
        {
            if (_layerManager != null)
            {
                _layerManager.OnLayerCreated.RemoveListener(OnLayerCreated);
                _layerManager.OnLayerDeleted.RemoveListener(OnLayerDeleted);
            }

            // Dispose runtime input actions
            DisposeInputActions();

            // Clean up all handlers
            foreach (var handler in _handlers.Values)
            {
                if (handler != null)
                    handler.Dispose();
            }
            _handlers.Clear();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize with references.
        /// </summary>
        public void Initialize(Canvas canvas, LayerManager layerManager)
        {
            _canvas = canvas;
            _layerManager = layerManager;

            if (_layerManager != null)
            {
                _layerManager.OnLayerCreated.AddListener(OnLayerCreated);
                _layerManager.OnLayerDeleted.AddListener(OnLayerDeleted);
            }
        }

        /// <summary>
        /// Select a specific layer.
        /// </summary>
        public void SelectLayer(LayerObject layer)
        {
            if (layer == null || !_isTransformMode) return;

            // Deselect previous
            if (_selectedHandler != null)
            {
                _selectedHandler.Deselect();
            }

            _selectedLayer = layer;

            // Get or create handler
            if (!_handlers.TryGetValue(layer, out _selectedHandler))
            {
                CreateHandlerForLayer(layer);
                _selectedHandler = _handlers[layer];
            }

            _selectedHandler?.Select();
            OnLayerSelected?.Invoke(layer);
        }

        /// <summary>
        /// Clear current selection.
        /// </summary>
        public void ClearSelection()
        {
            if (_selectedHandler != null)
            {
                _selectedHandler.Deselect();
            }

            _selectedLayer = null;
            _selectedHandler = null;
            OnSelectionCleared?.Invoke();
        }

        /// <summary>
        /// Try to select a layer at the given screen position.
        /// </summary>
        public bool TrySelectAtPosition(Vector2 screenPosition)
        {
            if (!_isTransformMode || _canvas == null) return false;

            // Raycast to find layers
            var results = new List<RaycastResult>();
            var pointerData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };

            EventSystem.current.RaycastAll(pointerData, results);

            foreach (var result in results)
            {
                var layer = result.gameObject.GetComponent<LayerObject>();
                if (layer == null)
                    layer = result.gameObject.GetComponentInParent<LayerObject>();

                if (layer != null && _handlers.ContainsKey(layer))
                {
                    SelectLayer(layer);
                    return true;
                }
            }

            // Clicked on empty space - clear selection
            ClearSelection();
            return false;
        }

        /// <summary>
        /// Reset selected layer to original size.
        /// </summary>
        public void ResetSelectedToOriginalSize()
        {
            _selectedHandler?.ResetToOriginalSize();
        }

        /// <summary>
        /// Fit selected layer to canvas bounds.
        /// </summary>
        public void FitSelectedToCanvas(RectTransform canvasBounds)
        {
            _selectedHandler?.FitToCanvas(canvasBounds);
        }

        /// <summary>
        /// Delete the currently selected layer.
        /// </summary>
        public void DeleteSelected()
        {
            if (_selectedLayer == null || _layerManager == null) return;

            int index = _layerManager.Layers.ToList().IndexOf(_selectedLayer);
            if (index >= 0)
            {
                ClearSelection();
                _layerManager.DeleteLayer(index);
            }
        }

        /// <summary>
        /// Enable click-to-select on a layer's RawImage.
        /// </summary>
        public void EnableClickToSelect(LayerObject layer)
        {
            if (layer == null || layer.RawImage == null) return;

            // Enable raycast
            layer.RawImage.raycastTarget = true;

            // Add click handler if not present
            var trigger = layer.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = layer.gameObject.AddComponent<EventTrigger>();

            // Check if already added
            bool hasClickHandler = false;
            foreach (var entry in trigger.triggers)
            {
                if (entry.eventID == EventTriggerType.PointerClick)
                {
                    hasClickHandler = true;
                    break;
                }
            }

            if (!hasClickHandler)
            {
                var clickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
                clickEntry.callback.AddListener((data) =>
                {
                    if (_isTransformMode)
                    {
                        SelectLayer(layer);
                    }
                });
                trigger.triggers.Add(clickEntry);
            }
        }
        #endregion

        #region Private Methods
        private void HandleInput()
        {
            // Get the active deselect action
            InputAction deselectAction = _deselectAction?.action ?? _runtimeDeselectAction;
            InputAction deleteAction = _deleteAction?.action ?? _runtimeDeleteAction;

            // Deselect on Escape (or configured action)
            if (deselectAction != null && deselectAction.WasPerformedThisFrame())
            {
                ClearSelection();
            }

            // Delete selected on Delete key (or configured action)
            if (deleteAction != null && deleteAction.WasPerformedThisFrame() && _selectedLayer != null)
            {
                DeleteSelected();
            }
        }

        private void OnLayerCreated(LayerObject layer)
        {
            CreateHandlerForLayer(layer);
            EnableClickToSelect(layer);
        }

        private void OnLayerDeleted(LayerObject layer)
        {
            if (_handlers.TryGetValue(layer, out var handler))
            {
                if (_selectedHandler == handler)
                {
                    ClearSelection();
                }

                handler.Dispose();
                _handlers.Remove(layer);
            }
        }

        private void CreateHandlerForLayer(LayerObject layer)
        {
            if (layer == null || _handlers.ContainsKey(layer)) return;

            var handler = layer.gameObject.AddComponent<ImageTransformHandler>();
            handler.AttachTo(layer, _canvas);
            handler.MaintainAspectRatio = _maintainAspectRatio;

            handler.OnTransformChanged += () => OnLayerTransformed?.Invoke(layer);

            _handlers[layer] = handler;
            EnableClickToSelect(layer);
        }

        private void SetupInputActions()
        {
            // Create runtime deselect action if not assigned
            if (_deselectAction == null)
            {
                _runtimeDeselectAction = new InputAction("Deselect", InputActionType.Button);
                _runtimeDeselectAction.AddBinding("<Keyboard>/escape");
            }

            // Create runtime delete action if not assigned
            if (_deleteAction == null)
            {
                _runtimeDeleteAction = new InputAction("Delete", InputActionType.Button);
                _runtimeDeleteAction.AddBinding("<Keyboard>/delete");
                _runtimeDeleteAction.AddBinding("<Keyboard>/backspace");
            }
        }

        private void EnableInputActions()
        {
            _runtimeDeselectAction?.Enable();
            _runtimeDeleteAction?.Enable();
        }

        private void DisableInputActions()
        {
            _runtimeDeselectAction?.Disable();
            _runtimeDeleteAction?.Disable();
        }

        private void DisposeInputActions()
        {
            _runtimeDeselectAction?.Dispose();
            _runtimeDeleteAction?.Dispose();
            _runtimeDeselectAction = null;
            _runtimeDeleteAction = null;
        }
        #endregion
    }
}
