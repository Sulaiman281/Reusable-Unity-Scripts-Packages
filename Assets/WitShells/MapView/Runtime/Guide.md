# MapView Asset — Step‑by‑Step User Guide

This guide shows how to add the MapView asset to any Unity project, configure it, and use it to render map tiles, pan/zoom, cache, and place objects (markers) on the map.

The MapView asset is composed of:

- `MapViewLayout` (main controller split across partial scripts for input, movement, tiles, zoom and utilities)
- `MapSettings` (ScriptableObject with provider/config)
- `MapTileManager` and `TileView` (tile lifecycle and rendering)
- Downloader subsystem (`DownloaderTiles`, `TilesDownloader`, `StreamTileFetcher`, `DbWorker`) for fetching/caching tiles
- Placements subsystem (`Placable`, `PlacableData`, `PlacablePrefabCatalog`) for placing prefabs at geo-coordinates
- Utilities (`DatabaseUtils`, `DbQuery`, `Utils`) supporting the above

---

## 1) Create Map Settings

`MapSettings` is a ScriptableObject containing tile provider configuration and UI defaults.

Steps:

- In Project window: Right‑click → Create → WitShells → MapView → `MapSettings`.
- Open the created asset and set:
  - Provider/API (URL pattern or key, depending on your provider integration).
  - Initial latitude/longitude and zoom.
  - Min/Max zoom range.
  - Tile size (pixels) and map grid size (how many tiles to render around center).
  - Cache settings (enable local DB cache, cache directory/path).

Tip: If your provider requires HTTPS and API keys, keep the key in `MapSettings` and never hardcode it in scripts.

---

## 2) Add MapView to a Scene

- Create an empty GameObject named `MapView`.
- Add component `MapViewLayout` (the main controller).
- In the inspector of `MapViewLayout`:
  - Assign your `MapSettings` asset.
  - Optionally assign a `PlacablePrefabCatalog` for markers/objects.
  - Configure input options (mouse/touch) and movement/zoom speeds.

Camera: Use a standard perspective camera looking at the map plane (MapViewLayout manages tile plane and transforms).

---

## 3) Understand the Core Scripts

- `MapViewLayout.cs`: Orchestrates map lifecycle; owns references to `MapSettings`, `MapTileManager`, downloader queue, and UI input hooks.

  - `MapViewLayout.Input.cs`: Handles user input (mouse/touch drag, wheel/pinch zoom).
  - `MapViewLayout.Movement.cs`: Smooth pans and inertia.
  - `MapViewLayout.Zoom.cs`: Clamps and applies zoom levels and triggers tile reload when zoom changes.
  - `MapViewLayout.Tiles.cs`: Computes which tile coordinates are visible and asks `MapTileManager` to load/unload.
  - `MapViewLayout.Markers.cs`: Manages markers/placables; converts geo to world and instantiates from catalog.
  - `MapViewLayout.Utils.cs`: Shared helpers (geo conversions, grid math, clamping, coordinate transforms).

- `MapSettings.cs`: ScriptableObject with provider and behavior configuration.

- `MapTileManager.cs`: Creates/destroys `TileView` GameObjects; requests tile images via the downloader/cache; updates tile materials.
- `TileView.cs`: MonoBehaviour rendering a single tile (material/texture assignment, coordinate reference).

- Downloader subsystem:

  - `DownloaderTiles.cs`: High-level tile download requests based on XYZ tile coordinates and provider URL.
  - `TilesDownloader.cs`: Manages a queue, concurrency limits, backoff/retry.
  - `StreamTileFetcher.cs`: Performs the actual HTTP stream download.
  - `DbWorker.cs`: Background thread to write/read tile bytes to the local database/cache.
  - `DatabaseUtils.cs`, `DbQuery.cs`: Utilities and query structs for cache lookup and storage.

- Placements subsystem:
  - `PlacablePrefabCatalog.cs`: Catalog ScriptableObject mapping logical marker types to prefabs.
  - `PlacableData.cs`: Data asset for a specific instance (lat/lon, type, any metadata).
  - `Placable.cs`: MonoBehaviour attached to spawned prefabs, holding runtime state (selection, hover, etc.).

---

## 4) Configure Tile Provider

The Downloader expects a tile URL pattern (e.g., `{z}/{x}/{y}.png`). You can:

- Use a public provider like OpenStreetMap (respect terms and rate limits).
- Use your own tile server.

In `MapSettings` set:

- Base URL, path pattern, and any query params (e.g., `?apiKey=...`).
- If you use retina tiles, set tile size accordingly.
- Optional user-agent header and request timeout.

---

## 6) Cache & Offline

Enable caching to reduce bandwidth and accelerate loading:

- In `MapSettings`, enable local cache.
- `DbWorker` writes tile bytes keyed by `{z,x,y}`.
- On startup, `DatabaseUtils` checks cache first; if found, `TileView` is updated immediately; otherwise a network request is queued.

Tip: Clear cache directory when changing providers or styles to avoid mismatched tiles.

---

## 7) Panning & Zooming

`MapViewLayout` manages interactive controls:

