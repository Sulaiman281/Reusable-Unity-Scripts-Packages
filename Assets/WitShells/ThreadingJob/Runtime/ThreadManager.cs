using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using WitShells.DesignPatterns;
using WitShells.DesignPatterns.Core;

namespace WitShells.ThreadingJob
{
    public partial class ThreadManager : MonoSingleton<ThreadManager>, IDisposable
    {
        [Header("Stats")]
        [SerializeField] private ThreadManagerStats stats;

        [Header("Thread Configuration")]
        [SerializeField] private int maxThreads = 4;

        private List<JobThread> _jobThreads = new List<JobThread>();
        private Queue<ThreadJobItem> _threadJobs = new Queue<ThreadJobItem>();

        private bool _isShuttingDown;

        // Properties
        public int ActiveThreads => _jobThreads.Count(jt => jt.IsBusy);
        public int QueuedJobs => _jobThreads.Sum(jt => jt.PendingJobCount);

        public override void Awake()
        {
            base.Awake();
            Initialize();
        }

        private void Initialize()
        {
            // Create and start JobThread pool
            for (int i = 0; i < maxThreads; i++)
            {
                var jobThread = new JobThread();
                jobThread.Start();
                _jobThreads.Add(jobThread);
            }

            WitLogger.Log($"[ThreadManager] Initialized with {maxThreads} JobThreads");
        }

        private void FixedUpdate()
        {
            // Update each JobThread's main thread processing
            foreach (var jobThread in _jobThreads)
            {
                jobThread.MainThreadUpdate();
            }
        }

        private void Update()
        {
            ProcessMainThreadQueue();
            stats = GetStats();
        }

        private void ProcessMainThreadQueue()
        {
            while (_threadJobs.Count > 0)
            {
                var availableThread = GetAvailableJobThread();
                if (availableThread != null)
                {
                    var jobItem = _threadJobs.Dequeue();
                    if (availableThread.TryEnqueue(jobItem))
                    {

                    }
                    else
                    {
                        _threadJobs.Enqueue(jobItem); // re-enqueue if failed
                    }
                }
                else
                {
                    break; // No available threads, exit loop
                }
            }
        }

        /// <summary>
        /// Find the best available JobThread for enqueueing a job.
        /// Prefers non-busy threads, then threads with the smallest queue.
        /// </summary>
        private JobThread GetAvailableJobThread()
        {
            if (_jobThreads.Count == 0) return null;

            // First, try to find a non-busy thread
            var nonBusyThread = _jobThreads.FirstOrDefault(jt => !jt.IsBusy && jt.IsRunning);
            if (nonBusyThread != null) return nonBusyThread;

            // If all are busy, find the one with the smallest queue
            return _jobThreads.Where(jt => jt.IsRunning).OrderBy(jt => jt.PendingJobCount).FirstOrDefault();
        }

        /// <summary>
        /// Helper method to enqueue multiple jobs into a single JobThread.
        /// Distributes jobs across available threads if no single thread can handle all.
        /// </summary>
        public bool EnqueueJobsBatch(List<ThreadJobItem> jobs)
        {
            if (_isShuttingDown)
            {
                WitLogger.LogWarning("[ThreadManager] Cannot enqueue jobs batch, manager is shutting down");
                return false;
            }

            if (jobs == null || jobs.Count == 0) return false;

            // Try to find a single JobThread that can handle all jobs
            var bestThread = GetAvailableJobThread();
            if (bestThread == null)
            {
                WitLogger.LogWarning("[ThreadManager] No available JobThreads for batch enqueue");
                return false;
            }

            // Attempt to enqueue all jobs to the best thread
            bool allEnqueued = true;
            foreach (var job in jobs)
            {
                if (!bestThread.TryEnqueue(job))
                {
                    // If this thread can't handle more, try others
                    bestThread = GetAvailableJobThread();
                    if (bestThread != null && bestThread.TryEnqueue(job))
                    {
                        WitLogger.Log($"[ThreadManager] Job {job.JobId} enqueued to alternate thread {bestThread.Id}");
                    }
                    else
                    {
                        allEnqueued = false;
                        WitLogger.LogWarning($"[ThreadManager] Failed to enqueue job {job.JobId} in batch");
                    }
                }
                else
                {
                    WitLogger.Log($"[ThreadManager] Job {job.JobId} enqueued to thread {bestThread.Id}");
                }
            }

            return allEnqueued;
        }

