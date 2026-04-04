using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SQLite;
using UnityEngine;
using UnityEngine.Events;
using WitShells.DesignPatterns;
using WitShells.DesignPatterns.Core;
using WitShells.ThreadingJob;

namespace WitShells.MapView
{
    public class MapTileManager : MonoSingleton<MapTileManager>, IDisposable
    {
        [Header("Map Online Settings")]
        public bool canFetchOnline => MapSettings.Instance.useOnlineMap;
        public bool canCacheTiles = true;

        [Header("Cache Settings")]
        [SerializeField] private int maxCachedTiles = 100;
        private Dictionary<Vector2Int, Tile> _cachedTiles = new Dictionary<Vector2Int, Tile>();

        public string DirectoryPath => Path.Combine(Application.persistentDataPath, "OfflineMaps");
        // Single unified offline database — all regions share this one file.
        private const string OfflineDbFileName = "maptiles.db";
        public string FilePath => Path.Combine(DirectoryPath, OfflineDbFileName);

        public bool HasValidFile => File.Exists(FilePath) && new FileInfo(FilePath).Length > 0;

        private SQLiteConnection _dbConnection;

        // ── Active region download ──────────────────────────────────────────────
        private DownloaderTiles _activeDownloader;
        
        public DownloaderTiles ActiveDownloader => _activeDownloader;

        public SQLiteConnection DbConnection
        {
            get
            {
                if (_dbConnection != null)
                {
                    return _dbConnection;
                }
                else if (CreateDatabase(out _dbConnection))
                {
                    return _dbConnection;
                }
                else return null;
            }
        }

        [Header("Events")]
        // Fired per-tile when a single tile is stored/fetched
        public UnityEvent<Vector2Int, Tile> OnTileFetched;
        // Fired when a batch of tiles is stored/fetched (optimized consumer should subscribe to this)
        public UnityEvent<List<Tile>> OnTilesFetched;

        void Start()
        {
            // Initialize DB connection without disposing it immediately.
            var conn = DbConnection;
            if (conn == null)
            {
                WitLogger.LogError("Failed to create or open the database.");
            }
            else
            {
                try
                {
                    // Optional: enable WAL for safer concurrent access (recommended if background threads write)
                    try { conn.Execute("PRAGMA journal_mode=WAL;"); } catch { /* ignore if not supported */ }

                    WitLogger.Log("Database opened successfully.");
                }
                catch (Exception ex)
                {
                    WitLogger.LogWarning($"Database init warning: {ex.Message}");
                }
            }
        }

        // void Update()
        // {
        //     PollDownloadProgress();
        // }

        // private void PollDownloadProgress()
        // {
        //     if (_activeDownloader == null || _downloadFinishFired) return;

        //     _onDownloadProgress?.Invoke(_activeDownloader.Progress);

        //     if (_activeDownloader.IsCancelled)
        //     {
        //         _downloadFinishFired = true;
        //         _onDownloadCancelled?.Invoke();
        //         ClearDownloadState();
        //         return;
        //     }

        //     if (_activeDownloader.TotalTiles > 0 && _activeDownloader.TilesDownloaded >= _activeDownloader.TotalTiles)
        //     {
        //         _downloadFinishFired = true;
        //         _onDownloadComplete?.Invoke();
        //         ClearDownloadState();
        //     }
        // }

        // private void ClearDownloadState()
        // {
        //     _activeDownloader = null;
        //     _onDownloadProgress = null;
        //     _onDownloadComplete = null;
        //     _onDownloadCancelled = null;
        // }

        public bool HasCachedTile(Vector2Int coordinate, out Tile tile, bool withLabels)
        {
            bool result = _cachedTiles.TryGetValue(coordinate, out tile);
            if (!result) return false;
            return withLabels ? (tile.GeoData != null && tile.GeoData.Length > 0) : (tile.NormalData != null && tile.NormalData.Length > 0);
        }

