using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SQLite;
using UnityEngine;
using UnityEngine.Events;
using WitShells.DesignPatterns.Core;
using WitShells.ThreadingJob;

namespace WitShells.MapView
{
    public class MapTileManager : MonoSingleton<MapTileManager>, IDisposable
    {
        [Header("Map Online Settings")]
        public bool canFetchOnline => MapSettings.Instance.useOnlineMap;
        public bool canCacheTiles = true;

        [Header("Offline Map Settings")]
        [SerializeField] private string fileName;

        [Header("Cache Settings")]
        [SerializeField] private int maxCachedTiles = 100;
        private Dictionary<Vector2Int, Tile> _cachedTiles = new Dictionary<Vector2Int, Tile>();

        public string DirectoryPath => Path.Combine(Application.persistentDataPath, "OfflineMaps");
        public string FilePath => Path.Combine(DirectoryPath, $"{fileName}.db");

        public bool HasValidFile => File.Exists(FilePath) && new FileInfo(FilePath).Length > 0;

        private SQLiteConnection _dbConnection;

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
        public UnityEvent<Vector2Int, Tile> OnTileFetched;

        void Start()
        {
            using var conn = DbConnection;
            if (conn == null)
            {
                Debug.LogError("Failed to create or open the database.");
            }
            else
            {
                Debug.Log("Database opened successfully.");
            }
        }

        public bool HasCachedTile(Vector2Int coordinate, out Tile tile, bool withLabels)
        {
            bool result = _cachedTiles.TryGetValue(coordinate, out tile);
            if (!result) return false;
            return withLabels ? (tile.GeoData != null && tile.GeoData.Length > 0) : (tile.NormalData != null && tile.NormalData.Length > 0);
        }

        public void StartStreamFetch(List<Vector2Int> enqueuedTiles, int zoomLevel, bool showLabels)
        {
            if (enqueuedTiles.Count == 0) return;

            var streamTileFetcher = new StreamTileFetcher(FilePath, enqueuedTiles, zoomLevel, showLabels, canFetchOnline, canCacheTiles);
            ThreadManager.Instance.EnqueueStreamingJob(
                streamTileFetcher,
                onProgress: (tile) =>
                {
                    if (tile != null)
                    {
                        var coord = new Vector2Int(tile.TileX, tile.TileY);
                        StoreInCache(tile);
                        OnTileFetched?.Invoke(new Vector2Int(tile.TileX, tile.TileY), tile);
                    }
                },
                onComplete: () =>
                {
                    Debug.Log("Completed streaming tile fetch.");
                },
                onError: (ex) =>
                {
                    Debug.LogError($"Error during streaming tile fetch: {ex.Message}");
                });
        }


        public void CancelFetch(string threadId)
        {
            ThreadManager.Instance.CancelJob(threadId);
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
                    Debug.LogError($"Error fetching tile: {ex.Message}");
                    onComplete?.Invoke(null);
                });
        }

        private void StoreInCache(Tile tile)
        {
            if (tile == null) return;

            var coord = new Vector2Int(tile.TileX, tile.TileY);
            if (_cachedTiles.Count > maxCachedTiles)
            {
                var toRemove = _cachedTiles.First();
                _cachedTiles.Remove(toRemove.Key);
            }
            _cachedTiles[coord] = tile;
        }



        #region Database Management

        public bool CreateDatabase(out SQLiteConnection connection)
        {
            connection = null;
            try
            {
                if (!Directory.Exists(DirectoryPath))
                {
                    Directory.CreateDirectory(DirectoryPath);
                }

                if (!File.Exists(FilePath))
                {
                    File.Create(FilePath).Dispose();
                }

                connection = new SQLiteConnection(FilePath);
                // Creates the table only if it does not exist; no-op if it already exists
                connection.CreateTable<Tile>();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating database: {ex.Message}");
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

        public Tile GetTile(Vector2Int coordinate, int zoomLevel)
        {
            return GetTile(coordinate.x, coordinate.y, zoomLevel, DbConnection);
        }

        public static Tile GetTile(int x, int y, int zoomLevel, SQLiteConnection connection)
        {
            var tile = connection.Query<Tile>(
                            "SELECT * FROM Tile WHERE TileX = ? AND TileY = ? AND Zoom = ? LIMIT 1",
                            x, y, zoomLevel
                        ).FirstOrDefault();

            return tile;
        }

        #endregion

        public void Dispose()
        {
            _dbConnection?.Close();
            _dbConnection = null;

            _cachedTiles.Clear();
        }


        #region Test Logs

#if UNITY_EDITOR

        [ContextMenu("Log Database Path")]
        public void LogDatabasePath()
        {
            Debug.Log($"Database Path: {FilePath}");
        }

#endif

        #endregion
    }
}