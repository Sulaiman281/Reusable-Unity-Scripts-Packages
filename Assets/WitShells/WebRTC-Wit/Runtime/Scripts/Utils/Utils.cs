using Unity.WebRTC;
using UnityEngine.Events;

namespace WitShells.WebRTCWit
{
    public static class Utils
    {
        /// <summary>
        /// Generate a short alphanumeric matchmaking code (e.g., 6 chars).
        /// </summary>
        public static string GenerateCode(int length = 6)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // avoid confusing chars
            var rnd = new System.Random(UnityEngine.Random.Range(int.MinValue, int.MaxValue));
            var buffer = new char[length];
            for (int i = 0; i < length; i++) buffer[i] = chars[rnd.Next(chars.Length)];
            return new string(buffer);
        }

        /// <summary>
        /// Attach a matchmaking code to a message (stored in sessionId).
        /// </summary>
        public static void AttachCode(SignalMessage msg, string code)
        {
            msg.sessionId = code;
        }

        /// <summary>
        /// Check if a message matches the provided code.
        /// </summary>
        public static bool MatchesCode(SignalMessage msg, string code)
        {
            return !string.IsNullOrEmpty(code) && msg != null && string.Equals(msg.sessionId, code, System.StringComparison.Ordinal);
        }

        /// <summary>
        /// Build a wire signal including scope and code for easy routing.
        /// Wire: WIT_RTC_SIGNAL|{scope}|{code}|{json}
        /// </summary>
        public static string BuildWireSignalWithCode(SignalMessage msg, string scope, string code)
        {
            AttachCode(msg, code);
            var json = SerializeSignal(msg);
            return $"WIT_RTC_SIGNAL|{scope}|{code}|{json}";
        }

        /// <summary>
        /// Try parse wire signal with scope and code.
        /// </summary>
        public static bool TryParseWireSignalWithCode(string wire, out string scope, out string code, out SignalMessage msg)
        {
            scope = null; code = null; msg = null;
            if (string.IsNullOrEmpty(wire)) return false;
            const string prefix = "WIT_RTC_SIGNAL|";
            if (!wire.StartsWith(prefix, System.StringComparison.Ordinal)) return false;
            var idx1 = wire.IndexOf('|');
            var idx2 = wire.IndexOf('|', idx1 + 1);
            var idx3 = wire.IndexOf('|', idx2 + 1);
            if (idx1 < 0 || idx2 < 0 || idx3 < 0) return false;
            scope = wire.Substring(idx1 + 1, idx2 - idx1 - 1);
            code = wire.Substring(idx2 + 1, idx3 - idx2 - 1);
            var json = wire.Substring(idx3 + 1);
            msg = DeserializeSignal(json);
            return msg != null && MatchesCode(msg, code);
        }
        /// <summary>
        /// Serialize a SignalMessage to JSON.
        /// </summary>
        public static string SerializeSignal(SignalMessage msg)
        {
            return UnityEngine.JsonUtility.ToJson(msg);
        }

