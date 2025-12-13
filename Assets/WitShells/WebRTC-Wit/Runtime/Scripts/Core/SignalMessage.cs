using System;

namespace WitShells.WebRTCWit
{
    [Serializable]
    public class SignalMessage
    {
        public string scope;
        public string type; // "offer", "answer", "candidate"
        public string sdp;
        public string candidate;
        public string sdpMid;
        public int sdpMLineIndex = -1;
        public string sessionId;
        public string fromId;
        public string toId;
    }
}
