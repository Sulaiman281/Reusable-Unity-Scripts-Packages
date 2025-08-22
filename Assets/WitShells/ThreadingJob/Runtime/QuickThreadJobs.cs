namespace WitShells.ThreadingJob
{
    using System;
    using System.Threading.Tasks;
    using UnityEngine.Events;

    // Quick job implementations
    public static class QuickThreadJobs
    {
        // Simple function job
        public static void RunFunction<T>(Func<T> function, UnityAction<T> onComplete, UnityAction<Exception> onError = null)
        {
            var job = new FunctionJob<T>(function);
            ThreadManager.Instance.EnqueueJob(job, onComplete, onError);
        }

        // Simple async function job
        public static void RunFunctionAsync<T>(Func<Task<T>> asyncFunction, UnityAction<T> onComplete, UnityAction<Exception> onError = null)
        {
            var job = new AsyncFunctionJob<T>(asyncFunction);
            ThreadManager.Instance.EnqueueJob(job, onComplete, onError);
        }

        // Simple action job (no return value)
        public static void RunAction(Action action, UnityAction onComplete = null, UnityAction<Exception> onError = null)
        {
            var job = new ActionJob(action);
            ThreadManager.Instance.EnqueueJob(job, (_) => onComplete?.Invoke(), onError);
        }

        // Simple async action job
        public static void RunActionAsync(Func<Task> asyncAction, UnityAction onComplete = null, UnityAction<Exception> onError = null)
        {
            var job = new AsyncActionJob(asyncAction);
            ThreadManager.Instance.EnqueueJob(job, (_) => onComplete?.Invoke(), onError);
        }

        // Streaming job with progress
        public static void RunStreamingFunction<T>(Func<Action<T>, Action, Task> streamingFunction, UnityAction<T> onProgress, UnityAction onComplete = null, UnityAction<Exception> onError = null)
        {
            var job = new StreamingFunctionJob<T>(streamingFunction);
            ThreadManager.Instance.EnqueueStreamingJob(job, onProgress, onComplete, onError);
        }

        // File operations
        public static void ReadFileAsync(string filePath, UnityAction<string> onComplete, UnityAction<Exception> onError = null)
        {
            RunFunctionAsync(() => System.IO.File.ReadAllTextAsync(filePath), onComplete, onError);
        }

        public static void WriteFileAsync(string filePath, string content, UnityAction onComplete = null, UnityAction<Exception> onError = null)
        {
            RunActionAsync(() => System.IO.File.WriteAllTextAsync(filePath, content), onComplete, onError);
        }

        // Web request
        public static void DownloadStringAsync(string url, UnityAction<string> onComplete, UnityAction<Exception> onError = null)
        {
            RunFunctionAsync(async () =>
            {
                using var client = new System.Net.Http.HttpClient();
                return await client.GetStringAsync(url);
            }, onComplete, onError);
        }

        // Heavy computation with progress
        public static void RunHeavyComputation(int iterations, UnityAction<float> onProgress, UnityAction<float> onComplete = null, UnityAction<Exception> onError = null)
        {
            var job = new HeavyComputationJob(iterations);
            ThreadManager.Instance.EnqueueStreamingJob(job, onProgress, () => onComplete?.Invoke(1f), onError);
        }
    }

    // Internal job implementations
    internal class FunctionJob<T> : ThreadJob<T>
    {
        private readonly Func<T> function;
        public FunctionJob(Func<T> function) => this.function = function;
        public override T Execute() => function();
    }

    internal class AsyncFunctionJob<T> : ThreadJob<T>
    {
        private readonly Func<Task<T>> asyncFunction;
        public override bool IsAsync => true;
        public AsyncFunctionJob(Func<Task<T>> asyncFunction) => this.asyncFunction = asyncFunction;
        public override Task<T> ExecuteAsync() => asyncFunction();

        public override T Execute()
        {
            throw new NotImplementedException();
        }
    }

    internal class ActionJob : ThreadJob<bool>
    {
        private readonly Action action;
        public ActionJob(Action action) => this.action = action;
        public override bool Execute() { action(); return true; }
    }

    internal class AsyncActionJob : ThreadJob<bool>
    {
        private readonly Func<Task> asyncAction;
        public override bool IsAsync => true;
        public AsyncActionJob(Func<Task> asyncAction) => this.asyncAction = asyncAction;
        public override async Task<bool> ExecuteAsync() { await asyncAction(); return true; }
    }

    internal class StreamingFunctionJob<T> : ThreadJob<T>
    {
        private readonly Func<Action<T>, Action, Task> streamingFunction;
        public override bool IsStreaming => true;
        public override bool IsAsync => true;
        public StreamingFunctionJob(Func<Action<T>, Action, Task> streamingFunction) => this.streamingFunction = streamingFunction;
        public override Task ExecuteStreamingAsync(Action<T> onProgress, Action onComplete = null) => streamingFunction(onProgress, onComplete);
    }

    internal class HeavyComputationJob : ThreadJob<float>
    {
        private readonly int iterations;
        public override bool IsStreaming => true;
        public HeavyComputationJob(int iterations) => this.iterations = iterations;

        public override void ExecuteStreaming(Action<float> onProgress, Action onComplete = null)
        {
            for (int i = 0; i < iterations; i++)
            {
                // Simulate heavy work
                System.Threading.Thread.Sleep(10);
                float progress = (float)(i + 1) / iterations;
                onProgress?.Invoke(progress);
            }
            onComplete?.Invoke();
        }
    }
}