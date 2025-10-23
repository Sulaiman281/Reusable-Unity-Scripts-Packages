using System;
using System.Numerics;
using SQLite;
using UnityEngine;

namespace WitShells.MapView
{
    [Serializable]
    public class Tile : IEquatable<Tile>
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [NotNull]
        public int TileX { get; set; }
        [NotNull]
        public int TileY { get; set; }
        [NotNull]
        public int Zoom { get; set; }
        public byte[] NormalData { get; set; }
        public byte[] GeoData { get; set; }

        public override string ToString()
        {
            return $"X: {TileX}, Y: {TileY}";
        }

        public bool Equals(Tile other)
        {
            if (other == null) return false;
            return this.TileX == other.TileX && this.TileY == other.TileY && this.Zoom == other.Zoom;
        }

        public bool Equals(Vector2Int other)
        {
            return this.TileX == other.x && this.TileY == other.y;
        }
    }
}