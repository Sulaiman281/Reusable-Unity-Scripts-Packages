using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SQLite;
using UnityEngine;
using WitShells.DesignPatterns;

namespace WitShells.MapView
{
    /// <summary>
    /// Unity Editor diagnostic tool for managing and validating map tile databases.
    /// Provides context menu operations for database analysis, tile downloading, and data integrity checks.
    /// This component is primarily intended for development and WitLoggerging purposes.
    /// </summary>
    public sealed class TilesDatabaseDiagnostics : MonoBehaviour
    {
        #region Configuration Fields

        [Header("Geographic Bounds")]
        [Tooltip("Southern boundary latitude in decimal degrees")]
        [SerializeField] private float _fromLatitude;
        
        [Tooltip("Northern boundary latitude in decimal degrees")]
        [SerializeField] private float _toLatitude;
        
        [Tooltip("Western boundary longitude in decimal degrees")]
        [SerializeField] private float _fromLongitude;
        
        [Tooltip("Eastern boundary longitude in decimal degrees")]
        [SerializeField] private float _toLongitude;

        [Header("Zoom Level Configuration")]
        [Tooltip("Minimum zoom level to process")]
        [SerializeField] private int _fromZoom = 12;
        
        [Tooltip("Maximum zoom level to process")]
        [SerializeField] private int _toZoom = 19;

        [Header("Database Configuration")]
        [Tooltip("Name of the map database file")]
        [SerializeField] private string _mapName;

        [Header("Runtime Statistics")]
        [Tooltip("Number of tiles successfully processed")]
        [SerializeField, Space] private int _processedCount;
        
        [Tooltip("Number of failed tile operations")]
        [SerializeField] private int _failedCount;

        [Tooltip("Collection of tiles that have been updated during operations")]
        [SerializeField] private List<Tile> _updatedTiles = new List<Tile>();

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the southern boundary latitude in decimal degrees.
        /// </summary>
        public float FromLatitude
        {
            get => _fromLatitude;
            set => _fromLatitude = ValidateLatitude(value, nameof(FromLatitude));
        }

        /// <summary>
        /// Gets or sets the northern boundary latitude in decimal degrees.
        /// </summary>
        public float ToLatitude
        {
            get => _toLatitude;
            set => _toLatitude = ValidateLatitude(value, nameof(ToLatitude));
        }

        /// <summary>
        /// Gets or sets the western boundary longitude in decimal degrees.
        /// </summary>
        public float FromLongitude
        {
            get => _fromLongitude;
            set => _fromLongitude = ValidateLongitude(value, nameof(FromLongitude));
        }

        /// <summary>
        /// Gets or sets the eastern boundary longitude in decimal degrees.
        /// </summary>
        public float ToLongitude
        {
            get => _toLongitude;
            set => _toLongitude = ValidateLongitude(value, nameof(ToLongitude));
        }

        /// <summary>
        /// Gets or sets the minimum zoom level to process.
        /// </summary>
        public int FromZoom
        {
            get => _fromZoom;
            set => _fromZoom = ValidateZoomLevel(value, nameof(FromZoom));
        }

        /// <summary>
        /// Gets or sets the maximum zoom level to process.
        /// </summary>
        public int ToZoom
        {
            get => _toZoom;
            set => _toZoom = ValidateZoomLevel(value, nameof(ToZoom));
        }

        /// <summary>
        /// Gets or sets the name of the map database file.
        /// </summary>
        public string MapName
        {
            get => _mapName;
            set => _mapName = ValidateMapName(value);
        }

        /// <summary>
        /// Gets the number of tiles successfully processed.
        /// </summary>
        public int ProcessedCount => _processedCount;

        /// <summary>
        /// Gets the number of failed tile operations.
        /// </summary>
        public int FailedCount => _failedCount;

        /// <summary>
        /// Gets a read-only view of tiles that have been updated during operations.
        /// </summary>
        public IReadOnlyList<Tile> UpdatedTiles => _updatedTiles;

        #endregion

        #region Nested Types

        /// <summary>
        /// Represents database table column information from SQLite PRAGMA table_info() command.
        /// </summary>
        public sealed class DatabaseColumnInfo
        {
            /// <summary>Gets or sets the column index.</summary>
            public int cid { get; set; }
            
