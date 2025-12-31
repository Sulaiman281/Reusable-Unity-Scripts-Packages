namespace WitShells.CanvasDrawTool
{
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.Events;
    using System.Collections.Generic;

    /// <summary>
    /// Layer panel UI for managing layers.
    /// Displays layer list with visibility, lock, and selection controls.
    /// Updated for LayerObject-based system.
    /// </summary>
    public class LayerPanelUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LayerManager _layerManager;
        [SerializeField] private Transform _layerListContainer;
        [SerializeField] private GameObject _layerItemPrefab;
        [SerializeField] private ScrollRect _scrollRect;

        [Header("Buttons")]
        [SerializeField] private Button _addLayerButton;
        [SerializeField] private Button _deleteLayerButton;
        [SerializeField] private Button _duplicateLayerButton;
        [SerializeField] private Button _mergeDownButton;
        [SerializeField] private Button _mergeVisibleButton;
        [SerializeField] private Button _flattenButton;
        [SerializeField] private Button _moveUpButton;
        [SerializeField] private Button _moveDownButton;

        [Header("Events")]
        public UnityEvent<int> OnLayerSelected;
        public UnityEvent OnLayersChanged;

        private List<LayerItemUI> _layerItems = new List<LayerItemUI>();
        private int _selectedIndex = -1;

        // Public accessors
        public LayerManager LayerManager
        {
            get => _layerManager;
            set
            {
                _layerManager = value;
                RefreshLayerList();
            }
        }

        public int SelectedIndex => _selectedIndex;

        private void Awake()
        {
            SetupButtons();
        }

        private void Start()
        {
            RefreshLayerList();
        }

        private void OnEnable()
        {
            if (_layerManager != null)
            {
                _layerManager.OnLayerCreated.AddListener(OnLayerCreatedHandler);
                _layerManager.OnLayerDeleted.AddListener(OnLayerDeletedHandler);
                _layerManager.OnActiveLayerChanged.AddListener(OnActiveLayerChanged);
            }
        }

        private void OnDisable()
        {
            if (_layerManager != null)
            {
                _layerManager.OnLayerCreated.RemoveListener(OnLayerCreatedHandler);
                _layerManager.OnLayerDeleted.RemoveListener(OnLayerDeletedHandler);
                _layerManager.OnActiveLayerChanged.RemoveListener(OnActiveLayerChanged);
            }
        }

        /// <summary>
        /// Set up button click listeners.
        /// </summary>
        private void SetupButtons()
        {
            if (_addLayerButton != null)
                _addLayerButton.onClick.AddListener(AddLayer);

            if (_deleteLayerButton != null)
                _deleteLayerButton.onClick.AddListener(DeleteSelectedLayer);

            if (_duplicateLayerButton != null)
                _duplicateLayerButton.onClick.AddListener(DuplicateSelectedLayer);

            if (_mergeDownButton != null)
                _mergeDownButton.onClick.AddListener(MergeDown);

            if (_mergeVisibleButton != null)
                _mergeVisibleButton.onClick.AddListener(MergeVisible);

            if (_flattenButton != null)
                _flattenButton.onClick.AddListener(FlattenAll);

            if (_moveUpButton != null)
                _moveUpButton.onClick.AddListener(MoveSelectedUp);

            if (_moveDownButton != null)
                _moveDownButton.onClick.AddListener(MoveSelectedDown);
        }

        /// <summary>
        /// Refresh the entire layer list UI.
        /// </summary>
        public void RefreshLayerList()
        {
            // Clear existing items
            foreach (var item in _layerItems)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }
            _layerItems.Clear();

            if (_layerManager == null || _layerListContainer == null || _layerItemPrefab == null)
            {
                return;
            }

            // Create items in reverse order (top layer at top of list)
            for (int i = _layerManager.LayerCount - 1; i >= 0; i--)
            {
                CreateLayerItem(i);
            }

            // Select active layer
            _selectedIndex = _layerManager.ActiveLayerIndex;
            UpdateSelection();
            UpdateButtonStates();
        }

        /// <summary>
        /// Create a layer item UI element.
        /// </summary>
        private void CreateLayerItem(int layerIndex)
        {
            if (layerIndex < 0 || layerIndex >= _layerManager.LayerCount) return;
            LayerObject layer = _layerManager.Layers[layerIndex];
            if (layer == null) return;

            GameObject itemObj = Instantiate(_layerItemPrefab, _layerListContainer);
            LayerItemUI itemUI = itemObj.GetComponent<LayerItemUI>();

            if (itemUI == null)
            {
                itemUI = itemObj.AddComponent<LayerItemUI>();
            }

            itemUI.Initialize(layer, layerIndex);
            itemUI.OnClicked += () => SelectLayer(layerIndex);
            itemUI.OnVisibilityChanged += (visible) => SetLayerVisibility(layerIndex, visible);
            itemUI.OnLockChanged += (locked) => SetLayerLocked(layerIndex, locked);
            itemUI.OnNameChanged += (name) => SetLayerName(layerIndex, name);
            itemUI.OnOpacityChanged += (opacity) => SetLayerOpacity(layerIndex, opacity);

            _layerItems.Add(itemUI);
        }

        /// <summary>
        /// Select a layer by index.
        /// </summary>
        public void SelectLayer(int index)
        {
            if (_layerManager == null) return;

            _layerManager.SetActiveLayer(index);
            _selectedIndex = index;

            UpdateSelection();
            UpdateButtonStates();

            OnLayerSelected?.Invoke(index);
        }

        /// <summary>
        /// Update selection visual state.
        /// </summary>
        private void UpdateSelection()
        {
            foreach (var item in _layerItems)
            {
                if (item != null)
                {
                    item.SetSelected(item.LayerIndex == _selectedIndex);
                }
            }
        }

        /// <summary>
        /// Update button interactable states.
        /// </summary>
        private void UpdateButtonStates()
        {
            if (_layerManager == null) return;

            bool hasSelection = _selectedIndex >= 0;
            bool canDelete = hasSelection && _layerManager.LayerCount > 1;
            bool canMoveUp = hasSelection && _selectedIndex < _layerManager.LayerCount - 1;
            bool canMoveDown = hasSelection && _selectedIndex > 0;
            bool canMergeDown = hasSelection && _selectedIndex > 0;

            if (_deleteLayerButton != null) _deleteLayerButton.interactable = canDelete;
            if (_duplicateLayerButton != null) _duplicateLayerButton.interactable = hasSelection;
            if (_moveUpButton != null) _moveUpButton.interactable = canMoveUp;
            if (_moveDownButton != null) _moveDownButton.interactable = canMoveDown;
            if (_mergeDownButton != null) _mergeDownButton.interactable = canMergeDown;
            if (_mergeVisibleButton != null) _mergeVisibleButton.interactable = _layerManager.LayerCount > 1;
            if (_flattenButton != null) _flattenButton.interactable = _layerManager.LayerCount > 1;
        }

        /// <summary>
        /// Add a new layer.
        /// </summary>
        public void AddLayer()
        {
            if (_layerManager == null) return;

            _layerManager.CreateLayer($"Layer {_layerManager.LayerCount + 1}");
            int newIndex = _layerManager.LayerCount - 1;
            SelectLayer(newIndex);
        }

        /// <summary>
        /// Delete the selected layer.
        /// </summary>
        public void DeleteSelectedLayer()
        {
            if (_layerManager == null || _layerManager.LayerCount <= 1) return;

            _layerManager.DeleteLayer(_selectedIndex);
        }

        /// <summary>
        /// Duplicate the selected layer.
        /// </summary>
        public void DuplicateSelectedLayer()
        {
            if (_layerManager == null || _selectedIndex < 0) return;

            LayerObject newLayer = _layerManager.DuplicateLayer(_selectedIndex);
            if (newLayer != null)
            {
                int newIndex = _layerManager.LayerCount - 1;
                SelectLayer(newIndex);
            }
        }

        /// <summary>
        /// Merge selected layer down.
        /// </summary>
        public void MergeDown()
        {
            if (_layerManager == null || _selectedIndex <= 0) return;

            _layerManager.MergeDown(_selectedIndex);
        }

        /// <summary>
        /// Merge all visible layers.
        /// </summary>
        public void MergeVisible()
        {
            if (_layerManager == null) return;

            _layerManager.MergeVisible();
        }

        /// <summary>
        /// Flatten all layers.
        /// </summary>
        public void FlattenAll()
        {
            if (_layerManager == null) return;

            _layerManager.Flatten();
        }

        /// <summary>
        /// Move selected layer up.
        /// </summary>
        public void MoveSelectedUp()
        {
            if (_layerManager == null || _selectedIndex < 0) return;
            if (_selectedIndex >= _layerManager.LayerCount - 1) return;

            _layerManager.MoveLayerUp(_selectedIndex);
            _selectedIndex++;
            RefreshLayerList();
        }

        /// <summary>
        /// Move selected layer down.
        /// </summary>
        public void MoveSelectedDown()
        {
            if (_layerManager == null || _selectedIndex < 0) return;
            if (_selectedIndex <= 0) return;

            _layerManager.MoveLayerDown(_selectedIndex);
            _selectedIndex--;
            RefreshLayerList();
        }

        /// <summary>
        /// Set layer visibility.
        /// </summary>
        private void SetLayerVisibility(int index, bool visible)
        {
            if (_layerManager == null || index < 0 || index >= _layerManager.LayerCount) return;

            LayerObject layer = _layerManager.Layers[index];
            if (layer != null)
            {
                layer.IsVisible = visible;
                _layerManager.UpdateComposite();
            }
        }

        /// <summary>
        /// Set layer locked state.
        /// </summary>
        private void SetLayerLocked(int index, bool locked)
        {
            if (_layerManager == null || index < 0 || index >= _layerManager.LayerCount) return;

            LayerObject layer = _layerManager.Layers[index];
            if (layer != null)
            {
                layer.IsLocked = locked;
            }
        }

        /// <summary>
        /// Set layer name.
        /// </summary>
        private void SetLayerName(int index, string name)
        {
            if (_layerManager == null || index < 0 || index >= _layerManager.LayerCount) return;

            LayerObject layer = _layerManager.Layers[index];
            if (layer != null)
            {
                layer.LayerName = name;
            }
        }

        /// <summary>
        /// Set layer opacity.
        /// </summary>
        private void SetLayerOpacity(int index, float opacity)
        {
            if (_layerManager == null || index < 0 || index >= _layerManager.LayerCount) return;

            LayerObject layer = _layerManager.Layers[index];
            if (layer != null)
            {
                layer.Opacity = opacity;
                _layerManager.UpdateComposite();
            }
        }

        // Event handlers
        private void OnLayerCreatedHandler(LayerObject layer)
        {
            RefreshLayerList();
            OnLayersChanged?.Invoke();
        }

        private void OnLayerDeletedHandler(LayerObject layer)
        {
            RefreshLayerList();
            OnLayersChanged?.Invoke();
        }

        private void OnActiveLayerChanged(int index)
        {
            _selectedIndex = index;
            UpdateSelection();
            UpdateButtonStates();
        }
    }

    /// <summary>
    /// Individual layer item UI component.
    /// Updated for LayerObject-based system.
    /// </summary>
    public class LayerItemUI : MonoBehaviour
    {
        [SerializeField] private TMPro.TMP_Text _nameLabel;
        [SerializeField] private TMPro.TMP_InputField _nameInput;
        [SerializeField] private Toggle _visibilityToggle;
        [SerializeField] private Toggle _lockToggle;
        [SerializeField] private Slider _opacitySlider;
        [SerializeField] private RawImage _thumbnailImage;
        [SerializeField] private Image _selectionHighlight;
        [SerializeField] private Button _selectButton;

        public System.Action OnClicked;
        public System.Action<bool> OnVisibilityChanged;
        public System.Action<bool> OnLockChanged;
        public System.Action<string> OnNameChanged;
        public System.Action<float> OnOpacityChanged;

        private LayerObject _layer;
        private int _layerIndex;
        private bool _isSelected;

        public int LayerIndex => _layerIndex;
        public bool IsSelected => _isSelected;

        public void Initialize(LayerObject layer, int index)
        {
            _layer = layer;
            _layerIndex = index;

            // Set initial values
            if (_nameLabel != null) _nameLabel.text = layer.LayerName;
            if (_nameInput != null)
            {
                _nameInput.text = layer.LayerName;
                _nameInput.onEndEdit.AddListener((text) => OnNameChanged?.Invoke(text));
            }

            if (_visibilityToggle != null)
            {
                _visibilityToggle.isOn = layer.IsVisible;
                _visibilityToggle.onValueChanged.AddListener((val) => OnVisibilityChanged?.Invoke(val));
            }

            if (_lockToggle != null)
            {
                _lockToggle.isOn = layer.IsLocked;
                _lockToggle.onValueChanged.AddListener((val) => OnLockChanged?.Invoke(val));
            }

            if (_opacitySlider != null)
            {
                _opacitySlider.value = layer.Opacity;
                _opacitySlider.onValueChanged.AddListener((val) => OnOpacityChanged?.Invoke(val));
            }

            if (_thumbnailImage != null)
            {
                _thumbnailImage.texture = layer.Texture;
            }

            if (_selectButton != null)
            {
                _selectButton.onClick.AddListener(() => OnClicked?.Invoke());
            }
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;

            if (_selectionHighlight != null)
            {
                _selectionHighlight.enabled = selected;
            }
        }

        public void UpdateThumbnail()
        {
            if (_thumbnailImage != null && _layer != null)
            {
                _thumbnailImage.texture = _layer.Texture;
            }
        }
    }
}
