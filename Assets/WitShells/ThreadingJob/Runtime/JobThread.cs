using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WitShells.DesignPatterns;

namespace WitShells.ThreadingJob
{
    public class JobThread : IDisposable
    {
        public string Id { get; }
        // internal worker task + queue for async/await friendly processing
        private readonly BlockingCollection<ThreadJobItem> _jobQueue =
                    new BlockingCollection<ThreadJobItem>(boundedCapacity: 1000);
        private Task _processingTask;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private ConcurrentQueue<Action> _mainThreadQueue = new ConcurrentQueue<Action>();
        private ConcurrentQueue<string> _logQueue = new ConcurrentQueue<string>();
        private volatile bool _isRunning;
        private volatile bool _isBusy;

        public bool IsBusy => _isBusy;
        public bool IsRunning => _isRunning;
        public int PendingJobCount => _jobQueue.Count;

        public JobThread()
        {
            Id = Guid.NewGuid().ToString();
            // nothing else to do here; Start() will begin the processing task
        }

        public void CancelJob(string jobId)
        {
            // get threadjobitem from jobqueue via id
            var toCancel = _jobQueue.FirstOrDefault(j => string.Equals(j.JobId, jobId, StringComparison.Ordinal));
            if (toCancel != null)
            {
                _jobQueue.TryTake(out var _);
            }
        }

        public void MainThreadUpdate()
        {
            // Process main thread actions
            while (_mainThreadQueue.TryDequeue(out var action))
            {
                try { action?.Invoke(); } catch (Exception ex) { WitLogger.LogError($"[JobThread:{Id}] MainThread action error: {ex}"); }
            }

            // Process log messages
            while (_logQueue.TryDequeue(out var logMessage))
            {
                WitLogger.Log(logMessage);
            }
        }

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;

            // Keep the real loop task so Dispose/Stop can wait on it correctly
            var loopTask = Task.Run(() => ProcessLoopAsync(_cts.Token));
            loopTask.ContinueWith(t =>
            {
                Error($"[JobThread:{Id}] Processing exception: {t.Exception.Flatten()}");
            }, TaskContinuationOptions.OnlyOnFaulted);
            _processingTask = loopTask;

        }

        public void Stop()
        {
            if (!_isRunning) return;
            _cts.Cancel();
            _jobQueue.CompleteAdding();
        }

        /// <summary>
        /// Enqueue a job for this dedicated thread to process.
        /// Fire-and-forget; use EnqueueAsync to await result.
        /// </summary>
        public bool TryEnqueue(ThreadJobItem job)
        {
            if (job == null) return false;
            if (!_isRunning || _jobQueue.IsAddingCompleted) return false;

            // Non-blocking enqueue that respects bounded capacity (no Count race, no blocking)
            if (!_jobQueue.TryAdd(job))
            {
                Warn($"[JobThread:{Id}] Job queue full; rejecting job {job.JobId}.");
                return false;
            }
            return true;
        }

        private void EnqueueMainThreadAction(Action action)
        {
            _mainThreadQueue.Enqueue(action);
        }

        private void Log(string message)
        {
            _logQueue.Enqueue(message);
        }

        private void Warn(string message)
        {
            _logQueue.Enqueue($"[WARNING] {message}");
        }

        private void Error(string message)
        {
            _logQueue.Enqueue($"[ERROR] {message}");
        }

        private async Task ProcessLoopAsync(CancellationToken ct)
        {
            try
            {

                foreach (var jobItem in _jobQueue.GetConsumingEnumerable(ct))
                {
                    if (ct.IsCancellationRequested) break;

                    _isBusy = true;
                    try
                    {
                        // Process job
                        if (jobItem.IsStreaming)
                        {
                            await HandleStreamingJobAsync(jobItem, ct).ConfigureAwait(false);
                        }
                        else if (jobItem.IsAsync)
                        {
                            await HandleParallelJobAsync(jobItem, ct).ConfigureAwait(false);
                        }
                        else
                        {
                            var res = jobItem.Execute();
                            SafeResult(jobItem, res);
                        }
                        Log($"[JobThread:{Id}] Job {jobItem.JobId} completed successfully.");
                    }
                    catch (OperationCanceledException)
                    {
                        EnqueueMainThreadAction(() => jobItem.OnError?.Invoke(new OperationCanceledException("Job cancelled")));
                        Warn($"[JobThread:{Id}] Job {jobItem.JobId} cancelled.");
                    }
                    catch (Exception ex)
                    {
                        EnqueueMainThreadAction(() => jobItem.OnError?.Invoke(ex));
                        Warn($"[JobThread:{Id}] Job {jobItem.JobId} encountered an error: {ex.Message}");
                    }
                    finally
                    {
                        _isBusy = false;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on cancellation
            }
            catch (Exception ex)
            {
                Error($"[JobThread:{Id}] Processing loop error: {ex}");
            }
            finally
            {
                Log($"[JobThread:{Id}] Processing loop has exited.");
                _isRunning = false;
            }
        }

        private void SafeResult(ThreadJobItem jobItem, object result)
        {
            EnqueueMainThreadAction(() =>
            {
                jobItem.OnResult?.Invoke(result);
                jobItem.OnComplete?.Invoke();
            });
        }

        private async Task HandleParallelJobAsync(ThreadJobItem jobItem, CancellationToken ct)
        {
            var results = await jobItem.ExecuteAsync().ConfigureAwait(false);
            SafeResult(jobItem, results);
        }

        private async Task HandleStreamingJobAsync(ThreadJobItem jobItem, CancellationToken ct)
        {
            void SafeProgress(object obj)
            {
                EnqueueMainThreadAction(() =>
                {
                    jobItem.OnProgress?.Invoke(obj);
                });
            }

            void SafeComplete()
            {
                EnqueueMainThreadAction(() =>
                {
                    jobItem.OnComplete?.Invoke();
                });
            }

            if (jobItem.IsAsync)
            {
                var task = jobItem.ExecuteStreamingAsync((obj) => SafeProgress(obj), () => SafeComplete());
                if (task != null) await task.ConfigureAwait(false);
            }
            else
            {
                // run synchronous streaming on a background thread to avoid blocking the processing loop
                jobItem.ExecuteStreaming((obj) => SafeProgress(obj), () => SafeComplete());
            }
        }

        public void Dispose()
        {
            try
            {
                _cts.Cancel();
                _jobQueue.CompleteAdding();
                _processingTask?.Wait(2000);
            }
            catch (Exception ex)
            {
                WitLogger.LogWarning($"[JobThread:{Id}] Dispose error: {ex}");
                _processingTask.Dispose(); // force dispose
            }
            finally
            {
                _cts.Dispose();
            }
        }
    }
}