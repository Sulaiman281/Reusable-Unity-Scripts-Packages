using System;
using UnityEngine;

namespace WitShells.MapView
{
    public enum PlacementType
    {
        WorldObject,
        CanvasUI
    }

    [Serializable]
    public struct PlacableData : IEquatable<PlacableData>
    {
        public PlacementType PlacementType;
        public Coordinates Coordinates;
        public float ZoomLevel;

        // additional for consistency and accurate placement and also support for previous old datasets
        public int TileX;
        public int TileY;
        public float NormalizedX;
        public float NormalizedY;

        public (double Lat, double Lon) GetLatLon()
        {
            return Utils.TileNormalizedToLatLon(TileX, TileY, (int)ZoomLevel, NormalizedX, NormalizedY);
        }

        public bool Equals(PlacableData other)
        {
            return Coordinates.Equals(other.Coordinates) &&
                   ZoomLevel == other.ZoomLevel &&
                   TileX == other.TileX &&
                   TileY == other.TileY &&
                   NormalizedX == other.NormalizedX &&
                   NormalizedY == other.NormalizedY;
        }
    }

    public class Placable : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] protected PlacableData placableData;

        // Helpers
        public Coordinates Coordinates => placableData.Coordinates;
        public float ZoomLevel => placableData.ZoomLevel;
        public PlacementType Type => placableData.PlacementType;
        public PlacableData Data => placableData;

        public void UpdateCoordinates(Coordinates newCoordinates, float newZoomLevel)
        {
            placableData.Coordinates = newCoordinates;
            placableData.ZoomLevel = newZoomLevel;
        }

        public void UpdateScale(float currentZoomLevel, float maxZoomLevel)
        {
            var delta = currentZoomLevel - placableData.ZoomLevel;
            var scale = Vector3.one + new Vector3(delta, delta, delta);
            // clamp to min .01 and max 30
            scale = Vector3.Max(scale, Vector3.one * 0.01f);
            scale = Vector3.Min(scale, Vector3.one * 30f);
            transform.localScale = scale;
        }
    }
}