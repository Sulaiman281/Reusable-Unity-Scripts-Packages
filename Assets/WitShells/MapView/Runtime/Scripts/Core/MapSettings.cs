using System;
using System.Collections.Generic;
using UnityEngine;

namespace WitShells.MapView
{
    [CreateAssetMenu(fileName = "MapSettings", menuName = "WitShells/MapView/MapSettings", order = 1)]
    public class MapSettings : ScriptableObject
    {
        public static MapSettings Instance
        {
            get
            {
                return Resources.Load<MapSettings>("MapSettings");
            }
        }

        [Header("Map File")]
        [SerializeField] private MapFile mapFile;

        [Header("Map Settings")]
        public bool useOnlineMap = true;
        public bool showLabels = false;

        [Header("Grid Settings")]
        [SerializeField] private bool enableGrid = false;
        [SerializeField] private int totalHorizontalGridLines = 8;
        [SerializeField] private int totalVerticalGridLines = 4;
        [SerializeField] private float gridSpacing = 64f;
        [SerializeField] private float gridLineThickness = 1f;
        [SerializeField] private Color gridLineColor = Color.white;
        [SerializeField] private bool perfectSquareGrid = false;

        [Header("Grid Label Settings")]
        [SerializeField] private bool enableGridLabels = false;
        [SerializeField] private int gridLabelOffsetIndex = 1;
        [SerializeField] private bool zoomLabelOffset = false;
        [SerializeField] private int zoomLabelOffsetMinIndex = 1;
        [SerializeField] private int zoomLabelOffsetMaxIndex = 3;
        [SerializeField] private Vector2 verticalGridLabelOffset = Vector2.zero;
        [SerializeField] private Vector2 horizontalGridLabelOffset = Vector2.zero;
        [SerializeField] private int gridLabelFontSize = 18;
        [SerializeField] private Color gridLabelColor = Color.white;

        [Header("Marker Settings")]
        [SerializeField] private bool canDragMarkers = true;
        [SerializeField] private bool canSelectMarkers = true;

        public bool CanDragMarkers => canDragMarkers;
        public bool CanSelectMarkers => canSelectMarkers;
        public bool EnableGrid => enableGrid;
        public int TotalHorizontalGridLines => Mathf.Max(1, totalHorizontalGridLines);
        public int TotalVerticalGridLines => Mathf.Max(1, totalVerticalGridLines);
        public float GridSpacing => Mathf.Max(1f, gridSpacing);
        public float GridLineThickness => Mathf.Max(0.1f, gridLineThickness);
        public Color GridLineColor => gridLineColor;
        public bool PerfectSquareGrid => perfectSquareGrid;
        public bool EnableGridLabels => enableGridLabels;
        public int GridLabelOffsetIndex => Mathf.Max(1, gridLabelOffsetIndex);
        public bool ZoomLabelOffset => zoomLabelOffset;
        public int ZoomLabelOffsetMinIndex => Mathf.Max(1, zoomLabelOffsetMinIndex);
        public int ZoomLabelOffsetMaxIndex => Mathf.Max(ZoomLabelOffsetMinIndex, zoomLabelOffsetMaxIndex);
        public Vector2 VerticalGridLabelOffset => verticalGridLabelOffset;
        public Vector2 HorizontalGridLabelOffset => horizontalGridLabelOffset;
        public int GridLabelFontSize => Mathf.Max(1, gridLabelFontSize);
        public Color GridLabelColor => gridLabelColor;

        public MapFile MapFile => mapFile;

        [Header("Offline Region Catalog")]
        [SerializeField] private List<MapFile> regions = new List<MapFile>();

        /// <summary>All saved offline map regions.</summary>
        public IReadOnlyList<MapFile> Regions => regions;

