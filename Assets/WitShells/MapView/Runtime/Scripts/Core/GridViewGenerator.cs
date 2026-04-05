using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WitShells.DesignPatterns.Core;
using WitShells.MapView;

public class GridViewGenerator : MonoBehaviour
{
    [SerializeField] private MapViewLayout mapViewLayout;
    [SerializeField] private RectTransform lineContainer;
    [SerializeField] private RectTransform linePrefab;
    [SerializeField] private TMP_Text labelPrefab;

    [Header("Settings")]
    [SerializeField] private bool updateOnlyOnMapUpdated = true;
    [SerializeField] private float updateInterval = 0.5f;

    private float _lastUpdateTime;
    private ObjectPool<RectTransform> _linePool;
    private ObjectPool<RectTransform> _labelPool;
    private readonly List<RectTransform> _activeLines = new List<RectTransform>(128);
    private readonly List<RectTransform> _activeLabels = new List<RectTransform>(64);
    private readonly List<float> _verticalLinePositions = new List<float>(16);
    private readonly List<float> _horizontalLinePositions = new List<float>(16);
    private readonly List<float> _globalVerticalLinePositions = new List<float>(32);
    private readonly List<float> _globalHorizontalLinePositions = new List<float>(32);
    private bool _isMapUpdatedRegistered;
    private string[] _topLabelTexts;
    private string[] _bottomLabelTexts;
    private string[] _leftLabelTexts;
    private string[] _rightLabelTexts;

    private ObjectPool<RectTransform> LinePool
    {
        get
        {
            if (_linePool == null)
            {
                _linePool = new ObjectPool<RectTransform>(() =>
                {
                    if (linePrefab == null)
                        return null;

                    var line = Instantiate(linePrefab, lineContainer != null ? lineContainer : transform);
                    line.gameObject.SetActive(false);
                    return line;
                });
            }

            return _linePool;
        }
    }

    private ObjectPool<RectTransform> LabelPool
    {
        get
        {
            if (_labelPool == null)
            {
                _labelPool = new ObjectPool<RectTransform>(() =>
                {
                    if (labelPrefab == null)
                        return null;

                    var label = Instantiate(labelPrefab, lineContainer != null ? lineContainer : transform);
                    label.gameObject.SetActive(false);
                    return label.rectTransform;
                });
            }

            return _labelPool;
        }
    }

    private void Start()
    {
        EnsureMapViewLayout();
        UpdateMapUpdatedSubscription();
        RefreshGrid();
    }

    private void OnEnable()
    {
        EnsureMapViewLayout();
        UpdateMapUpdatedSubscription();
    }

    private void OnDisable()
    {
        UnregisterMapUpdated();
    }

    private void LateUpdate()
    {
        if (updateOnlyOnMapUpdated)
            return;

        if (Time.time - _lastUpdateTime < updateInterval)
            return;

        RefreshGrid();
        _lastUpdateTime = Time.time;
    }

    private void RefreshGrid()
    {
        ReleaseActiveLines();
        ReleaseActiveLabels();

        EnsureMapViewLayout();

        if (mapViewLayout == null)
            return;

        var settings = MapSettings.Instance;
        if (settings == null)
            return;

        if (!settings.EnableGrid)
        {
            ClearTileLines();
            return;
        }

        var rectTransform = mapViewLayout.RectTransform;
        if (rectTransform == null)
            return;

        float viewportWidth = rectTransform.rect.width;
        float viewportHeight = rectTransform.rect.height;
        GetSpacing(viewportWidth, viewportHeight, settings, out float spacingX, out float spacingY);

        if (!TryGetTileMetrics(out float tileWidth, out float tileHeight))
            return;

        var vpCenter = rectTransform.rect.center;
        BuildGlobalLinePositions(vpCenter.x, spacingX, settings.TotalVerticalGridLines, _globalVerticalLinePositions);
        BuildGlobalLinePositions(vpCenter.y, spacingY, settings.TotalHorizontalGridLines, _globalHorizontalLinePositions);

        foreach (var tile in mapViewLayout.GetAllTiles())
        {
            if (tile == null)
                continue;

            BuildTileLinePositions(tile, tileWidth, tileHeight, _globalVerticalLinePositions, _globalHorizontalLinePositions, _verticalLinePositions, _horizontalLinePositions);
            ApplyLinesToTile(tile, _verticalLinePositions, _horizontalLinePositions, settings.GridLineColor, settings.GridLineThickness);
        }

        if (settings.EnableGridLabels)
            ApplyLabels(settings);
    }

