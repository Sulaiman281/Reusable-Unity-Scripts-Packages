using UnityEngine;

namespace WitShells.MapView
{
    public partial class MapViewLayout
    {
        #region Zoom

        private void HandleZoomUpdate()
        {
            currentZoomLevel = Mathf.Clamp(currentZoomLevel + zoomVelocity, minZoomLevel, maxZoomLevel + .9f);
            float decay = Mathf.Pow(inertiaDamping, Time.unscaledDeltaTime * 60f);
            zoomVelocity *= decay;

            var zoom = (int)currentZoomLevel;
            if (Mathf.Abs(zoomVelocity) <= 0.01f && zoom != zoomLevel)
                SetZoomUpdate(zoom);

            var zoomDelta = currentZoomLevel - zoomLevel;
            ZoomLayer().localScale = Vector3.one * (1 + zoomDelta);
        }

        private void SetZoomUpdate(int value)
        {
            foreach (Transform child in ZoomLayer())
            {
                if (child.TryGetComponent<TileView>(out var tile))
                {
                    tile.gameObject.SetActive(false);
                    MoveTileToDirection.RemoveListener(tile.MoveTo);
                    Pool.Release(tile);
                }
            }
            ZoomLayer().gameObject.SetActive(false);

            var (lat, lon) = _hasClicked
                ? (SelectedCoordinates.Latitude, SelectedCoordinates.Longitude)
                : Utils.TileXYToLonLat(CenterCoordiante.x, CenterCoordiante.y, zoomLevel);

            zoomLevel = Mathf.Clamp(value, minZoomLevel, maxZoomLevel);
            ZoomLayer().gameObject.SetActive(true);
            ZoomLayer().localScale = Vector3.one;

            CenterCoordiante = Utils.LatLonToTile(lat, lon, zoomLevel);
            CenterTile = null;

            if (IsLocationBoundsLessThenScreen())
            {
                GenerateAllTiles();
                isFixedLayout = true;
            }
            else
            {
                GenerateScreenFillingTiles();
                isFixedLayout = false;
            }
        }

        #endregion
    }
}
