// KinectData.cs — Data model classes for the WitPose Kinect WebSocket stream.
// Matches the JSON payload produced by kinect_stream.py exactly.
//
// Requires: Newtonsoft.Json for Unity
//   Package Manager → Add package by name: com.unity.nuget.newtonsoft-json

using System.Collections.Generic;
using Newtonsoft.Json;

namespace WitPose
{
    // ─────────────────────────────────────────────────────────────────
    // ENUMS
    // ─────────────────────────────────────────────────────────────────

    public enum TrackingState
    {
        NotTracked = 0,
        Inferred   = 1,
        Tracked    = 2,
    }

    public enum HandState
    {
        Unknown    = 0,
        NotTracked = 1,
        Open       = 2,
        Closed     = 3,
        Lasso      = 4,
    }

    // ─────────────────────────────────────────────────────────────────
    // JOINT NAMES — matches Python JOINT_NAMES list
    // ─────────────────────────────────────────────────────────────────

    public static class JointName
    {
        public const string SpineBase     = "SpineBase";
        public const string SpineMid      = "SpineMid";
        public const string Neck          = "Neck";
        public const string Head          = "Head";
        public const string ShoulderLeft  = "ShoulderLeft";
        public const string ElbowLeft     = "ElbowLeft";
        public const string WristLeft     = "WristLeft";
        public const string HandLeft      = "HandLeft";
        public const string ShoulderRight = "ShoulderRight";
        public const string ElbowRight    = "ElbowRight";
        public const string WristRight    = "WristRight";
        public const string HandRight     = "HandRight";
        public const string HipLeft       = "HipLeft";
        public const string KneeLeft      = "KneeLeft";
        public const string AnkleLeft     = "AnkleLeft";
        public const string FootLeft      = "FootLeft";
        public const string HipRight      = "HipRight";
        public const string KneeRight     = "KneeRight";
        public const string AnkleRight    = "AnkleRight";
        public const string FootRight     = "FootRight";
        public const string SpineShoulder = "SpineShoulder";
        public const string HandTipLeft   = "HandTipLeft";
        public const string ThumbLeft     = "ThumbLeft";
        public const string HandTipRight  = "HandTipRight";
        public const string ThumbRight    = "ThumbRight";

        public static readonly string[] All = new[]
        {
            SpineBase, SpineMid, Neck, Head,
            ShoulderLeft, ElbowLeft, WristLeft, HandLeft,
            ShoulderRight, ElbowRight, WristRight, HandRight,
            HipLeft, KneeLeft, AnkleLeft, FootLeft,
            HipRight, KneeRight, AnkleRight, FootRight,
            SpineShoulder, HandTipLeft, ThumbLeft, HandTipRight, ThumbRight,
        };
    }

    // ─────────────────────────────────────────────────────────────────
    // RAW JSON MODELS  (matched 1-to-1 to Python JSON keys)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>Raw 3D position from the Kinect (metres, camera space).</summary>
    public class KinectPosition
    {
        [JsonProperty("x")] public float X;
        [JsonProperty("y")] public float Y;
        [JsonProperty("z")] public float Z;
    }

    /// <summary>Raw quaternion rotation from the Kinect.</summary>
    public class KinectRotation
    {
        [JsonProperty("x")] public float X;
        [JsonProperty("y")] public float Y;
        [JsonProperty("z")] public float Z;
        [JsonProperty("w")] public float W;
    }

    /// <summary>Full data for one skeleton joint.</summary>
    public class KinectJoint
    {
        [JsonProperty("position")]      public KinectPosition Position;
        [JsonProperty("rotation")]      public KinectRotation Rotation;
        [JsonProperty("tracking_state")] public int TrackingStateRaw;
        [JsonProperty("tracked")]       public bool Tracked;
        [JsonProperty("inferred")]      public bool Inferred;

        /// <summary>Strongly-typed tracking state.</summary>
        [JsonIgnore]
        public TrackingState TrackingState => (TrackingState)TrackingStateRaw;
    }

    /// <summary>Hand state data for one hand.</summary>
    public class KinectHandData
    {
        [JsonProperty("state")]      public string State;
        [JsonProperty("state_id")]   public int    StateId;
        [JsonProperty("confidence")] public int    Confidence;

        /// <summary>Strongly-typed hand state.</summary>
        [JsonIgnore]
        public HandState HandState => (HandState)StateId;

        /// <summary>True if Kinect is confident about this reading.</summary>
        [JsonIgnore]
        public bool IsConfident => Confidence == 1;
    }

    /// <summary>Both hands data block from a body frame.</summary>
    public class KinectHands
    {
        [JsonProperty("left")]  public KinectHandData Left;
        [JsonProperty("right")] public KinectHandData Right;
    }

    // ─────────────────────────────────────────────────────────────────
    // FRAME TYPES
    // ─────────────────────────────────────────────────────────────────

    /// <summary>A complete body tracking frame from kinect_stream.py.</summary>
    public class KinectBodyFrame
    {
        [JsonProperty("type")]        public string Type;
        [JsonProperty("timestamp")]   public double Timestamp;
        [JsonProperty("frame_index")] public int    FrameIndex;
        [JsonProperty("tracked")]     public bool   Tracked;

        /// <summary>25 joints keyed by name (see JointName constants).</summary>
        [JsonProperty("joints")]      public Dictionary<string, KinectJoint> Joints;
        [JsonProperty("hands")]       public KinectHands Hands;

        /// <summary>Convenience accessor — returns null if joint is missing.</summary>
        public KinectJoint GetJoint(string name)
        {
            if (Joints == null) return null;
            Joints.TryGetValue(name, out var j);
            return j;
        }
    }

    /// <summary>Heartbeat packet sent when no body is tracked.</summary>
    public class KinectHeartbeat
    {
        [JsonProperty("type")]      public string Type;
        [JsonProperty("tracked")]   public bool   Tracked;
        [JsonProperty("timestamp")] public double Timestamp;
    }

    // ─────────────────────────────────────────────────────────────────
    // UNITY-SPACE HELPERS
    // ─────────────────────────────────────────────────────────────────

    /// <summary>Utility to convert Kinect camera-space data to Unity world-space.</summary>
    public static class KinectConvert
    {
        /// <summary>
        /// Convert a Kinect camera-space position to Unity space.
        /// Kinect: right-handed (X=right, Y=up, Z=away from sensor).
        /// Unity:  left-handed  (X=right, Y=up, Z=forward).
        /// Negate X to mirror the skeleton correctly.
        /// </summary>
        public static UnityEngine.Vector3 ToPosition(KinectPosition p)
            => new UnityEngine.Vector3(-p.X, p.Y, p.Z);

        /// <summary>
        /// Convert a Kinect quaternion to Unity quaternion.
        /// Right-hand → left-hand: negate X and Z components.
        /// </summary>
        public static UnityEngine.Quaternion ToRotation(KinectRotation r)
            => new UnityEngine.Quaternion(-r.X, r.Y, -r.Z, r.W);
    }
}
