using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using WitShells.DesignPatterns.Core;

namespace WitShells.ThreadingJob
{
    public partial class ThreadManager : MonoSingleton<ThreadManager>, IDisposable
    {
        [Header("Thread Configuration")]
        [SerializeField] private int maxThreads = 4;
        [SerializeField] private int maxQueueSize = 1000;
        [SerializeField] private bool enableDebugLogs = false;

        // Concurrent collections for thread-safe operations
        private readonly ConcurrentQueue<Action> _mainThreadQueue = new ConcurrentQueue<Action>();
        private readonly ConcurrentQueue<ThreadJobItem> _threadJobQueue = new ConcurrentQueue<ThreadJobItem>();
        private readonly ConcurrentBag<Thread> _runningThreads = new ConcurrentBag<Thread>();
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _jobCancellationTokens = new ConcurrentDictionary<string, CancellationTokenSource>();
        private readonly ConcurrentDictionary<string, ThreadJobItem> _activeJobs = new ConcurrentDictionary<string, ThreadJobItem>();

        // Thread-safe counters
        private volatile int _activeThreads = 0;
        private volatile bool _isShuttingDown = false;
        private volatile bool _isRunning = false;

        // Background processing thread
        private Thread _processingThread;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        // Events for monitoring
        public UnityEvent<int> OnActiveThreadsChanged;
        public UnityEvent<int> OnQueueSizeChanged;
        public UnityEvent<string> OnJobCompleted;
        public UnityEvent<string> OnJobFailed;

        // Properties
        public int ActiveThreads => _activeThreads;
        public int QueuedJobs => _threadJobQueue.Count;
        public int PendingMainThreadActions => _mainThreadQueue.Count;
        public bool IsRunning => _isRunning;

        public override void Awake()
        {
            base.Awake();
            Initialize();
        }

        private void Initialize()
        {
            _isRunning = true;

            // Start background processing thread
            _processingThread = new Thread(ProcessingLoop)
            {
                IsBackground = true,
                Name = "ThreadManager_ProcessingLoop"
            };
            _processingThread.Start();

            if (enableDebugLogs)
                Debug.Log($"[ThreadManager] Initialized with max {maxThreads} threads");
        }

        private void Update()
        {
            // Process main thread queue
            ProcessMainThreadQueue();

            // Cleanup finished threads
            CleanupFinishedThreads();

            // Update UI events
            UpdateEvents();
        }

        private void ProcessMainThreadQueue()
        {
            int processedCount = 0;
            const int maxProcessPerFrame = 10; // Prevent frame drops

            while (processedCount < maxProcessPerFrame && _mainThreadQueue.TryDequeue(out Action action))
            {
                try
                {
                    action?.Invoke();
                    processedCount++;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ThreadManager] Error executing main thread action: {ex}");
                }
            }
        }

