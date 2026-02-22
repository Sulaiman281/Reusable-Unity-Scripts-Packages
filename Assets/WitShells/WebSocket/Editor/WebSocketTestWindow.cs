using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using WitShells.DesignPatterns;
using WitShells.WebSocket;

namespace WitShells.WebSocket.Editor
{
    /// <summary>
    /// Editor window for testing WebSocket connections and functionality.
    /// Requires a selected GameObject with a WebSocketHandler component.
    /// </summary>
    public class WebSocketTestWindow : EditorWindow
    {
        private const int MaxLogLines = 200;

        private WebSocketHandler m_Handler;
        private GameObject m_SelectedObject;

        private ConnectionSettingsObject m_Settings;
        private string m_Path = "/";
        private int m_ProtocolIndex = -1;
        private string m_SendText = "hello";

        private Vector2 m_LogScroll;
        private readonly List<string> m_LogLines = new List<string>();
        private readonly ConcurrentQueue<string> m_LogQueue = new ConcurrentQueue<string>();

        [MenuItem("WitShells/WebSocket/Test Connection", true)]
        public static bool ValidateOpen()
        {
            return Selection.activeGameObject != null;
        }

        [MenuItem("WitShells/WebSocket/Test Connection")]
        public static void Open()
        {
            GetWindow<WebSocketTestWindow>("WebSocket Test");
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            RefreshSelectedObject();
        }

        private void OnDisable()
        {
            try
            {
                EditorApplication.update -= OnEditorUpdate;
                UnregisterEvents();
            }
            catch (System.Exception ex)
            {
                WitLogger.LogError($"[WebSocketTestWindow] Error during OnDisable: {ex.Message}");
            }
        }

        private void OnEditorUpdate()
        {
            var hasChanges = false;
            while (m_LogQueue.TryDequeue(out var line))
            {
                m_LogLines.Add(line);
                if (m_LogLines.Count > MaxLogLines)
                {
                    m_LogLines.RemoveAt(0);
                }
                hasChanges = true;
            }

            if (hasChanges)
            {
                Repaint();
            }
        }

        private void OnGUI()
        {
            using (new GUILayout.VerticalScope())
            {
                DrawSettings();
                EditorGUILayout.Space(6);
                DrawControls();
                EditorGUILayout.Space(6);
                DrawSend();
                EditorGUILayout.Space(6);
                DrawLogs();
            }
        }

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("Selected Object", EditorStyles.boldLabel);

            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Object:", GUILayout.Width(60));
                EditorGUILayout.LabelField(m_SelectedObject ? m_SelectedObject.name : "None", EditorStyles.helpBox);

                if (GUILayout.Button("Refresh", GUILayout.Width(60)))
                {
                    RefreshSelectedObject();
                }
            }

            if (m_SelectedObject == null)
            {
                EditorGUILayout.HelpBox("Please select a GameObject in the hierarchy.", MessageType.Warning);
                return;
            }

