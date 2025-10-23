using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SQLite;
using UnityEngine;
using WitShells.ThreadingJob;

namespace WitShells.MapView
{
    public class TilesDownloader : MonoBehaviour
    {
        [Header("Parameters")]
        public float fromLatitude;
        public float toLatitude;
        public float fromLongitude;
        public float toLongitude;

        public int fromZoom = 12;
        public int toZoom = 19;

        public string mapName;

        [Header("Runtime")]
        public int count;
        public int fail;

        public SQLiteConnection _currentConnection;
        public List<Tile> updatedTiles = new();

        [ContextMenu("MinXY MaxXY")]
        public void CalculateMinMaxXY()
        {
            var (xMin, xMax, yMin, yMax) = Utils.TileRangeForBounds(fromLatitude, fromLongitude, toLatitude, toLongitude, fromZoom);
            Debug.Log($"Zoom {fromZoom}: MinXY: \n{xMin},\n{yMin} - MaxXY: \n{xMax},\n{yMax}");
        }

        [ContextMenu("MinMaxXY CenterXY")]
        public void CalculateCenterXY()
        {
            var (xMin, xMax, yMin, yMax) = Utils.TileRangeForBounds(fromLatitude, fromLongitude, toLatitude, toLongitude, fromZoom);
            int centerX = (xMin + xMax) / 2;
            int centerY = (yMin + yMax) / 2;
            Debug.Log($"Zoom {fromZoom}: CenterXY: {centerX},{centerY}");
        }

        [ContextMenu("Total Tiles")]
        public void CalculateTotalTiles()
        {
            int totalTiles = 0;
            for (int z = fromZoom; z <= toZoom; z++)
            {
                var (xMin, xMax, yMin, yMax) = Utils.TileRangeForBounds(fromLatitude, fromLongitude, toLatitude, toLongitude, z);
                int count = (xMax - xMin + 1) * (yMax - yMin + 1);
                totalTiles += count;
                Debug.Log($"Zoom {z}: Total Tiles: {count}");
            }
            Debug.Log($"Total Tiles from zoom {fromZoom} to {toZoom}: {totalTiles}");
        }

        [ContextMenu("Store Empty Tiles In Database")]
        public void StoreEmptyTilesInDatabase()
        {
            StopAllCoroutines();
            StartCoroutine(StoreEmptyTiles());
        }

        public class TableInfo
        {
            public int cid { get; set; }
            public string name { get; set; }
            public string type { get; set; }
            public int notnull { get; set; }
            public string dflt_value { get; set; }
            public int pk { get; set; }
        }


        [ContextMenu("Table Info")]
        public void ShowTableInfo()
        {
            if (LoadDatabaseIfExists(out var connection))
            {
                if (DoesTableExist<Tile>(connection))
                {
                    var query = connection.CreateCommand("SELECT COUNT(*) FROM Tile");
                    int totalCount = query.ExecuteScalar<int>();
                    Debug.Log($"Total tiles in database: {totalCount}");

                    var result = connection.Query<TableInfo>("PRAGMA table_info(Tile);");

                    foreach (var row in result)
                    {
                        Debug.Log($"Column: {row.name}, Type: {row.type}, PK: {row.pk}");
                    }

                }
                else
                {
                    Debug.Log("Tile table does not exist in the database.");
                }
            }
            else
            {
                Debug.Log("Database does not exist.");
            }
        }

        [ContextMenu("Check Tiles In Database")]
        public void CheckTilesInDatabase()
        {
            if (LoadDatabaseIfExists(out var connection))
            {
                if (DoesTableExist<Tile>(connection))
                {
                    var query = connection.CreateCommand("SELECT * FROM Tile WHERE (DataBase64 IS NULL OR GeoBase64 IS NULL)");
                    var tilesWithoutData = query.ExecuteQuery<Tile>();
                    if (tilesWithoutData == null) return;
                    int count = 0;
                    foreach (var tile in tilesWithoutData)
                    {
                        count++;
                        // Debug.Log($"Tile without data - Zoom: {tile.Zoom}, X: {tile.TileX}, Y: {tile.TileY}");
                    }
                    Debug.Log($"Total tiles without data: {count}");
                }
                else
                {
                    Debug.Log("Tile table does not exist in the database.");
                }
            }
            else
            {
                Debug.Log("Database does not exist.");
            }
        }

