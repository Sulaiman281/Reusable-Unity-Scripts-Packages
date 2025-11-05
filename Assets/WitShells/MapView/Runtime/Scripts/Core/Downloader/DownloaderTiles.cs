using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using SQLite;
using UnityEngine;
using WitShells.DesignPatterns;
using WitShells.ThreadingJob;

namespace WitShells.MapView
{
    [Serializable]
    public struct MapFile
    {
        public string MapName;
        public Coordinates TopLeft;
        public Coordinates BottomRight;
        public int MinZoom;
        public int MaxZoom;
    }

    [Serializable]
    public class DownloaderTiles
    {
        private SQLiteConnection _dbConnection;
        private MapFile _mapFile;
        [SerializeField] private int _totalTiles = 0;
        [SerializeField] private int _tilesDownloaded = 0;
        [SerializeField]
        private float _progress = 0f;

        private int _completedFetchChains = 0;

        public string DirectoryPath => Path.Combine(Application.persistentDataPath, "OfflineMaps");
        public string FilePath => Path.Combine(DirectoryPath, $"{_mapFile.MapName}.db");

        private List<Tile> _downloadedTiles = new List<Tile>();

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

        public DownloaderTiles(MapFile mapFile)
        {
            _mapFile = mapFile;
        }


        public void DownloadMapFile()
        {
            // reset progress counter visible in inspector
            _progress = 0f;

            // Compute initial counts and initialize placeholders on background threads to avoid main-thread stalls.
            ComputeInitialDownloadedCountAsync((already) =>
            {
                // _totalTiles = already;
                _progress = _totalTiles == 0 ? 0f : Mathf.Clamp01((float)_tilesDownloaded / (float)_totalTiles);

                // Ensure placeholder rows are present (performed asynchronously)
                InitializePlaceholderTilesAsync();

                // Start streaming fetch chains from the min zoom (main-thread kickoff of streaming jobs is lightweight)
                var z = _mapFile.MinZoom;
                // First, fetch missing normal tiles (no labels)
                FetchMissingForZoom(z, showLabels: false);
                // Then fetch missing geo tiles (labels)
                FetchMissingForZoom(z, showLabels: true);
            });
        }

        private void ComputeInitialDownloadedCountAsync(Action<int> onComplete)
        {
            if (onComplete == null) return;
            try
            {
                // Capture FilePath on the main thread so the background query doesn't access Unity APIs
                var dbPath = FilePath;

                DbQuery.EnqueueQuery<Tuple<int, int>>(dbPath, (conn) =>
                {
                    int downloadedCount = 0;
                    int totalPossibleTiles = 0;

                    for (int z = _mapFile.MinZoom; z <= _mapFile.MaxZoom; z++)
                    {
                        var (xMin, xMax, yMin, yMax) = Utils.TileRangeForBounds(_mapFile.TopLeft.Latitude, _mapFile.TopLeft.Longitude, _mapFile.BottomRight.Latitude, _mapFile.BottomRight.Longitude, z);
                        int tilesForZoom = (xMax - xMin + 1) * (yMax - yMin + 1);
                        totalPossibleTiles += tilesForZoom * 2; // Normal + Geo data for each tile

                        try
                        {
                            var normalSql = "SELECT COUNT(1) FROM Tile WHERE Zoom = ? AND TileX BETWEEN ? AND ? AND TileY BETWEEN ? AND ? AND NormalData IS NOT NULL AND length(NormalData) > 0";
                            var geoSql = "SELECT COUNT(1) FROM Tile WHERE Zoom = ? AND TileX BETWEEN ? AND ? AND TileY BETWEEN ? AND ? AND GeoData IS NOT NULL AND length(GeoData) > 0";

                            var normalCount = conn.ExecuteScalar<int>(normalSql, z, xMin, xMax, yMin, yMax);
                            var geoCount = conn.ExecuteScalar<int>(geoSql, z, xMin, xMax, yMin, yMax);
                            downloadedCount += normalCount + geoCount;
                        }
                        catch { }
                    }
                    return new Tuple<int, int>(downloadedCount, totalPossibleTiles);
                }, (result) =>
                {
                    try
                    {
                        var downloadedCount = result.Item1;
                        var totalPossible = result.Item2;
                        // _tilesDownloaded = downloadedCount;

                        WitLogger.Log($"Initial state: {downloadedCount}/{totalPossible} tile data pieces already downloaded ({(downloadedCount * 100f / totalPossible):F1}% complete)");
                        onComplete(downloadedCount);
                    }
                    catch { }
                }, (ex) => { WitLogger.LogWarning($"ComputeInitialDownloadedCountAsync failed: {ex.Message}"); onComplete?.Invoke(0); });
            }
            catch (Exception ex)
            {
                WitLogger.LogWarning($"Error queuing initial count job: {ex.Message}");
                onComplete?.Invoke(0);
            }
        }