    private void EnsureMapViewLayout()
    {
        if (mapViewLayout == null)
            mapViewLayout = FindFirstObjectByType<MapViewLayout>();
    }

    private void UpdateMapUpdatedSubscription()
    {
        if (updateOnlyOnMapUpdated)
            RegisterMapUpdated();
        else
            UnregisterMapUpdated();
    }

    private void RegisterMapUpdated()
    {
        if (_isMapUpdatedRegistered)
            return;

        if (mapViewLayout == null)
            return;

        mapViewLayout.OnMapUpdated.AddListener(OnMapUpdated);
        _isMapUpdatedRegistered = true;
    }

    private void UnregisterMapUpdated()
    {
        if (!_isMapUpdatedRegistered)
            return;

        if (mapViewLayout != null)
            mapViewLayout.OnMapUpdated.RemoveListener(OnMapUpdated);

        _isMapUpdatedRegistered = false;
    }

    private void OnMapUpdated()
    {
        RefreshGrid();
        _lastUpdateTime = Time.time;
    }

    public void SetLabelsText(string[] top, string[] bottom, string[] left, string[] right)
    {
        _topLabelTexts = top;
        _bottomLabelTexts = bottom;
        _leftLabelTexts = left;
        _rightLabelTexts = right;
        RefreshGrid();
    }

    private void GetSpacing(float viewportWidth, float viewportHeight, MapSettings settings, out float spacingX, out float spacingY)
    {
        spacingX = spacingY = settings.GridSpacing;

        if (settings.PerfectSquareGrid)
        {
            float minSpacingX = viewportWidth / settings.TotalVerticalGridLines;
            float minSpacingY = viewportHeight / settings.TotalHorizontalGridLines;

            spacingX = spacingY = Mathf.Max(minSpacingX, minSpacingY);
            return;
        }

    }

    private bool TryGetTileMetrics(out float tileWidth, out float tileHeight)
    {
        tileWidth = 0f;
        tileHeight = 0f;

        bool hasAnyTile = false;
        foreach (var tile in mapViewLayout.GetAllTiles())
        {
            if (tile == null)
                continue;

            if (!hasAnyTile)
            {
                tileWidth = tile.RectTransform.rect.width;
                tileHeight = tile.RectTransform.rect.height;
                hasAnyTile = tileWidth > 0f && tileHeight > 0f;
                break;
            }
        }

        if (!hasAnyTile)
            return false;

        return true;
    }

    private void BuildGlobalLinePositions(float center, float spacing, int totalLines, List<float> globalPositions)
    {
        globalPositions.Clear();
        if (spacing <= 0.001f || totalLines <= 0)
            return;

        // Distribute lines symmetrically around the viewport center
        float startOffset = -((totalLines - 1) * 0.5f) * spacing;
        for (int i = 0; i < totalLines; i++)
        {
            globalPositions.Add(center + startOffset + (i * spacing));
        }
    }

    private void BuildTileLinePositions(
        TileView tile,
        float tileWidth,
        float tileHeight,
        IReadOnlyList<float> globalVerticalPositions,
        IReadOnlyList<float> globalHorizontalPositions,
        List<float> verticalPositions,
        List<float> horizontalPositions)
    {
        verticalPositions.Clear();
        horizontalPositions.Clear();

        if (tileWidth <= 0f || tileHeight <= 0f)
            return;

        // Use actual screen-space bounds from localPosition (Unity Y-up, in MapViewLayout local space)
        var rt = tile.RectTransform;
        float tileLeft = rt.localPosition.x - rt.pivot.x * tileWidth;
        float tileRight = tileLeft + tileWidth;
        float tileBottom = rt.localPosition.y - rt.pivot.y * tileHeight;
        float tileTop = tileBottom + tileHeight;

        for (int i = 0; i < globalVerticalPositions.Count; i++)
        {
            float globalX = globalVerticalPositions[i];
            if (globalX <= tileLeft + 0.001f || globalX >= tileRight - 0.001f)
                continue;

            float localX = globalX - tileLeft;
            verticalPositions.Add(localX);
        }

        for (int i = 0; i < globalHorizontalPositions.Count; i++)
        {
            float globalY = globalHorizontalPositions[i];
            if (globalY <= tileBottom + 0.001f || globalY >= tileTop - 0.001f)
                continue;

            float localY = globalY - tileBottom;
            horizontalPositions.Add(localY);
        }
    }

    private void ClearTileLines()
    {
        ReleaseActiveLines();
        ReleaseActiveLabels();
    }

