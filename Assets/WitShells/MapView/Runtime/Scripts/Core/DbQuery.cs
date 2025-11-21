using System;
using SQLite;
using UnityEngine.Events;
using WitShells.ThreadingJob;

namespace WitShells.MapView
{
    /// <summary>
    /// Helper to run read-only database queries on ThreadManager and return results on the main thread.
    /// </summary>
    public static class DbQuery
    {
        private class QueryJob<TResult> : ThreadJob<TResult>
        {
            private readonly string _path;
            private readonly Func<SQLiteConnection, TResult> _work;

            public QueryJob(string path, Func<SQLiteConnection, TResult> work)
            {
                _path = path;
                _work = work ?? throw new ArgumentNullException(nameof(work));
                IsAsync = true;
            }

            public override TResult Execute()
            {
                // Not used for async path
                return default;
            }

            public override async System.Threading.Tasks.Task<TResult> ExecuteAsync()
            {
                return await System.Threading.Tasks.Task.Run(() =>
                {
                    SQLiteConnection conn = null;
                    try
                    {
                        conn = DatabaseUtils.EnsureDatabaseWithSchema(_path);
                        if (conn == null)
                        {
                            throw new InvalidOperationException($"Failed to open database connection to '{_path}'");
                        }
                        
                        var result = _work(conn);
                        // ensure we return a fully materialized result if the work returned
                        // something that might lazily depend on the connection. It's the caller's
                        // responsibility to materialize; we only ensure the connection is closed.
                        return result;
                    }
                    catch (Exception ex)
                    {
                        ConcurrentLoggerBehaviour.Enqueue($"DbQuery: query failed for '{_path}': {ex.Message}");
                        throw;
                    }
                    finally
                    {
                        DatabaseUtils.SafeCloseConnection(conn);
                    }
                }).ConfigureAwait(false);
            }
        }

        public static string EnqueueQuery<TResult>(string path, Func<SQLiteConnection, TResult> work, UnityAction<TResult> onComplete, UnityAction<Exception> onError = null)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (work == null) throw new ArgumentNullException(nameof(work));

            var job = new QueryJob<TResult>(path, work);
            return ThreadManager.Instance.EnqueueJob(job, onComplete, onError);
        }
    }
}
