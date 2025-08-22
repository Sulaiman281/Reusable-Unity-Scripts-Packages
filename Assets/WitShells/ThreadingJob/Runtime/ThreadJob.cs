using System;
using System.Threading.Tasks;

namespace WitShells.ThreadingJob
{
    public abstract class ThreadJob<TResult>
    {
        public TResult Result { get; protected set; }
        public Exception Exception { get; protected set; }
        public virtual bool IsAsync { get; protected set; } = false;
        public virtual bool IsStreaming { get; protected set; } = false;

        public virtual TResult Execute()
        {
            throw new NotImplementedException();
        }

        public virtual Task<TResult> ExecuteAsync()
        {
            return Task.FromResult(Execute());
        }

        // Streaming execution - reports progress/intermediate results
        public virtual void ExecuteStreaming(Action<TResult> onProgress, Action onComplete = null)
            => throw new NotImplementedException();
        public virtual Task ExecuteStreamingAsync(Action<TResult> onProgress, Action onComplete = null)
            => throw new NotImplementedException();
    }
}