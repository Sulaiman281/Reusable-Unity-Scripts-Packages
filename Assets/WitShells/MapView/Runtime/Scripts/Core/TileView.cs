using UnityEngine;
using UnityEngine.UI;
using WitShells.DesignPatterns;

namespace WitShells.MapView
{
    public class TileView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RawImage tileImage;
        public RawImage TileImage => tileImage;

        [Header("Runtime")]
        public Vector2Int Coordinate;

        private Tile _tileData;

        public RectTransform RectTransform => transform as RectTransform;

        private bool _showLabels = false;

        private void OnDisable()
        {
            tileImage.texture = null;
        }

        public void ChangeLabelMode(bool showLabels)
        {
            if (_tileData == null) return;
            _showLabels = showLabels;
            SetData(_tileData, Coordinate);
        }

        public void SetData(Tile data, Vector2Int coordinate)
        {
            Coordinate = coordinate;
            name = $"Tile_{coordinate.x}_{coordinate.y}";
            SetData(data);
        }

        public void SetData(Tile data)
        {
            _tileData = data;
            var imageData = _showLabels ? data?.GeoData : data?.NormalData;
            WitLogger.Log($"Setting tile data for TileView {name}, ShowLabels: {_showLabels}, Data Null: {data == null}, ImageData Null: {imageData == null}");
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

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private float normX;
        [SerializeField] private float normY;

        [ContextMenu("Copy LatLon")]
        private void CopyLatLon()
        {
            var (lat, lon) = Utils.TileNormalizedToLatLon(Coordinate.x, Coordinate.y, 20, normX, normY);
            GUIUtility.systemCopyBuffer = $"{lat}, {lon}";
            Debug.Log($"Copied LatLon to clipboard: {lat}, {lon}");
        }

#endif
    }
}