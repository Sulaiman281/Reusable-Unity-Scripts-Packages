using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using WitShells.DesignPatterns;
using WitShells.ThreadingJob;

namespace WitShells.Broadcast
{
    /// <summary>
    /// Listens for UDP responses on a given port and raises events.
    /// Usage:
    /// var svc = new BroadcastService();
    /// svc.OnResponseReceived += (msg, remote) => { ... };
    /// svc.StartBroadcast("ip_request", port: 7777, repeatIntervalMs: 0, waitForResponses: true, singleResponse: false);
    /// svc.Stop();
    /// svc.Dispose();
    /// </summary>
    public class BroadcastService : IDisposable
    {
        /// <summary>Raised when a response packet is received. Message is UTF8-decoded.</summary>
        public event Action<string, IPEndPoint> OnResponseReceived;

        /// <summary>
        /// Start listening for UDP responses on a port. If singleResponse is true, stop after first message.
        /// </summary>
        public void StartListening(int port, bool singleResponse = true)
        {
            var listenJob = new BroadcastListenJob(port, singleResponse);
            ThreadManager.Instance.EnqueueStreamingJob<string>(listenJob,
                onProgress: (msg) =>
                {
                    var parts = msg.Split('|');
                    if (parts.Length >= 3 && int.TryParse(parts[1], out var p))
                    {
                        var ep = new IPEndPoint(IPAddress.Parse(parts[0]), p);
                        OnResponseReceived?.Invoke(parts[2], ep);
                    }
                    else
                    {
                        OnResponseReceived?.Invoke(msg, new IPEndPoint(IPAddress.None, 0));
                    }
                },
                onComplete: null,
                onError: ex => WitLogger.LogWarning($"BroadcastService: listen job error: {ex.Message}"));
        }

        /// <summary>Stops listening (no-op; streaming job ends on singleResponse). For future cancellation support.</summary>
        public void Stop()
        {
            // If we add cancellation tokens to jobs, cancel here.
        }

        public void Dispose()
        {
            Stop();
        }
    }

    // Note: sending is handled by a separate sender class.

    /// <summary>
    /// Streaming job that listens on UDP and reports each received message via onProgress.
    /// Reports combined string "ip|port|payload" to avoid custom structs across job boundary.
    /// </summary>
    internal class BroadcastListenJob : ThreadJob<string>
    {
        private readonly int _port;
        private readonly bool _singleResponse;
        public override bool IsStreaming { get; protected set; } = true;

        public BroadcastListenJob(int port, bool singleResponse)
        {
            _port = port;
            _singleResponse = singleResponse;
        }

        public override void ExecuteStreaming(Action<string> onProgress, Action onComplete = null)
        {
            UdpClient listener = null;
            try
            {
                listener = new UdpClient(_port);
                while (true)
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = listener.Receive(ref remoteEP);
                    string message = Encoding.UTF8.GetString(data);
                    onProgress?.Invoke($"{remoteEP.Address}|{remoteEP.Port}|{message}");
                    if (_singleResponse) break;
                }
            }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                Exception = ex;
            }
            finally
            {
                try { listener?.Close(); } catch { }
                onComplete?.Invoke();
            }
        }
    }
}
