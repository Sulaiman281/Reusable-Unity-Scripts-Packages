using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using UnityEngine;
using WitShells.ThreadingJob;

namespace WitShells.MapView
{
    // Reports progress as batches (List<Tile>) up to a maximum per batch.
    // Note: this fetcher reads existing tiles from the DB, and if missing will download from online
    // based on the flags passed in, but it will NOT persist downloaded tiles to the DB.
    public class StreamTileFetcher : ThreadJob<List<Tile>>
    {
        private readonly List<Vector2Int> _tilesToFetch;
        private readonly int _zoomLevel;
        private readonly bool _showLabels;
        private readonly bool _onlineDownload;
        private readonly bool _useDatabase;
        public readonly string database;
        // configurable limits
        private readonly int _queryChunkSize = 250;       // number of tiles per SELECT chunk to avoid SQLite param limits
        private readonly int _maxConcurrentDownloads = 6; // parallel download concurrency

        public StreamTileFetcher(string database, List<Vector2Int> tilesToFetch, int zoomLevel, bool showLabels, bool onlineDownload = true, bool useDatabase = true)
        {
            this.database = database;
            _tilesToFetch = tilesToFetch;
            _zoomLevel = zoomLevel;
            _showLabels = showLabels;
            _onlineDownload = onlineDownload;
            _useDatabase = useDatabase;

            IsStreaming = true;
            IsAsync = true;
        }