    private void ApplyLabels(MapSettings settings)
    {
        if (labelPrefab == null)
            return;

        if (_globalVerticalLinePositions.Count == 0 || _globalHorizontalLinePositions.Count == 0)
            return;

        var rectTransform = mapViewLayout != null ? mapViewLayout.RectTransform : null;
        if (rectTransform == null)
            return;

        int effectiveOffsetIndex = GetEffectiveLabelOffsetIndex(settings);
        if (!TryGetLabelGuidePositions(settings, rectTransform.rect, effectiveOffsetIndex, out float leftX, out float rightX, out float bottomY, out float topY))
            return;

        for (int i = 0; i < _globalVerticalLinePositions.Count; i++)
        {
            float x = _globalVerticalLinePositions[i];
            CreateLabelOnTile(new Vector2(x, topY), settings.VerticalGridLabelOffset, GetLabelText(_topLabelTexts, i, GetVerticalLabelText(i)), settings);
            CreateLabelOnTile(new Vector2(x, bottomY), settings.VerticalGridLabelOffset, GetLabelText(_bottomLabelTexts, i, GetVerticalLabelText(i)), settings);
        }

        for (int i = 0; i < _globalHorizontalLinePositions.Count; i++)
        {
            float y = _globalHorizontalLinePositions[i];
            CreateLabelOnTile(new Vector2(leftX, y), settings.HorizontalGridLabelOffset, GetLabelText(_leftLabelTexts, i, GetHorizontalLabelText(i)), settings);
            CreateLabelOnTile(new Vector2(rightX, y), settings.HorizontalGridLabelOffset, GetLabelText(_rightLabelTexts, i, GetHorizontalLabelText(i)), settings);
        }
    }

    private int GetEffectiveLabelOffsetIndex(MapSettings settings)
    {
        int manualOffset = settings.GridLabelOffsetIndex;
        if (!settings.ZoomLabelOffset || mapViewLayout == null)
            return manualOffset;

        float zoom = mapViewLayout.CurrentZoomLevel;
        float fraction = zoom - Mathf.Floor(zoom);

        // Track decimal zoom from x.1 to x.9 and map to min/max offset index.
        float t = Mathf.InverseLerp(0.1f, 0.9f, fraction);
        int autoOffset = Mathf.RoundToInt(Mathf.Lerp(settings.ZoomLabelOffsetMinIndex, settings.ZoomLabelOffsetMaxIndex, t));
        return Mathf.Max(1, autoOffset);
    }

    private bool TryGetLabelGuidePositions(MapSettings settings, Rect viewportRect, int labelOffsetIndex, out float leftX, out float rightX, out float bottomY, out float topY)
    {
        leftX = 0f;
        rightX = 0f;
        bottomY = 0f;
        topY = 0f;

        int verticalOffset = Mathf.Max(1, labelOffsetIndex) - 1;
        int horizontalOffset = Mathf.Max(1, labelOffsetIndex) - 1;

        if (verticalOffset >= _globalVerticalLinePositions.Count || horizontalOffset >= _globalHorizontalLinePositions.Count)
            return false;

        float leftGuideX = _globalVerticalLinePositions[verticalOffset];
        float rightGuideX = _globalVerticalLinePositions[_globalVerticalLinePositions.Count - 1 - verticalOffset];
        float bottomGuideY = _globalHorizontalLinePositions[horizontalOffset];
        float topGuideY = _globalHorizontalLinePositions[_globalHorizontalLinePositions.Count - 1 - horizontalOffset];

        leftX = (viewportRect.xMin + leftGuideX) * 0.5f;
        rightX = (viewportRect.xMax + rightGuideX) * 0.5f;
        bottomY = (viewportRect.yMin + bottomGuideY) * 0.5f;
        topY = (viewportRect.yMax + topGuideY) * 0.5f;
        return true;
    }

    private string GetLabelText(string[] customTexts, int index, string fallback)
    {
        if (customTexts != null && index >= 0 && index < customTexts.Length && !string.IsNullOrEmpty(customTexts[index]))
            return customTexts[index];

        return fallback;
    }

    private string GetVerticalLabelText(int index)
    {
        return (index + 1).ToString();
    }

    private string GetHorizontalLabelText(int index)
    {
        return (_globalHorizontalLinePositions.Count - index).ToString();
    }

    private void CreateLabelOnTile(Vector2 baseGlobalPosition, Vector2 offset, string text, MapSettings settings)
    {
        Vector2 targetGlobalPosition = baseGlobalPosition + offset;
        if (!TryResolveTileLocalPoint(targetGlobalPosition, out var tile, out var localPosition))
            return;

        var labelRect = LabelPool.Get();
        if (labelRect == null)
            return;

        ConfigureLabel(labelRect, tile.GridLineContainer, localPosition, text, settings);
        _activeLabels.Add(labelRect);
    }

