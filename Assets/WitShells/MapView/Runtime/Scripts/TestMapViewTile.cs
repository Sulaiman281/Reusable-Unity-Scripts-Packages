// using UnityEngine;
// using UnityEngine.UI;
// using WitShells.ThreadingJob;

// public class TestMapViewTile : MonoBehaviour
// {
//     [SerializeField] private SpriteRenderer targetImage;
//     public string baseUrl = "https://mt1.google.com/vt/lyrs=y&x={x}&y={y}&z={z}";
//     public float lat = 37.7749f;
//     public float lon = -122.4194f;
//     public float z = 3;
//     public Vector2Int tileXY = new Vector2Int(0, 0);

//     [ContextMenu("Fetch Tile")]
//     public void FetchTile()
//     {
//         WitShells.MapView.Utils.LatLonToTileXY(lat, lon, z, out int x, out int y);

//         tileXY = WitShells.MapView.Utils.LatLonToTile(lat, lon, (int)z);

//         string url = baseUrl.Replace("{x}", x.ToString())
//                              .Replace("{y}", y.ToString())
//                              .Replace("{z}", z.ToString());
//         var fetchJob = new WitShells.MapView.FetchTileJob(url);

//         ThreadManager.Instance.EnqueueJob(fetchJob, onComplete: (result) =>
//         {
//             Debug.Log($"Fetched tile {x},{y} at zoom {z}, size: {(result != null ? result.Length : 0)} bytes");
//             if (result != null)
//             {
//                 targetImage.sprite = Sprite.Create(WitShells.MapView.Utils.BytesToTexture(result), new Rect(0, 0, 100, 100), Vector2.zero);
//             }
//         }, onError: (ex) =>
//         {
//             Debug.LogError($"Error fetching tile: {ex.Message}");
//         });

//     }
// }