        /// <summary>Returns true if a region with the given name exists in the catalog.</summary>
        public bool HasRegion(string name)
        {
            for (int i = 0; i < regions.Count; i++)
                if (string.Equals(regions[i].MapName, name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>Attempts to find a region by name. Returns false if not found.</summary>
        public bool TryGetRegion(string name, out MapFile region)
        {
            for (int i = 0; i < regions.Count; i++)
            {
                if (string.Equals(regions[i].MapName, name, StringComparison.OrdinalIgnoreCase))
                {
                    region = regions[i];
                    return true;
                }
            }
            region = default;
            return false;
        }

        /// <summary>Adds a region to the catalog. Replaces an existing entry with the same name.</summary>
        public void AddRegion(MapFile region)
        {
            for (int i = 0; i < regions.Count; i++)
            {
                if (string.Equals(regions[i].MapName, region.MapName, StringComparison.OrdinalIgnoreCase))
                {
                    regions[i] = region;
                    return;
                }
            }
            regions.Add(region);
        }

        /// <summary>Removes the region with the given name from the catalog. Does nothing if not found.</summary>
        public void RemoveRegion(string name)
        {
            for (int i = regions.Count - 1; i >= 0; i--)
                if (string.Equals(regions[i].MapName, name, StringComparison.OrdinalIgnoreCase))
                    regions.RemoveAt(i);
        }

        [Header("Events")]
        public static Action<bool> OnDragSettingsChanged;

        public void SetUseOnlineMap(bool value)
        {
            useOnlineMap = value;
        }

        public void SetShowLabels(bool value)
        {
            showLabels = value;
        }

        public void SetEnableGrid(bool value)
        {
            enableGrid = value;
        }

        public void SetTotalHorizontalGridLines(int value)
        {
            totalHorizontalGridLines = Mathf.Max(1, value);
        }

        public void SetTotalVerticalGridLines(int value)
        {
            totalVerticalGridLines = Mathf.Max(1, value);
        }

        public void SetGridSpacing(float value)
        {
            gridSpacing = Mathf.Max(1f, value);
        }

        public void SetGridLineThickness(float value)
        {
            gridLineThickness = Mathf.Max(0.1f, value);
        }

        public void SetGridLineColor(Color value)
        {
            gridLineColor = value;
        }

        public void SetPerfectSquareGrid(bool value)
        {
            perfectSquareGrid = value;
        }

        public void SetEnableGridLabels(bool value)
        {
            enableGridLabels = value;
        }

        public void SetGridLabelOffsetIndex(int value)
        {
            gridLabelOffsetIndex = Mathf.Max(1, value);
        }

        public void SetZoomLabelOffset(bool value)
        {
            zoomLabelOffset = value;
        }

        public void SetZoomLabelOffsetMinIndex(int value)
        {
            zoomLabelOffsetMinIndex = Mathf.Max(1, value);
            if (zoomLabelOffsetMaxIndex < zoomLabelOffsetMinIndex)
                zoomLabelOffsetMaxIndex = zoomLabelOffsetMinIndex;
        }

        public void SetZoomLabelOffsetMaxIndex(int value)
        {
            zoomLabelOffsetMaxIndex = Mathf.Max(1, value);
            if (zoomLabelOffsetMaxIndex < zoomLabelOffsetMinIndex)
                zoomLabelOffsetMaxIndex = zoomLabelOffsetMinIndex;
        }

        public void SetVerticalGridLabelOffset(Vector2 value)
        {
            verticalGridLabelOffset = value;
        }

        public void SetHorizontalGridLabelOffset(Vector2 value)
        {
            horizontalGridLabelOffset = value;
        }

        public void SetGridLabelFontSize(int value)
        {
            gridLabelFontSize = Mathf.Max(1, value);
        }

        public void SetGridLabelColor(Color value)
        {
            gridLabelColor = value;
        }

        public void SetCanSelectMarkers(bool canSelect)
        {
            canSelectMarkers = canSelect;
        }

        public void SetRegions(List<MapFile> value)
        {
            regions = value ?? new List<MapFile>();
        }


        public void SetCanDragMarkers(bool canDrag)
        {
            canDragMarkers = canDrag;
            OnDragSettingsChanged?.Invoke(canDrag);
        }

        public void SetCurrentMapFile(MapFile newMapFile)
        {
            mapFile = newMapFile;
        }

    }
}