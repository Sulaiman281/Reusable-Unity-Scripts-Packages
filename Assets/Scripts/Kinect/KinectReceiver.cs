// KinectReceiver.cs — Unity MonoBehaviour WebSocket client for WitPose Kinect stream.
//
// SETUP:
//   1. Package Manager → Add package by name: com.unity.nuget.newtonsoft-json
//   2. Project Settings → Player → Other Settings → Api Compatibility Level: .NET 4.x
//   3. Attach this component to any GameObject in your scene.
//
// USAGE:
//   • Subscribe to OnBodyFrame and OnBodyLost events.
//   • Read LatestFrame directly for polling.
//   • KinectConvert.ToPosition() / ToRotation() convert to Unity space.

using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace WitPose
{
    // ─────────────────────────────────────────────────────────────────
    // UNITY EVENTS  (serialisable so they appear in the Inspector)
    // ─────────────────────────────────────────────────────────────────

    [Serializable] public class BodyFrameEvent : UnityEvent<KinectBodyFrame> { }
    [Serializable] public class BodyLostEvent  : UnityEvent { }

    // ─────────────────────────────────────────────────────────────────
    // RECEIVER
    // ─────────────────────────────────────────────────────────────────

    public class KinectReceiver : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────
        [Header("Connection")]
        [Tooltip("WebSocket host — must match WEBSOCKET_HOST in kinect_stream.py")]
        public string Host = "localhost";

        [Tooltip("WebSocket port — must match WEBSOCKET_PORT in kinect_stream.py")]
        public int Port = 8080;

        [Tooltip("Seconds between reconnection attempts when disconnected")]
        [Range(0.5f, 10f)]
        public float ReconnectDelay = 2f;

        [Header("Events")]
        [Tooltip("Fired on the main thread every time a new body frame arrives")]
        public BodyFrameEvent OnBodyFrame;

        [Tooltip("Fired on the main thread when body tracking is lost")]
        public BodyLostEvent OnBodyLost;

        // ── Public state ──────────────────────────────────────────────
        /// <summary>Most recently received body frame. Null when no body is tracked.</summary>
        public KinectBodyFrame LatestFrame { get; private set; }

        /// <summary>Whether a body is currently being tracked.</summary>
        public bool IsTracked { get; private set; }

        /// <summary>Current WebSocket connection state.</summary>
        public WebSocketState ConnectionState => _ws?.State ?? WebSocketState.None;

        /// <summary>Whether the receiver is actively connected.</summary>
        public bool IsConnected => _ws?.State == WebSocketState.Open;

        // ── Internals ─────────────────────────────────────────────────
        private ClientWebSocket _ws;
        private CancellationTokenSource _cts;
        private readonly ConcurrentQueue<Action> _mainThreadQueue = new ConcurrentQueue<Action>();
        private bool _wasTracked;

        // ─────────────────────────────────────────────────────────────
        // LIFECYCLE
        // ─────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            _cts = new CancellationTokenSource();
            _ = ConnectLoop(_cts.Token);
        }

        private void OnDisable()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _ws?.Dispose();
            _ws = null;
        }

        private void Update()
        {
            // Drain the thread-safe queue — dispatch callbacks on the main thread.
            while (_mainThreadQueue.TryDequeue(out var action))
                action?.Invoke();
        }

        // ─────────────────────────────────────────────────────────────
        // CONNECTION LOOP  (background task — auto-reconnects)
        // ─────────────────────────────────────────────────────────────

        private async Task ConnectLoop(CancellationToken ct)
        {
            var uri = new Uri($"ws://{Host}:{Port}");

            while (!ct.IsCancellationRequested)
            {
                _ws?.Dispose();
                _ws = new ClientWebSocket();

                try
                {
                    Debug.Log($"[KinectReceiver] Connecting to {uri} …");
                    await _ws.ConnectAsync(uri, ct);
                    Debug.Log($"[KinectReceiver] Connected to {uri}");
                    await ReceiveLoop(ct);
                }
                catch (OperationCanceledException)
                {
                    break;  // Intentional shutdown
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[KinectReceiver] Disconnected: {ex.Message}. Retrying in {ReconnectDelay}s …");
                }

                if (!ct.IsCancellationRequested)
                    await Task.Delay(TimeSpan.FromSeconds(ReconnectDelay), ct).ContinueWith(_ => { });
            }

            Debug.Log("[KinectReceiver] Receiver stopped.");
        }

        // ─────────────────────────────────────────────────────────────
        // RECEIVE LOOP  (background — runs while connected)
        // ─────────────────────────────────────────────────────────────

        private async Task ReceiveLoop(CancellationToken ct)
        {
            var buffer = new byte[65536];

            while (_ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", ct);
                    break;
                }

                if (result.MessageType != WebSocketMessageType.Text)
                    continue;

                // Handle fragmented messages (unlikely at 30 FPS but safe)
                int totalBytes = result.Count;
                while (!result.EndOfMessage)
                {
                    if (totalBytes >= buffer.Length)
                        Array.Resize(ref buffer, buffer.Length * 2);

                    result = await _ws.ReceiveAsync(
                        new ArraySegment<byte>(buffer, totalBytes, buffer.Length - totalBytes), ct);
                    totalBytes += result.Count;
                }

                var json = Encoding.UTF8.GetString(buffer, 0, totalBytes);
                Debug.Log($"[KinectReceiver] Received message: {json}");
                ProcessMessage(json);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // MESSAGE PROCESSING  (still on background thread — enqueues to main)
        // ─────────────────────────────────────────────────────────────

        private void ProcessMessage(string json)
        {
            try
            {
                var root = JObject.Parse(json);
                var type = root["type"]?.ToString();

                if (type == "body_frame")
                {
                    var frame = root.ToObject<KinectBodyFrame>();
                    _mainThreadQueue.Enqueue(() =>
                    {
                        LatestFrame = frame;
                        IsTracked   = frame.Tracked;
                        _wasTracked = true;
                        OnBodyFrame?.Invoke(frame);
                    });
                }
                else if (type == "heartbeat")
                {
                    _mainThreadQueue.Enqueue(() =>
                    {
                        if (_wasTracked)
                        {
                            _wasTracked = false;
                            IsTracked   = false;
                            LatestFrame = null;
                            OnBodyLost?.Invoke();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[KinectReceiver] Failed to parse message: {ex.Message}");
            }
        }
    }
}
