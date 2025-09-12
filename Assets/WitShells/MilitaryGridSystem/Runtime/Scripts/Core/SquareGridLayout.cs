using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using WitShells.DesignPatterns.Core;
using TMPro;
using System.Linq;

namespace WitShells.MilitaryGridSystem
{
    public class SquareGridLayout : MonoBehaviour
    {
        #region Enums and Structs
        public enum GridLabel
        {
            Left,
            Right,
            Top,
            Bottom
        }

        public enum GridType
        {
            Fixed,           // Fixed cell size
            AreaBased,       // Total area in km² with box size in km²
            DimensionBased   // Horizontal and vertical distances with cell size in km
        }

        public enum LabelType
        {
            None,
            Sequential,
            TransformBased
        }

        [System.Serializable]
        public struct FixedGridSettings
        {
            public float cellSize;
        }

        [System.Serializable]
        public struct AreaBasedGridSettings
        {
            public float totalAreaKmSquare;
            public float boxSizeKmSquare;
        }

        [System.Serializable]
        public struct DimensionBasedGridSettings
        {
            public float horizontalDistanceKm;
            public float verticalDistanceKm;
            public float gridCellSizeKm;
        }

        [System.Serializable]
        public struct LabelSettings
        {
            public int labelSize;
            public int labelIndexOffset;
            public Vector2 labelOffset;
            public int sequentialStartingNumber;
            public int customUtmReminder;
        }

        // Helper struct to organize grid dimensions
        public struct GridDimensions
        {
            public int Columns;
            public int Rows;
            public float CellWidth;
            public float CellHeight;
        }
        #endregion

        #region Serialized Fields
        [Header("Grid Settings")]
        [SerializeField] private RectTransform linePrefab;
        [SerializeField] private TMP_Text labelPrefab;
        [SerializeField] private Color lineColor = Color.white;
        [SerializeField] private float lineThickness = 1f;
        [SerializeField] private bool maintainAspectRatio = true;
        [SerializeField] private GridType gridType = GridType.Fixed;
        [SerializeField] private Vector3 offsetPosition;

        [Header("Grid Type Settings")]
        [SerializeField] private FixedGridSettings fixedSettings = new FixedGridSettings { cellSize = 80f };
        [SerializeField] private AreaBasedGridSettings areaSettings = new AreaBasedGridSettings { totalAreaKmSquare = 100f, boxSizeKmSquare = 5f };
        [SerializeField]
        private DimensionBasedGridSettings dimensionSettings = new DimensionBasedGridSettings
        {
            horizontalDistanceKm = 10f,
            verticalDistanceKm = 10f,
            gridCellSizeKm = 1f
        };

        [Header("Label Settings")]
        [SerializeField] private LabelType labelType = LabelType.None;
        [SerializeField] private bool showLabels = true;
        [SerializeField]
        private LabelSettings labelSettings = new LabelSettings
        {
            labelSize = 30,
            labelIndexOffset = 1,
            sequentialStartingNumber = 10,
            customUtmReminder = 1000
        };
        #endregion

        #region Private Fields
        [SerializeField] private RectTransform canvasRect;
        private readonly List<RectTransform> horizontalLines = new List<RectTransform>();
        private readonly List<RectTransform> verticalLines = new List<RectTransform>();
        private readonly Dictionary<GridLabel, List<RectTransform>> gridLabels = new Dictionary<GridLabel, List<RectTransform>>()
        {
            { GridLabel.Left, new List<RectTransform>() },
            { GridLabel.Right, new List<RectTransform>() },
            { GridLabel.Top, new List<RectTransform>() },
            { GridLabel.Bottom, new List<RectTransform>() }
        };

        private ObjectPool<RectTransform> linePool;
        private ObjectPool<RectTransform> labelPool;
        private float currentCellWidth;
        private float currentCellHeight;
        #endregion

        #region Properties
        public GridType CurrentGridType => gridType;
        public LabelType CurrentLabelType => labelType;
        public bool MaintainAspectRatio => maintainAspectRatio;

        // Expose current settings as properties
        public FixedGridSettings FixedSettings => fixedSettings;
        public AreaBasedGridSettings AreaSettings => areaSettings;
        public DimensionBasedGridSettings DimensionSettings => dimensionSettings;
        public LabelSettings CurrentLabelSettings => labelSettings;

