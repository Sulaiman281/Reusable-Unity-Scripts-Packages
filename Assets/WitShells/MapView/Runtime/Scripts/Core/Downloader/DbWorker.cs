using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using SQLite;
using UnityEngine;
using WitShells.DesignPatterns;

namespace WitShells.MapView
{
    /// <summary>
    /// Thread-safe database writer that ensures serialized write operations to SQLite databases.
    /// Manages one dedicated worker thread per database file to prevent write conflicts and ensure data integrity.
    /// Uses the singleton pattern to maintain one worker instance per database path.
    /// </summary>
    /// <remarks>
    /// This class implements the single-writer pattern for SQLite databases to avoid "database is locked" errors.
    /// Each database file gets its own dedicated worker thread with a blocking queue for write operations.
    /// The worker processes batches of tiles transactionally for optimal performance.
    /// </remarks>
    public sealed class DatabaseWriter : IDisposable
    {
        #region Private Fields

        private readonly string _databasePath;
        private readonly SQLiteConnection _databaseConnection;
        private readonly BlockingCollection<TileBatch> _writeQueue;
        private readonly Thread _workerThread;
        private volatile bool _isShuttingDown;

        private static readonly ConcurrentDictionary<string, DatabaseWriter> ActiveWriters =
            new ConcurrentDictionary<string, DatabaseWriter>(StringComparer.OrdinalIgnoreCase);

        private const int DefaultQueueCapacity = 1000;
        private const int ThreadJoinTimeoutMs = 5000;

        #endregion

        #region Constructor and Factory

        /// <summary>
        /// Creates a new database writer instance for the specified database path.
        /// </summary>
        /// <param name="databasePath">Absolute path to the SQLite database file.</param>
        /// <exception cref="ArgumentException">Thrown when database path is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when database connection fails.</exception>
        private DatabaseWriter(string databasePath)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
                throw new ArgumentException("Database path cannot be null or empty.", nameof(databasePath));

            _databasePath = databasePath;
            _writeQueue = new BlockingCollection<TileBatch>(new ConcurrentQueue<TileBatch>(), DefaultQueueCapacity);

            try
            {
                _databaseConnection = DatabaseUtils.EnsureDatabaseWithSchema(databasePath);
                if (_databaseConnection == null)
                {
                    throw new InvalidOperationException($"Failed to create database connection for '{databasePath}'");
                }

                WitLogger.Log($"DatabaseWriter: Initialized for database '{Path.GetFileName(databasePath)}'");
            }
            catch (Exception ex)
            {
                WitLogger.LogError($"DatabaseWriter: Failed to initialize database '{databasePath}': {ex.Message}");
                throw;
            }

            _workerThread = new Thread(ProcessWriteQueue)
            {
                IsBackground = true,
                Name = $"DbWriter-{Path.GetFileNameWithoutExtension(databasePath)}"
            };
            _workerThread.Start();
        }

        /// <summary>
        /// Gets or creates a database writer instance for the specified path.
        /// </summary>
        /// <param name="databasePath">Absolute path to the SQLite database file.</param>
        /// <returns>Database writer instance for the specified path.</returns>
        /// <exception cref="ArgumentException">Thrown when database path is null or empty.</exception>
        public static DatabaseWriter GetOrCreateWriter(string databasePath)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
                throw new ArgumentException("Database path cannot be null or empty.", nameof(databasePath));

