using UnityEngine;
using UnityEngine.UI;

namespace WitShells.MapView
{
    public class TileView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RawImage tileImage;
        public RawImage TileImage => tileImage;

        [Header("Runtime")]
        public Vector2Int Coordinate;

        private bool _showLabels = false;

        private void OnDisable()
        {
            tileImage.texture = null;
        }

        public void SetData(Tile data, Vector2Int coordinate)
        {
            Coordinate = coordinate;
            name = $"Tile_{coordinate.x}_{coordinate.y}";
            SetData(data);
        }

        public void SetData(Tile data)
        {
            var imageData = _showLabels ? data?.GeoData : data?.NormalData;
            if (data != null && imageData != null)
            {
                var texture = new Texture2D(2, 2);
                texture.LoadImage(imageData);
                tileImage.texture = texture;
            }
            else
            {
                tileImage.texture = null;
            }
        }

        public void UpdateCoordinate(Vector2Int coordinate, int zoom, bool showLabels = false)
        {
            name = $"Tile_{coordinate.x}_{coordinate.y}";

            if (Coordinate == coordinate) return;

            Coordinate = coordinate;
            _showLabels = showLabels;
        }

        public void MoveTo(Vector3 position)
        {
            var targetPosition = transform.localPosition + position;
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * 10f);
        }
    }
}