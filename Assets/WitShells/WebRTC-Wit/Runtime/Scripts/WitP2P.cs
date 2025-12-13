namespace WitShells.WebRTCWit
{
    using System;
    using System.Collections.Generic;
    using System.Collections;
    using UnityEngine;
    using Unity.WebRTC;

    public class WitP2P : MonoBehaviour
    {
        [Header("P2P Settings")]
        public string scope = "SampleScope";
        public string matchmakingCode = "";
        public string channelName = "data";
        public string signalingUrl = "ws://127.0.0.1:8080";

        private RTCPeerConnection pc;
        private RTCDataChannel dataChannel;
        private WebSocketSignalingClient signaling;
        private bool signalingReady;
        private bool isHost;

        // SDP/ICE state
        private bool remoteDescriptionSet;
        private readonly List<RTCIceCandidateInit> pendingRemoteIce = new List<RTCIceCandidateInit>();

        // Single signaling handler guard
        private bool subscribed;

        // Stats
        private int sentCount;
        private int recvCount;

        // --- Public API ---
        [ContextMenu("Host")]
        public void Host()
        {
            isHost = true;
            if (string.IsNullOrEmpty(matchmakingCode)) matchmakingCode = Utils.GenerateCode(6);
            SetupSignalingOnce();
            StartCoroutine(StartHostFlow());
        }

        [ContextMenu("Join")]
        public void Join()
        {
            isHost = false;
            if (string.IsNullOrEmpty(matchmakingCode))
            {
                Debug.LogError("Set matchmakingCode before Join().");
                return;
            }
            SetupSignalingOnce();
            StartCoroutine(StartJoinFlow());
        }

        [ContextMenu("Send Ping")]
        public void SendPing()
        {
            if (dataChannel != null && dataChannel.ReadyState == RTCDataChannelState.Open)
            {
                var msg = System.Text.Encoding.UTF8.GetBytes(isHost ? "ping-from-host" : "ping-from-join");
                dataChannel.Send(msg);
                sentCount++;
            }
            else
            {
                Debug.LogWarning("Data channel not open.");
            }
        }

        // --- Setup ---
        private void SetupSignalingOnce()
        {
            if (signaling == null)
            {
                signaling = new WebSocketSignalingClient();
                signaling.OnMessage += OnSignalMessage;
                signaling.OnError += err => Debug.LogError($"[Signaling] {err}");
                _ = signaling.ConnectAsync(signalingUrl).ContinueWith(t => signalingReady = signaling.IsConnected);
            }
            else if (!subscribed)
            {
                signaling.OnMessage += OnSignalMessage;
            }
            subscribed = true;
        }

        private IEnumerator EnsureSignalingConnected(float timeoutSeconds)
        {
            var deadline = Time.time + timeoutSeconds;
            while (!signalingReady && Time.time < deadline) yield return null;
            if (!signalingReady) Debug.LogError($"Signaling not connected at {signalingUrl}.");
        }

        private RTCPeerConnection CreatePeer()
        {
            var config = Utils.CreateRtcConfig(new[] { "stun:stun.l.google.com:19302" });
            var peer = new RTCPeerConnection(ref config);

            peer.OnIceCandidate = cand =>
            {
                var msg = Utils.CreateIceSignal(cand.Candidate, cand.SdpMid, cand.SdpMLineIndex ?? -1, fromId: isHost ? "host" : "join");
                var wire = Utils.BuildWireSignalWithCode(msg, scope, matchmakingCode);
                SendSignal(wire);
            };

            peer.OnDataChannel = ch =>
            {
                if (!isHost)
                {
                    dataChannel = ch;
                    RegisterDataChannelEvents(ch);
                }
            };

            return peer;
        }

        private void RegisterDataChannelEvents(RTCDataChannel ch)
        {
            Utils.RegisterDataChannelEvents(ch,
                onOpen: d => Debug.Log("[P2P] Channel opened: " + d.Label),
                onClose: d => Debug.Log("[P2P] Channel closed: " + d.Label),
                onMessage: (d, bytes) => { recvCount++; Debug.Log("[P2P] Msg: " + System.Text.Encoding.UTF8.GetString(bytes)); }
            );
        }

        // --- Host Flow ---
        private IEnumerator StartHostFlow()
        {
            yield return EnsureSignalingConnected(5f);

            pc = CreatePeer();

            // Host creates stable data channel
            dataChannel = pc.CreateDataChannel(channelName);
            RegisterDataChannelEvents(dataChannel);

            // Offer: create -> set local -> send
            var offerOp = pc.CreateOffer();
            yield return offerOp;
            var offer = offerOp.Desc;
            yield return pc.SetLocalDescription(ref offer);

            var offerMsg = Utils.CreateSdpSignal(offer.sdp, "offer", fromId: "host");
            var wireOffer = Utils.BuildWireSignalWithCode(offerMsg, scope, matchmakingCode);
            SendSignal(wireOffer);

            Debug.Log($"[Host] Waiting for answer (code {matchmakingCode})...");
        }

        // --- Join Flow ---
        private IEnumerator StartJoinFlow()
        {
            yield return EnsureSignalingConnected(5f);
            pc = CreatePeer();
            Debug.Log($"[Join] Waiting for offer (code {matchmakingCode})...");
        }

        // --- Signaling ---
        private void SendSignal(string wire)
        {
            if (signalingReady)
                _ = signaling.SendAsync(wire);
            else
                Debug.LogWarning("Signaling not ready");
        }

        private void OnSignalMessage(string wire)
        {
            if (!Utils.TryParseWireSignalWithCode(wire, out var s, out var code, out var sig)) return;
            if (s != scope || code != matchmakingCode) return;

            if (sig.type == "offer" && !isHost)
            {
                // Join: set remote, create answer, set local, send answer
                var remote = new RTCSessionDescription { type = RTCSdpType.Offer, sdp = sig.sdp };
                StartCoroutine(HandleSetRemoteThenAnswer(remote));
            }
            else if (sig.type == "answer" && isHost)
            {
                // Host: set remote
                var remote = new RTCSessionDescription { type = RTCSdpType.Answer, sdp = sig.sdp };
                StartCoroutine(HandleSetRemote(remote));
            }
            else if (sig.type == "candidate")
            {
                var init = new RTCIceCandidateInit { candidate = sig.candidate, sdpMid = sig.sdpMid, sdpMLineIndex = sig.sdpMLineIndex };
                if (remoteDescriptionSet && pc != null)
                {
                    pc.AddIceCandidate(new RTCIceCandidate(init));
                }
                else
                {
                    pendingRemoteIce.Add(init);
                }
            }
        }

        private IEnumerator HandleSetRemote(RTCSessionDescription remote)
        {
            yield return Utils.SetRemoteDescriptionCoroutine(pc, remote, Debug.LogError);
            remoteDescriptionSet = true;
            FlushPendingIce();
        }

        private IEnumerator HandleSetRemoteThenAnswer(RTCSessionDescription remote)
        {
            yield return Utils.SetRemoteDescriptionCoroutine(pc, remote, Debug.LogError);
            remoteDescriptionSet = true;
            FlushPendingIce();

            var ansOp = pc.CreateAnswer();
            yield return ansOp;
            var ans = ansOp.Desc;
            yield return pc.SetLocalDescription(ref ans);

            var ansMsg = Utils.CreateSdpSignal(ans.sdp, "answer", fromId: "join");
            var wireAns = Utils.BuildWireSignalWithCode(ansMsg, scope, matchmakingCode);
            SendSignal(wireAns);
        }

        private void FlushPendingIce()
        {
            if (pc == null) return;
            for (int i = 0; i < pendingRemoteIce.Count; i++)
            {
                pc.AddIceCandidate(new RTCIceCandidate(pendingRemoteIce[i]));
            }
            pendingRemoteIce.Clear();
        }

        private void OnDestroy()
        {
            try { dataChannel?.Close(); } catch { }
            try { pc?.Close(); } catch { }
            dataChannel = null;
            pc = null;
        }
    }
}
