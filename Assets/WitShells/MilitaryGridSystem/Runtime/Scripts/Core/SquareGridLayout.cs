using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using WitShells.DesignPatterns.Core;
using TMPro;
using UnityEngine.EventSystems;

namespace WitShells.MilitaryGridSystem
{
    public class SquareGridLayout : MonoBehaviour
    {
        public enum GridLabel
        {
            Left,
            Right,
            Top,
            Bottom
        }

        [Header("Grid Settings")]
        [SerializeField] private RectTransform linePrefab;  // Prefab with Image component
        [SerializeField] private TMP_Text labelPrefab;
        [SerializeField] private Color lineColor = Color.white;
        [SerializeField] private float lineThickness = 1f;
        [SerializeField] private float cellSize = 80f;
        [SerializeField] private int labelIndexOffset = 1;
        [SerializeField] private Vector2 labelOffset = new Vector2(0, 0);
        [SerializeField] private bool showLabels = true;

        [Header("Labels Settings")]
        [SerializeField] private int labelSize = 30;
        [SerializeField] private bool generateLabelsWithCellsTransform;
        [SerializeField] private int customUtmReminder = 1000;

        [SerializeField] private Vector3 offSetPosition;

        private RectTransform canvasRect;
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

        public RectTransform CanvasRect
        {
            get
            {
                if (canvasRect == null)
                {
                    canvasRect = GetComponent<RectTransform>();
                }
                return canvasRect;
            }
        }

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
                        if (image)
                        {
                            image.color = lineColor;
                        }
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

        public int Rows => Mathf.FloorToInt(Height / cellSize) + 1;
        public int Columns => Mathf.FloorToInt(Width / cellSize) + 1;

        public bool ShowLabels
        {
            get => showLabels;
            set
            {
                showLabels = value;
                UpdateLabelVisibility();
            }
        }
#if UNITY_EDITOR

        void OnValidate()
        {
            if (linePrefab == null)
            {
                linePrefab = Resources.Load<RectTransform>("GridPrefab/linePrefab");
            }

            if (labelPrefab == null)
            {
                labelPrefab = Resources.Load<TMP_Text>("GridPrefab/labelPrefab");
            }
        }
#endif

        private void Start()
        {
            RegenerateGrid();
        }

        public void SetTotalCells(int totalCells)
        {
            cellSize = Mathf.Max(50f, Mathf.Min(Width, Height) / totalCells); // Minimum 50 units for visibility
            RegenerateGrid();
        }

        public void SetCellSize(float newSize)
        {
            cellSize = Mathf.Max(50f, newSize); // Minimum 50 units for visibility
            RegenerateGrid();
        }

        public void SetLabels(string[] labels, GridLabel position)
        {
            ReleaseLabels(position);
            switch (position)
            {
                case GridLabel.Left:
                    AddLeftRowLabels(labels);
                    break;
                case GridLabel.Right:
                    AddRightRowLabels(labels);
                    break;
                case GridLabel.Top:
                    AddTopRowLabels(labels);
                    break;
                case GridLabel.Bottom:
                    AddBottomRowLabels(labels);
                    break;
            }
        }

        public void RegenerateGrid()
        {
            GenerateGrid();
            if (generateLabelsWithCellsTransform)
            {
                GenerateTransformBaseSequentialLabels();
            }
        }