            return ActiveWriters.GetOrAdd(databasePath, path => new DatabaseWriter(path));
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// Represents a batch of tiles to be written to the database.
        /// </summary>
        private readonly struct TileBatch
        {
            public readonly IReadOnlyList<Tile> Tiles;
            public readonly DateTime EnqueuedAt;

            public TileBatch(IReadOnlyList<Tile> tiles)
            {
                Tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
                EnqueuedAt = DateTime.UtcNow;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Enqueues a batch of tiles for asynchronous database writing.
        /// </summary>
        /// <param name="databasePath">Path to the target database.</param>
        /// <param name="tiles">Collection of tiles to write.</param>
        /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the writer is shutting down.</exception>
        public static void EnqueueTileBatch(string databasePath, IReadOnlyList<Tile> tiles)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
                throw new ArgumentException("Database path cannot be null or empty.", nameof(databasePath));

            if (tiles == null || tiles.Count == 0)
                return; // No work to do

            try
            {
                var writer = GetOrCreateWriter(databasePath);
                var batch = new TileBatch(tiles);

                if (!writer._writeQueue.TryAdd(batch, TimeSpan.FromSeconds(1)))
                {
                    WitLogger.LogWarning($"DatabaseWriter: Failed to enqueue batch for '{databasePath}' - queue may be full");
                }
            }
            catch (Exception ex)
            {
                WitLogger.LogError($"DatabaseWriter: Failed to enqueue tiles for '{databasePath}': {ex.Message}");
            }
        }

        /// <summary>
        /// Safely disposes the database writer for the specified path.
        /// </summary>
        /// <param name="databasePath">Path to the database writer to dispose.</param>
        /// <returns>True if a writer was found and disposed, false otherwise.</returns>
        public static bool DisposeWriter(string databasePath)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
                return false;

            if (ActiveWriters.TryRemove(databasePath, out var writer))
            {
                try
                {
                    writer.Dispose();
                    return true;
                }
                catch (Exception ex)
                {
                    WitLogger.LogError($"DatabaseWriter: Error disposing writer for '{databasePath}': {ex.Message}");
                }
            }
            return false;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Main processing loop for the worker thread. Continuously processes write batches from the queue.
        /// </summary>
        private void ProcessWriteQueue()
        {
            try
            {
                foreach (var batch in _writeQueue.GetConsumingEnumerable())
                {
                    if (_isShuttingDown)
                        break;

                    ProcessTileBatch(batch);
                }
            }
            catch (ThreadInterruptedException)
            {
                if (!_isShuttingDown)
                {
                    WitLogger.LogError($"DatabaseWriter: Processing loop terminated by interrupt for '{_databasePath}'");
                }
            }
            catch (Exception ex)
            {
                WitLogger.LogError($"DatabaseWriter: Processing loop terminated with error for '{_databasePath}': {ex.Message}");
            }
            finally
            {
                WitLogger.Log($"DatabaseWriter: Worker thread exiting for '{Path.GetFileName(_databasePath)}'");
            }
        }

        /// <summary>
        /// Processes a single batch of tiles, writing them to the database transactionally.
        /// </summary>
        /// <param name="batch">The tile batch to process.</param>
        private void ProcessTileBatch(TileBatch batch)
        {
            if (batch.Tiles == null || batch.Tiles.Count == 0)
                return;

            try
            {
                if (_databaseConnection == null)
                {
                    WitLogger.LogWarning($"DatabaseWriter: Skipping batch - database connection is null for '{_databasePath}'");
                    return;
                }

                _databaseConnection.RunInTransaction(() =>
                {
                    foreach (var tile in batch.Tiles)
                    {
                        try
                        {
                            WriteTileToDatabase(tile);
                        }
                        catch (Exception ex)
                        {
                            WitLogger.LogWarning($"DatabaseWriter: Failed to write tile ({tile.TileX},{tile.TileY},{tile.Zoom}): {ex.Message}");
                        }
                    }
                });

                // Log batch completion for large batches
                if (batch.Tiles.Count > 10)
                {
                    var processingTime = DateTime.UtcNow - batch.EnqueuedAt;
                    WitLogger.Log($"DatabaseWriter: Processed {batch.Tiles.Count} tiles in {processingTime.TotalMilliseconds:F0}ms");
                }
            }
            catch (ThreadInterruptedException)
            {
                if (_isShuttingDown)
                    return; // Expected during shutdown

                throw; // Unexpected interruption
            }
            catch (Exception ex)
            {
                WitLogger.LogError($"DatabaseWriter: Batch write failed for '{_databasePath}': {ex.Message}");
                Thread.Sleep(100); // Brief backoff to avoid tight error loops
            }
        }

        /// <summary>
        /// Writes a single tile to the database using INSERT OR IGNORE and conditional UPDATE pattern.
        /// </summary>
        /// <param name="tile">The tile to write to the database.</param>
        private void WriteTileToDatabase(Tile tile)
        {
            var hasNormalData = tile.NormalData?.Length > 0;
            var hasGeoData = tile.GeoData?.Length > 0;

            if (!hasNormalData && !hasGeoData)
            {
                // Placeholder row: insert if missing, don't update any data columns
                _databaseConnection.Execute(
                    "INSERT OR IGNORE INTO Tile (TileX, TileY, Zoom) VALUES (?, ?, ?)",
                    tile.TileX, tile.TileY, tile.Zoom);
            }
            else
            {
                // Ensure a row exists before attempting updates
                _databaseConnection.Execute(
                    "INSERT OR IGNORE INTO Tile (TileX, TileY, Zoom) VALUES (?, ?, ?)",
                    tile.TileX, tile.TileY, tile.Zoom);

                // Update only non-empty columns, and only if the existing column is empty
                if (hasNormalData)
                {
                    _databaseConnection.Execute(
                        "UPDATE Tile SET NormalData = ? WHERE TileX = ? AND TileY = ? AND Zoom = ? AND (NormalData IS NULL OR length(NormalData) = 0)",
                        tile.NormalData, tile.TileX, tile.TileY, tile.Zoom);
                }

                if (hasGeoData)
                {
                    _databaseConnection.Execute(
                        "UPDATE Tile SET GeoData = ? WHERE TileX = ? AND TileY = ? AND Zoom = ? AND (GeoData IS NULL OR length(GeoData) = 0)",
                        tile.GeoData, tile.TileX, tile.TileY, tile.Zoom);
                }
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_isShuttingDown)
                return; // Already disposing

            try
            {
                // Signal shutdown and stop accepting new batches
                _isShuttingDown = true;
                _writeQueue.CompleteAdding();

                // Wait for the worker thread to finish processing existing batches
                if (!_workerThread.Join(ThreadJoinTimeoutMs))
                {
                    WitLogger.LogWarning($"DatabaseWriter: Worker thread for '{_databasePath}' did not exit within {ThreadJoinTimeoutMs}ms timeout");
                }
            }
            catch (Exception ex)
            {
                WitLogger.LogError($"DatabaseWriter: Error during shutdown for '{_databasePath}': {ex.Message}");
            }
            finally
            {
                // Close database connection and cleanup resources
                try
                {
                    DatabaseUtils.SafeCloseConnection(_databaseConnection, checkpoint: true);
                    _writeQueue?.Dispose();
                }
                catch (Exception ex)
                {
                    WitLogger.LogError($"DatabaseWriter: Error closing resources for '{_databasePath}': {ex.Message}");
                }

                // Remove from active writers
                ActiveWriters.TryRemove(_databasePath, out _);
                WitLogger.Log($"DatabaseWriter: Disposed for '{Path.GetFileName(_databasePath)}'");
            }
        }

        #endregion
    }
}