        public void StartStreamFetch(List<Vector2Int> enqueuedTiles, int zoomLevel, bool showLabels)
        {
            if (enqueuedTiles.Count == 0) return;

            var streamTileFetcher = new StreamTileFetcher(FilePath, enqueuedTiles, zoomLevel, showLabels, canFetchOnline);
            ThreadManager.Instance.EnqueueStreamingJob(
                streamTileFetcher,
                onProgress: (tiles) =>
                {
                    if (tiles == null || tiles.Count == 0) return;
                    StoreTilesInCache(tiles);
                },
                onComplete: () =>
                {
                    WitLogger.Log("Completed streaming tile fetch.");
                },
                onError: (ex) =>
                {
                    WitLogger.LogError($"Error during streaming tile fetch: {ex.Message}");
                });
        }


        public void CancelFetch(string threadId)
        {
            ThreadManager.Instance.CancelJob(threadId);
        }

        /// <summary>
        /// Starts downloading all tiles for <paramref name="region"/> into the unified offline DB.
        /// Only missing tiles are fetched — already-downloaded tiles are skipped automatically.
        /// </summary>
        /// <param name="region">Region bounds and zoom range to download.</param>
        /// <param name="onProgress">Called every frame with 0–1 progress while downloading.</param>
        /// <param name="onComplete">Called once when all tiles have been saved.</param>
        /// <param name="onCancelled">Called if the download is cancelled via <see cref="CancelRegionDownload"/>.</param>
        public void StartRegionDownload(MapFile region, Action<float> onProgress = null, Action onComplete = null, Action onCancelled = null)
        {
            if (_activeDownloader.Progress > 0f && _activeDownloader.Progress < 1f)
            {
                WitLogger.LogWarning("A region download is already in progress. Call CancelRegionDownload() first.");
                return;
            }

            // Redirect DownloaderTiles to write into the unified DB by matching the constant filename.
            var unifiedRegion = new MapFile
            {
                MapName = System.IO.Path.GetFileNameWithoutExtension(OfflineDbFileName), // "maptiles"
                TopLeft = region.TopLeft,
                BottomRight = region.BottomRight,
                MinZoom = region.MinZoom,
                MaxZoom = region.MaxZoom,
            };

            _activeDownloader = new DownloaderTiles(unifiedRegion);
            _activeDownloader.DownloadMapFile();
        }

        /// <summary>Cancels the currently running region download, if any.</summary>
        public void CancelRegionDownload()
        {
            _activeDownloader?.CancelDownload();
        }

        // ── Region coverage / availability API ─────────────────────────────────

        /// <summary>
        /// Asynchronously checks what fraction of <paramref name="region"/>'s tiles are already stored
        /// in the offline DB. Returns 0.0 (nothing downloaded) to 1.0 (region fully available).
        /// </summary>
        public void GetRegionCoverage(MapFile region, UnityAction<float> onResult, UnityAction<Exception> onError = null)
        {
            var dbPath = FilePath;
            var topLeft = region.TopLeft;
            var bottomRight = region.BottomRight;
            int minZoom = region.MinZoom;
            int maxZoom = region.MaxZoom;

            DbQuery.EnqueueQuery<float>(dbPath, conn =>
            {
                int total = 0, downloaded = 0;
                for (int z = minZoom; z <= maxZoom; z++)
                {
                    var (xMin, xMax, yMin, yMax) = Utils.TileRangeForBounds(
                        topLeft.Latitude, topLeft.Longitude,
                        bottomRight.Latitude, bottomRight.Longitude, z);
                    int tilesForZoom = (xMax - xMin + 1) * (yMax - yMin + 1);
                    total += tilesForZoom * 2; // normal + geo per tile

                    const string normalSql = "SELECT COUNT(1) FROM Tile WHERE Zoom=? AND TileX BETWEEN ? AND ? AND TileY BETWEEN ? AND ? AND NormalData IS NOT NULL AND length(NormalData)>0";
                    const string geoSql = "SELECT COUNT(1) FROM Tile WHERE Zoom=? AND TileX BETWEEN ? AND ? AND TileY BETWEEN ? AND ? AND GeoData IS NOT NULL AND length(GeoData)>0";
                    downloaded += conn.ExecuteScalar<int>(normalSql, z, xMin, xMax, yMin, yMax);
                    downloaded += conn.ExecuteScalar<int>(geoSql, z, xMin, xMax, yMin, yMax);
                }
                return total == 0 ? 0f : Mathf.Clamp01((float)downloaded / total);
            }, onResult, onError);
        }

