using System.Net;
using UnityEditor;
using UnityEngine;
using WitShells.Broadcast;

namespace WitShells.BroadcastEditor
{
    public class BroadcastWindow : EditorWindow
    {
        private int _tabIndex = 0; // 0 = Listen, 1 = Send
        private string[] _tabs = new[] { "Listen", "Send" };

        // Listen inputs
        private int _listenPort = 7777;
        private bool _singleResponse = false;
        private string _lastResponse = "";
        private BroadcastService _service;

        // Send inputs
        private string _message = "ip_request";
        private int _sendPort = 7777;
        private BroadcastSender.PeriodicMode _mode = BroadcastSender.PeriodicMode.LoopSingle;
        private int _intervalMs = 1000;
        private int _maxRetries = 3;
        private BroadcastSender _sender;

        [MenuItem("WitShells/Broadcast Tester")]
        public static void ShowWindow()
        {
            var win = GetWindow<BroadcastWindow>(false, "Broadcast Tester", true);
            win.minSize = new Vector2(420, 280);
        }

        private void OnEnable()
        {
            _service = new BroadcastService();
            _service.OnResponseReceived += OnResponseReceived;
            _sender = new BroadcastSender();
        }

        private void OnDisable()
        {
            try
            {
                _service.OnResponseReceived -= OnResponseReceived;
                _service.Stop();
                _sender.Stop();
            }
            catch { }
        }

        private void OnGUI()
        {
            _tabIndex = GUILayout.Toolbar(_tabIndex, _tabs);
            GUILayout.Space(8);

            if (_tabIndex == 0)
                DrawListenTab();
            else
                DrawSendTab();
        }

        private void DrawListenTab()
        {
            EditorGUILayout.LabelField("Listen for UDP Responses", EditorStyles.boldLabel);
            _listenPort = EditorGUILayout.IntField("Port", _listenPort);
            _singleResponse = EditorGUILayout.ToggleLeft("Stop after first response", _singleResponse);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Start Listening"))
            {
                _lastResponse = "";
                _service.StartListening(_listenPort, _singleResponse);
            }
            if (GUILayout.Button("Stop"))
            {
                _service.Stop();
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Last Response:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(_lastResponse, MessageType.None);
        }

        private void DrawSendTab()
        {
            EditorGUILayout.LabelField("Send UDP Broadcast", EditorStyles.boldLabel);
            _message = EditorGUILayout.TextField("Message", _message);
            _sendPort = EditorGUILayout.IntField("Port", _sendPort);
            _mode = (BroadcastSender.PeriodicMode)EditorGUILayout.EnumPopup("Mode", _mode);
            if (_mode != BroadcastSender.PeriodicMode.RetryOnFailure)
            {
                _intervalMs = EditorGUILayout.IntField("Interval (ms)", _intervalMs);
            }
            else
            {
                _intervalMs = EditorGUILayout.IntField("Retry Delay (ms)", _intervalMs);
                _maxRetries = EditorGUILayout.IntField("Max Retries", _maxRetries);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Start Sending"))
            {
                if (_mode == BroadcastSender.PeriodicMode.UntilResponse)
                {
                    // Ensure listener is subscribed to stop the sender on response
                    _service.OnResponseReceived -= OnResponseReceived; // avoid dup
                    _service.OnResponseReceived += OnResponseReceived;
                    _service.StartListening(_sendPort, singleResponse: false);
                    _sender.Start(_message, _sendPort, _mode, _intervalMs, listenService: _service);
                }
                else if (_mode == BroadcastSender.PeriodicMode.RetryOnFailure)
                {
                    _sender.Start(_message, _sendPort, _mode, _intervalMs, _maxRetries);
                }
                else
                {
                    _sender.Start(_message, _sendPort, _mode, _intervalMs);
                }
            }
            if (GUILayout.Button("Stop"))
            {
                _sender.Stop();
            }
            GUILayout.EndHorizontal();
        }

        private void OnResponseReceived(string payload, IPEndPoint ep)
        {
            _lastResponse = $"{ep} :: {payload}";
            Repaint();
        }
    }
}
