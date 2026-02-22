using System;
using System.Collections.Concurrent;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using WebSocketSharp;
using WitShells.DesignPatterns;

namespace WitShells.WebSocket
{
    /// <summary>
    /// Represents the current state of the WebSocket connection.
    /// </summary>
    public enum WebSocketState
    {
        /// <summary>No connection has been initiated</summary>
        None,
        /// <summary>Connection is being established</summary>
        Connecting,
        /// <summary>Connection is open and ready for communication</summary>
        Open,
        /// <summary>Connection is being closed</summary>
        Closing,
        /// <summary>Connection has been closed</summary>
        Closed,
        /// <summary>An error occurred during connection or communication</summary>
        Error
    }

    /// <summary>
    /// UnityEvent for string messages.
    /// </summary>
    [Serializable]
    public class WebSocketStringEvent : UnityEvent<string> { }

    /// <summary>
    /// UnityEvent for binary data.
    /// </summary>
    [Serializable]
    public class WebSocketBinaryEvent : UnityEvent<byte[]> { }

    /// <summary>
    /// A Unity WebSocket client component that provides easy integration with Unity's event system.
    /// Handles connection management, message sending/receiving, and provides Unity Events for all operations.
    /// </summary>
    [AddComponentMenu("WitShells/WebSocket Handler")]
    public class WebSocketHandler : MonoBehaviour
    {
        #region Public Properties
        
        /// <summary>
        /// Current state of the WebSocket connection.
        /// </summary>
        public WebSocketState State { get; private set; } = WebSocketState.None;
        
        #endregion
        
        #region Inspector Fields
        
        [Header("Connection Settings")]
        [SerializeField] 
        [Tooltip("Configuration settings for WebSocket connection")]
        private ConnectionSettingsObject connectionSettings;

        [Header("Unity Events")]
        [Tooltip("Invoked when connection is successfully opened")]
        public UnityEvent OnConnectionOpened;
        
        [Tooltip("Invoked when connection is closed")]
        public UnityEvent OnConnectionClosed;
        
        [Tooltip("Invoked when an error occurs")]
        public WebSocketStringEvent OnError;
        
        [Tooltip("Invoked when a text message is received")]
        public WebSocketStringEvent OnTextMessageReceived;
        
        [Tooltip("Invoked when binary data is received")]
        public WebSocketBinaryEvent OnBinaryDataReceived;
        
        #endregion
        
        #region Private Fields
        
        /// <summary>
        /// The underlying WebSocketSharp WebSocket instance.
        /// </summary>
        private WebSocketSharp.WebSocket m_WebSocket;
        
        /// <summary>
        /// Queue for thread-safe event marshaling from background WebSocket thread to Unity main thread.
        /// </summary>
        private readonly ConcurrentQueue<System.Action> m_MainThreadActions = new ConcurrentQueue<System.Action>();
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Initializes the WebSocket connection with the specified settings.
        /// </summary>
        /// <param name="settings">Connection configuration settings</param>
        /// <param name="pathOverride">Optional path to override the default path (e.g., "/api/websocket")</param>
        /// <param name="protocolIndex">Index of the protocol to use from settings, or -1 for default</param>
        /// <exception cref="ArgumentNullException">Thrown when settings is null</exception>
        public void Initialize(ConnectionSettingsObject settings, string pathOverride = null, int protocolIndex = -1)
        {
            connectionSettings = settings;

            if (connectionSettings == null)
            {
                WitLogger.LogError("[WebSocketHandler] Connection settings cannot be null.");
                State = WebSocketState.Error;
                throw new ArgumentNullException(nameof(settings), "Connection settings cannot be null.");
            }

            var baseUri = connectionSettings.GetServerUri();
            var uriBuilder = new UriBuilder(baseUri);

            if (!string.IsNullOrWhiteSpace(pathOverride))
            {
                uriBuilder.Path = pathOverride.StartsWith("/") ? pathOverride : "/" + pathOverride;
            }

            if (protocolIndex < 0)
            {
                protocolIndex = connectionSettings.defaultProtocolIndex;
            }

            string selectedProtocol = null;
            if (connectionSettings.protocols != null &&
                connectionSettings.protocols.Length > 0 &&
                protocolIndex >= 0 &&
                protocolIndex < connectionSettings.protocols.Length)
            {
                selectedProtocol = connectionSettings.protocols[protocolIndex];
            }

            CreateWebSocketConnection(uriBuilder.Uri.ToString(), selectedProtocol);
        }