        /// <summary>
        /// Asynchronously checks whether <paramref name="region"/> is fully available offline.
        /// Calls <paramref name="onResult"/> with true if coverage is 100%, false otherwise.
        /// </summary>
        public void IsRegionAvailable(MapFile region, Action<bool> onResult, UnityAction<Exception> onError = null)
        {
            GetRegionCoverage(region, coverage => onResult?.Invoke(coverage >= 1.0f), onError);
        }

        /// <summary>
        /// Looks up <paramref name="regionName"/> in <see cref="MapSettings.Regions"/> then checks
        /// availability. Calls <paramref name="onResult"/> with false if the name is not in the catalog.
        /// </summary>
        public void IsRegionAvailable(string regionName, Action<bool> onResult, UnityAction<Exception> onError = null)
        {
            if (!MapSettings.Instance.TryGetRegion(regionName, out var region))
            {
                onResult?.Invoke(false);
                return;
            }
            IsRegionAvailable(region, onResult, onError);
        }

        public void FetchTile(Vector2Int coordinate, int zoomLevel, bool showLabels, UnityAction<Tile> onComplete, out string threadId)
        {
            // Check cache first
            if (HasCachedTile(coordinate, out var cachedTile, showLabels))
            {
                onComplete?.Invoke(cachedTile);
                threadId = string.Empty;
                return;
            }

            threadId = ThreadManager.Instance.EnqueueJob(
                new FetchTileJob(FilePath, coordinate, zoomLevel, showLabels, canFetchOnline, canCacheTiles),
                onComplete: (result) =>
                {
                    onComplete?.Invoke(result);
                    StoreInCache(result);
                },
                onError: (ex) =>
                {
                    WitLogger.LogError($"Error fetching tile: {ex.Message}");
                    onComplete?.Invoke(null);
                });
        }

        private void StoreInCache(Tile tile, bool invokeSingleEvent = true)
        {
            if (tile == null) return;

            var coord = new Vector2Int(tile.TileX, tile.TileY);
            // Evict when reaching capacity
            if (_cachedTiles.Count >= maxCachedTiles && _cachedTiles.Count > 0)
            {
                var toRemove = _cachedTiles.First();
                _cachedTiles.Remove(toRemove.Key);
            }
            _cachedTiles[coord] = tile;
            // Notify single-tile listeners (main-thread callers expect this)
            if (invokeSingleEvent)
            {
                try { OnTileFetched?.Invoke(coord, tile); } catch { }
            }
        }

        private void StoreTilesInCache(List<Tile> tiles)
        {
            foreach (var tile in tiles)
            {
                // prevent per-tile single events during bulk store to avoid duplicate notifications
                StoreInCache(tile, invokeSingleEvent: false);
            }
            // Notify batch listeners after storing all tiles. Use try/catch to avoid propagation.
            try { OnTilesFetched?.Invoke(tiles); } catch { }
        }


        #region Database Management

        public bool CreateDatabase(out SQLiteConnection connection)
        {
            try
            {
                connection = DatabaseUtils.EnsureDatabaseWithSchema(FilePath);
                return connection != null;
            }
            catch (Exception ex)
            {
                connection = null;
                WitLogger.LogError($"Error creating database: {ex.Message}");
                return false;
            }
        }

        public Tile GetCenterTile(int zoomLevel)
        {
            var centerTile = DbConnection.Query<Tile>(
                            "SELECT * FROM Tile WHERE Zoom = ? ORDER BY TileX, TileY LIMIT 1 OFFSET " +
                            "(SELECT COUNT(*) FROM Tile WHERE Zoom = ?) / 2",
                            zoomLevel, zoomLevel
                        ).FirstOrDefault();
            return centerTile;
        }

