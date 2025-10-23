using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using UnityEngine;
using WitShells.ThreadingJob;

namespace WitShells.MapView
{
    public class StreamTileFetcher : ThreadJob<Tile>
    {
        private readonly List<Vector2Int> _tilesToFetch;
        private readonly int _zoomLevel;
        private readonly bool _showLabels;
        private readonly bool _onlineDownload;
        private readonly bool _cacheTile;
        public readonly string database;

        public SQLiteConnection DbConnection
        {
            get
            {
                if (File.Exists(database))
                {
                    return new SQLiteConnection(database);
                }
                else
                {
                    throw new FileNotFoundException($"Database file not found at path: {database}");
                }
            }
        }

        public StreamTileFetcher(string database, List<Vector2Int> tilesToFetch, int zoomLevel, bool showLabels, bool onlineDownload = true, bool cacheTile = true)
        {
            this.database = database;
            _tilesToFetch = tilesToFetch;
            _zoomLevel = zoomLevel;
            _showLabels = showLabels;
            _onlineDownload = onlineDownload;
            _cacheTile = cacheTile;
            IsStreaming = true;
            IsAsync = true;
        }

        public override async Task ExecuteStreamingAsync(Action<Tile> onProgress, Action onComplete = null)
        {
            try
            {
                using var db = new SQLiteConnection(database);
                // Optional pragmas for speed (safe for read-mostly workloads)
                try
                {
                    db.Execute("PRAGMA journal_mode=WAL;");
                    db.Execute("PRAGMA synchronous=NORMAL;");
                }
                catch { /* ignore */ }

                var existingTiles = BulkFetchTiles(db, _tilesToFetch, _zoomLevel);

                var toDownload = new List<Vector2Int>();
                var toCache = new ConcurrentQueue<Tile>();

                foreach (var coord in _tilesToFetch)
                {
                    if (existingTiles.TryGetValue(coord, out var tile))
                    {
                        var b64 = _showLabels ? tile.GeoData : tile.NormalData;
                        if (b64 != null && b64.Length > 0)
                        {
                            onProgress?.Invoke(tile);
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

                const int maxConcurrentDownloads = 6;
                using var semaphore = new System.Threading.SemaphoreSlim(maxConcurrentDownloads);
                var downloadTasks = new List<Task>();

                foreach (var coord in toDownload)
                {
                    var task = Task.Run(async () =>
                    {
                        await semaphore.WaitAsync().ConfigureAwait(false);
                        try
                        {
                            var url = Utils.MakeTileUrl(_showLabels, coord.x, coord.y, _zoomLevel);
                            var data = await FetchTileJob.DownloadTile(url);
                            if (data == null || data.Length == 0)
                            {
                                return;
                            }

                            if (existingTiles.TryGetValue(coord, out var existingTile))
                            {
                                if (_showLabels)
                                {
                                    existingTile.GeoData = data;
                                }
                                else
                                {
                                    existingTile.NormalData = data;
                                }
                                if (_cacheTile)
                                {
                                    toCache.Enqueue(existingTile);
                                }
                                onProgress?.Invoke(existingTile);
                                return;
                            }

                            var tile = new Tile
                            {
                                TileX = coord.x,
                                TileY = coord.y,
                                Zoom = _zoomLevel,
                            };

                            if (_showLabels)
                            {
                                tile.GeoData = data;
                            }
                            else
                            {
                                tile.NormalData = data;
                            }

                            if (_cacheTile)
                            {
                                toCache.Enqueue(tile);
                            }

                            onProgress?.Invoke(tile);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    downloadTasks.Add(task);
                }

                await Task.WhenAll(downloadTasks).ConfigureAwait(false);
                downloadTasks.Clear();

                if (_cacheTile && toCache.Count > 0)
                {
                    try
                    {
                        db.RunInTransaction(() =>
                        {
                            foreach (var t in toCache)
                            {
                                db.InsertOrReplace(t);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error caching tiles: {ex.Message}");
                    }
                }

                onComplete?.Invoke();
            }
            catch (Exception ex)
            {
                Exception = ex;
                throw;
            }
        }

        public static Dictionary<Vector2Int, Tile> BulkFetchTiles(SQLiteConnection db, List<Vector2Int> tilesToFetch, int zoomLevel)
        {
            var result = new Dictionary<Vector2Int, Tile>(tilesToFetch.Count);
            var sb = new StringBuilder();
            var args = new List<object> { zoomLevel };

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
    }
}