            if (m_Handler == null)
            {
                EditorGUILayout.HelpBox("Selected object doesn't have WebSocketHandler component.", MessageType.Warning);
                if (GUILayout.Button("Add WebSocketHandler Component"))
                {
                    m_Handler = m_SelectedObject.AddComponent<WebSocketHandler>();
                    EnqueueLog("Added WebSocketHandler component to selected object.");
                }
                return;
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Connection", EditorStyles.boldLabel);
            m_Settings = (ConnectionSettingsObject)EditorGUILayout.ObjectField(
                "Settings",
                m_Settings,
                typeof(ConnectionSettingsObject),
                false);

            m_Path = EditorGUILayout.TextField("Path", m_Path);
            m_ProtocolIndex = EditorGUILayout.IntField("Protocol Index", m_ProtocolIndex);

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Initialize"))
                {
                    if (m_Handler != null && m_Settings != null)
                    {
                        UnregisterEvents(); // Unregister first to avoid duplicates
                        RegisterEvents();   // Register events
                        m_Handler.Initialize(m_Settings, m_Path, m_ProtocolIndex);
                        EnqueueLog("Initialized WebSocket handler and registered events.");
                    }
                    else
                    {
                        EnqueueLog("Error: Handler or Settings is null");
                    }
                }

                GUILayout.FlexibleSpace();

                var state = m_Handler != null ? m_Handler.State.ToString() : "None";
                EditorGUILayout.LabelField("State", state, GUILayout.Width(200));
            }
        }

        private void DrawControls()
        {
            EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);

            if (m_Handler == null)
            {
                EditorGUILayout.HelpBox("WebSocketHandler component is required.", MessageType.Info);
                return;
            }

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Connect"))
                {
                    try
                    {
                        m_Handler.Connect();
                    }
                    catch (System.Exception ex)
                    {
                        EnqueueLog($"Error connecting: {ex.Message}");
                    }
                }

                if (GUILayout.Button("Close"))
                {
                    try
                    {
                        m_Handler.Close();
                    }
                    catch (System.Exception ex)
                    {
                        EnqueueLog($"Error closing: {ex.Message}");
                    }
                }
            }
        }

        private void DrawSend()
        {
            EditorGUILayout.LabelField("Send Message", EditorStyles.boldLabel);

            if (m_Handler == null)
            {
                EditorGUILayout.HelpBox("WebSocketHandler component is required.", MessageType.Info);
                return;
            }

            m_SendText = EditorGUILayout.TextField("Text Message", m_SendText);

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Send as Binary (UTF-8)"))
                {
                    try
                    {
                        if (m_Handler.State == WebSocketState.Open)
                        {
                            var bytes = Encoding.UTF8.GetBytes(m_SendText ?? string.Empty);
                            if (m_Handler.SendBinaryData(bytes))
                            {
                                EnqueueLog($"Sent binary data: {m_SendText}");
                            }
                            else
                            {
                                EnqueueLog("Failed to send binary data");
                            }
                        }
                        else
                        {
                            EnqueueLog($"Error: Cannot send, connection state is {m_Handler.State}");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        EnqueueLog($"Error sending binary data: {ex.Message}");
                    }
                }

                if (GUILayout.Button("Send as Text"))
                {
                    try
                    {
                        if (m_Handler.State == WebSocketState.Open)
                        {
                            if (m_Handler.SendTextMessage(m_SendText ?? string.Empty))
                            {
                                EnqueueLog($"Sent text message: {m_SendText}");
                            }
                            else
                            {
                                EnqueueLog("Failed to send text message");
                            }
                        }
                        else
                        {
                            EnqueueLog($"Error: Cannot send, connection state is {m_Handler.State}");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        EnqueueLog($"Error sending text message: {ex.Message}");
                    }
                }
            }
        }

        private void DrawLogs()
        {
            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Logs", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Clear", GUILayout.Width(50)))
                {
                    m_LogLines.Clear();
                    while (m_LogQueue.TryDequeue(out _)) { }
                }
            }

            m_LogScroll = EditorGUILayout.BeginScrollView(m_LogScroll, GUILayout.Height(220));
            for (var i = 0; i < m_LogLines.Count; i++)
            {
                EditorGUILayout.LabelField(m_LogLines[i]);
            }
            EditorGUILayout.EndScrollView();
        }

        private void RefreshSelectedObject()
        {
            // Unregister from previous handler
            UnregisterEvents();

            // Get selected object and its WebSocketHandler component
            m_SelectedObject = Selection.activeGameObject;
            m_Handler = m_SelectedObject?.GetComponent<WebSocketHandler>();

            if (m_SelectedObject != null)
            {
                EnqueueLog($"Selected object: {m_SelectedObject.name}");
                if (m_Handler != null)
                {
                    EnqueueLog("Found WebSocketHandler component.");
                }
                else
                {
                    EnqueueLog("No WebSocketHandler component found on selected object.");
                }
            }
            else
            {
                EnqueueLog("No object selected.");
            }
        }

        private void RegisterEvents()
        {
            if (m_Handler == null) return;

            try
            {
                m_Handler.OnConnectionOpened.AddListener(HandleConnectionOpened);
                m_Handler.OnConnectionClosed.AddListener(HandleConnectionClosed);
                m_Handler.OnError.AddListener(HandleError);
                m_Handler.OnTextMessageReceived.AddListener(HandleTextMessage);
                m_Handler.OnBinaryDataReceived.AddListener(HandleBinaryMessage);

                Debug.Log("[WebSocketTestWindow] Events registered successfully");
            }
            catch (System.Exception ex)
            {
                WitLogger.LogError($"[WebSocketTestWindow] Error registering events: {ex.Message}");
            }
        }

        private void UnregisterEvents()
        {
            if (m_Handler == null) return;

            try
            {
                m_Handler.OnConnectionOpened.RemoveListener(HandleConnectionOpened);
                m_Handler.OnConnectionClosed.RemoveListener(HandleConnectionClosed);
                m_Handler.OnError.RemoveListener(HandleError);
                m_Handler.OnTextMessageReceived.RemoveListener(HandleTextMessage);
                m_Handler.OnBinaryDataReceived.RemoveListener(HandleBinaryMessage);
            }
            catch (System.Exception ex)
            {
                WitLogger.LogWarning($"[WebSocketTestWindow] Error unregistering events: {ex.Message}");
            }
        }

        private void HandleConnectionOpened()
        {
            Debug.Log("[WebSocketTestWindow] HandleConnectionOpened called");
            EnqueueLog("Connected successfully.");
        }

        private void HandleConnectionClosed()
        {
            Debug.Log("[WebSocketTestWindow] HandleConnectionClosed called");
            EnqueueLog("Connection closed.");
        }

        private void HandleError(string message)
        {
            EnqueueLog($"Error: {message}");
        }

        private void HandleTextMessage(string message)
        {
            Debug.Log($"[WebSocketTestWindow] HandleTextMessage called with: '{message}'");
            EnqueueLog($"Text Received: {message}");
        }

        private void HandleBinaryMessage(byte[] data)
        {
            Debug.Log($"[WebSocketTestWindow] HandleBinaryMessage called with {data?.Length ?? 0} bytes");
            var message = data != null ? Encoding.UTF8.GetString(data) : string.Empty;
            EnqueueLog($"Binary Received: {data?.Length ?? 0} bytes - {message}");
        }

        private void EnqueueLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            m_LogQueue.Enqueue($"[{timestamp}] {message}");
        }
    }
}