        /// <summary>
        /// Asynchronous version of GetCenterTile - runs the query on the ThreadManager and returns on main thread.
        /// </summary>
        public void GetCenterTileAsync(int zoomLevel, UnityAction<Tile> onComplete, UnityAction<Exception> onError = null)
        {
            string sql = "SELECT * FROM Tile WHERE Zoom = ? ORDER BY TileX, TileY LIMIT 1 OFFSET (SELECT COUNT(*) FROM Tile WHERE Zoom = ?) / 2";
            DbQuery.EnqueueQuery(TileDbPath(), conn => conn.Query<Tile>(sql, zoomLevel, zoomLevel).FirstOrDefault(), onComplete, onError);
        }

        public Tile GetTile(Vector2Int coordinate, int zoomLevel)
        {
            return GetTile(coordinate.x, coordinate.y, zoomLevel, DbConnection);
        }

        /// <summary>
        /// Asynchronous version of GetTile - runs the query on a background thread and returns the result on the main thread.
        /// </summary>
        public void GetTileAsync(Vector2Int coordinate, int zoomLevel, UnityAction<Tile> onComplete, UnityAction<Exception> onError = null)
        {
            DbQuery.EnqueueQuery(TileDbPath(), conn => GetTile(coordinate.x, coordinate.y, zoomLevel, conn), onComplete, onError);
        }

        private string TileDbPath()
        {
            return FilePath;
        }

        public static Tile GetTile(int x, int y, int zoomLevel, SQLiteConnection connection)
        {
            var tile = connection.Query<Tile>(
                            "SELECT * FROM Tile WHERE TileX = ? AND TileY = ? AND Zoom = ? LIMIT 1",
                            x, y, zoomLevel
                        ).FirstOrDefault();

            return tile;
        }

        public void RevealDatabaseInFileExplorer()
        {
            var path = FilePath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                WitLogger.LogWarning($"Database file not found: {path}");
                return;
            }

#if UNITY_EDITOR
            try
            {
                UnityEditor.EditorUtility.RevealInFinder(path);
            }
            catch (Exception ex)
            {
                WitLogger.LogError($"RevealInFinder failed: {ex}");
            }
#elif !UNITY_ANDROID
            try
            {
            var dir = Path.GetDirectoryName(path);
            if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
            {
                // select the file in Explorer
                Process.Start("explorer.exe", $"/select,\"{path}\"");
            }
            else if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor)
            {
                // reveal in Finder
                Process.Start("open", $"-R \"{path}\"");
            }
            else
            {
                // Linux: open containing folder
                Process.Start("xdg-open", dir);
            }
            }
            catch (Exception ex)
            {
                WitLogger.LogError($"Could not open file explorer: {ex}");
            }
#else
            WitLogger.LogWarning("File explorer reveal not supported on Android platform");
#endif
        }


        #endregion

        public void Dispose()
        {
            DatabaseUtils.SafeCloseConnection(_dbConnection, checkpoint: true);
            _dbConnection = null;

            // Dispose any DbWorker associated with this database file so background writers exit
            try { DatabaseWriter.DisposeWriter(FilePath); } catch { }

            _cachedTiles.Clear();
        }

        protected override void OnDestroy()
        {
            // Ensure background DB worker is disposed when the manager is destroyed.
            try { Dispose(); } catch { }
            base.OnDestroy();
        }

        #region Test Logs

#if UNITY_EDITOR

        [ContextMenu("Log Database Path")]
        public void LogDatabasePath()
        {
            WitLogger.Log($"Database Path: {FilePath}");
        }

        [ContextMenu("Reveal Database In Explorer")]
        public void RevealDatabaseInExplorer()
        {
            RevealDatabaseInFileExplorer();
        }

#endif

        #endregion
    }
}