        private void GenerateGrid()
        {
            // Clear existing lines
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

            // Generate new lines
            for (int i = 0; i < Rows; i++)
            {
                var line = CreateLine();
                line.gameObject.SetActive(true);
                line.anchoredPosition = new Vector2(0, i * cellSize);
                line.sizeDelta = new Vector2(Width, lineThickness);
                horizontalLines.Add(line);
            }

            for (int i = 0; i < Columns; i++)
            {
                var line = CreateLine();
                line.gameObject.SetActive(true);
                line.anchoredPosition = new Vector2(i * cellSize, 0);
                line.sizeDelta = new Vector2(lineThickness, Height);
                verticalLines.Add(line);
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

        private void AddLeftRowLabels(string[] leftRight)
        {
            for (int i = labelIndexOffset; i < Rows - labelIndexOffset && i < leftRight.Length; i++)
            {
                var label = CreateLabel();
                label.GetComponent<TMP_Text>().text = leftRight[i];
                label.transform.SetParent(transform, false);
                label.anchoredPosition = new Vector2(labelSize / 2, i * labelSize + labelOffset.y);
                gridLabels[GridLabel.Left].Add(label);
            }
        }

        private void AddRightRowLabels(string[] leftRight)
        {
            for (int i = labelIndexOffset; i < Rows - labelIndexOffset && i < leftRight.Length; i++)
            {
                var label = CreateLabel();
                label.GetComponent<TMP_Text>().text = leftRight[i];
                label.transform.SetParent(transform, false);
                label.anchoredPosition = new Vector2(Width - labelSize / 2, i * labelSize + labelOffset.y);
                gridLabels[GridLabel.Right].Add(label);
            }
        }

        private void AddBottomRowLabels(string[] topBottom)
        {
            for (int i = labelIndexOffset; i < Columns - labelIndexOffset && i < topBottom.Length; i++)
            {
                var label = CreateLabel();
                label.GetComponent<TMP_Text>().text = topBottom[i];
                label.transform.SetParent(transform, false);
                label.anchoredPosition = new Vector2(i * labelSize + labelOffset.x, Height - labelSize / 2 + labelOffset.y);
                gridLabels[GridLabel.Bottom].Add(label);
            }
        }

        private void AddTopRowLabels(string[] topBottom)
        {
            for (int i = labelIndexOffset; i < Columns - labelIndexOffset && i < topBottom.Length; i++)
            {
                var label = CreateLabel();
                label.GetComponent<TMP_Text>().text = topBottom[i];
                label.transform.SetParent(transform, false);
                label.anchoredPosition = new Vector2(i * labelSize + labelOffset.x, labelSize / 2 + labelOffset.y);
                gridLabels[GridLabel.Top].Add(label);
            }
        }

        private RectTransform CreateLine()
        {
            return LinePool.Get();
        }

        private RectTransform CreateLabel()
        {
            var label = LabelPool.Get();
            label.gameObject.SetActive(true);
            label.GetComponent<TMP_Text>().color = lineColor;
            label.sizeDelta = new Vector2(labelSize, labelSize);
            return label;
        }

        public void SetColor(Color newColor)
        {
            lineColor = newColor;

            // Update existing lines
            foreach (var line in horizontalLines)
            {
                if (line)
                {
                    var image = line.GetComponent<Image>();
                    if (image) image.color = lineColor;
                }
            }

            foreach (var line in verticalLines)
            {
                if (line)
                {
                    var image = line.GetComponent<Image>();
                    if (image) image.color = lineColor;
                }
            }
        }


        #region Generate Custom Norting & Easting
        private void GenerateTransformBaseSequentialLabels()
        {
            ReleaseLabels(GridLabel.Left);
            ReleaseLabels(GridLabel.Right);
            ReleaseLabels(GridLabel.Top);
            ReleaseLabels(GridLabel.Bottom);

            // left
            for (int i = labelIndexOffset; i < Rows - labelIndexOffset; i++)
            {
                var anchor = new Vector2(cellSize / 2, i * cellSize + labelOffset.y);
                var label = CreateLabel();
                label.transform.SetParent(transform, false);
                label.anchoredPosition = anchor;
                var worldPos = AnchorPositionToWorldPosition(label.transform as RectTransform);
                label.GetComponent<TMP_Text>().text = WordPosToNorthing(worldPos);
                gridLabels[GridLabel.Left].Add(label);
            }

            // right
            for (int i = labelIndexOffset; i < Rows - labelIndexOffset; i++)
            {
                var anchor = new Vector2(Width - cellSize / 2, i * cellSize + labelOffset.y);
                var label = CreateLabel();
                label.transform.SetParent(transform, false);
                label.anchoredPosition = anchor;
                var worldPos = AnchorPositionToWorldPosition(label.transform as RectTransform);
                label.GetComponent<TMP_Text>().text = WordPosToNorthing(worldPos);
                gridLabels[GridLabel.Right].Add(label);
            }

            // top
            for (int i = labelIndexOffset; i < Columns - labelIndexOffset; i++)
            {
                var anchor = new Vector2(i * cellSize + labelOffset.x, cellSize / 2 + labelOffset.y);
                var label = CreateLabel();
                label.transform.SetParent(transform, false);
                label.anchoredPosition = anchor;
                var worldPos = AnchorPositionToWorldPosition(label.transform as RectTransform);
                label.GetComponent<TMP_Text>().text = WordPosToEasting(worldPos);
                gridLabels[GridLabel.Top].Add(label);
            }

            // bottom
            for (int i = labelIndexOffset; i < Columns - labelIndexOffset; i++)
            {
                var anchor = new Vector2(i * cellSize + labelOffset.x, Height - cellSize / 2 + labelOffset.y);
                var label = CreateLabel();
                label.transform.SetParent(transform, false);
                label.anchoredPosition = anchor;
                var worldPos = AnchorPositionToWorldPosition(label.transform as RectTransform);
                label.GetComponent<TMP_Text>().text = WordPosToEasting(worldPos);
                gridLabels[GridLabel.Bottom].Add(label);
            }

        }

        public Vector3 AnchorPositionToWorldPosition(RectTransform rectTransform)
        {
            // var worldPoint = (transform as RectTransform).TransformPoint(re);
            // var containerLocalPoint = (transform as RectTransform).InverseTransformPoint(worldPoint);
            // worldPoint = (transform as RectTransform).TransformPoint(containerLocalPoint);

            var worldPoint = rectTransform.position;

            var canvas = transform.root.GetComponentInChildren<Canvas>();

            var cam = Camera.main;
            var screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPoint);
            var worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, canvas.planeDistance));
            return worldPos - offSetPosition;
        }

        private string WordPosToEasting(Vector3 worldPos)
        {
            int easting = Mathf.RoundToInt(worldPos.x) % customUtmReminder;
            return easting.ToString();
        }

        private string WordPosToNorthing(Vector3 worldPos)
        {
            int northing = Mathf.RoundToInt(worldPos.z) % customUtmReminder;
            return northing.ToString();
        }
        #endregion
    }
}