        /// <summary>
        /// Deserialize a SignalMessage from JSON.
        /// Returns null if parse fails.
        /// </summary>
        public static SignalMessage DeserializeSignal(string json)
        {
            try
            {
                return UnityEngine.JsonUtility.FromJson<SignalMessage>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Build the wire format for signaling used by this project: "WIT_RTC_SIGNAL|{scope}|{json}".
        /// </summary>
        public static string BuildWireSignal(SignalMessage msg, string scope)
        {
            var json = SerializeSignal(msg);
            return $"WIT_RTC_SIGNAL|{scope}|{json}";
        }

        /// <summary>
        /// Try to parse the wire format built with BuildWireSignal.
        /// </summary>
        public static bool TryParseWireSignal(string wire, out string scope, out SignalMessage msg)
        {
            scope = null;
            msg = null;
            if (string.IsNullOrEmpty(wire)) return false;
            const string prefix = "WIT_RTC_SIGNAL|";
            if (!wire.StartsWith(prefix, System.StringComparison.Ordinal)) return false;
            var idx1 = wire.IndexOf('|');
            var idx2 = wire.IndexOf('|', idx1 + 1);
            if (idx2 <= idx1) return false;
            scope = wire.Substring(idx1 + 1, idx2 - idx1 - 1);
            var json = wire.Substring(idx2 + 1);
            msg = DeserializeSignal(json);
            return msg != null;
        }

        /// <summary>
        /// Create an RTCConfiguration with optional STUN and TURN servers.
        /// Example: new[] { "stun:stun.l.google.com:19302" }
        /// </summary>
        public static Unity.WebRTC.RTCConfiguration CreateRtcConfig(string[] stunUrls = null,
            string[] turnUrls = null, string turnUsername = null, string turnCredential = null)
        {
            const string defaultStun = "stun:stun.l.google.com:19302";
            if (stunUrls == null || stunUrls.Length == 0)
            {
                stunUrls = new[] { defaultStun };
            }

            var servers = new System.Collections.Generic.List<Unity.WebRTC.RTCIceServer>();
            if (stunUrls != null && stunUrls.Length > 0)
            {
                servers.Add(new Unity.WebRTC.RTCIceServer { urls = stunUrls });
            }
            if (turnUrls != null && turnUrls.Length > 0)
            {
                servers.Add(new Unity.WebRTC.RTCIceServer { urls = turnUrls, username = turnUsername, credential = turnCredential });
            }

            return new Unity.WebRTC.RTCConfiguration { iceServers = servers.ToArray() };
        }

        /// <summary>
        /// Create a DataChannel init object for reliable/unreliable channels.
        /// </summary>
        public static Unity.WebRTC.RTCDataChannelInit CreateDataChannelInit(bool reliable)
        {
            var init = new Unity.WebRTC.RTCDataChannelInit();
            init.ordered = reliable;
            if (reliable)
            {
                init.maxRetransmits = null;
            }
            else
            {
                init.maxRetransmits = 0; // unreliable
            }
            return init;
        }

        /// <summary>
        /// Coroutine helper to set local description on a peer connection and yield until complete.
        /// On error, optional onError is invoked with message.
        /// </summary>
        public static System.Collections.IEnumerator SetLocalDescriptionCoroutine(Unity.WebRTC.RTCPeerConnection pc, Unity.WebRTC.RTCSessionDescription desc, System.Action<string> onError = null)
        {
            var d = desc;
            var op = pc.SetLocalDescription(ref d);
            yield return op;
            if (op.IsError)
            {
                onError?.Invoke(op.Error.message);
            }
        }

        /// <summary>
        /// Coroutine helper to set remote description on a peer connection and yield until complete.
        /// </summary>
        public static System.Collections.IEnumerator SetRemoteDescriptionCoroutine(Unity.WebRTC.RTCPeerConnection pc, Unity.WebRTC.RTCSessionDescription desc, System.Action<string> onError = null)
        {
            var d = desc;
            var op = pc.SetRemoteDescription(ref d);
            yield return op;
            if (op.IsError)
            {
                onError?.Invoke(op.Error.message);
            }
        }

        /// <summary>
        /// Create a SignalMessage for an SDP offer/answer.
        /// </summary>
        public static SignalMessage CreateSdpSignal(string sdp, string type, string fromId = null, string toId = null)
        {
            return new SignalMessage { type = type, sdp = sdp, fromId = fromId, toId = toId };
        }

        /// <summary>
        /// Create a SignalMessage for an ICE candidate.
        /// </summary>
        public static SignalMessage CreateIceSignal(string candidate, string sdpMid, int sdpMLineIndex, string fromId = null, string toId = null)
        {
            return new SignalMessage { type = "candidate", candidate = candidate, sdpMid = sdpMid, sdpMLineIndex = sdpMLineIndex, fromId = fromId, toId = toId };
        }

        public static void RegisterDataChannelEvents(RTCDataChannel channel,
            UnityAction<Unity.WebRTC.RTCDataChannel> onOpen = null,
            UnityAction<Unity.WebRTC.RTCDataChannel> onClose = null,
            UnityAction<Unity.WebRTC.RTCDataChannel, byte[]> onMessage = null)
        {
            if (onOpen != null)
            {
                channel.OnOpen += () => onOpen.Invoke(channel);
            }
            if (onClose != null)
            {
                channel.OnClose += () => onClose.Invoke(channel);
            }
            if (onMessage != null)
            {
                channel.OnMessage += bytes => onMessage.Invoke(channel, bytes);
            }
        }
    }
}