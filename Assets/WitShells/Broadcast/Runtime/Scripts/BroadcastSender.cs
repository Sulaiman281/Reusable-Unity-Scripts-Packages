using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using WitShells.ThreadingJob;
using WitShells.DesignPatterns;

namespace WitShells.Broadcast
{
    /// <summary>
    /// Sends UDP broadcast packets with optional periodic modes.
    /// Modes:
    /// - LoopSingle: repeat the same message at a fixed interval.
    /// - UntilResponse: repeat until a response is observed (subscribe to a BroadcastService instance).
    /// - RetryOnFailure: re-send only when local send throws; limited attempts.
    /// </summary>
    public class BroadcastSender : IDisposable
    {
        private Timer _timer;
        private bool _stopped;

        public enum PeriodicMode
        {
            LoopSingle,
            UntilResponse,
            RetryOnFailure,
        }

        /// <summary>
        /// Start periodic sending in the selected mode.
        /// For UntilResponse, pass a listening service; when it raises OnResponseReceived the sender stops.
        /// </summary>
        public void Start(string message, int port, PeriodicMode mode, int intervalMs = 1000, int maxRetries = 3, BroadcastService listenService = null)
        {
            Stop();

            switch (mode)
            {
                case PeriodicMode.LoopSingle:
                {
                    _timer = new Timer(_ =>
                    {
                        ThreadManager.Instance.EnqueueJob(new BroadcastSendJob(message, port), _ => { }, ex => WitLogger.LogWarning($"BroadcastSender: send failed: {ex.Message}"));
                    }, null, 0, intervalMs);
                    break;
                }
                case PeriodicMode.UntilResponse:
                {
                    if (listenService == null)
                    {
                        WitLogger.LogWarning("BroadcastSender: UntilResponse mode requires a BroadcastService instance");
                        return;
                    }

                    void Handler(string payload, IPEndPoint ep)
                    {
                        try { _timer?.Dispose(); } catch { }
                        _timer = null;
                        listenService.OnResponseReceived -= Handler;
                        _stopped = true;
                    }
                    listenService.OnResponseReceived += Handler;

                    _timer = new Timer(_ =>
                    {
                        if (_stopped) return;
                        ThreadManager.Instance.EnqueueJob(new BroadcastSendJob(message, port), __ => { }, ex => WitLogger.LogWarning($"BroadcastSender: send failed: {ex.Message}"));
                    }, null, 0, intervalMs);
                    break;
                }
                case PeriodicMode.RetryOnFailure:
                {
                    int attempts = 0;
                    void TrySendOnce()
                    {
                        attempts++;
                        ThreadManager.Instance.EnqueueJob(new BroadcastSendJob(message, port),
                            onComplete: _ => { /* success: no repeat */ },
                            onError: ex =>
                            {
                                if (attempts >= maxRetries)
                                {
                                    WitLogger.LogWarning($"BroadcastSender: send failed after {attempts} attempts: {ex.Message}");
                                    return;
                                }
                                // schedule next attempt
                                try
                                {
                                    _timer?.Dispose();
                                    _timer = new Timer(__ => TrySendOnce(), null, intervalMs, Timeout.Infinite);
                                }
                                catch { }
                            });
                    }

                    TrySendOnce();
                    break;
                }
            }
        }

        public void Stop()
        {
            _stopped = true;
            try { _timer?.Dispose(); } catch { }
            _timer = null;
        }

        public void Dispose()
        {
            Stop();
        }
    }

    /// <summary>
    /// ThreadingJob to send one UDP broadcast packet.
    /// </summary>
    internal class BroadcastSendJob : ThreadJob<bool>
    {
        private readonly string _message;
        private readonly int _port;

        public BroadcastSendJob(string message, int port)
        {
            _message = message;
            _port = port;
        }

        public override bool Execute()
        {
            try
            {
                using (var client = new UdpClient())
                {
                    client.EnableBroadcast = true;
                    var bytes = Encoding.UTF8.GetBytes(_message ?? string.Empty);
                    var ep = new IPEndPoint(IPAddress.Broadcast, _port);
                    client.Send(bytes, bytes.Length, ep);
                }
                return true;
            }
            catch (Exception ex)
            {
                Exception = ex;
                return false;
            }
        }
    }
}