        public RectTransform CanvasRect =>
            canvasRect ?? (canvasRect = GetComponent<RectTransform>());

        private ObjectPool<RectTransform> LinePool
        {
            get
            {
                if (linePool == null)
                {
                    linePool = new ObjectPool<RectTransform>(() =>
                    {
                        var line = Instantiate(linePrefab, transform);
                        var image = line.GetComponent<Image>();
                        if (image) image.color = lineColor;
                        return line;
                    });
                }
                return linePool;
            }
        }

        private ObjectPool<RectTransform> LabelPool
        {
            get
            {
                if (labelPool == null)
                {
                    labelPool = new ObjectPool<RectTransform>(() =>
                    {
                        var label = Instantiate(labelPrefab, transform);
                        return label.rectTransform;
                    });
                }
                return labelPool;
            }
        }

        public float Width => CanvasRect.rect.width;
        public float Height => CanvasRect.rect.height;

        public int Rows => Mathf.FloorToInt(Height / fixedSettings.cellSize) + 1;
        public int Columns => Mathf.FloorToInt(Width / fixedSettings.cellSize) + 1;

        public int ActualRows { get; private set; }
        public int ActualColumns { get; private set; }

        public bool ShowLabels
        {
            get => showLabels;
            set
            {
                showLabels = value;
                UpdateLabelVisibility();
            }
        }
        #endregion

        #region Unity Lifecycle
#if UNITY_EDITOR
        void OnValidate()
        {
            if (linePrefab == null)
                linePrefab = Resources.Load<RectTransform>("GridPrefab/linePrefab");

            if (labelPrefab == null)
                labelPrefab = Resources.Load<TMP_Text>("GridPrefab/labelPrefab");
        }
#endif

        private void Start()
        {
            RegenerateGrid();
        }
        #endregion

        #region Public Methods
        public void RegenerateGrid()
        {
            ClearExistingGrid();

            // Calculate grid dimensions based on selected grid type
            GridDimensions dimensions = CalculateGridDimensions();

            // Apply calculated dimensions
            ActualColumns = dimensions.Columns;
            ActualRows = dimensions.Rows;
            currentCellWidth = dimensions.CellWidth;
            currentCellHeight = dimensions.CellHeight;

            // Generate grid lines
            GenerateGridLines(dimensions);

            // Generate labels if needed
            if (showLabels && labelType != LabelType.None)
            {
                GenerateLabels();
            }
        }

        public void SetGridType(GridType type)
        {
            gridType = type;
            RegenerateGrid();
        }

        public void SetLabelType(LabelType type)
        {
            labelType = type;
            RegenerateGrid();
        }

        public void SetFixedGridSettings(float cellSize)
        {
            fixedSettings.cellSize = Mathf.Max(30f, cellSize);
            if (gridType == GridType.Fixed)
                RegenerateGrid();
        }

        public void SetAreaBasedGridSettings(float areaKmSquare, float boxKmSquare)
        {
            areaSettings.totalAreaKmSquare = areaKmSquare;
            areaSettings.boxSizeKmSquare = boxKmSquare;
            gridType = GridType.AreaBased;
            RegenerateGrid();
        }

        public void SetDimensionBasedGridSettings(float horizontalKm, float verticalKm, float cellSizeKm)
        {
            dimensionSettings.horizontalDistanceKm = horizontalKm;
            dimensionSettings.verticalDistanceKm = verticalKm;
            dimensionSettings.gridCellSizeKm = cellSizeKm;
            gridType = GridType.DimensionBased;
            RegenerateGrid();
        }

        public void SetLabelSettings(int labelSize, int startNumber, int indexOffset)
        {
            labelSettings.labelSize = labelSize;
            labelSettings.sequentialStartingNumber = startNumber;
            labelSettings.labelIndexOffset = Mathf.Min(0, indexOffset);
            RegenerateGrid();
        }

        public void SetMaintainAspectRatio(bool maintain)
        {
            maintainAspectRatio = maintain;
            RegenerateGrid();
        }

        public void SetColor(Color newColor)
        {
            lineColor = newColor;
            UpdateLineColors();
        }

