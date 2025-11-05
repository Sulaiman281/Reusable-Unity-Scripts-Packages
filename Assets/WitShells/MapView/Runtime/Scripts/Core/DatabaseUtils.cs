using System;
using System.IO;
using SQLite;
using WitShells.DesignPatterns;

namespace WitShells.MapView
{
    /// <summary>
    /// Centralized utility for creating and ensuring proper database schema.
    /// All code should use EnsureDatabaseWithSchema instead of ad-hoc CreateTable calls.
    /// </summary>
    public static class DatabaseUtils
    {
        /// <summary>
        /// Determines if a SQLite result code represents an actual error that should be logged.
        /// Success codes like OK, Done, Row are considered normal and should not be logged as warnings.
        /// </summary>
        /// <param name="result">SQLite result code</param>
        /// <returns>True if this is an actual error, false if it's a success/status message</returns>
        private static bool IsActualSQLiteError(SQLite3.Result result)
        {
            // These are success/status codes that should not be logged as errors
            return result != SQLite3.Result.OK &&
                   result != SQLite3.Result.Done &&
                   result != SQLite3.Result.Row;
        }

        /// <summary>
        /// Creates or opens a database connection with proper schema and unique constraints.
        /// This is the single method all code should use for database creation.
        /// </summary>
        /// <param name="dbPath">Full path to the database file</param>
        /// <returns>Configured SQLiteConnection with proper schema, or null on failure</returns>
        public static SQLiteConnection EnsureDatabaseWithSchema(string dbPath)
        {
            if (string.IsNullOrEmpty(dbPath))
            {
                WitLogger.LogError("DatabaseUtils.EnsureDatabaseWithSchema: dbPath cannot be null or empty");
                return null;
            }

            try
            {
                // Ensure directory exists
                string directory = Path.GetDirectoryName(dbPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Create or open connection
                var connection = new SQLiteConnection(dbPath);

                // Apply standard pragmas for better performance and concurrency
                try
                {
                    connection.Execute("PRAGMA journal_mode=WAL;");
                    connection.Execute("PRAGMA busy_timeout=5000;");
                    connection.Execute("PRAGMA synchronous=NORMAL;");
                    WitLogger.Log($"DatabaseUtils: Successfully applied pragmas to '{Path.GetFileName(dbPath)}'");
                }
                catch (SQLiteException ex) when (IsActualSQLiteError(ex.Result))
                {
                    // Only log actual SQLite errors, not status/success messages
                    WitLogger.LogWarning($"DatabaseUtils: Failed to apply pragmas to '{Path.GetFileName(dbPath)}': {ex.Message} (SQLite Result: {ex.Result})");
                }
                catch (SQLiteException ex)
                {
                    // SQLite success messages that we should ignore (like "not an error")
                    // These are normal status messages, not actual errors
                    WitLogger.Log($"DatabaseUtils: SQLite status message for '{Path.GetFileName(dbPath)}': {ex.Message} (Result: {ex.Result})");
                }
                catch (Exception ex) when (!ex.Message.Contains("not an error"))
                {
                    // Log other unexpected exceptions, but ignore "not an error" messages from any source
                    WitLogger.LogWarning($"DatabaseUtils: Unexpected error applying pragmas to '{Path.GetFileName(dbPath)}': {ex.Message}");
                }

                // Create table with proper schema
                connection.CreateTable<Tile>();

                // Ensure unique index exists to prevent coordinate duplicates
                try
                {
                    connection.Execute("CREATE UNIQUE INDEX IF NOT EXISTS idx_tile_coord ON Tile(TileX, TileY, Zoom);");
                }
                catch (Exception ex)
                {
                    WitLogger.LogWarning($"DatabaseUtils: Failed to create unique index for '{dbPath}': {ex.Message}");
                }

                return connection;
            }
            catch (Exception ex)
            {
                WitLogger.LogError($"DatabaseUtils.EnsureDatabaseWithSchema: Failed to create/open database '{dbPath}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates or opens a read-only database connection with minimal pragmas.
        /// Used for background read queries where we don't need to modify schema.
        /// </summary>
        /// <param name="dbPath">Full path to the database file</param>
        /// <returns>SQLiteConnection for read operations, or null on failure</returns>
        public static SQLiteConnection CreateReadOnlyConnection(string dbPath)
        {
            if (string.IsNullOrEmpty(dbPath))
            {
                WitLogger.LogError("DatabaseUtils.CreateReadOnlyConnection: dbPath cannot be null or empty");
                return null;
            }

            try
            {
                var connection = new SQLiteConnection(dbPath);

                // Apply minimal pragmas for read operations
                // Skip pragmas that might cause warnings in some SQLite versions
                try
                {
                    connection.Execute("PRAGMA journal_mode=WAL;");
                }
                catch (SQLiteException ex) when (IsActualSQLiteError(ex.Result))
                {
                    // Only log actual SQLite errors, not status messages
                    WitLogger.LogWarning($"DatabaseUtils: Failed to apply read-only pragmas to '{Path.GetFileName(dbPath)}': {ex.Message} (SQLite Result: {ex.Result})");
                }
                catch (SQLiteException ex)
                {
                    // SQLite success messages - just log at info level
                    WitLogger.Log($"DatabaseUtils: SQLite status for read-only connection to '{Path.GetFileName(dbPath)}': {ex.Message}");
                }
                catch (Exception ex) when (!ex.Message.Contains("not an error"))
                {
                    // Log other unexpected exceptions, but ignore "not an error" messages
                    WitLogger.LogWarning($"DatabaseUtils: Unexpected error applying read-only pragmas to '{Path.GetFileName(dbPath)}': {ex.Message}");
                }

                return connection;
            }
            catch (Exception ex)
            {
                WitLogger.LogError($"DatabaseUtils.CreateReadOnlyConnection: Failed to open database '{dbPath}': {ex.Message}");
                return null;
            }
        }        /// <summary>
                 /// Safely closes and disposes a database connection with optional WAL checkpoint.
                 /// </summary>
                 /// <param name="connection">Connection to close</param>
                 /// <param name="checkpoint">Whether to perform WAL checkpoint before closing</param>
        public static void SafeCloseConnection(SQLiteConnection connection, bool checkpoint = false)
        {
            if (connection == null) return;

            try
            {
                if (checkpoint)
                {
                    try
                    {
                        connection.Execute("PRAGMA wal_checkpoint(TRUNCATE);");
                    }
                    catch (SQLiteException ex) when (IsActualSQLiteError(ex.Result))
                    {
                        // Only log actual SQLite errors, not status messages
                        WitLogger.LogWarning($"DatabaseUtils: Checkpoint failed: {ex.Message} (SQLite Result: {ex.Result})");
                    }
                    catch (SQLiteException ex)
                    {
                        // SQLite success messages for checkpoint
                        WitLogger.Log($"DatabaseUtils: Checkpoint status: {ex.Message}");
                    }
                    catch (Exception ex) when (!ex.Message.Contains("not an error"))
                    {
                        // Log other unexpected exceptions, but ignore "not an error" messages
                        WitLogger.LogWarning($"DatabaseUtils: Checkpoint failed: {ex.Message}");
                    }
                }
            }
            catch { }
            finally
            {
                try
                {
                    connection.Close();
                    connection.Dispose();
                }
                catch (Exception ex)
                {
                    WitLogger.LogWarning($"DatabaseUtils: Failed to close connection: {ex.Message}");
                }
            }
        }
    }
}