            /// <summary>Gets or sets the column name.</summary>
            public string name { get; set; }
            
            /// <summary>Gets or sets the column data type.</summary>
            public string type { get; set; }
            
            /// <summary>Gets or sets whether the column allows NULL values (1 = NOT NULL, 0 = allows NULL).</summary>
            public int notnull { get; set; }
            
            /// <summary>Gets or sets the default value for the column.</summary>
            public string dflt_value { get; set; }
            
            /// <summary>Gets or sets whether the column is part of the primary key (1 = yes, 0 = no).</summary>
            public int pk { get; set; }
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Validates that a latitude value is within the valid range.
        /// </summary>
        /// <param name="latitude">The latitude value to validate.</param>
        /// <param name="parameterName">The name of the parameter for error reporting.</param>
        /// <returns>The validated latitude value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when latitude is outside the valid range.</exception>
        private static float ValidateLatitude(float latitude, string parameterName)
        {
            if (latitude < -90f || latitude > 90f)
                throw new ArgumentOutOfRangeException(parameterName, latitude, "Latitude must be between -90 and 90 degrees.");
            return latitude;
        }

        /// <summary>
        /// Validates that a longitude value is within the valid range.
        /// </summary>
        /// <param name="longitude">The longitude value to validate.</param>
        /// <param name="parameterName">The name of the parameter for error reporting.</param>
        /// <returns>The validated longitude value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when longitude is outside the valid range.</exception>
        private static float ValidateLongitude(float longitude, string parameterName)
        {
            if (longitude < -180f || longitude > 180f)
                throw new ArgumentOutOfRangeException(parameterName, longitude, "Longitude must be between -180 and 180 degrees.");
            return longitude;
        }

        /// <summary>
        /// Validates that a zoom level is within the reasonable range for tile maps.
        /// </summary>
        /// <param name="zoomLevel">The zoom level to validate.</param>
        /// <param name="parameterName">The name of the parameter for error reporting.</param>
        /// <returns>The validated zoom level.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when zoom level is outside the valid range.</exception>
        private static int ValidateZoomLevel(int zoomLevel, string parameterName)
        {
            if (zoomLevel < 0 || zoomLevel > 22)
                throw new ArgumentOutOfRangeException(parameterName, zoomLevel, "Zoom level must be between 0 and 22.");
            return zoomLevel;
        }

        /// <summary>
        /// Validates and sanitizes a map name for use as a filename.
        /// </summary>
        /// <param name="mapName">The map name to validate.</param>
        /// <returns>The validated and sanitized map name.</returns>
        /// <exception cref="ArgumentException">Thrown when map name is invalid.</exception>
        private static string ValidateMapName(string mapName)
        {
            if (string.IsNullOrWhiteSpace(mapName))
                throw new ArgumentException("Map name cannot be null, empty, or whitespace.", nameof(mapName));

            // Remove invalid filename characters
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string sanitized = mapName;
            foreach (char c in invalidChars)
            {
                sanitized = sanitized.Replace(c, '_');
            }

            return sanitized.Trim();
        }

        /// <summary>
        /// Validates the current geographic bounds configuration.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when bounds are invalid.</exception>
        private void ValidateGeographicBounds()
        {
            if (_fromLatitude >= _toLatitude)
                throw new InvalidOperationException("FromLatitude must be less than ToLatitude.");

            if (_fromLongitude >= _toLongitude)
                throw new InvalidOperationException("FromLongitude must be less than ToLongitude.");
        }

        /// <summary>
        /// Validates the current zoom level range configuration.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when zoom range is invalid.</exception>
        private void ValidateZoomRange()
        {
            if (_fromZoom > _toZoom)
                throw new InvalidOperationException("FromZoom must be less than or equal to ToZoom.");
        }

        /// <summary>
        /// Validates the complete configuration including map name, geographic bounds, and zoom range.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
        private void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_mapName))
                throw new InvalidOperationException("MapName must be specified.");

