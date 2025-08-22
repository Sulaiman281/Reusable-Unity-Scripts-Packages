using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using WitShells.DesignPatterns.Core;

namespace WitShells.ThreadingJob
{


    public partial class ThreadManager : MonoSingleton<ThreadManager>
    {
        [SerializeField] private int maxThreads = 4;
        private readonly Queue<Action> mainThreadQueue = new Queue<Action>();
        private readonly List<Thread> runningThreads = new List<Thread>();
        private readonly Queue<Action> threadQueue = new Queue<Action>();
        private int activeThreads = 0;

        private void Update()
        {
            lock (mainThreadQueue)
            {
                while (mainThreadQueue.Count > 0)
                {
                    mainThreadQueue.Dequeue()?.Invoke();
                }
            }

            CleanupFinishedThreads();
            RunQueuedThreads();
        }

        private void CleanupFinishedThreads()
        {
            lock (runningThreads)
            {
                for (int i = runningThreads.Count - 1; i >= 0; i--)
                {
                    if (!runningThreads[i].IsAlive)
                    {
                        runningThreads.RemoveAt(i);
                        activeThreads--;
                    }
                }
            }
        }

        // Original single-result method
        public void EnqueueJob<TResult>(ThreadJob<TResult> job, UnityAction<TResult> onComplete, UnityAction<Exception> onError = null)
        {
            if (job.IsStreaming)
            {
                Debug.LogWarning("Use EnqueueStreamingJob for streaming jobs");
                return;
            }

            if (job.IsAsync)
            {
                // Async jobs run on ThreadPool, not separate threads
                _ = Task.Run(async () =>
                {
                    TResult result = default;
                    Exception exception = null;
                    try
                    {
                        result = await job.ExecuteAsync();
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }

                    lock (mainThreadQueue)
                    {
                        mainThreadQueue.Enqueue(() =>
                        {
                            if (exception == null)
                                onComplete?.Invoke(result);
                            else
                                onError?.Invoke(exception);
                        });
                    }
                });
            }
            else
            {
                // Use synchronous execution
                void SyncThreadAction()
                {
                    TResult result = default;
                    Exception exception = null;
                    try
                    {
                        result = job.Execute();
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }

                    lock (mainThreadQueue)
                    {
                        mainThreadQueue.Enqueue(() =>
                        {
                            if (exception == null)
                                onComplete?.Invoke(result);
                            else
                                onError?.Invoke(exception);
                        });
                    }
                }

                lock (threadQueue)
                {
                    threadQueue.Enqueue(() =>
                    {
                        var thread = new Thread(SyncThreadAction) { IsBackground = true };
                        lock (runningThreads)
                        {
                            runningThreads.Add(thread);
                            activeThreads++;
                        }
                        thread.Start();
                    });
                }
            }
        }

        // New streaming method with progress callbacks
        public void EnqueueStreamingJob<TResult>(
            ThreadJob<TResult> job,
            UnityAction<TResult> onProgress,
            UnityAction onComplete = null,
            UnityAction<Exception> onError = null)
        {
            if (!job.IsStreaming)
            {
                Debug.LogWarning("Use EnqueueJob for non-streaming jobs");
                return;
            }

            if (job.IsAsync)
            {
                _ = Task.Run(async () =>
                {
                    Exception exception = null;
                    try
                    {
                        await job.ExecuteStreamingAsync(
                            (result) =>
                            {
                                lock (mainThreadQueue)
                                {
                                    mainThreadQueue.Enqueue(() => onProgress?.Invoke(result));
                                }
                            },
                            () =>
                            {
                                lock (mainThreadQueue)
                                {
                                    mainThreadQueue.Enqueue(() => onComplete?.Invoke());
                                }
                            }
                        );
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                        lock (mainThreadQueue)
                        {
                            mainThreadQueue.Enqueue(() => onError?.Invoke(exception));
                        }
                    }
                });
            }
            else
            {
                void SyncStreamingAction()
                {
                    Exception exception = null;
                    try
                    {
                        job.ExecuteStreaming(
                            (result) =>
                            {
                                lock (mainThreadQueue)
                                {
                                    mainThreadQueue.Enqueue(() => onProgress?.Invoke(result));
                                }
                            },
                            () =>
                            {
                                lock (mainThreadQueue)
                                {
                                    mainThreadQueue.Enqueue(() => onComplete?.Invoke());
                                }
                            }
                        );
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                        lock (mainThreadQueue)
                        {
                            mainThreadQueue.Enqueue(() => onError?.Invoke(exception));
                        }
                    }
                }

                lock (threadQueue)
                {
                    threadQueue.Enqueue(() =>
                    {
                        var thread = new Thread(SyncStreamingAction) { IsBackground = true };
                        lock (runningThreads)
                        {
                            runningThreads.Add(thread);
                            activeThreads++;
                        }
                        thread.Start();
                    });
                }
            }
        }

        private void RunQueuedThreads()
        {
            lock (threadQueue)
            {
                while (activeThreads < maxThreads && threadQueue.Count > 0)
                {
                    var action = threadQueue.Dequeue();
                    action?.Invoke();
                }
            }
        }

        protected override void OnDestroy()
        {
            // Clean shutdown
            lock (runningThreads)
            {
                foreach (var thread in runningThreads)
                {
                    if (thread.IsAlive)
                        thread.Join(1000); // Wait up to 1 second
                }
                runningThreads.Clear();
            }

            base.OnDestroy();
        }
    }
}