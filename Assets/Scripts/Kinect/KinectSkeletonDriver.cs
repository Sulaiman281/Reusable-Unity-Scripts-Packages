// KinectSkeletonDriver.cs — Example: drive 25 Unity Transforms from Kinect joint data.
//
// Attach to any GameObject.
// Assign the KinectReceiver reference and populate the JointTransforms array
// in the Inspector (25 entries, one per joint in JointName.All order).

using UnityEngine;

namespace WitPose
{
    public class KinectSkeletonDriver : MonoBehaviour
    {
        [Header("Source")]
        public KinectReceiver Receiver;

        [Header("Scale")]
        [Tooltip("Multiplier applied to joint positions (1 = metres)")]
        public float PositionScale = 1f;

        [Header("Joint Transforms")]
        [Tooltip("25 transforms in the same order as JointName.All:\n" +
                 "SpineBase, SpineMid, Neck, Head, ShoulderLeft, ElbowLeft,\n" +
                 "WristLeft, HandLeft, ShoulderRight, ElbowRight, WristRight,\n" +
                 "HandRight, HipLeft, KneeLeft, AnkleLeft, FootLeft, HipRight,\n" +
                 "KneeRight, AnkleRight, FootRight, SpineShoulder, HandTipLeft,\n" +
                 "ThumbLeft, HandTipRight, ThumbRight")]
        public Transform[] JointTransforms = new Transform[25];

        // ─────────────────────────────────────────────────────────────
        // POLLING UPDATE  (alternative to event subscription)
        // ─────────────────────────────────────────────────────────────

        private void Update()
        {
            if (Receiver == null || !Receiver.IsTracked || Receiver.LatestFrame == null)
                return;

            ApplyFrame(Receiver.LatestFrame);
        }

        // ─────────────────────────────────────────────────────────────
        // APPLY
        // ─────────────────────────────────────────────────────────────

        private void ApplyFrame(KinectBodyFrame frame)
        {
            for (int i = 0; i < JointName.All.Length; i++)
            {
                if (i >= JointTransforms.Length || JointTransforms[i] == null)
                    continue;

                var joint = frame.GetJoint(JointName.All[i]);
                if (joint == null) continue;

                // Skip NotTracked joints — leave transform where it is
                if (joint.TrackingState == TrackingState.NotTracked)
                    continue;

                JointTransforms[i].localPosition =
                    KinectConvert.ToPosition(joint.Position) * PositionScale;

                JointTransforms[i].localRotation =
                    KinectConvert.ToRotation(joint.Rotation);
            }
        }
    }
}