        private void InitializePlaceholderTilesAsync()
        {
            if (string.IsNullOrEmpty(FilePath)) return;

            // Capture values that must be accessed from the main thread before queuing the background work.
            var dbPath = FilePath;
            var topLeft = _mapFile.TopLeft;
            var bottomRight = _mapFile.BottomRight;
            var minZoom = _mapFile.MinZoom;
            var maxZoom = _mapFile.MaxZoom;

            WitLogger.Log($"Initializing placeholder tiles for zoom levels {minZoom}-{maxZoom}...");

            for (int z = minZoom; z <= maxZoom; z++)
            {
                int zoom = z; // capture per-iteration

                // Run a background job that computes the coordinate range and counts existing rows.
                // The job returns the beforeCount and the list of placeholders count; actual enqueueing
                // to DbWorker is done on the main thread (in the onComplete) to avoid calling
                // Unity APIs from worker threads.
                DbQuery.EnqueueQuery<Tuple<int, int, List<Tile>>>(dbPath, (conn) =>
                {
                    int beforeCount = 0;
                    int totalNeeded = 0;
                    try
                    {
                        var (xMin, xMax, yMin, yMax) = Utils.TileRangeForBounds(topLeft.Latitude, topLeft.Longitude, bottomRight.Latitude, bottomRight.Longitude, zoom);
                        totalNeeded = (xMax - xMin + 1) * (yMax - yMin + 1);
                        beforeCount = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM Tile WHERE Zoom = ? AND TileX BETWEEN ? AND ? AND TileY BETWEEN ? AND ?", zoom, xMin, xMax, yMin, yMax);
                    }
                    catch { }

                    var placeholders = new List<Tile>();
                    try
                    {
                        var (xMin, xMax, yMin, yMax) = Utils.TileRangeForBounds(topLeft.Latitude, topLeft.Longitude, bottomRight.Latitude, bottomRight.Longitude, zoom);
                        for (int x = xMin; x <= xMax; x++)
                        {
                            for (int y = yMin; y <= yMax; y++)
                            {
                                placeholders.Add(new Tile { TileX = x, TileY = y, Zoom = zoom });
                            }
                        }
                    }
                    catch { }

                    return new Tuple<int, int, List<Tile>>(beforeCount, totalNeeded, placeholders);
                }, (result) =>
                {
                    try
                    {
                        var beforeCount = result.Item1;
                        var totalNeeded = result.Item2;
                        var placeholders = result.Item3 ?? new List<Tile>();

                        int missingCount = totalNeeded - beforeCount;
                        WitLogger.Log($"Zoom {zoom}: {beforeCount}/{totalNeeded} tiles exist, {missingCount} missing, enqueueing {placeholders.Count} placeholders");

                        // Enqueue to DbWorker from main thread to avoid Unity API calls on worker threads.
                        try
                        {
                            DatabaseWriter.EnqueueTileBatch(dbPath, placeholders);
                        }
                        catch (Exception ex)
                        {
                            WitLogger.LogWarning($"Failed to enqueue placeholders for zoom {zoom}: {ex.Message}");
                        }
                    }
                    catch { }
                }, (ex) => { WitLogger.LogWarning($"InitializePlaceholderTiles job failed for zoom {zoom}: {ex.Message}"); });
            }
        }

        private void StoreInDatabase(List<Tile> tiles)
        {
            // Enqueue tiles to the background DB worker instead of writing directly here to avoid
            // concurrent-writer conflicts. We still update the in-memory counters so progress
            // reported in the inspector advances immediately. The DbWorker will log any write failures.
            if (tiles == null || tiles.Count == 0) return;

            try
            {
                DatabaseWriter.EnqueueTileBatch(FilePath, new List<Tile>(tiles));
            }
            catch (Exception ex)
            {
                WitLogger.LogError($"Error enqueuing tiles for DB persist: {ex.Message}");
            }
            finally
            {
                try { _progress = _totalTiles == 0 ? 0f : Mathf.Clamp01((float)_tilesDownloaded / (float)_totalTiles); } catch { _progress = 0f; }
            }
        }

