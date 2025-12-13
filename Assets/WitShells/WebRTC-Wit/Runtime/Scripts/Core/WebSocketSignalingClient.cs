using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;

namespace WitShells.WebRTCWit
{
    public class WebSocketSignalingClient : IDisposable
    {
        private ClientWebSocket _ws;
        private CancellationTokenSource _cts;
        public event Action<string> OnMessage;
        public event Action<string> OnError;
        public bool IsConnected => _ws != null && _ws.State == WebSocketState.Open;

        public async Task ConnectAsync(string url)
        {
            _ws = new ClientWebSocket();
            _cts = new CancellationTokenSource();
            try
            {
                await _ws.ConnectAsync(new Uri(url), _cts.Token);
                _ = ReceiveLoop();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
            }
        }

        public async Task SendAsync(string message)
        {
            if (!IsConnected) { OnError?.Invoke("WebSocket not connected"); return; }
            var buffer = System.Text.Encoding.UTF8.GetBytes(message);
            await _ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cts.Token);
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[8192];
            try
            {
                while (_ws.State == WebSocketState.Open)
                {
                    var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", _cts.Token);
                        break;
                    }
                    var text = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                    OnMessage?.Invoke(text);
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
            }
        }

        public void Dispose()
        {
            try { _cts?.Cancel(); } catch { }
            _ws?.Dispose();
            _cts?.Dispose();
        }
    }
}