        [ContextMenu("Download All Missing Tiles Data")]
        public void DownloadAllTiles()
        {
            StopAllCoroutines();
            StartCoroutine(DownloadImageData());
        }

        [ContextMenu("StopCoroutines")]
        public void StopAll()
        {
            StopAllCoroutines();
        }

        private IEnumerator DownloadImageData()
        {
            if (LoadDatabaseIfExists(out var connection))
            {
                if (DoesTableExist<Tile>(connection))
                {
                    updatedTiles.Clear();
                    _currentConnection = connection;
                    var query = connection.CreateCommand("SELECT * FROM Tile WHERE (NormalData IS NULL OR GeoBase64 IS NULL)");
                    var tilesWithoutData = query.ExecuteQuery<Tile>();
                    if (tilesWithoutData == null) yield break;
                    Debug.Log("Total Tiles: " + tilesWithoutData.Count);
                }
            }
        }

        [ContextMenu("Update Downloaded Tiles")]
        public void UpdateAll()
        {
            if (_currentConnection == null) return;
            if (updatedTiles == null || updatedTiles.Count == 0) return;
            _currentConnection.UpdateAll(updatedTiles);
            updatedTiles.Clear();
        }

        private IEnumerator StoreEmptyTiles()
        {
            using var connection = CreateConnection();
            if (!DoesTableExist<Tile>(connection))
                connection.CreateTable<Tile>();

            List<Tile> tiles = new List<Tile>();

            for (int z = fromZoom; z <= toZoom; z++)
            {
                var (xMin, xMax, yMin, yMax) = Utils.TileRangeForBounds(fromLatitude, fromLongitude, toLatitude, toLongitude, z);
                for (int x = xMin; x <= xMax; x++)
                {
                    for (int y = yMin; y <= yMax; y++)
                    {
                        var checkTileCmd = connection.CreateCommand(
                            "SELECT COUNT(*) FROM Tile WHERE TileX = ? AND TileY = ? AND Zoom = ?", x, y, z);

                        int count = checkTileCmd.ExecuteScalar<int>();

                        if (count == 0)
                        {
                            var tile = new Tile
                            {
                                TileX = x,
                                TileY = y,
                                Zoom = z,
                            };
                            tiles.Add(tile);
                        }
                    }
                }
            }

            // Bulk insert all tiles at once
            connection.InsertAll(tiles);
            Debug.Log($"Inserted {tiles.Count} tiles into the database.");
            yield return null;
        }

        private bool DatabaseExists()
        {
            return Directory.Exists(Path.Combine(Application.persistentDataPath, "OfflineMaps"));
        }

        private SQLiteConnection CreateConnection()
        {
            if (!DatabaseExists())
            {
                Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "OfflineMaps"));
            }

            string dbPath = Path.Combine(Application.persistentDataPath, "OfflineMaps", $"{mapName}.db");
            return new SQLiteConnection(dbPath);
        }

        private bool LoadDatabaseIfExists(out SQLiteConnection connection)
        {
            connection = null;
            if (DatabaseExists())
            {
                connection = CreateConnection();
                return true;
            }
            return false;
        }

        private bool DoesTableExist<T>(SQLiteConnection connection)
        {
            var tableInfo = connection.GetTableInfo(typeof(T).Name);
            return tableInfo.Count > 0;
        }

        [ContextMenu("Center Tile In The Database")]
        public void FindCenterTile()
        {
            if (LoadDatabaseIfExists(out _currentConnection))
            {
                if (DoesTableExist<Tile>(_currentConnection))
                {
                    var zoom = fromZoom;
                    var centerTile = _currentConnection.Query<Tile>(
                            "SELECT * FROM Tile WHERE Zoom = ? ORDER BY TileX, TileY LIMIT 1 OFFSET " +
                            "(SELECT COUNT(*) FROM Tile WHERE Zoom = ?) / 2",
                            zoom, zoom
                        ).FirstOrDefault();

                    Debug.Log(centerTile.ToString());
                }
            }
        }
    }
}