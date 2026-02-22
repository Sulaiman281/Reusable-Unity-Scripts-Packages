# WitShells WebSocket

A robust and user-friendly WebSocket client implementation for Unity with full Unity Events integration, connection management, and comprehensive editor testing tools.

## Features

- **Easy Integration**: MonoBehaviour-based component that integrates seamlessly with Unity
- **Unity Events**: Full Unity Events support for all WebSocket events (connect, disconnect, message received, errors)
- **Connection Management**: Robust connection state tracking and error handling
- **ScriptableObject Configuration**: Reusable connection settings with support for secure connections (WSS)
- **Editor Testing Tools**: Comprehensive editor window for testing connections without runtime
- **Protocol Support**: Multiple protocol support with configurable selection
- **Binary & Text Messaging**: Support for both text and binary message types
- **Thread-Safe**: Proper main thread marshaling for Unity Events

## Installation

1. Copy the `WitShells/WebSocket` folder to your Unity project's Assets folder
2. The package will automatically be available in your project

## Quick Start

### 1. Create Connection Settings

Right-click in Project window → Create → WitShells → WebSocket → Connection Settings

Configure your WebSocket server details:
- **Server URL**: Your WebSocket server URL (e.g., `ws://localhost:8080`)
- **Use Secure Connection**: Check for WSS (secure WebSocket) connections
- **Protocols**: Array of supported protocols
- **Default Protocol Index**: Which protocol to use by default

### 2. Add WebSocket Handler to GameObject

```csharp
// Add component via Inspector or code
var handler = gameObject.AddComponent<WebSocketHandler>();
```

### 3. Initialize and Connect

```csharp
public class WebSocketExample : MonoBehaviour
{
    [SerializeField] private WebSocketHandler webSocketHandler;
    [SerializeField] private ConnectionSettingsObject connectionSettings;

    void Start()
    {
        // Register for events
        webSocketHandler.OnConnectionOpened.AddListener(OnConnected);
        webSocketHandler.OnConnectionClosed.AddListener(OnDisconnected);
        webSocketHandler.OnError.AddListener(OnError);
        webSocketHandler.OnTextMessageReceived.AddListener(OnTextReceived);
        webSocketHandler.OnBinaryDataReceived.AddListener(OnBinaryReceived);

        // Initialize and connect
        webSocketHandler.Initialize(connectionSettings);
        webSocketHandler.Connect();
    }

    void OnConnected()
    {
        Debug.Log("WebSocket connected!");
        webSocketHandler.SendTextMessage("Hello Server!");
    }

    void OnDisconnected()
    {
        Debug.Log("WebSocket disconnected!");
    }

    void OnError(string error)
    {
        Debug.LogError($"WebSocket error: {error}");
    }

    void OnTextReceived(string message)
    {
        Debug.Log($"Text received: {message}");
    }

    void OnBinaryReceived(byte[] data)
    {
        Debug.Log($"Binary data received: {data.Length} bytes");
    }
}
```

## API Reference

### WebSocketHandler

#### Properties
- `WebSocketState State { get; }` - Current connection state

#### Methods
- `void Initialize(ConnectionSettingsObject settings, string pathOverride = null, int protocolIndex = -1)` - Initialize the WebSocket
- `void Connect()` - Connect to the WebSocket server
- `void Close()` - Close the connection
- `bool SendTextMessage(string message)` - Send text message
- `bool SendBinaryData(byte[] data)` - Send binary data

#### Unity Events
- `OnConnectionOpened` - Invoked when connection opens
- `OnConnectionClosed` - Invoked when connection closes
- `OnError` - Invoked on errors (provides error message)
- `OnTextMessageReceived` - Invoked when text message is received
- `OnBinaryDataReceived` - Invoked when binary data is received

### WebSocketState Enum
- `None` - No connection initiated
- `Connecting` - Connection in progress
- `Open` - Connection established
- `Closing` - Connection closing
- `Closed` - Connection closed
- `Error` - Error occurred

## Testing

### Using the Editor Test Window

1. Select a GameObject in the hierarchy
2. Menu → WitShells → WebSocket → Test Connection
3. Add WebSocketHandler component if needed
4. Configure connection settings
5. Initialize and test your connection

The test window provides:
- Component validation and auto-addition
- Real-time connection state monitoring
- Message sending capabilities (text and binary)
- Live log display with timestamps
- Event callback monitoring

## Best Practices

1. **Always check connection state** before sending messages
2. **Handle errors gracefully** by subscribing to OnError event
3. **Dispose properly** - The component automatically cleans up on destroy
4. **Use connection settings** - Create reusable ScriptableObject configs
5. **Test in editor first** - Use the test window to validate your setup

## Dependencies

- Unity 2021.3 or later
- WebSocketSharp (included in most Unity versions)

## Troubleshooting

### Connection Issues
- Verify server URL and port
- Check firewall settings
- Ensure server is running and accepts WebSocket connections
- Try testing with the editor test window first

### Event Not Firing
- Ensure events are registered before calling Connect()
- Check that the GameObject with WebSocketHandler is active
- Verify connection state is `Open` before expecting message events

### Performance
- The component automatically handles thread marshaling
- All Unity Events are called on the main thread
- Large binary messages are supported but consider chunking for very large data

## License

MIT License - see LICENSE file for details

## Support

For issues and feature requests, please contact support@witshells.com