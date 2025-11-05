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

        public MapFile MapFile => mapFile;

    }
}