        private void ProcessingLoop()
        {
            try
            {
                while (_isRunning && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    ProcessThreadQueue();
                    Thread.Sleep(10); // Small delay to prevent CPU spinning
                }
            }
            catch (ThreadAbortException)
            {
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ThreadManager] Processing loop error: {ex}");
            }
        }

        private void ProcessThreadQueue()
        {
            while (_activeThreads < maxThreads &&
                   _threadJobQueue.TryDequeue(out ThreadJobItem jobItem) &&
                   !_isShuttingDown)
            {
                StartJobExecution(jobItem);
            }
        }

        private void StartJobExecution(ThreadJobItem jobItem)
        {
            // Get the specific cancellation token for this job
            var jobCancellationToken = _jobCancellationTokens.TryGetValue(jobItem.JobId, out var tokenSource)
                ? tokenSource.Token
                : CancellationToken.None;

            if (jobItem.IsAsync)
            {
                // Use Task.Run for async jobs
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ExecuteAsyncJob(jobItem, jobCancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        if (enableDebugLogs)
                            Debug.Log($"[ThreadManager] Job {jobItem.JobId} was cancelled");
                    }
                    catch (Exception ex)
                    {
                        HandleJobError(jobItem, ex);
                    }
                    finally
                    {
                        CleanupJob(jobItem.JobId);
                    }
                }, _cancellationTokenSource.Token);
            }
            else
            {
                // Create dedicated thread for sync jobs
                var thread = new Thread(() =>
                {
                    try
                    {
                        ExecuteSyncJob(jobItem, jobCancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        if (enableDebugLogs)
                            Debug.Log($"[ThreadManager] Job {jobItem.JobId} was cancelled");
                    }
                    catch (Exception ex)
                    {
                        HandleJobError(jobItem, ex);
                    }
                    finally
                    {
                        CleanupJob(jobItem.JobId);
                        Interlocked.Decrement(ref _activeThreads);
                    }
                })
                {
                    IsBackground = true,
                    Name = $"ThreadJob_{jobItem.JobId}"
                };

                _runningThreads.Add(thread);
                Interlocked.Increment(ref _activeThreads);
                thread.Start();
            }
        }

        private async Task ExecuteAsyncJob(ThreadJobItem jobItem, CancellationToken cancellationToken)
        {
            try
            {
                if (jobItem.IsStreaming)
                {
                    await jobItem.ExecuteStreamingAsync(
                        (result) => EnqueueMainThreadAction(() => jobItem.OnProgress?.Invoke(result)),
                        () => EnqueueMainThreadAction(() => jobItem.OnComplete?.Invoke())
                    );
                }
                else
                {
                    var result = await jobItem.ExecuteAsync();
                    EnqueueMainThreadAction(() => jobItem.OnResult?.Invoke(result));
                }

                EnqueueMainThreadAction(() => OnJobCompleted?.Invoke(jobItem.JobId));
            }
            catch (Exception ex)
            {
                HandleJobError(jobItem, ex);
            }
        }

        private void ExecuteSyncJob(ThreadJobItem jobItem, CancellationToken cancellationToken)
        {
            try
            {
                if (jobItem.IsStreaming)
                {
                    jobItem.ExecuteStreaming(
                        (result) => EnqueueMainThreadAction(() => jobItem.OnProgress?.Invoke(result)),
                        () => EnqueueMainThreadAction(() => jobItem.OnComplete?.Invoke())
                    );
                }
                else
                {
                    var result = jobItem.Execute();
                    EnqueueMainThreadAction(() => jobItem.OnResult?.Invoke(result));
                }

                EnqueueMainThreadAction(() => OnJobCompleted?.Invoke(jobItem.JobId));
            }
            catch (Exception ex)
            {
                HandleJobError(jobItem, ex);
            }
            finally
            {
                Interlocked.Decrement(ref _activeThreads);
            }
        }

        private void HandleJobError(ThreadJobItem jobItem, Exception ex)
        {
            EnqueueMainThreadAction(() =>
            {
                jobItem.OnError?.Invoke(ex);
                OnJobFailed?.Invoke(jobItem.JobId);
            });

            if (enableDebugLogs)
                Debug.LogError($"[ThreadManager] Job {jobItem.JobId} failed: {ex}");
        }

        private void EnqueueMainThreadAction(Action action)
        {
            if (_isShuttingDown) return;

            if (_mainThreadQueue.Count >= maxQueueSize)
            {
                Debug.LogWarning("[ThreadManager] Main thread queue is full, dropping action");
                return;
            }

            _mainThreadQueue.Enqueue(action);
        }

        private void CleanupFinishedThreads()
        {
            // Note: ConcurrentBag doesn't support removing items efficiently
            // For production, consider using a different collection or periodic full cleanup
            // This is simplified for the example
        }

        private void UpdateEvents()
        {
            // Trigger events for monitoring (throttled to avoid spam)
            if (Time.frameCount % 30 == 0) // Every 30 frames
            {
                OnActiveThreadsChanged?.Invoke(_activeThreads);
                OnQueueSizeChanged?.Invoke(_threadJobQueue.Count);
            }
        }

        // Public API methods
        public string EnqueueJob<TResult>(ThreadJob<TResult> job, UnityAction<TResult> onComplete, UnityAction<Exception> onError = null)
        {
            if (_isShuttingDown)
            {
                Debug.LogWarning("[ThreadManager] Cannot enqueue job, manager is shutting down");
                return null;
            }

            if (job.IsStreaming)
            {
                Debug.LogWarning("Use EnqueueStreamingJob for streaming jobs");
                return null;
            }

            var jobItem = new ThreadJobItem<TResult>(job, onComplete, onError);

            if (_threadJobQueue.Count >= maxQueueSize)
            {
                Debug.LogWarning("[ThreadManager] Thread job queue is full, rejecting job");
                onError?.Invoke(new InvalidOperationException("Job queue is full"));
                return null;
            }

            // Create cancellation token for this specific job
            var jobCancellationToken = new CancellationTokenSource();
            _jobCancellationTokens.TryAdd(jobItem.JobId, jobCancellationToken);
            _activeJobs.TryAdd(jobItem.JobId, jobItem);

            _threadJobQueue.Enqueue(jobItem);

            if (enableDebugLogs)
                Debug.Log($"[ThreadManager] Enqueued job {jobItem.JobId}");

            return jobItem.JobId; // Return the job ID
        }

        public void EnqueueStreamingJob<TResult>(
            ThreadJob<TResult> job,
            UnityAction<TResult> onProgress,
            UnityAction onComplete = null,
            UnityAction<Exception> onError = null)
        {
            if (_isShuttingDown)
            {
                Debug.LogWarning("[ThreadManager] Cannot enqueue streaming job, manager is shutting down");
                return;
            }

            if (!job.IsStreaming)
            {
                Debug.LogWarning("Use EnqueueJob for non-streaming jobs");
                return;
            }

            var jobItem = new StreamingThreadJobItem<TResult>(job, onProgress, onComplete, onError);

            if (_threadJobQueue.Count >= maxQueueSize)
            {
                Debug.LogWarning("[ThreadManager] Thread job queue is full, rejecting streaming job");
                onError?.Invoke(new InvalidOperationException("Job queue is full"));
                return;
            }

            _threadJobQueue.Enqueue(jobItem);

            if (enableDebugLogs)
                Debug.Log($"[ThreadManager] Enqueued streaming job {jobItem.JobId}");
        }

        public bool CancelJob(string jobId)
        {
            if (string.IsNullOrEmpty(jobId)) return false;

            if (_jobCancellationTokens.TryRemove(jobId, out var tokenSource))
            {
                tokenSource.Cancel();
                tokenSource.Dispose();

                _activeJobs.TryRemove(jobId, out _);

                if (enableDebugLogs)
                    Debug.Log($"[ThreadManager] Cancelled job {jobId}");

                return true;
            }

            return false;
        }

        public bool IsJobActive(string jobId)
        {
            return _jobCancellationTokens.ContainsKey(jobId);
        }

        public string[] GetActiveJobIds()
        {
            return _jobCancellationTokens.Keys.ToArray();
        }

        // Utility methods
        public void ClearQueue()
        {
            while (_threadJobQueue.TryDequeue(out _)) { }
            if (enableDebugLogs)
                Debug.Log("[ThreadManager] Queue cleared");
        }

        public ThreadManagerStats GetStats()
        {
            return new ThreadManagerStats
            {
                ActiveThreads = _activeThreads,
                QueuedJobs = _threadJobQueue.Count,
                PendingMainThreadActions = _mainThreadQueue.Count,
                MaxThreads = maxThreads,
                IsRunning = _isRunning
            };
        }

        // Cleanup and disposal
        protected override void OnDestroy()
        {
            Dispose();
            base.OnDestroy();
        }

        public void Dispose()
        {
            if (_isShuttingDown) return;

            _isShuttingDown = true;
            _isRunning = false;

            if (enableDebugLogs)
                Debug.Log("[ThreadManager] Starting shutdown...");

            // Cancel all pending operations
            _cancellationTokenSource?.Cancel();

            // Wait for processing thread to finish
            if (_processingThread != null && _processingThread.IsAlive)
            {
                if (!_processingThread.Join(2000)) // Wait up to 2 seconds
                {
                    _processingThread.Abort();
                }
            }

            // Wait for running threads to complete
            var timeout = DateTime.Now.AddSeconds(3);
            while (_activeThreads > 0 && DateTime.Now < timeout)
            {
                Thread.Sleep(100);
            }

            // Force cleanup
            _cancellationTokenSource?.Dispose();

            if (enableDebugLogs)
                Debug.Log("[ThreadManager] Shutdown complete");
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _isRunning)
            {
                // Pause job processing
                ClearQueue();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _isRunning)
            {
                // Optional: pause or clear non-critical jobs
            }
        }

        private void CleanupJob(string jobId)
        {
            if (_jobCancellationTokens.TryRemove(jobId, out var tokenSource))
            {
                tokenSource.Dispose();
            }
            _activeJobs.TryRemove(jobId, out _);
        }
    }

    // Supporting classes
    public abstract class ThreadJobItem
    {
        public string JobId { get; } = Guid.NewGuid().ToString("N")[..8];
        public DateTime CreatedAt { get; } = DateTime.Now;
        public abstract bool IsAsync { get; }
        public abstract bool IsStreaming { get; }
        public UnityAction<Exception> OnError { get; set; }

        // Add generic access properties
        public abstract UnityAction<object> OnResult { get; }
        public abstract UnityAction<object> OnProgress { get; }
        public abstract UnityAction OnComplete { get; }

        public abstract object Execute();
        public abstract Task<object> ExecuteAsync();
        public abstract void ExecuteStreaming(Action<object> onProgress, Action onComplete);
        public abstract Task ExecuteStreamingAsync(Action<object> onProgress, Action onComplete);
    }

    public class ThreadJobItem<TResult> : ThreadJobItem
    {
        private readonly ThreadJob<TResult> _job;
        private readonly UnityAction<TResult> _onResult;

        public override bool IsAsync => _job.IsAsync;
        public override bool IsStreaming => _job.IsStreaming;

        // Generic access to typed callbacks
        public override UnityAction<object> OnResult =>
            _onResult != null ? (obj) => _onResult((TResult)obj) : null;

        public override UnityAction<object> OnProgress => null; // Not used for regular jobs
        public override UnityAction OnComplete => null; // Not used for regular jobs

        public ThreadJobItem(ThreadJob<TResult> job, UnityAction<TResult> onResult, UnityAction<Exception> onError)
        {
            _job = job;
            _onResult = onResult;
            OnError = onError;
        }

        public override object Execute() => _job.Execute();
        public override async Task<object> ExecuteAsync() => await _job.ExecuteAsync();

        public override void ExecuteStreaming(Action<object> onProgress, Action onComplete)
            => throw new NotSupportedException("Use StreamingThreadJobItem for streaming jobs");

        public override Task ExecuteStreamingAsync(Action<object> onProgress, Action onComplete)
            => throw new NotSupportedException("Use StreamingThreadJobItem for streaming jobs");
    }

    public class StreamingThreadJobItem<TResult> : ThreadJobItem
    {
        private readonly ThreadJob<TResult> _job;
        private readonly UnityAction<TResult> _onProgress;
        private readonly UnityAction _onComplete;

        public override bool IsAsync => _job.IsAsync;
        public override bool IsStreaming => true;

        // Generic access to typed callbacks
        public override UnityAction<object> OnResult => null; // Not used for streaming jobs

        public override UnityAction<object> OnProgress =>
            _onProgress != null ? (obj) => _onProgress((TResult)obj) : null;

        public override UnityAction OnComplete => _onComplete;

        public StreamingThreadJobItem(ThreadJob<TResult> job, UnityAction<TResult> onProgress, UnityAction onComplete, UnityAction<Exception> onError)
        {
            _job = job;
            _onProgress = onProgress;
            _onComplete = onComplete;
            OnError = onError;
        }

        public override object Execute() => throw new NotSupportedException("Use ExecuteStreaming for streaming jobs");
        public override Task<object> ExecuteAsync() => throw new NotSupportedException("Use ExecuteStreamingAsync for streaming jobs");

        public override void ExecuteStreaming(Action<object> onProgress, Action onComplete)
            => _job.ExecuteStreaming(result => onProgress?.Invoke(result), onComplete);

        public override async Task ExecuteStreamingAsync(Action<object> onProgress, Action onComplete)
            => await _job.ExecuteStreamingAsync(result => onProgress?.Invoke(result), onComplete);
    }

    [Serializable]
    public struct ThreadManagerStats
    {
        public int ActiveThreads;
        public int QueuedJobs;
        public int PendingMainThreadActions;
        public int MaxThreads;
        public bool IsRunning;

        public override string ToString()
        {
            return $"Threads: {ActiveThreads}/{MaxThreads}, Queued: {QueuedJobs}, Pending: {PendingMainThreadActions}, Running: {IsRunning}";
        }
    }
}