        // Public API methods (keeping same signatures for backward compatibility)
        public string EnqueueJob<TResult>(ThreadJob<TResult> job, UnityAction<TResult> onComplete, UnityAction<Exception> onError = null)
        {
            if (_isShuttingDown)
            {
                WitLogger.LogWarning("[ThreadManager] Cannot enqueue job, manager is shutting down");
                return null;
            }

            if (job.IsStreaming)
            {
                WitLogger.LogWarning("Use EnqueueStreamingJob for streaming jobs");
                return null;
            }

            var jobItem = new ThreadJobItem<TResult>(job, onComplete, onError);

            // Create cancellation token for this specific job
            var jobCancellationToken = new CancellationTokenSource();

            // Find available JobThread and enqueue
            var availableThread = GetAvailableJobThread();
            if (availableThread == null)
            {
                WitLogger.LogWarning("[ThreadManager] No available JobThreads");
                onError?.Invoke(new InvalidOperationException("No available threads"));
                return null;
            }

            if (!availableThread.TryEnqueue(jobItem))
            {
                WitLogger.LogWarning("[ThreadManager] Failed to enqueue job to available thread");
                onError?.Invoke(new InvalidOperationException("Failed to enqueue job"));
                return null;
            }

            WitLogger.Log($"[ThreadManager] Enqueued job {jobItem.JobId} to thread {availableThread.Id}");
            return jobItem.JobId;
        }

        public string EnqueueStreamingJob<TResult>(
            ThreadJob<TResult> job,
            UnityAction<TResult> onProgress,
            UnityAction onComplete = null,
            UnityAction<Exception> onError = null)
        {
            if (_isShuttingDown)
            {
                WitLogger.LogWarning("[ThreadManager] Cannot enqueue streaming job, manager is shutting down");
                onError?.Invoke(new InvalidOperationException("Manager is shutting down"));
                return null;
            }

            if (!job.IsStreaming)
            {
                WitLogger.LogWarning("Use EnqueueJob for non-streaming jobs");
                onError?.Invoke(new InvalidOperationException("Job is not streaming"));
                return null;
            }

            var jobItem = new StreamingThreadJobItem<TResult>(job, onProgress, onComplete, onError);

            // Find available JobThread and enqueue
            var availableThread = GetAvailableJobThread();
            if (availableThread == null)
            {
                WitLogger.LogWarning("[ThreadManager] No available JobThreads for streaming job");
                onError?.Invoke(new InvalidOperationException("No available threads"));
                return null;
            }

            if (!availableThread.TryEnqueue(jobItem))
            {
                WitLogger.LogWarning("[ThreadManager] Failed to enqueue streaming job to available thread");
                onError?.Invoke(new InvalidOperationException("Failed to enqueue streaming job"));
                return null;
            }
            WitLogger.Log($"[ThreadManager] Enqueued streaming job {jobItem.JobId} to thread {availableThread.Id}");
            return jobItem.JobId;
        }

        public bool CancelJob(string jobId)
        {
            foreach (var jobThread in _jobThreads)
            {
                jobThread.CancelJob(jobId);
            }
            return true;
        }

        public bool IsJobActive(string jobId)
        {
            return false;
        }

        public string[] GetActiveJobIds()
        {
            return Array.Empty<string>();
        }

        public ThreadManagerStats GetStats()
        {
            return new ThreadManagerStats
            {
                ActiveThreads = _jobThreads.Count(jt => !jt.IsBusy),
                QueuedJobs = _jobThreads.Sum(jt => jt.PendingJobCount),
                PendingMainThreadActions = _threadJobs.Count,
                MaxThreads = maxThreads,
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
            WitLogger.Log("[ThreadManager] Starting shutdown...");

            // Stop and dispose all JobThreads
            foreach (var jobThread in _jobThreads)
            {
                try
                {
                    jobThread.Dispose();
                }
                catch (Exception ex)
                {
                    WitLogger.LogError($"[ThreadManager] Error disposing JobThread {jobThread.Id}: {ex}");
                }
            }

            _jobThreads.Clear();


            WitLogger.Log("[ThreadManager] Shutdown complete");
        }

        private void OnApplicationPause(bool pauseStatus)
        {

        }

        private void OnApplicationFocus(bool hasFocus)
        {

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