        private void FetchMissingForZoom(int zoomLevel, bool showLabels)
        {
            // Use DbQuery to fetch the list of missing coordinates on a background thread to avoid blocking.
            try
            {
                string column = showLabels ? "GeoData" : "NormalData";
                string dataType = showLabels ? "geo" : "normal";
                var dbPath = FilePath; // capture main-thread-only path

                DbQuery.EnqueueQuery<Tuple<List<Vector2Int>, int, int>>(dbPath, (conn) =>
                {
                    // Calculate expected tiles for this zoom level
                    var (xMin, xMax, yMin, yMax) = Utils.TileRangeForBounds(_mapFile.TopLeft.Latitude, _mapFile.TopLeft.Longitude, _mapFile.BottomRight.Latitude, _mapFile.BottomRight.Longitude, zoomLevel);
                    int expectedTiles = (xMax - xMin + 1) * (yMax - yMin + 1);

                    // Get missing tiles
                    var rows = conn.Query<Tile>($"SELECT TileX, TileY, Zoom FROM Tile WHERE Zoom = ? AND ({column} IS NULL OR length({column}) = 0)", zoomLevel);
                    var toDownload = new List<Vector2Int>(rows.Count);
                    foreach (var r in rows) toDownload.Add(new Vector2Int(r.TileX, r.TileY));

                    // Get total tile count for this zoom level (actual rows in DB)
                    int totalTiles = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM Tile WHERE Zoom = ?", zoomLevel);

                    return new Tuple<List<Vector2Int>, int, int>(toDownload, totalTiles, expectedTiles);
                }, (result) =>
                {
                    var toDownload = result.Item1;
                    var totalTiles = result.Item2;
                    var expectedTiles = result.Item3;

                    // Check if we're missing placeholder rows
                    if (totalTiles < expectedTiles)
                    {
                        WitLogger.LogWarning($"Zoom {zoomLevel}: Missing {expectedTiles - totalTiles} placeholder tiles! Only {totalTiles}/{expectedTiles} rows exist in DB.");
                        WitLogger.LogWarning($"Run 'Store Empty Tiles In Database' or check InitializePlaceholderTilesAsync for this zoom level.");
                    }

                    if (toDownload == null || toDownload.Count == 0)
                    {
                        if (totalTiles == 0)
                        {
                            WitLogger.LogError($"Zoom {zoomLevel} ({dataType}): No tiles exist in database! Missing placeholder initialization.");
                        }
                        else
                        {
                            WitLogger.Log($"Zoom {zoomLevel} ({dataType}): All {totalTiles} tiles already have data, skipping download.");
                        }

                        // Continue to next zoom level even if no downloads needed
                        if (zoomLevel < _mapFile.MaxZoom)
                        {
                            int nextZoom = zoomLevel + 1;
                            FetchMissingForZoom(nextZoom, showLabels);
                        }
                        else
                        {
                            int completed = Interlocked.Increment(ref _completedFetchChains);
                            if (completed == 2)
                            {
                                WitLogger.Log("All missing tiles have been checked/fetched.");
                                if (_downloadedTiles.Count > 0)
                                {
                                    StoreInDatabase(new List<Tile>(_downloadedTiles));
                                    _downloadedTiles.Clear();
                                }
                                DisposeDatabase();
                            }
                        }
                        return;
                    }

                    int existingCount = totalTiles - toDownload.Count;
                    _totalTiles += totalTiles;
                    _tilesDownloaded += existingCount;

                    WitLogger.Log($"Zoom {zoomLevel} ({dataType}): {existingCount}/{totalTiles} tiles have data, downloading {toDownload.Count} missing tiles (expected: {expectedTiles})");

                    var fetcher = new StreamTileFetcher(dbPath, toDownload, zoomLevel, showLabels, true, false);
                    ThreadManager.Instance.EnqueueStreamingJob(
                        fetcher,
                        onProgress: (tiles) =>
                        {
                            if (tiles == null || tiles.Count == 0) return;
                            // Update DB with provided tile data (only fields present)
                            _downloadedTiles.AddRange(tiles);

                            _tilesDownloaded += tiles.Count;

                            if (_downloadedTiles.Count >= 100)
                            {
                                StoreInDatabase(new List<Tile>(_downloadedTiles));
                                _downloadedTiles.Clear();
                            }
                        },
                        onComplete: () =>
                        {
                            // Enqueue next zoom level if any
                            WitLogger.Log($"Completed {dataType} data fetch for zoom {zoomLevel}");

                            if (zoomLevel < _mapFile.MaxZoom)
                            {
                                int nextZoom = zoomLevel + 1;
                                FetchMissingForZoom(nextZoom, showLabels);
                            }
                            else
                            {
                                int completed = Interlocked.Increment(ref _completedFetchChains);

                                if (completed == 2)
                                {
                                    WitLogger.Log("All missing tiles have been fetched.");
                                    if (_downloadedTiles.Count > 0)
                                    {
                                        StoreInDatabase(new List<Tile>(_downloadedTiles));
                                        _downloadedTiles.Clear();
                                    }
                                    DisposeDatabase();
                                }
                            }
                        },
                        onError: (ex) => { WitLogger.LogError($"Error fetching missing {dataType} tiles for zoom {zoomLevel}: {ex.Message}"); }
                    );
                }, (ex) => { WitLogger.LogError($"Error preparing {dataType} downloads for zoom {zoomLevel}: {ex.Message}"); });
            }
            catch (Exception ex)
            {
                WitLogger.LogError($"Error preparing downloads for zoom {zoomLevel}: {ex.Message}");
            }
        }

        #region DB Management

        public bool CreateDatabase(out SQLiteConnection connection)
        {
            try
            {
                connection = DatabaseUtils.EnsureDatabaseWithSchema(FilePath);
                if (connection != null)
                {
                    _dbConnection = connection; // cache writer connection
                    return true;
                }
                else
                {
                    WitLogger.LogError("Failed to create database connection via DatabaseUtils");
                    return false;
                }
            }
            catch (Exception ex)
            {
                connection = null;
                WitLogger.LogError($"Error creating database: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checkpoint WAL and close/dispose the cached writer connection.
        /// </summary>
        public void DisposeDatabase()
        {
            if (_dbConnection == null) return;
            try
            {
                DatabaseUtils.SafeCloseConnection(_dbConnection, checkpoint: true);
            }
            catch { }
            finally
            {
                _dbConnection = null;
            }
        }

        #endregion
    }
}