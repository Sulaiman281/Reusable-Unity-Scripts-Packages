using System;
using UnityEngine;

namespace WitShells.WebSocket
{
    [CreateAssetMenu(fileName = "WebSocketConnectionSettings", menuName = "WitShells/WebSocket/Connection Settings")]
    public class ConnectionSettingsObject : ScriptableObject
    {
        [Header("Connection Settings")]
        public string serverUrl = "ws://localhost:8080";
        public string[] protocols;
        public int defaultProtocolIndex = 0;
        public bool useSecureConnection = false;

        public Uri GetServerUri()
        {
            var scheme = useSecureConnection ? "wss" : "ws";
            var uriBuilder = new UriBuilder(scheme, new Uri(serverUrl).Host, new Uri(serverUrl).Port);
            return uriBuilder.Uri;
        }
    }
}