        public override async Task ExecuteStreamingAsync(Action<List<Tile>> onProgress, Action onComplete = null)
        {
            try
            {
                var toDownload = new List<Vector2Int>();
                Dictionary<Vector2Int, Tile> existingTiles = new Dictionary<Vector2Int, Tile>();
                var tilesToPersist = new List<Tile>();
                // Read existing tiles using a read-only connection created on this thread.
                if (_useDatabase)
                {

                    using (var dbRead = DatabaseUtils.CreateReadOnlyConnection(database))
                    {
                        if (dbRead == null)
                        {
                            throw new InvalidOperationException($"Failed to open read-only database connection to '{database}'");
                        }
                        
                        ConcurrentLoggerBehaviour.Enqueue($"Fetching from {database} for zoom {_zoomLevel}, {_tilesToFetch.Count} tiles");

                        // Prefer a single bulk query when the number of tiles is reasonable to avoid
                        // multiple round-trips. Fall back to chunked queries if the list is very large.
                        const int singleQueryThreshold = 500; // safe heuristic; adjust if needed
                        if (_tilesToFetch.Count <= singleQueryThreshold)
                        {
                            existingTiles = BulkFetchTilesSingle(dbRead, _tilesToFetch, _zoomLevel);
                        }
                        else
                        {
                            existingTiles = BulkFetchTilesChunked(dbRead, _tilesToFetch, _zoomLevel, _queryChunkSize);
                        }
                    }

                    // Build download list by checking existing tiles and data presence.
                    // Emit all tiles that are already present in the DB as a single bulk progress update.
                    var presentTiles = new List<Tile>();

                    foreach (var coord in _tilesToFetch)
                    {
                        if (existingTiles.TryGetValue(coord, out var tile))
                        {
                            var dataPresent = (_showLabels ? tile.GeoData : tile.NormalData) is { Length: > 0 };
                            if (dataPresent)
                            {
                                presentTiles.Add(tile);
                                continue;
                            }

                            if (!_onlineDownload) continue;
                            toDownload.Add(coord);
                        }
                        else
                        {
                            if (!_onlineDownload) continue;
                            toDownload.Add(coord);
                        }
                    }

                    // Invoke a single bulk progress update for tiles fetched from DB so consumers get
                    // a full snapshot immediately.
                    if (presentTiles.Count > 0)
                    {
                        ConcurrentLoggerBehaviour.Enqueue($"Emitting {presentTiles.Count} tiles from DB for zoom {_zoomLevel}");
                        try { onProgress?.Invoke(presentTiles); } catch { }
                    }
                }
                else
                {
                    // If not using database, all tiles need to be downloaded if online download is allowed.
                    if (_onlineDownload)
                    {
                        toDownload = new List<Vector2Int>(_tilesToFetch);
                    }
                    else
                    {
                        throw new InvalidOperationException("Cannot fetch tiles: database usage is disabled and online download is not allowed.");
                    }
                }

                // Download tiles with bounded concurrency
                using var semaphore = new System.Threading.SemaphoreSlim(_maxConcurrentDownloads);
                var downloadTasks = new List<Task>(toDownload.Count);

                // Buffer progress updates to avoid flooding the main-thread action queue.
                var progressBuffer = new List<Tile>();
                var progressLock = new object();
                const int progressBatchSize = 20; // flush every N tiles
                var lastFlush = DateTime.UtcNow;
                const double maxFlushIntervalSeconds = 0.5; // ensure UI updates at least every 0.5s

                foreach (var coord in toDownload)
                {
                    var task = Task.Run(async () =>
                    {
                        await semaphore.WaitAsync().ConfigureAwait(false);
                        try
                        {
                            var url = Utils.MakeTileUrl(_showLabels, coord.x, coord.y, _zoomLevel);
                            var data = await FetchTileJob.DownloadTile(url).ConfigureAwait(false);
                            if (data == null || data.Length == 0)
                            {
                                ConcurrentLoggerBehaviour.Enqueue($"Empty tile downloaded {coord.x},{coord.y} z{_zoomLevel}", LogType.Warning);
                                return;
                            }

                            Tile resultingTile;
                            if (existingTiles.TryGetValue(coord, out var existingTile))
                            {
                                // fill missing data in-memory only
                                if (_showLabels) existingTile.GeoData = data; else existingTile.NormalData = data;
                                resultingTile = existingTile;
                            }
                            else
                            {
                                resultingTile = new Tile { TileX = coord.x, TileY = coord.y, Zoom = _zoomLevel };
                                if (_showLabels) resultingTile.GeoData = data; else resultingTile.NormalData = data;
                            }

                            // Buffer the progress update and flush in batches or on timer to reduce main-thread enqueue pressure.
                            List<Tile> flushBatch = null;
                            lock (progressLock)
                            {
                                progressBuffer.Add(resultingTile);
                                var now = DateTime.UtcNow;
                                var shouldFlushBySize = progressBuffer.Count >= progressBatchSize;
                                var shouldFlushByTime = (now - lastFlush).TotalSeconds >= maxFlushIntervalSeconds;
                                if (shouldFlushBySize || shouldFlushByTime)
                                {
                                    flushBatch = new List<Tile>(progressBuffer);
                                    progressBuffer.Clear();
                                    lastFlush = now;
                                }
                            }

                            if (flushBatch != null)
                            {
                                try { onProgress?.Invoke(flushBatch); } catch { }
                            }

                            // collect for optional DB persistence later
                            if (_useDatabase)
                            {
                                lock (tilesToPersist)
                                {
                                    tilesToPersist.Add(resultingTile);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ConcurrentLoggerBehaviour.Enqueue($"Download error {coord.x},{coord.y} z{_zoomLevel}: {ex.Message}", LogType.Error);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    downloadTasks.Add(task);
                }

                // Wait for all downloads to finish
                await Task.WhenAll(downloadTasks).ConfigureAwait(false);



                ConcurrentLoggerBehaviour.Enqueue($"Completed streaming fetch for zoom {_zoomLevel} downloaded {toDownload.Count} tiles");

                // Flush any remaining buffered progress updates.
                lock (progressLock)
                {
                    if (progressBuffer.Count > 0)
                    {
                        try { onProgress?.Invoke(new List<Tile>(progressBuffer)); } catch { }
                        progressBuffer.Clear();
                    }
                }

                // If requested, enqueue downloaded tiles to the background DB worker to persist them.
                // This avoids concurrent writer conflicts by serializing writes per DB file.
                if (_useDatabase && tilesToPersist.Count > 0)
                {
                    try
                    {
                        DatabaseWriter.EnqueueTileBatch(database, new List<Tile>(tilesToPersist));
                    }
                    catch (Exception ex)
                    {
                        ConcurrentLoggerBehaviour.Enqueue($"Error enqueuing downloaded tiles to DbWorker: {ex.Message}", LogType.Error);
                    }
                }

                onComplete?.Invoke();
            }
            catch (Exception ex)
            {
                Exception = ex;
                ConcurrentLoggerBehaviour.Enqueue($"ExecuteStreamingAsync top-level error: {ex.Message}", LogType.Error);
                throw;
            }
        }

        /// <summary>
        /// Chunked fetch that avoids SQLite parameter limits by querying tiles in smaller groups.
        /// </summary>
        public static Dictionary<Vector2Int, Tile> BulkFetchTilesChunked(SQLiteConnection db, List<Vector2Int> tilesToFetch, int zoomLevel, int chunkSize = 200)
        {
            var result = new Dictionary<Vector2Int, Tile>(tilesToFetch.Count);
            if (tilesToFetch == null || tilesToFetch.Count == 0) return result;

            for (int start = 0; start < tilesToFetch.Count; start += chunkSize)
            {
                var count = Math.Min(chunkSize, tilesToFetch.Count - start);
                var slice = tilesToFetch.GetRange(start, count);
                var args = new List<object> { zoomLevel };
                var sb = new StringBuilder();
                sb.Append("SELECT * FROM Tile WHERE Zoom = ? AND (");

                for (int i = 0; i < slice.Count; i++)
                {
                    if (i > 0) sb.Append(" OR ");
                    sb.Append("(TileX = ? AND TileY = ?)");
                    args.Add(slice[i].x);
                    args.Add(slice[i].y);
                }

                sb.Append(");");
                var rows = db.Query<Tile>(sb.ToString(), args.ToArray());
                foreach (var t in rows)
                {
                    var key = new Vector2Int(t.TileX, t.TileY);
                    result[key] = t;
                }
            }

            return result;
        }

        /// <summary>
        /// Attempt a single bulk SELECT for all requested tiles. If the list is empty returns an empty map.
        /// Note: may fail if the number of SQL parameters exceeds SQLite limits; caller should
        /// decide whether to call this or fallback to chunked variant.
        /// </summary>
        private static Dictionary<Vector2Int, Tile> BulkFetchTilesSingle(SQLiteConnection db, List<Vector2Int> tilesToFetch, int zoomLevel)
        {
            var result = new Dictionary<Vector2Int, Tile>(tilesToFetch.Count);
            if (tilesToFetch == null || tilesToFetch.Count == 0) return result;

            var args = new List<object> { zoomLevel };
            var sb = new StringBuilder();
            sb.Append("SELECT * FROM Tile WHERE Zoom = ? AND (");

            for (int i = 0; i < tilesToFetch.Count; i++)
            {
                if (i > 0) sb.Append(" OR ");
                sb.Append("(TileX = ? AND TileY = ?)");
                args.Add(tilesToFetch[i].x);
                args.Add(tilesToFetch[i].y);
            }

            sb.Append(");");
            var rows = db.Query<Tile>(sb.ToString(), args.ToArray());
            foreach (var t in rows)
            {
                var key = new Vector2Int(t.TileX, t.TileY);
                result[key] = t;
            }

            return result;
        }

        // Remove the obsolete TryApplyPragmas method since DatabaseUtils handles pragma setup

        // logging is handled by ConcurrentLoggerBehaviour (console)
    }
}