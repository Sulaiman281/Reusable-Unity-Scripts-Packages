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
    public class PlacableData : IEquatable<PlacableData>
    {
        public string Id;

        [Tooltip("Prefab key from PlacablePrefabCatalog")]
        public string Key;
        public PlacementType PlacementType;
        public Coordinates Coordinates;
        public float ZoomLevel;

        // additional for consistency and accurate placement and also support for previous old datasets
        public int TileX;
        public int TileY;
        public float NormalizedX;
        public float NormalizedY;

        public PlacableData()
        {
            Id = Guid.NewGuid().ToString();
        }

        public PlacableData(string id, string key, PlacementType placementType, Coordinates coordinates, float zoomLevel, int tileX, int tileY, float normalizedX, float normalizedY)
        {
            Id = id;
            Key = key;
            PlacementType = placementType;
            Coordinates = coordinates;
            ZoomLevel = zoomLevel;
            TileX = tileX;
            TileY = tileY;
            NormalizedX = normalizedX;
            NormalizedY = normalizedY;
        }

        public PlacableData(string key, PlacementType placementType, Coordinates coordinates, float zoomLevel)
        {
            Id = Guid.NewGuid().ToString();
            Key = key;
            PlacementType = placementType;
            Coordinates = coordinates;
            ZoomLevel = zoomLevel;

            // calculate tile and normalized positions
            (TileX, TileY, NormalizedX, NormalizedY) = Utils.LatLonToTileNormalized(coordinates.Latitude, coordinates.Longitude, (int)zoomLevel);
        }

        public PlacableData(string key, PlacementType placementType, int tileX, int tileY, float normalizedX, float normalizedY, float zoomLevel)
        {
            Id = Guid.NewGuid().ToString();
            Key = key;
            PlacementType = placementType;
            TileX = tileX;
            TileY = tileY;
            NormalizedX = normalizedX;
            NormalizedY = normalizedY;
            ZoomLevel = zoomLevel;

            // calculate coordinates
            (double lat, double lon) = Utils.TileNormalizedToLatLon(tileX, tileY, (int)zoomLevel, normalizedX, normalizedY);
            Coordinates = new Coordinates
            {
                Latitude = lat,
                Longitude = lon
            };
        }

        public (double Lat, double Lon) GetLatLon()
        {
            return Utils.TileNormalizedToLatLon(TileX, TileY, (int)ZoomLevel, NormalizedX, NormalizedY);
        }

        public bool Equals(PlacableData other)
        {
            return Id == other.Id &&
                   Coordinates.Equals(other.Coordinates) &&
                   ZoomLevel == other.ZoomLevel &&
                   TileX == other.TileX &&
                   TileY == other.TileY &&
                   NormalizedX == other.NormalizedX &&
                   NormalizedY == other.NormalizedY;
        }

        public override bool Equals(object obj)
        {
            return obj is PlacableData other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Id?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ Coordinates.GetHashCode();
                hashCode = (hashCode * 397) ^ ZoomLevel.GetHashCode();
                hashCode = (hashCode * 397) ^ TileX;
                hashCode = (hashCode * 397) ^ TileY;
                hashCode = (hashCode * 397) ^ NormalizedX.GetHashCode();
                hashCode = (hashCode * 397) ^ NormalizedY.GetHashCode();
                return hashCode;
            }
        }
    }

    public interface IPlacableData<TData>
    {
        PlacableData Data { get; }
        TData CustomData { get; }
        void Initialize(PlacableData data, TData customData);
        void UpdateFromData();
    }
}