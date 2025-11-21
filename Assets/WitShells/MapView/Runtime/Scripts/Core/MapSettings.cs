using System;
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

        [Header("Events")]
        public static Action<bool> OnDragSettingsChanged;


        public void SetCanDragMarkers(bool canDrag)
        {
            canDragMarkers = canDrag;
            OnDragSettingsChanged?.Invoke(canDrag);
        }

    }
}