            ValidateGeographicBounds();
            ValidateZoomRange();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the full path to the database file for the current map.
        /// </summary>
        /// <returns>The complete path to the database file.</returns>
        private string GetDatabasePath()
        {
            if (string.IsNullOrWhiteSpace(_mapName))
                throw new InvalidOperationException("MapName must be specified before accessing database path.");

            return Path.Combine(Application.persistentDataPath, "OfflineMaps", $"{_mapName}.db");
        }

        /// <summary>
        /// Creates a new database connection with proper configuration.
        /// </summary>
        /// <returns>A configured SQLite database connection.</returns>
        private SQLiteConnection CreateDatabaseConnection()
        {
            string databasePath = GetDatabasePath();
            return DatabaseUtils.EnsureDatabaseWithSchema(databasePath);
        }

        /// <summary>
        /// Checks if a table exists in the database.
        /// </summary>
        /// <typeparam name="T">The type representing the table.</typeparam>
        /// <param name="connection">The database connection to check.</param>
        /// <returns>True if the table exists, false otherwise.</returns>
        private static bool DoesTableExist<T>(SQLiteConnection connection)
        {
            try
            {
                var tableMapping = connection.GetMapping<T>();
                var result = connection.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=?", 
                    tableMapping.TableName);
                return result > 0;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Diagnostic Operations

        /// <summary>
        /// Calculates and displays the minimum and maximum tile coordinates for the current geographic bounds.
        /// </summary>
        [ContextMenu("Calculate Min/Max Tile Coordinates")]
        public void CalculateMinMaxTileCoordinates()
        {
            try
            {
                ValidateGeographicBounds();
                var (xMin, xMax, yMin, yMax) = Utils.TileRangeForBounds(_fromLatitude, _fromLongitude, _toLatitude, _toLongitude, _fromZoom);
                WitLogger.Log($"Zoom Level {_fromZoom}:\nMin Coordinates: ({xMin}, {yMin})\nMax Coordinates: ({xMax}, {yMax})");
            }
            catch (Exception ex)
            {
                WitLogger.LogError($"Failed to calculate tile coordinates: {ex.Message}");
            }
        }

        /// <summary>
        /// Calculates and displays the center tile coordinates for the current geographic bounds.
        /// </summary>
        [ContextMenu("Calculate Center Tile Coordinates")]
        public void CalculateCenterTileCoordinates()
        {
            try
            {
                ValidateGeographicBounds();
                var (xMin, xMax, yMin, yMax) = Utils.TileRangeForBounds(_fromLatitude, _fromLongitude, _toLatitude, _toLongitude, _fromZoom);
                int centerX = (xMin + xMax) / 2;
                int centerY = (yMin + yMax) / 2;
                WitLogger.Log($"Zoom Level {_fromZoom}: Center Coordinates: ({centerX}, {centerY})");
            }
            catch (Exception ex)
            {
                WitLogger.LogError($"Failed to calculate center coordinates: {ex.Message}");
            }
        }

        /// <summary>
        /// Calculates and displays the total number of tiles for each zoom level and overall total.
        /// </summary>
        [ContextMenu("Calculate Total Tile Count")]
        public void CalculateTotalTileCount()
        {
            try
            {
                ValidateGeographicBounds();
                ValidateZoomRange();

                int totalTiles = 0;
                for (int zoomLevel = _fromZoom; zoomLevel <= _toZoom; zoomLevel++)
                {
                    var (xMin, xMax, yMin, yMax) = Utils.TileRangeForBounds(_fromLatitude, _fromLongitude, _toLatitude, _toLongitude, zoomLevel);
                    int tilesForZoom = (xMax - xMin + 1) * (yMax - yMin + 1);
                    totalTiles += tilesForZoom;
                    WitLogger.Log($"Zoom Level {zoomLevel}: {tilesForZoom:N0} tiles");
                }
                WitLogger.Log($"Total tiles across zoom levels {_fromZoom}-{_toZoom}: {totalTiles:N0}");
            }
            catch (Exception ex)
            {
                WitLogger.LogError($"Failed to calculate total tile count: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates placeholder entries in the database for all tiles within the specified geographic bounds and zoom levels.
        /// </summary>
        [ContextMenu("Store Empty Tiles In Database")]
        public void StoreEmptyTilesInDatabase()
        {
            try
            {
                ValidateConfiguration();
                StopAllCoroutines();
                StartCoroutine(StoreEmptyTilesCoroutine());
            }
            catch (Exception ex)
            {
                WitLogger.LogError($"Failed to start empty tile storage: {ex.Message}");
            }
        }
        /// <summary>
        /// Displays detailed information about the database table structure and tile count.
        /// </summary>
        [ContextMenu("Show Database Table Information")]
        public void ShowDatabaseTableInformation()
        {
            try
            {
                ValidateConfiguration();
                string databasePath = GetDatabasePath();
                
                if (!File.Exists(databasePath))
                {
                    WitLogger.LogWarning("Database file does not exist.");
                    return;
                }

                using var connection = CreateDatabaseConnection();
                if (!DoesTableExist<Tile>(connection))
                {
                    WitLogger.LogWarning("Tile table does not exist in the database.");
                    return;
                }

                // Get tile count
                var totalTileCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Tile");
                WitLogger.Log($"Total tiles in database: {totalTileCount:N0}");

                // Get table structure information
                var columnInfoList = connection.Query<DatabaseColumnInfo>("PRAGMA table_info(Tile);");
                WitLogger.Log("Table Structure:");
                foreach (var columnInfo in columnInfoList)
                {
                    string nullable = columnInfo.notnull == 1 ? "NOT NULL" : "NULLABLE";
                    string primaryKey = columnInfo.pk == 1 ? " (PRIMARY KEY)" : "";
                    WitLogger.Log($"  Column: {columnInfo.name}, Type: {columnInfo.type}, {nullable}{primaryKey}");
                }
            }
            catch (Exception ex)
            {
                WitLogger.LogError($"Failed to retrieve database information: {ex.Message}");
            }
        }

        [ContextMenu("Check Tiles In Database")]
        public void CheckTilesInDatabase()
        {
            string dbPath = GetDbPath();
            if (!File.Exists(dbPath))
            {
                WitLogger.Log("Database does not exist.");
                return;
            }

            using var connection = CreateConnection();
            if (DoesTableExist<Tile>(connection))
            {
                var query = connection.CreateCommand("SELECT * FROM Tile WHERE (NormalData IS NULL OR GeoData IS NULL)");
                var tilesWithoutData = query.ExecuteQuery<Tile>();
                if (tilesWithoutData == null) return;
                WitLogger.Log($"Total tiles without data: {tilesWithoutData.Count}");
            }
            else
            {
                WitLogger.Log("Tile table does not exist in the database.");
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
            string dbPath = GetDbPath();
            if (!File.Exists(dbPath))
            {
                WitLogger.Log("Database does not exist.");
                yield break;
            }

            using var connection = CreateConnection();
            if (DoesTableExist<Tile>(connection))
            {
                updatedTiles.Clear();
                var query = connection.CreateCommand("SELECT * FROM Tile WHERE (NormalData IS NULL OR GeoData IS NULL)");
                var tilesWithoutData = query.ExecuteQuery<Tile>();
                if (tilesWithoutData == null) yield break;
                WitLogger.Log("Total Tiles: " + tilesWithoutData.Count);
            }
        }

        [ContextMenu("Update Downloaded Tiles")]
        public void UpdateAll()
        {
            if (updatedTiles == null || updatedTiles.Count == 0) return;
            try
            {
                string dbPath = Path.Combine(Application.persistentDataPath, "OfflineMaps", $"{mapName}.db");
                DatabaseWriter.EnqueueTileBatch(dbPath, new System.Collections.Generic.List<Tile>(updatedTiles));
                updatedTiles.Clear();
            }
            catch (System.Exception ex)
            {
                WitLogger.LogError($"Failed to enqueue UpdateAll tiles: {ex.Message}");
            }
        }

        private IEnumerator StoreEmptyTiles()
        {
            // Use the centralized database creation method which ensures proper schema and unique index
            if (!EnsureDatabaseWithSchema())
            {
                WitLogger.LogError("Failed to create or verify database schema.");
                yield break;
            }

            List<Tile> tiles = new List<Tile>();

            for (int z = fromZoom; z <= toZoom; z++)
            {
                var (xMin, xMax, yMin, yMax) = Utils.TileRangeForBounds(fromLatitude, fromLongitude, toLatitude, toLongitude, z);
                for (int x = xMin; x <= xMax; x++)
                {
                    for (int y = yMin; y <= yMax; y++)
                    {
                        // Create placeholder tile - DbWorker will use INSERT OR IGNORE to avoid duplicates
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

            // Bulk insert all tiles at once via background writer with proper deduplication
            string dbPath = GetDbPath();
            try
            {
                DatabaseWriter.EnqueueTileBatch(dbPath, tiles);
                WitLogger.Log($"Enqueued {tiles.Count} placeholder tiles for insertion into the database.");
            }
            catch (System.Exception ex)
            {
                WitLogger.LogError($"Failed to enqueue bulk insert: {ex.Message}");
            }
            yield return null;
        }

        private string GetDbPath()
        {
            return Path.Combine(Application.persistentDataPath, "OfflineMaps", $"{mapName}.db");
        }

        private bool DatabaseExists()
        {
            string dbPath = GetDbPath();
            return File.Exists(dbPath);
        }

        private SQLiteConnection CreateConnection()
        {
            string dbPath = GetDbPath();
            return DatabaseUtils.EnsureDatabaseWithSchema(dbPath);
        }

        private bool LoadDatabaseIfExists(out SQLiteConnection connection)
        {
            connection = null;
            string dbPath = GetDbPath();
            if (File.Exists(dbPath))
            {
                connection = DatabaseUtils.EnsureDatabaseWithSchema(dbPath);
                return connection != null;
            }
            return false;
        }

        /// <summary>
        /// Ensure database exists with proper schema and unique index. This is the centralized
        /// method all code should use instead of ad-hoc CreateTable calls.
        /// </summary>
        private bool EnsureDatabaseWithSchema()
        {
            try
            {
                string databasePath = GetDatabasePath();
                using var connection = DatabaseUtils.EnsureDatabaseWithSchema(databasePath);
                return connection != null;
            }
            catch (System.Exception ex)
            {
                WitLogger.LogError($"Error ensuring database schema: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Legacy Compatibility (for existing methods)

        // Legacy field access for compatibility with existing methods
        private float fromLatitude => _fromLatitude;
        private float toLatitude => _toLatitude;
        private float fromLongitude => _fromLongitude;
        private float toLongitude => _toLongitude;
        private int fromZoom => _fromZoom;
        private int toZoom => _toZoom;
        private string mapName => _mapName;
        private List<Tile> updatedTiles => _updatedTiles;
        private int count => _processedCount;
        private int fail => _failedCount;
        
        // Legacy method names for compatibility
        // private string GetDbPath() => GetDatabasePath();
        // private SQLiteConnection CreateConnection() => CreateDatabaseConnection();

        private IEnumerator StoreEmptyTilesCoroutine() => StoreEmptyTiles();

        #endregion

        #region Context Menu Operations (Legacy)

        [ContextMenu("Center Tile In The Database")]
        public void FindCenterTile()
        {
            string dbPath = GetDbPath();
            if (!File.Exists(dbPath))
            {
                WitLogger.Log("Database does not exist.");
                return;
            }

            using var connection = CreateConnection();
            if (DoesTableExist<Tile>(connection))
            {
                var zoom = fromZoom;
                var centerTile = connection.Query<Tile>(
                        "SELECT * FROM Tile WHERE Zoom = ? ORDER BY TileX, TileY LIMIT 1 OFFSET " +
                        "(SELECT COUNT(*) FROM Tile WHERE Zoom = ?) / 2",
                        zoom, zoom
                    ).FirstOrDefault();

                if (centerTile != null)
                    WitLogger.Log(centerTile.ToString());
                else
                    WitLogger.Log("No center tile found.");
            }
        }

        // Diagnostic helpers
        private class DuplicateInfo
        {
            public int Zoom { get; set; }
            public int TileX { get; set; }
            public int TileY { get; set; }
            public int Cnt { get; set; }
        }

        [ContextMenu("Find Duplicate Tiles")]
        public void FindDuplicateTiles()
        {
            string dbPath = GetDbPath();
            if (!File.Exists(dbPath))
            {
                WitLogger.Log("Database does not exist.");
                return;
            }

            using var connection = CreateConnection();
            if (!DoesTableExist<Tile>(connection))
            {
                WitLogger.Log("Tile table does not exist in the database.");
                return;
            }

            var duplicates = connection.Query<DuplicateInfo>(
                "SELECT Zoom, TileX, TileY, COUNT(*) AS Cnt FROM Tile GROUP BY Zoom, TileX, TileY HAVING COUNT(*) > 1");

            WitLogger.Log($"Duplicate coordinate groups: {duplicates.Count}");
            foreach (var d in duplicates)
            {
                WitLogger.Log($"Zoom {d.Zoom} X {d.TileX} Y {d.TileY} -> {d.Cnt} rows");
            }
        }

        private class BlobInfo
        {
            public int Id { get; set; }
            public int Zoom { get; set; }
            public int TileX { get; set; }
            public int TileY { get; set; }
            public int NormalLen { get; set; }
            public int GeoLen { get; set; }
        }

        [ContextMenu("Show Largest Tile Blobs")]
        public void ShowLargestTileBlobs()
        {
            string dbPath = GetDbPath();
            if (!File.Exists(dbPath))
            {
                WitLogger.Log("Database does not exist.");
                return;
            }

            using var connection = CreateConnection();
            if (!DoesTableExist<Tile>(connection))
            {
                WitLogger.Log("Tile table does not exist in the database.");
                return;
            }

            var blobs = connection.Query<BlobInfo>(
                "SELECT Id, Zoom, TileX, TileY, LENGTH(NormalData) AS NormalLen, LENGTH(GeoData) AS GeoLen FROM Tile ORDER BY (LENGTH(NormalData) + LENGTH(GeoData)) DESC LIMIT 20");

            WitLogger.Log("Top 20 largest tile blobs:");
            foreach (var b in blobs)
            {
                WitLogger.Log($"Id:{b.Id} Zoom:{b.Zoom} X:{b.TileX} Y:{b.TileY} Normal:{b.NormalLen} Geo:{b.GeoLen} Total:{(b.NormalLen + b.GeoLen)}");
            }
        }

        [ContextMenu("Check Zoom Level Data")]
        public void CheckZoomLevelData()
        {
            string dbPath = GetDbPath();
            if (!File.Exists(dbPath))
            {
                WitLogger.Log("Database does not exist.");
                return;
            }

            using var connection = CreateConnection();
            if (!DoesTableExist<Tile>(connection))
            {
                WitLogger.Log("Tile table does not exist in the database.");
                return;
            }

            WitLogger.Log("=== Zoom Level Data Analysis ===");
            
            // Check data for each zoom level
            for (int z = fromZoom; z <= toZoom; z++)
            {
                var totalTiles = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Tile WHERE Zoom = ?", z);
                var tilesWithNormal = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Tile WHERE Zoom = ? AND NormalData IS NOT NULL AND length(NormalData) > 0", z);
                var tilesWithGeo = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Tile WHERE Zoom = ? AND GeoData IS NOT NULL AND length(GeoData) > 0", z);
                var tilesWithBoth = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Tile WHERE Zoom = ? AND NormalData IS NOT NULL AND length(NormalData) > 0 AND GeoData IS NOT NULL AND length(GeoData) > 0", z);
                
                // Calculate expected tiles for this zoom level
                var (xMin, xMax, yMin, yMax) = Utils.TileRangeForBounds(fromLatitude, fromLongitude, toLatitude, toLongitude, z);
                var expectedTiles = (xMax - xMin + 1) * (yMax - yMin + 1);
                
                WitLogger.Log($"Zoom {z}: {totalTiles}/{expectedTiles} tiles exist, Normal: {tilesWithNormal}, Geo: {tilesWithGeo}, Both: {tilesWithBoth}");
                
                if (totalTiles < expectedTiles)
                {
                    WitLogger.LogWarning($"  Missing {expectedTiles - totalTiles} placeholder tiles for zoom {z}");
                }
                
                if (z == toZoom && (tilesWithNormal < expectedTiles || tilesWithGeo < expectedTiles))
                {
                    WitLogger.LogWarning($"  Zoom {z} (max): Missing Normal: {expectedTiles - tilesWithNormal}, Missing Geo: {expectedTiles - tilesWithGeo}");
                }
            }
        }

        [ContextMenu("Check Specific Zoom Level Bounds")]
        public void CheckSpecificZoomBounds()
        {
            WitLogger.Log($"=== Bounds Analysis for Zoom {fromZoom}-{toZoom} ===");
            WitLogger.Log($"Coordinates: ({fromLatitude}, {fromLongitude}) to ({toLatitude}, {toLongitude})");
            
            for (int z = fromZoom; z <= toZoom; z++)
            {
                var (xMin, xMax, yMin, yMax) = Utils.TileRangeForBounds(fromLatitude, fromLongitude, toLatitude, toLongitude, z);
                var tileCount = (xMax - xMin + 1) * (yMax - yMin + 1);
                WitLogger.Log($"Zoom {z}: X=[{xMin},{xMax}] Y=[{yMin},{yMax}] -> {tileCount} tiles");
            }
        }

        [ContextMenu("Remove All Duplicate Tiles")]
        public void RemoveAllDuplicateTiles()
        {
            string dbPath = GetDbPath();
            if (!File.Exists(dbPath))
            {
                WitLogger.Log("Database does not exist.");
                return;
            }

            try
            {
                using var connection = CreateConnection();
                if (!DoesTableExist<Tile>(connection))
                {
                    WitLogger.Log("Tile table does not exist in the database.");
                    return;
                }

                // First, show the duplicate count
                var duplicateCount = connection.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM (SELECT Zoom, TileX, TileY FROM Tile GROUP BY Zoom, TileX, TileY HAVING COUNT(*) > 1)");
                WitLogger.Log($"Found {duplicateCount} coordinate groups with duplicates. Starting deduplication...");

                // Get the total count before deduplication
                var totalBefore = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Tile");
                WitLogger.Log($"Total rows before deduplication: {totalBefore}");

                // Create a temporary table with deduplicated data (keeping the row with smallest Id for each coordinate group)
                connection.Execute(@"
                    CREATE TEMPORARY TABLE Tile_Dedupe AS 
                    SELECT * FROM Tile 
                    WHERE Id IN (
                        SELECT MIN(Id) 
                        FROM Tile 
                        GROUP BY TileX, TileY, Zoom
                    )");

                // Drop the original table
                connection.Execute("DROP TABLE Tile");

                // Recreate the table with proper schema
                connection.CreateTable<Tile>();
                connection.Execute("CREATE UNIQUE INDEX IF NOT EXISTS idx_tile_coord ON Tile(TileX, TileY, Zoom);");

                // Copy deduplicated data back
                connection.Execute(@"
                    INSERT INTO Tile (Id, TileX, TileY, Zoom, NormalData, GeoData)
                    SELECT Id, TileX, TileY, Zoom, NormalData, GeoData
                    FROM Tile_Dedupe
                    ORDER BY Id");

                // Drop temporary table
                connection.Execute("DROP TABLE Tile_Dedupe");

                // Get the total count after deduplication
                var totalAfter = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Tile");
                WitLogger.Log($"Total rows after deduplication: {totalAfter}");
                WitLogger.Log($"Removed {totalBefore - totalAfter} duplicate rows.");

                // Verify no duplicates remain
                var remainingDuplicates = connection.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM (SELECT Zoom, TileX, TileY FROM Tile GROUP BY Zoom, TileX, TileY HAVING COUNT(*) > 1)");
                WitLogger.Log($"Remaining duplicate coordinate groups: {remainingDuplicates}");

                // Checkpoint the WAL to ensure changes are persisted
                try
                {
                    connection.Execute("PRAGMA wal_checkpoint(TRUNCATE);");
                    WitLogger.Log("Database checkpoint completed.");
                }
                catch (System.Exception ex)
                {
                    WitLogger.LogWarning($"Checkpoint failed: {ex.Message}");
                }
            }
            catch (System.Exception ex)
            {
                WitLogger.LogError($"Error during deduplication: {ex.Message}");
            }
        }
        #endregion
    }
}