        public string GetGridInfo()
        {
            switch (gridType)
            {
                case GridType.AreaBased:
                    return GetAreaBasedGridInfo();
                case GridType.DimensionBased:
                    return GetDimensionBasedGridInfo();
                default:
                    return GetFixedGridInfo();
            }
        }
        #endregion

        #region Private Methods
        private void ClearExistingGrid()
        {
            // Clear lines
            foreach (var line in horizontalLines)
            {
                line.gameObject.SetActive(false);
                if (line) LinePool.Release(line);
            }
            horizontalLines.Clear();

            foreach (var line in verticalLines)
            {
                line.gameObject.SetActive(false);
                if (line) LinePool.Release(line);
            }
            verticalLines.Clear();

            // Clear labels
            foreach (GridLabel labelPosition in System.Enum.GetValues(typeof(GridLabel)))
            {
                ReleaseLabels(labelPosition);
            }
        }

        private GridDimensions CalculateGridDimensions()
        {
            GridDimensions dimensions = new GridDimensions();

            switch (gridType)
            {
                case GridType.AreaBased:
                    CalculateAreaBasedDimensions(ref dimensions);
                    break;

                case GridType.DimensionBased:
                    CalculateDimensionBasedDimensions(ref dimensions);
                    break;

                default: // GridType.Fixed
                    CalculateFixedDimensions(ref dimensions);
                    break;
            }

            return dimensions;
        }

        private void CalculateFixedDimensions(ref GridDimensions dimensions)
        {
            dimensions.Columns = Columns;
            dimensions.Rows = Rows;
            dimensions.CellWidth = fixedSettings.cellSize;
            dimensions.CellHeight = fixedSettings.cellSize;
        }

        private void CalculateAreaBasedDimensions(ref GridDimensions dimensions)
        {
            float aspectRatio = Width / Height;
            float exactCellCount = areaSettings.totalAreaKmSquare / areaSettings.boxSizeKmSquare;

            // Calculate dimensions to better match the requested area
            int columns, rows;

            // Try two methods and pick the one closest to desired area
            // Method 1
            float sqrtCells = Mathf.Sqrt(exactCellCount);
            columns = Mathf.RoundToInt(sqrtCells * Mathf.Sqrt(aspectRatio));
            rows = Mathf.RoundToInt(exactCellCount / columns);
            float area1 = columns * rows * areaSettings.boxSizeKmSquare;

            // Method 2
            int altColumns = Mathf.RoundToInt(Mathf.Sqrt(exactCellCount * aspectRatio));
            int altRows = Mathf.RoundToInt(Mathf.Sqrt(exactCellCount / aspectRatio));
            float area2 = altColumns * altRows * areaSettings.boxSizeKmSquare;

            // Use method with result closest to desired area
            if (Mathf.Abs(area2 - areaSettings.totalAreaKmSquare) < Mathf.Abs(area1 - areaSettings.totalAreaKmSquare))
            {
                columns = altColumns;
                rows = altRows;
            }

            // Ensure minimum size
            dimensions.Columns = Mathf.Max(2, columns);
            dimensions.Rows = Mathf.Max(2, rows);

            // Calculate cell sizes
            dimensions.CellWidth = Width / dimensions.Columns;
            dimensions.CellHeight = Height / dimensions.Rows;

            if (maintainAspectRatio)
            {
                float minSize = Mathf.Min(dimensions.CellWidth, dimensions.CellHeight);
                dimensions.CellWidth = dimensions.CellHeight = minSize;
            }
        }

        private void CalculateDimensionBasedDimensions(ref GridDimensions dimensions)
        {
            // Calculate cells based on physical dimensions
            dimensions.Columns = Mathf.RoundToInt(dimensionSettings.horizontalDistanceKm / dimensionSettings.gridCellSizeKm);
            dimensions.Rows = Mathf.RoundToInt(dimensionSettings.verticalDistanceKm / dimensionSettings.gridCellSizeKm);

            // Ensure minimum size
            dimensions.Columns = Mathf.Max(2, dimensions.Columns);
            dimensions.Rows = Mathf.Max(2, dimensions.Rows);

            // Calculate cell sizes
            dimensions.CellWidth = Width / dimensions.Columns;
            dimensions.CellHeight = Height / dimensions.Rows;

            if (maintainAspectRatio)
            {
                float minSize = Mathf.Min(dimensions.CellWidth, dimensions.CellHeight);
                dimensions.CellWidth = dimensions.CellHeight = minSize;
            }
        }

