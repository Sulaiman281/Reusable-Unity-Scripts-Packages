namespace WitShells.WebRTCWit
{
    using UnityEngine;
    using Unity.WebRTC;
    using System.Collections;
    using WebSocketSharp;
    using System.Text;

    public class WebRtcSample : MonoBehaviour
    {
        public string channelName = "data";
        public string scope = "SampleScope";
        public string matchmakingCode = "";
        public string signalingUrl = "ws://127.0.0.1:8080";

        private WebSocket ws;
        private RTCPeerConnection _connection;
        private RTCDataChannel _dataChannel;

        public void Start()
        {
            ws = new WebSocket(signalingUrl);

            ws.OnOpen += (sender, e) =>
            {
                Debug.Log("WebSocket connected.");



            };


        }

        private IEnumerator CreateLocalPeer()
        {
            var config = Utils.CreateRtcConfig();
            _connection = new RTCPeerConnection(ref config);

            _connection.OnIceCandidate = candidate =>
            {
                // TODO: Send 'candidate' to the remote user via WebSocket/HTTP signaling
                Debug.Log($"Found ICE Candidate: {candidate.Candidate}");
            };

            // EVENT 2: Connection State Change
            _connection.OnIceConnectionChange = state =>
            {
                Debug.Log($"Connection State: {state}");
            };

            yield return null;
        }

        public IEnumerator StartConnection() // Call this to start
        {
            // 1. Create a Data Channel (Required to create an offer if no video/audio)
            _dataChannel = _connection.CreateDataChannel(channelName);
            Utils.RegisterDataChannelEvents(_dataChannel, OnChannelOpen, OnChannelClose, OnChannelMessage);

            // 2. Create the Offer
            var offerOp = _connection.CreateOffer();
            yield return offerOp; // Wait for it to finish

            if (offerOp.IsError) yield break;

            // 3. Set Local Description (Tell our local WebRTC about the offer)
            var desc = offerOp.Desc;
            var localDescOp = _connection.SetLocalDescription(ref desc);
            yield return localDescOp;

            // 4. SEND 'desc' TO OTHER USER (Signaling)
            // SendToSignalingServer(desc.sdp); 
            ws.Send(JsonUtility.ToJson(new
            {
                SessionType = desc.type.ToString(),
                Sdp = desc.sdp
            }));
        }


        private IEnumerator CreateOffer()
        {
            var offer = _connection.CreateOffer();
            yield return offer;

            var offerDesc = offer.Desc;
            var localDescOp = _connection.SetLocalDescription(ref offerDesc);
            yield return localDescOp;

            // send desc to server for receiver connection
            var offerSessionDesc = new
            {
                SessionType = offerDesc.type.ToString(),
                Sdp = offerDesc.sdp
            };
            ws.Send(JsonUtility.ToJson(offerSessionDesc));
        }


        // Call this when you receive the Offer from User A via Signaling
        public IEnumerator OnReceiveOffer(RTCSessionDescription remoteOffer)
        {
            // 1. Create your own peer connection first
            CreateLocalPeer();

            // 2. Set Remote Description (Tell our WebRTC about User A)
            var remoteDescOp = _connection.SetRemoteDescription(ref remoteOffer);
            yield return remoteDescOp;

            // 3. Create Answer
            var answerOp = _connection.CreateAnswer();
            yield return answerOp;

            // 4. Set Local Description (Tell our WebRTC about our Answer)
            var answerDesc = answerOp.Desc;
            var localDescOp = _connection.SetLocalDescription(ref answerDesc);
            yield return localDescOp;

            // 5. SEND 'answerDesc' BACK TO USER A (Signaling)
        }

        // Call this when User A receives the Answer from User B
        public IEnumerator OnReceiveAnswer(RTCSessionDescription remoteAnswer)
        {
            var remoteDescOp = _connection.SetRemoteDescription(ref remoteAnswer);
            yield return remoteDescOp;

            // Success! The connection is now establishing.
        }

        #region Data Channel Callbacks

        private void OnChannelOpen(RTCDataChannel channel)
        {
            Debug.Log("Data Channel Opened: " + channel.Label);
        }

        private void OnChannelClose(RTCDataChannel channel)
        {
            Debug.Log("Data Channel Closed: " + channel.Label);
        }

        private void OnChannelMessage(RTCDataChannel channel, byte[] bytes)
        {
            string message = Encoding.UTF8.GetString(bytes);
            Debug.Log("Data Channel Message Received: " + message);
        }

        #endregion
    }
}