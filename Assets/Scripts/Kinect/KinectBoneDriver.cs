// KinectBoneDriver.cs — Directly applies Kinect joint rotations to a Humanoid Animator's
// bone transforms.  No muscle-channel math, no WitPose dependency.
//
// The Kinect provides absolute joint orientations in sensor space.  This script
// converts them to Unity world space and applies them as bone.rotation on each frame.
// Use RotationOffset to align the sensor's coordinate frame with your scene.
//
// SETUP
// ──────
//   1. Attach to the same GameObject as your Humanoid Animator.
//   2. Assign the KinectReceiver in the Inspector.
//   3. Play — if the pose is rotated, adjust RotationOffset (typically Y = 180).

using UnityEngine;

namespace WitPose
{
    [AddComponentMenu("WitPose/Kinect Bone Driver")]
    public class KinectBoneDriver : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Source")]
        public KinectReceiver Receiver;

        [Header("Target")]
        public Animator TargetAnimator;

        [Header("Tuning")]
        [Tooltip("Slerp factor applied per frame (1 = instant, lower = smoother).")]
        [Range(0.01f, 1f)]
        public float Smoothing = 0.2f;

        [Tooltip("Euler offset applied to every joint rotation to align sensor space with scene.\n" +
                 "Start with Y = 180 if the character faces backwards.")]
        public Vector3 RotationOffset = Vector3.zero;

        [Tooltip("Move the Hips transform to match the Kinect SpineBase position.")]
        public bool DriveRootPosition = false;

        [Tooltip("Kinect metres to Unity units scale for root position.")]
        [Range(0.1f, 5f)]
        public float PositionScale = 1f;

        // ── Bone → Joint mapping ──────────────────────────────────────────────

        private static readonly (HumanBodyBones bone, string joint)[] k_Bindings =
        {
            (HumanBodyBones.Hips,          JointName.SpineBase    ),
            (HumanBodyBones.Spine,         JointName.SpineMid     ),
            (HumanBodyBones.Chest,         JointName.SpineShoulder),
            (HumanBodyBones.Neck,          JointName.Neck         ),
            (HumanBodyBones.Head,          JointName.Head         ),
            (HumanBodyBones.LeftUpperArm,  JointName.ShoulderLeft ),
            (HumanBodyBones.LeftLowerArm,  JointName.ElbowLeft    ),
            (HumanBodyBones.LeftHand,      JointName.WristLeft    ),
            (HumanBodyBones.RightUpperArm, JointName.ShoulderRight),
            (HumanBodyBones.RightLowerArm, JointName.ElbowRight   ),
            (HumanBodyBones.RightHand,     JointName.WristRight   ),
            (HumanBodyBones.LeftUpperLeg,  JointName.HipLeft      ),
            (HumanBodyBones.LeftLowerLeg,  JointName.KneeLeft     ),
            (HumanBodyBones.LeftFoot,      JointName.AnkleLeft    ),
            (HumanBodyBones.RightUpperLeg, JointName.HipRight     ),
            (HumanBodyBones.RightLowerLeg, JointName.KneeRight    ),
            (HumanBodyBones.RightFoot,     JointName.AnkleRight   ),
        };

        // ── Runtime state ─────────────────────────────────────────────────────

        private Transform[]  _boneTransforms;
        private string[]     _jointNames;
        private Quaternion[] _smoothed;

        private Transform    _hipsTransform;

        // ─────────────────────────────────────────────────────────────────────
        // LIFECYCLE
        // ─────────────────────────────────────────────────────────────────────

        private void Start()
        {
            if (TargetAnimator == null)
                TargetAnimator = GetComponent<Animator>();

            if (TargetAnimator == null)
            {
                Debug.LogError("[KinectBoneDriver] No Animator found.", this);
                enabled = false;
                return;
            }

            int count = k_Bindings.Length;
            _boneTransforms = new Transform[count];
            _jointNames     = new string[count];
            _smoothed       = new Quaternion[count];

            for (int i = 0; i < count; i++)
            {
                var (bone, joint) = k_Bindings[i];
                _boneTransforms[i] = TargetAnimator.GetBoneTransform(bone);
                _jointNames[i]     = joint;
                _smoothed[i]       = _boneTransforms[i] != null
                    ? _boneTransforms[i].rotation
                    : Quaternion.identity;
            }

            _hipsTransform = TargetAnimator.GetBoneTransform(HumanBodyBones.Hips);

            if (Receiver != null)
                Receiver.OnBodyFrame.AddListener(OnBodyFrame);
        }

        private void OnEnable()
        {
            // Re-subscribe on toggle (guard: Start hasn't run yet on first enable).
            if (Receiver != null && _smoothed != null)
                Receiver.OnBodyFrame.AddListener(OnBodyFrame);
        }

        private void OnDisable()
        {
            if (Receiver != null)
                Receiver.OnBodyFrame.RemoveListener(OnBodyFrame);
        }

        private void OnDestroy()
        {
            if (Receiver != null)
                Receiver.OnBodyFrame.RemoveListener(OnBodyFrame);
        }

        // ─────────────────────────────────────────────────────────────────────
        // FRAME CALLBACK
        // ─────────────────────────────────────────────────────────────────────

        private void OnBodyFrame(KinectBodyFrame frame)
        {
            var offsetRot = Quaternion.Euler(RotationOffset);
            float t = 1f - Mathf.Pow(1f - Smoothing, Time.deltaTime * 60f);

            for (int i = 0; i < _boneTransforms.Length; i++)
            {
                if (_boneTransforms[i] == null) continue;

                var joint = frame.GetJoint(_jointNames[i]);
                if (joint == null || joint.TrackingState == TrackingState.NotTracked) continue;

                // Convert Kinect sensor-space rotation to Unity world space,
                // then apply the scene-alignment offset.
                var target = offsetRot * KinectConvert.ToRotation(joint.Rotation);

                _smoothed[i] = Quaternion.Slerp(_smoothed[i], target, t);
                _boneTransforms[i].rotation = _smoothed[i];
            }

            if (DriveRootPosition && _hipsTransform != null)
            {
                var hip = frame.GetJoint(JointName.SpineBase);
                if (hip != null && hip.TrackingState != TrackingState.NotTracked)
                    _hipsTransform.position = KinectConvert.ToPosition(hip.Position) * PositionScale;
            }
        }
    }
}