        private void GenerateGridLines(GridDimensions dimensions)
        {
            if (maintainAspectRatio)
            {
                // Generate horizontal lines with square cells
                for (int i = 0; i <= dimensions.Rows; i++)
                {
                    var line = CreateLine();
                    line.anchoredPosition = new Vector2(0, i * dimensions.CellHeight);
                    line.sizeDelta = new Vector2(Width, lineThickness);
                    horizontalLines.Add(line);
                }

                // Generate vertical lines with square cells
                for (int i = 0; i <= dimensions.Columns; i++)
                {
                    var line = CreateLine();
                    line.anchoredPosition = new Vector2(i * dimensions.CellWidth, 0);
                    line.sizeDelta = new Vector2(lineThickness, Height);
                    verticalLines.Add(line);
                }
            }
            else
            {
                // Generate horizontal lines
                for (int i = 0; i <= dimensions.Rows; i++)
                {
                    var line = CreateLine();
                    line.anchoredPosition = new Vector2(0, i * dimensions.CellHeight);
                    line.sizeDelta = new Vector2(Width, lineThickness);
                    horizontalLines.Add(line);
                }

                // Generate vertical lines
                for (int i = 0; i <= dimensions.Columns; i++)
                {
                    var line = CreateLine();
                    line.anchoredPosition = new Vector2(i * dimensions.CellWidth, 0);
                    line.sizeDelta = new Vector2(lineThickness, Height);
                    verticalLines.Add(line);
                }
            }
        }

        private void GenerateLabels()
        {
            // Generate label positions for each side of the grid
            CreateLabelPositions(GridLabel.Left, true);    // Vertical
            CreateLabelPositions(GridLabel.Right, true);   // Vertical
            CreateLabelPositions(GridLabel.Top, false);    // Horizontal
            CreateLabelPositions(GridLabel.Bottom, false); // Horizontal
        }

        private void CreateLabelPositions(GridLabel side, bool isVertical)
        {
            int count = isVertical ? ActualRows : ActualColumns;
            int offset = labelSettings.labelIndexOffset;

            for (int i = offset; i < count - offset; i++)
            {
                Vector2 position = CalculateLabelPosition(side, i);
                string labelText = GetLabelText(side, i);

                var label = CreateLabel();
                label.anchoredPosition = position;
                label.GetComponent<TMP_Text>().text = labelText;
                gridLabels[side].Add(label);
            }
        }

        private Vector2 CalculateLabelPosition(GridLabel side, int index)
        {
            bool isVertical = side == GridLabel.Left || side == GridLabel.Right;

            return side switch
            {
                GridLabel.Left => new Vector2(
                                        currentCellWidth / 2 + (isVertical ? 0 : labelSettings.labelOffset.x),
                                        index * currentCellHeight + (isVertical ? labelSettings.labelOffset.y : 0)),
                GridLabel.Right => new Vector2(
                                        Width - currentCellWidth / 2 + (isVertical ? 0 : labelSettings.labelOffset.x),
                                        index * currentCellHeight + (isVertical ? labelSettings.labelOffset.y : 0)),
                GridLabel.Top => new Vector2(
                                        index * currentCellWidth + (isVertical ? 0 : labelSettings.labelOffset.x),
                                        currentCellHeight / 2 + (isVertical ? labelSettings.labelOffset.y : 0)),
                GridLabel.Bottom => new Vector2(
                                        index * currentCellWidth + (isVertical ? 0 : labelSettings.labelOffset.x),
                                        Height - currentCellHeight / 2 + (isVertical ? labelSettings.labelOffset.y : 0)),
                _ => Vector2.zero,
            };
        }

