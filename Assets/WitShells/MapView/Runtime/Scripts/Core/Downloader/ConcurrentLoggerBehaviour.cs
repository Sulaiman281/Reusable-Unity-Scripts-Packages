using System;
using System.Collections.Concurrent;
using UnityEngine;
using WitShells.DesignPatterns.Core;

namespace WitShells.MapView
{
    /// <summary>
    /// ConcurrentLoggerBehaviour receives log messages from any thread via the static Enqueue method
    /// and flushes them to the Unity Console inside Update on the main thread. It is created
    /// automatically on first use and marked DontDestroyOnLoad.
    /// </summary>
    public class ConcurrentLoggerBehaviour : MonoSingleton<ConcurrentLoggerBehaviour>
    {
        struct LogEntry { public LogType Type; public string Message; public DateTime Time; }

        private readonly ConcurrentQueue<LogEntry> _queue = new ConcurrentQueue<LogEntry>();

        // configurable limits
        [Tooltip("Maximum number of log messages flushed to the Console per frame.")]
        public int maxLogsPerFrame = 500;

        [Tooltip("If true, each message is prefixed with an ISO UTC timestamp.")]
        public bool showTimestamp = true;

        /// <summary>
        /// Enqueue a message to be logged on the main thread. Safe to call from any thread.
        /// </summary>
        public static void Enqueue(string message, LogType type = LogType.Log)
        {
            if (message == null) return;
            Instance._queue.Enqueue(new LogEntry { Type = type, Message = message, Time = DateTime.UtcNow });
        }


        private void Update()
        {
            int count = 0;
            while (count < maxLogsPerFrame && _queue.TryDequeue(out var entry))
            {
                var msg = showTimestamp ? $"[{entry.Time:O}] {entry.Message}" : entry.Message;
                switch (entry.Type)
                {
                    case LogType.Warning: Debug.LogWarning(msg); break;
                    case LogType.Error: Debug.LogError(msg); break;
                    case LogType.Assert: Debug.LogAssertion(msg); break;
                    case LogType.Exception: Debug.LogException(new Exception(msg)); break;
                    default: Debug.Log(msg); break;
                }
                count++;
            }
        }
    }
}