    private bool TryResolveTileLocalPoint(Vector2 globalPosition, out TileView resolvedTile, out Vector2 localPoint)
    {
        resolvedTile = null;
        localPoint = Vector2.zero;

        foreach (var tile in mapViewLayout.GetAllTiles())
        {
            if (tile == null || tile.RectTransform == null)
                continue;

            var rt = tile.RectTransform;
            float width = rt.rect.width;
            float height = rt.rect.height;
            if (width <= 0f || height <= 0f)
                continue;

            float tileLeft = rt.localPosition.x - rt.pivot.x * width;
            float tileRight = tileLeft + width;
            float tileBottom = rt.localPosition.y - rt.pivot.y * height;
            float tileTop = tileBottom + height;

            if (globalPosition.x < tileLeft || globalPosition.x > tileRight || globalPosition.y < tileBottom || globalPosition.y > tileTop)
                continue;

            resolvedTile = tile;
            localPoint = new Vector2(globalPosition.x - tileLeft, globalPosition.y - tileBottom);
            return true;
        }

        return false;
    }

    private void ConfigureLabel(RectTransform labelRect, RectTransform parent, Vector2 localPosition, string text, MapSettings settings)
    {
        labelRect.SetParent(parent, false);
        labelRect.gameObject.SetActive(true);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.zero;
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = localPosition;
        labelRect.localRotation = Quaternion.identity;
        labelRect.localScale = Vector3.one;

        var label = labelRect.GetComponent<TMP_Text>();
        if (label == null)
            return;

        label.text = text;
        label.fontSize = settings.GridLabelFontSize;
        label.color = settings.GridLabelColor;
        label.alignment = TextAlignmentOptions.Center;
    }

    private void ApplyLinesToTile(TileView tile, IReadOnlyList<float> verticalPositions, IReadOnlyList<float> horizontalPositions, Color color, float thickness)
    {
        float width = tile.RectTransform.rect.width;
        float height = tile.RectTransform.rect.height;

        if (width <= 0f || height <= 0f)
            return;

        for (int i = 0; i < verticalPositions.Count; i++)
        {
            float x = verticalPositions[i];
            if (x <= 0.001f || x >= width - 0.001f)
                continue;

            var line = LinePool.Get();
            if (line == null)
                continue;

            ConfigureLine(line, tile.GridLineContainer, new Vector2(x, 0f), new Vector2(x, height), color, thickness);
            _activeLines.Add(line);
        }

        for (int i = 0; i < horizontalPositions.Count; i++)
        {
            float y = horizontalPositions[i];
            if (y <= 0.001f || y >= height - 0.001f)
                continue;

            var line = LinePool.Get();
            if (line == null)
                continue;

            ConfigureLine(line, tile.GridLineContainer, new Vector2(0f, y), new Vector2(width, y), color, thickness);
            _activeLines.Add(line);
        }
    }

    private void ConfigureLine(RectTransform line, RectTransform parent, Vector2 start, Vector2 end, Color color, float thickness)
    {
        Vector2 direction = end - start;
        float length = direction.magnitude;
        if (length <= 0.001f)
            return;

        line.SetParent(parent, false);
        line.gameObject.SetActive(true);
        line.anchorMin = Vector2.zero;
        line.anchorMax = Vector2.zero;
        line.pivot = new Vector2(0f, 0.5f);
        line.sizeDelta = new Vector2(length, thickness);
        line.anchoredPosition = start;
        line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        var image = line.GetComponent<Image>();
        if (image != null)
            image.color = color;
    }

    private void ReleaseActiveLines()
    {
        for (int i = 0; i < _activeLines.Count; i++)
        {
            var line = _activeLines[i];
            if (line == null)
                continue;

            line.gameObject.SetActive(false);
            line.SetParent(lineContainer != null ? lineContainer : transform, false);
            LinePool.Release(line);
        }

        _activeLines.Clear();
    }

    private void ReleaseActiveLabels()
    {
        for (int i = 0; i < _activeLabels.Count; i++)
        {
            var label = _activeLabels[i];
            if (label == null)
                continue;

            label.gameObject.SetActive(false);
            label.SetParent(lineContainer != null ? lineContainer : transform, false);
            LabelPool.Release(label);
        }

        _activeLabels.Clear();
    }
}