        private string GetLabelText(GridLabel side, int index)
        {
            int adjustedIndex = index - labelSettings.labelIndexOffset;

            if (labelType == LabelType.Sequential)
            {
                return (labelSettings.sequentialStartingNumber + adjustedIndex).ToString();
            }
            else if (labelType == LabelType.TransformBased)
            {
                // Create a temporary rect transform at this position
                var tempLabel = LabelPool.Get();
                tempLabel.anchoredPosition = CalculateLabelPosition(side, index);
                var worldPos = AnchorPositionToWorldPosition(tempLabel);
                LabelPool.Release(tempLabel);

                // Determine if we need easting or northing based on the side
                bool isVertical = (side == GridLabel.Left || side == GridLabel.Right);
                return isVertical ? WordPosToNorthing(worldPos) : WordPosToEasting(worldPos);
            }

            return string.Empty;
        }

        private void ReleaseLabels(GridLabel position)
        {
            if (gridLabels.TryGetValue(position, out var labels))
            {
                foreach (var label in labels)
                {
                    label.gameObject.SetActive(false);
                    if (label) LabelPool.Release(label);
                }
                labels.Clear();
            }
        }

        private void UpdateLabelVisibility()
        {
            foreach (var kvp in gridLabels)
            {
                foreach (var label in kvp.Value)
                {
                    label.gameObject.SetActive(ShowLabels);
                }
            }
        }

        private void UpdateLineColors()
        {
            // Update existing lines
            foreach (var line in horizontalLines.Concat(verticalLines))
            {
                if (line)
                {
                    var image = line.GetComponent<Image>();
                    if (image) image.color = lineColor;
                }
            }
        }

        private RectTransform CreateLine()
        {
            var line = LinePool.Get();
            line.gameObject.SetActive(true);
            return line;
        }

        private RectTransform CreateLabel()
        {
            var label = LabelPool.Get();
            label.gameObject.SetActive(true);
            label.GetComponent<TMP_Text>().color = lineColor;
            label.sizeDelta = new Vector2(labelSettings.labelSize, labelSettings.labelSize);
            return label;
        }

        private Vector3 AnchorPositionToWorldPosition(RectTransform rectTransform)
        {
            var worldPoint = rectTransform.position;
            var canvas = transform.root.GetComponentInChildren<Canvas>();
            var cam = Camera.main;
            var screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPoint);
            var worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, canvas.planeDistance));
            return worldPos - offsetPosition;
        }

        private string WordPosToEasting(Vector3 worldPos)
        {
            int easting = Mathf.RoundToInt(worldPos.x) % labelSettings.customUtmReminder;
            return easting.ToString();
        }

        private string WordPosToNorthing(Vector3 worldPos)
        {
            int northing = Mathf.RoundToInt(worldPos.z) % labelSettings.customUtmReminder;
            return northing.ToString();
        }

        private string GetFixedGridInfo()
        {
            return $"Fixed grid: {ActualColumns}x{ActualRows} = {ActualColumns * ActualRows} cells\n" +
                   $"Cell size: {fixedSettings.cellSize}x{fixedSettings.cellSize} pixels\n" +
                   $"Screen resolution: {Width}x{Height} pixels";
        }

        private string GetAreaBasedGridInfo()
        {
            float actualArea = ActualColumns * ActualRows * areaSettings.boxSizeKmSquare;
            return $"Area-based grid: {ActualColumns}x{ActualRows} = {ActualColumns * ActualRows} cells\n" +
                   $"Box size: {areaSettings.boxSizeKmSquare} km²\n" +
                   $"Total area: {actualArea} km² (target: {areaSettings.totalAreaKmSquare} km²)\n" +
                   $"Screen resolution: {Width}x{Height} pixels";
        }

        private string GetDimensionBasedGridInfo()
        {
            float actualHorizontal = ActualColumns * dimensionSettings.gridCellSizeKm;
            float actualVertical = ActualRows * dimensionSettings.gridCellSizeKm;
            return $"Dimension-based grid: {ActualColumns}x{ActualRows} = {ActualColumns * ActualRows} cells\n" +
                   $"Cell size: {dimensionSettings.gridCellSizeKm}x{dimensionSettings.gridCellSizeKm} km\n" +
                   $"Horizontal: {actualHorizontal} km (target: {dimensionSettings.horizontalDistanceKm} km)\n" +
                   $"Vertical: {actualVertical} km (target: {dimensionSettings.verticalDistanceKm} km)\n" +
                   $"Screen resolution: {Width}x{Height} pixels";
        }
        #endregion
    }
}