        /// <summary>
        /// Initiates a connection to the WebSocket server.
        /// Must call Initialize() first before attempting to connect.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when WebSocket is not initialized</exception>
        public void Connect()
        {
            if (m_WebSocket == null)
            {
                const string errorMessage = "WebSocket is not initialized. Call Initialize() first.";
                WitLogger.LogError($"[WebSocketHandler] {errorMessage}");
                State = WebSocketState.Error;
                throw new InvalidOperationException(errorMessage);
            }

            if (State == WebSocketState.Connecting || State == WebSocketState.Open)
            {
                WitLogger.LogWarning($"[WebSocketHandler] Connection already in progress or established. Current state: {State}");
                return;
            }

            State = WebSocketState.Connecting;
            WitLogger.Log($"[WebSocketHandler] Attempting to connect to: {m_WebSocket.Url}");
            
            try
            {
                m_WebSocket.ConnectAsync();
            }
            catch (Exception ex)
            {
                WitLogger.LogError($"[WebSocketHandler] Failed to initiate connection: {ex.Message}");
                State = WebSocketState.Error;
                OnError?.Invoke(ex.Message);
            }
        }

        /// <summary>
        /// Gracefully closes the WebSocket connection.
        /// </summary>
        public void Close()
        {
            if (m_WebSocket == null || State == WebSocketState.Closed || State == WebSocketState.Closing)
            {
                return;
            }

            State = WebSocketState.Closing;
            WitLogger.Log("[WebSocketHandler] Closing WebSocket connection...");
            
            try
            {
                m_WebSocket.CloseAsync();
            }
            catch (Exception ex)
            {
                WitLogger.LogError($"[WebSocketHandler] Error while closing connection: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends binary data to the WebSocket server.
        /// </summary>
        /// <param name="data">Binary data to send</param>
        /// <returns>True if data was sent successfully, false otherwise</returns>
        public bool SendBinaryData(byte[] data)
        {
            if (m_WebSocket == null || State != WebSocketState.Open || data == null)
            {
                WitLogger.LogWarning($"[WebSocketHandler] Cannot send data. State: {State}, Data is null: {data == null}");
                return false;
            }

            try
            {
                m_WebSocket.SendAsync(data, null);
                WitLogger.Log($"[WebSocketHandler] Sent {data.Length} bytes of binary data");
                return true;
            }
            catch (Exception ex)
            {
                WitLogger.LogError($"[WebSocketHandler] Failed to send binary data: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Sends text message to the WebSocket server.
        /// </summary>
        /// <param name="message">Text message to send</param>
        /// <returns>True if message was sent successfully, false otherwise</returns>
        public bool SendTextMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                WitLogger.LogWarning("[WebSocketHandler] Cannot send empty or null message");
                return false;
            }
            
            var data = Encoding.UTF8.GetBytes(message);
            return SendBinaryData(data);
        }
        
        /// <summary>
        /// Legacy method for backward compatibility. Use SendBinaryData instead.
        /// </summary>
        /// <param name="data">Binary data to send</param>
        [System.Obsolete("Use SendBinaryData instead for better clarity", false)]
        public void Send(byte[] data)
        {
            SendBinaryData(data);
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Creates and configures the WebSocket connection.
        /// </summary>
        /// <param name="url">WebSocket server URL</param>
        /// <param name="protocol">Optional protocol to use</param>
        private void CreateWebSocketConnection(string url, string protocol)
        {
            DisposeWebSocket();

            var finalUrl = url;
            if (!string.IsNullOrWhiteSpace(protocol))
            {
                finalUrl = url.TrimEnd('/') + "/" + protocol;
            }

            WitLogger.Log($"[WebSocketHandler] Creating WebSocket connection to: {finalUrl}");
            m_WebSocket = new WebSocketSharp.WebSocket(finalUrl);

            // Register event handlers
            m_WebSocket.OnOpen += HandleConnectionOpened;
            m_WebSocket.OnClose += HandleConnectionClosed;
            m_WebSocket.OnError += HandleConnectionError;
            m_WebSocket.OnMessage += HandleMessageReceived;
        }

        /// <summary>
        /// Handles the WebSocket connection opened event.
        /// </summary>
        private void HandleConnectionOpened(object sender, EventArgs e)
        {
            State = WebSocketState.Open;
            WitLogger.Log("[WebSocketHandler] WebSocket connection opened successfully.");
            OnConnectionOpened?.Invoke();
        }

        /// <summary>
        /// Handles the WebSocket connection closed event.
        /// </summary>
        private void HandleConnectionClosed(object sender, CloseEventArgs e)
        {
            State = WebSocketState.Closed;
            WitLogger.Log($"[WebSocketHandler] WebSocket connection closed. Code: {e.Code}, Reason: {e.Reason}");
            OnConnectionClosed?.Invoke();
        }

        /// <summary>
        /// Handles WebSocket connection errors.
        /// </summary>
        private void HandleConnectionError(object sender, ErrorEventArgs e)
        {
            State = WebSocketState.Error;
            WitLogger.LogError($"[WebSocketHandler] WebSocket error occurred: {e.Message}");
            OnError?.Invoke(e.Message);
        }

        /// <summary>
        /// Handles incoming messages from the WebSocket server.
        /// </summary>
        private void HandleMessageReceived(object sender, MessageEventArgs e)
        {
            WitLogger.Log($"[WebSocketHandler] Message received - Type: {(e.IsText ? "Text" : "Binary")}, Size: {(e.IsText ? e.Data?.Length ?? 0 : e.RawData?.Length ?? 0)} {(e.IsText ? "chars" : "bytes")}");
            
            if (e.IsText)
            {
                WitLogger.Log($"[WebSocketHandler] About to invoke OnTextMessageReceived with: '{e.Data}', Listeners: {OnTextMessageReceived?.GetPersistentEventCount() ?? -1}");
                OnTextMessageReceived?.Invoke(e.Data);
                WitLogger.Log("[WebSocketHandler] OnTextMessageReceived invoked");
            }
            else if (e.IsBinary)
            {
                WitLogger.Log($"[WebSocketHandler] About to invoke OnBinaryDataReceived with {e.RawData?.Length ?? 0} bytes, Listeners: {OnBinaryDataReceived?.GetPersistentEventCount() ?? -1}");
                OnBinaryDataReceived?.Invoke(e.RawData);
                WitLogger.Log("[WebSocketHandler] OnBinaryDataReceived invoked");
            }
        }

        #endregion
        
        #region Unity Lifecycle
        
        /// <summary>
        /// Unity lifecycle method called when the component is being destroyed.
        /// </summary>
        private void OnDestroy()
        {
            DisposeWebSocket();
        }
        
        #endregion
        
        #region Cleanup
        
        /// <summary>
        /// Properly disposes of the WebSocket connection and cleans up resources.
        /// </summary>
        private void DisposeWebSocket()
        {
            if (m_WebSocket == null)
            {
                return;
            }

            WitLogger.Log("[WebSocketHandler] Disposing WebSocket connection...");

            try
            {
                // Unregister event handlers to prevent memory leaks
                m_WebSocket.OnOpen -= HandleConnectionOpened;
                m_WebSocket.OnClose -= HandleConnectionClosed;
                m_WebSocket.OnError -= HandleConnectionError;
                m_WebSocket.OnMessage -= HandleMessageReceived;
                
                // Close connection if still alive
                if (m_WebSocket.IsAlive)
                {
                    m_WebSocket.Close();
                }
            }
            catch (Exception ex)
            {
                WitLogger.LogWarning($"[WebSocketHandler] Error during WebSocket disposal: {ex.Message}");
            }
            finally
            {
                m_WebSocket = null;
                State = WebSocketState.None;
                WitLogger.Log("[WebSocketHandler] WebSocket disposed successfully.");
            }
        }
        
        #endregion
    
    }

}