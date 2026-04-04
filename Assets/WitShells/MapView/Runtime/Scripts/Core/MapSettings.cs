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

        [Header("Marker Settings")]
        [SerializeField] private bool canDragMarkers = true;
        [SerializeField] private bool canSelectMarkers = true;

        public bool CanDragMarkers => canDragMarkers;
        public bool CanSelectMarkers => canSelectMarkers;

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


        public void SetCanDragMarkers(bool canDrag)
        {
            canDragMarkers = canDrag;
            OnDragSettingsChanged?.Invoke(canDrag);
        }

    }
}