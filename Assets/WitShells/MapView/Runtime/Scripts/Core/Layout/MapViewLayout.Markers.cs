using UnityEngine;

namespace WitShells.MapView
{
    public partial class MapViewLayout
    {
        #region Marker Management

        private void HandleMarkerUpdate()
        {
            if (worldObjectMarkers == null || placableItems?.Items == null || placableItems.Items.Count == 0) return;

            foreach (var data in placableItems.Items)
            {

                bool exists = worldObjectMarkers.HasPlacableByData(data, out var placable);

                if (!HasWorldPositionInMapView(data, out var position))
                {
                    if (exists) worldObjectMarkers.ReleasePlacable(placable);
                    continue;
                }

                if (exists)
                {
                    placable.GameObject.transform.position = position;
                    placable.UpdateScale(currentZoomLevel, maxZoomLevel);
                }
                else
                {
                    placable = worldObjectMarkers.GetPlacable(data);
                    placable.UpdateCoordinates(data.Coordinates, data.ZoomLevel);
                }
            }
        }

        #endregion
    }
}
