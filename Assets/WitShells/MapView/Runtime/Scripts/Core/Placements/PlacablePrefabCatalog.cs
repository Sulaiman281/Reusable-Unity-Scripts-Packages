using UnityEngine;

namespace WitShells.MapView
{
    [CreateAssetMenu(fileName = "PlacablePrefabCatalog", menuName = "WitShells/MapView/Placable Prefab Catalog", order = 1)]
    public class PlacablePrefabCatalog : ScriptableObject
    {
        [System.Serializable]
        public struct PlacablePrefabEntry
        {
            public string Key;
            public GameObject Prefab;
        }

        [SerializeField] private PlacablePrefabEntry[] placablePrefabs;

        public GameObject GetPrefabByKey(string key)
        {
            foreach (var entry in placablePrefabs)
            {
                if (entry.Key == key)
                {
                    return entry.Prefab;
                }
            }
            return null;
        }
    }
}