- Drag to pan (mouse/touch).
- Scroll wheel or pinch to zoom.
- Zoom is clamped between min/max from `MapSettings`.
- On zoom or pan, `MapViewLayout.Tiles.cs` recomputes visible grid and triggers `MapTileManager` to adjust tiles.

Tune movement/zoom (speeds, inertia) in `MapViewLayout` inspector.

---

## 8) Add Markers / Objects (Placables)

Steps:

1. Create `PlacablePrefabCatalog` (Right‑click → Create → WitShells → MapView → Placable Prefab Catalog). Assign prefabs (e.g., pin, vehicle, base).
2. Create one or more `PlacableData` assets (Right‑click → Create → WitShells → MapView → Placable Data):
   - Set Latitude/Longitude and type (prefab key) and metadata.
3. Assign the catalog to `MapViewLayout`.
4. At runtime, call `MapViewLayout.Markers` API to spawn from `PlacableData` (or enable auto-load if implemented). The map will convert geo coordinates to world positions and instantiate the prefab with `Placable` component.

Runtime API Example (pseudo):

```
// Given a MapViewLayout reference 'map'
map.AddPlacable(placableData); // spawns and positions prefab at lat/lon
map.RemovePlacable(id);        // removes marker
map.Focus(lat, lon, zoom);     // centers and zooms on target
```

---

## 9) Scripting: Common Tasks

- Center map at a coordinate:

```
mapViewLayout.SetCenter(latitude, longitude);
```

- Programmatically change zoom:

```
mapViewLayout.SetZoom(zoomLevel);
```

- Preload tiles around area:

```
mapViewLayout.PreloadTiles(lat, lon, radiusTiles);
```

- Listen for tile loaded:

```
mapTileManager.OnTileLoaded += (tile) => { /* update UI */ };
```

Note: Actual method names may differ slightly depending on your current version; inspect `MapViewLayout.*.cs` and `MapTileManager.cs` for available API.

---

## 10) Performance Tips

- Limit concurrent downloads in `TilesDownloader` to avoid provider rate limits.
- Keep grid size modest (e.g., 5×5 or 7×7 tiles) for smooth panning.
- Use texture compression on tiles if your platform allows it.
- Enable caching; prewarm tiles in main menu for a smoother first view.

---

## 11) Troubleshooting

- Black tiles or missing imagery:
  - Check provider URL and API key.
  - Verify network connectivity; inspect `StreamTileFetcher` logs.
- Slow or stuttering pan:
  - Reduce grid size, adjust movement smoothing.
  - Limit concurrent downloads.
- Wrong tile alignment:
  - Confirm tile coordinate math in `MapViewLayout.Utils.cs` and `TileView` transforms.
- Cache not used:
  - Ensure cache enabled in `MapSettings` and DB path writeable.

---

## 12) Minimal Setup Checklist

- Copy `MapView` folder into your project.
- Create and assign a `MapSettings` asset.
- Drop `MapViewLayout` onto a GameObject in your scene.
- Set camera to view the map plane.
- Press Play; verify tiles load and you can pan/zoom.
- (Optional) Configure `PlacablePrefabCatalog` and add `PlacableData` assets to spawn markers.

---

## 13) Extending MapView

- Custom Tile Providers: implement URL builder in `DownloaderTiles` or add provider enum in `MapSettings`.
- Elevation/Topo overlays: create layered `TileView` materials and blend.
- Path/Route rendering: convert polyline geo coords to world space; draw using LineRenderer or Mesh.
- Selection/Interaction: extend `Placable` to support hover, select, context menus.

---

## 14) Script Index (What Each Script Does)

- Core:
  - `MapViewLayout` (+ partials: Input, Movement, Zoom, Tiles, Utils): main controller for user interaction and tile lifecycle.
  - `MapSettings`: config asset for provider, cache, defaults.
  - `MapTileManager`: orchestrates tile load/unload; instantiates `TileView`.
  - `TileView`: visual component for a single XYZ tile.
  - `DatabaseUtils`, `DbQuery`, `Utils`: helpers for cache and math.
- Downloader:
  - `DownloaderTiles`: translates tile requests to provider URLs and triggers downloads.
  - `TilesDownloader`: queued, concurrent download worker.
  - `StreamTileFetcher`: low-level HTTP fetch.
  - `DbWorker`: background cache writer/reader.
- Placements:
  - `PlacablePrefabCatalog`: maps types to prefabs.
  - `PlacableData`: geo + type for each marker/object.
  - `Placable`: runtime behaviour for spawned marker prefabs.
- Models:
  - `Tile`: data model for a tile (XYZ, texture bytes).

---

## 15) Example: Quick Start Script

Attach this to any GameObject and assign references in the inspector.

```
using UnityEngine;
using WitShells.MapView; // if your namespace differs, adjust

public class MapQuickStart : MonoBehaviour
{
		public MapViewLayout map;
		public MapSettings settings;

		void Start()
		{
				map.Settings = settings; // assign
				map.SetCenter(37.7749, -122.4194); // San Francisco
				map.SetZoom(12);
		}
}
```

This initializes the map, centers it, and sets a zoom level. Add placables using the Placements subsystem as needed.
