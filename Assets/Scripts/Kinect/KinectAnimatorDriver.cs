// KinectAnimatorDriver.cs — Drives a Unity Humanoid Animator from live Kinect data.
//
// Strategy
// ─────────
// The Kinect provides absolute joint positions (and raw rotations) in camera space.
// We compute bone-segment direction vectors, convert them to Unity space, then
// project the resulting local Euler angles onto the Humanoid muscle channels that
// match each body part.  Every muscle value is clamped through HumanMuscleJointLimits
// so the pose stays anatomically plausible.
//
// SETUP
// ──────
//   1. Attach this component to the same GameObject as (or a sibling of) your Animator.
//   2. Assign the KinectReceiver and the Humanoid Animator in the Inspector.
//   3. Optionally tweak the smoothing / position-scale sliders.
//
// REQUIREMENTS
//   • Animator must use a Humanoid avatar.
//   • com.unity.nuget.newtonsoft-json package installed.

using UnityEngine;
using WitShells.WitPose;

namespace WitPose
{
    [RequireComponent(typeof(Animator))]
    [AddComponentMenu("WitPose/Kinect Animator Driver")]
    public class KinectAnimatorDriver : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Source")]
        [Tooltip("KinectReceiver supplying live body frames.")]
        public KinectReceiver Receiver;

        [Header("Target")]
        [Tooltip("Humanoid Animator to drive.  Must be on this GameObject.")]
        public Animator TargetAnimator;

        [Header("Tuning")]
        [Tooltip("Lerp factor per second for muscle smoothing (1 = no smoothing, 0.1 = very smooth).")]
        [Range(0.01f, 1f)]
        public float Smoothing = 0.25f;

        [Tooltip("Metres per Unity unit for root position (1 = native Kinect metres).")]
        [Range(0.1f, 5f)]
        public float PositionScale = 1f;

        [Tooltip("When enabled, the root body position is applied to the avatar's root.")]
        public bool DriveRootPosition = false;

        [Header("Compatibility")]
        [Tooltip("Clears the Animator's RuntimeAnimatorController on Start so it cannot override " +
                 "the driven pose. Re-enable to blend with an existing Animator state machine.")]
        public bool DisableAnimatorController = true;

        // ── Runtime state ─────────────────────────────────────────────────────

        private HumanPoseHandler _poseHandler;
        private HumanPose        _pose;

        // Smoothed muscle array (same length as HumanTrait.MuscleCount = 95).
        private float[] _smoothedMuscles;

        // ─────────────────────────────────────────────────────────────────────
        // LIFECYCLE
        // ─────────────────────────────────────────────────────────────────────

        private void Start()
        {
            if (TargetAnimator == null)
                TargetAnimator = GetComponent<Animator>();

            if (TargetAnimator == null || TargetAnimator.avatar == null || !TargetAnimator.avatar.isHuman)
            {
                Debug.LogError("[KinectAnimatorDriver] TargetAnimator must reference a Humanoid avatar.", this);
                enabled = false;
                return;
            }

            _poseHandler = new HumanPoseHandler(TargetAnimator.avatar, TargetAnimator.transform);
            _pose        = new HumanPose();

            // Seed the smoothed buffer from the current pose.
            _poseHandler.GetHumanPose(ref _pose);

            // Ensure muscles array is correctly sized before we write to it each frame.
            if (_pose.muscles == null || _pose.muscles.Length < HumanMuscleJointLimits.MuscleCount)
                _pose.muscles = new float[HumanMuscleJointLimits.MuscleCount];

            _smoothedMuscles = new float[_pose.muscles.Length];
            System.Array.Copy(_pose.muscles, _smoothedMuscles, _smoothedMuscles.Length);

            // Neutralise root pose — let the GameObject transform own position/rotation.
            _pose.bodyPosition = Vector3.zero;
            _pose.bodyRotation = Quaternion.identity;

            // Clear the animator controller so it cannot override SetHumanPose each frame.
            // HumanPoseHandler only needs the avatar reference, not an active controller.
            if (DisableAnimatorController)
                TargetAnimator.runtimeAnimatorController = null;

            if (Receiver != null)
                Receiver.OnBodyFrame.AddListener(OnBodyFrame);
        }

        private void OnEnable()
        {
            // Re-subscribe if the component is toggled at runtime after Start.
            if (Receiver != null && _poseHandler != null)
                Receiver.OnBodyFrame.AddListener(OnBodyFrame);
        }

        private void OnDisable()
        {
            if (Receiver != null)
                Receiver.OnBodyFrame.RemoveListener(OnBodyFrame);
        }

        private void OnDestroy()
        {
            _poseHandler?.Dispose();
        }

        // ─────────────────────────────────────────────────────────────────────
        // FRAME CALLBACK  (fired by KinectReceiver on the main thread)
        // ─────────────────────────────────────────────────────────────────────

        private void OnBodyFrame(KinectBodyFrame frame)
        {
            if (_poseHandler == null) return;

            // 1. Compute target muscles from the received frame.
            var target = ComputeMuscles(frame);

            // 2. Smooth toward target.
            float t = 1f - Mathf.Pow(1f - Smoothing, Time.deltaTime * 60f);
            for (int i = 0; i < _smoothedMuscles.Length; i++)
                _smoothedMuscles[i] = Mathf.Lerp(_smoothedMuscles[i], target[i], t);

            // 3. Write muscles into the reused pose struct.
            //    We do NOT call GetHumanPose here — reading back from the Animator each frame
            //    would re-introduce whatever the (now-cleared) controller last wrote.
            int count = Mathf.Min(_smoothedMuscles.Length, _pose.muscles.Length);
            for (int i = 0; i < count; i++)
                _pose.muscles[i] = _smoothedMuscles[i];

            if (DriveRootPosition)
            {
                var hip = Receiver.LatestFrame.GetJoint(JointName.SpineBase);
                if (hip != null && hip.TrackingState != TrackingState.NotTracked)
                    _pose.bodyPosition = KinectConvert.ToPosition(hip.Position) * PositionScale;
            }

            _poseHandler.SetHumanPose(ref _pose);
        }

        // ─────────────────────────────────────────────────────────────────────
        // MUSCLE COMPUTATION
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Build a full 95-element muscle array from the Kinect body frame.
        /// Any muscle we cannot derive is left at its current smoothed value.
        /// </summary>
        private float[] ComputeMuscles(KinectBodyFrame frame)
        {
            // Start from the current smoothed state so undriven muscles stay put.
            float[] m = new float[HumanMuscleJointLimits.MuscleCount];
            for (int i = 0; i < m.Length; i++)
                m[i] = _smoothedMuscles[i];

            // ── Helpers ──────────────────────────────────────────────────────
            Vector3 Get(string name)
            {
                var j = frame.GetJoint(name);
                return (j != null && j.TrackingState != TrackingState.NotTracked)
                    ? KinectConvert.ToPosition(j.Position)
                    : Vector3.zero;
            }

            bool Valid(string name)
            {
                var j = frame.GetJoint(name);
                return j != null && j.TrackingState != TrackingState.NotTracked;
            }

            // ── Body landmarks ───────────────────────────────────────────────
            var spineBase     = Get(JointName.SpineBase);
            var spineMid      = Get(JointName.SpineMid);
            var spineShoulder = Get(JointName.SpineShoulder);
            var neck          = Get(JointName.Neck);
            var head          = Get(JointName.Head);

            var shoulderL = Get(JointName.ShoulderLeft);
            var elbowL    = Get(JointName.ElbowLeft);
            var wristL    = Get(JointName.WristLeft);
            var handL     = Get(JointName.HandLeft);

            var shoulderR = Get(JointName.ShoulderRight);
            var elbowR    = Get(JointName.ElbowRight);
            var wristR    = Get(JointName.WristRight);
            var handR     = Get(JointName.HandRight);

            var hipL   = Get(JointName.HipLeft);
            var kneeL  = Get(JointName.KneeLeft);
            var ankleL = Get(JointName.AnkleLeft);
            var footL  = Get(JointName.FootLeft);

            var hipR   = Get(JointName.HipRight);
            var kneeR  = Get(JointName.KneeRight);
            var ankleR = Get(JointName.AnkleRight);
            var footR  = Get(JointName.FootRight);

            // Build a reference frame from hip/shoulder line.
            var hips      = (hipL + hipR) * 0.5f;
            var shoulders = (shoulderL + shoulderR) * 0.5f;
            var spineDir  = (spineShoulder - spineBase).normalized;
            if (spineDir == Vector3.zero) spineDir = Vector3.up;

            // Right vector: points from left hip to right hip.
            var rightDir = (hipR - hipL).normalized;
            if (rightDir == Vector3.zero) rightDir = Vector3.right;

            // ── SPINE  (0-2) ─────────────────────────────────────────────────
            if (Valid(JointName.SpineBase) && Valid(JointName.SpineShoulder))
            {
                // Front-back flex: dot of spine dir with up (pitched < 0 = forward lean).
                float spinePitch = Vector3.Dot(spineDir, Vector3.forward); // -1..1 forward lean
                float spineLat   = Vector3.Dot(spineDir, rightDir);        // lateral tilt
                float spineRot   = EstimateAxialRotation(rightDir, Vector3.right);

                m[0]  = ApplyLimit(0,  -spinePitch);
                m[1]  = ApplyLimit(1,  spineLat);
                m[2]  = ApplyLimit(2,  spineRot);
                // Distribute symmetrically across Chest (3-5) and Upper Chest (6-8).
                m[3]  = ApplyLimit(3,  m[0] * 0.4f);
                m[4]  = ApplyLimit(4,  m[1] * 0.4f);
                m[5]  = ApplyLimit(5,  m[2] * 0.4f);
                m[6]  = ApplyLimit(6,  m[0] * 0.2f);
                m[7]  = ApplyLimit(7,  m[1] * 0.2f);
                m[8]  = ApplyLimit(8,  m[2] * 0.2f);
            }

            // ── NECK  (9-11) ─────────────────────────────────────────────────
            if (Valid(JointName.SpineShoulder) && Valid(JointName.Neck))
            {
                var neckDir = (neck - spineShoulder).normalized;
                float neckPitch  = Vector3.Dot(neckDir, Vector3.forward);
                float neckLat    = Vector3.Dot(neckDir, rightDir);
                float neckTwist  = EstimateAxialRotation(rightDir * -1f + Vector3.right, rightDir);

                m[9]  = ApplyLimit(9,  -neckPitch * 0.5f);
                m[10] = ApplyLimit(10, neckLat * 0.5f);
                m[11] = ApplyLimit(11, neckTwist * 0.5f);
            }

            // ── HEAD  (12-14) ────────────────────────────────────────────────
            if (Valid(JointName.Neck) && Valid(JointName.Head))
            {
                var headDir = (head - neck).normalized;
                float headPitch = Vector3.Dot(headDir, Vector3.forward);
                float headLat   = Vector3.Dot(headDir, rightDir);
                float headTwist = m[11]; // head inherits approximate neck turn

                m[12] = ApplyLimit(12, -headPitch * 0.6f);
                m[13] = ApplyLimit(13, headLat * 0.6f);
                m[14] = ApplyLimit(14, headTwist);
            }

            // ── LEFT ARM ─────────────────────────────────────────────────────
            if (Valid(JointName.ShoulderLeft) && Valid(JointName.ElbowLeft))
            {
                var upperArmDir = (elbowL - shoulderL).normalized;
                float armDown   = -upperArmDir.y;                              // up-down (abduction)
                float armFwd    = upperArmDir.z;                               // forward-back
                float armTwist  = Vector3.Dot(upperArmDir, Vector3.right);     // twist

                // Shoulder elevation / protraction  (37-38)
                m[37] = ApplyLimit(37, armDown);
                m[38] = ApplyLimit(38, armFwd);

                // Upper arm  (39-41)
                m[39] = ApplyLimit(39, -armDown);                              // abduction
                m[40] = ApplyLimit(40, armFwd);                                // forward flex
                m[41] = ApplyLimit(41, armTwist);                              // rotation

                // ── Left Elbow / Forearm  (42-43) ──────────────────────────
                if (Valid(JointName.WristLeft))
                {
                    var forearmDir = (wristL - elbowL).normalized;
                    float elbowBend = ComputeElbowBend(upperArmDir, forearmDir);
                    m[42] = ApplyLimit(42, elbowBend);

                    float forearmPro = EstimateForearmTwist(upperArmDir, forearmDir, rightDir);
                    m[43] = ApplyLimit(43, forearmPro);

                    // ── Left Wrist  (44-45) ────────────────────────────────
                    if (Valid(JointName.HandLeft))
                    {
                        var handDir = (handL - wristL).normalized;
                        float wristFlex = Vector3.Dot(handDir, forearmDir);
                        float wristDev  = Vector3.Dot(handDir,
                                          Vector3.Cross(forearmDir, Vector3.up).normalized);
                        m[44] = ApplyLimit(44, -wristFlex);
                        m[45] = ApplyLimit(45, wristDev);
                    }
                }
            }

            // ── RIGHT ARM ────────────────────────────────────────────────────
            if (Valid(JointName.ShoulderRight) && Valid(JointName.ElbowRight))
            {
                var upperArmDir = (elbowR - shoulderR).normalized;
                float armDown   = -upperArmDir.y;
                float armFwd    = upperArmDir.z;
                float armTwist  = -Vector3.Dot(upperArmDir, Vector3.right);   // mirrored

                m[46] = ApplyLimit(46, armDown);
                m[47] = ApplyLimit(47, armFwd);

                m[48] = ApplyLimit(48, -armDown);
                m[49] = ApplyLimit(49, armFwd);
                m[50] = ApplyLimit(50, armTwist);

                if (Valid(JointName.WristRight))
                {
                    var forearmDir = (wristR - elbowR).normalized;
                    float elbowBend = ComputeElbowBend(upperArmDir, forearmDir);
                    m[51] = ApplyLimit(51, elbowBend);

                    float forearmPro = EstimateForearmTwist(upperArmDir, forearmDir, -rightDir);
                    m[52] = ApplyLimit(52, forearmPro);

                    if (Valid(JointName.HandRight))
                    {
                        var handDir = (handR - wristR).normalized;
                        float wristFlex = Vector3.Dot(handDir, forearmDir);
                        float wristDev  = Vector3.Dot(handDir,
                                          Vector3.Cross(forearmDir, Vector3.up).normalized);
                        m[53] = ApplyLimit(53, -wristFlex);
                        m[54] = ApplyLimit(54, -wristDev);                     // mirrored
                    }
                }
            }

            // ── LEFT LEG ─────────────────────────────────────────────────────
            if (Valid(JointName.HipLeft) && Valid(JointName.KneeLeft))
            {
                var thighDir = (kneeL - hipL).normalized;
                float hipFB   = thighDir.z;                                    // flex / extend
                float hipAb   = -thighDir.x;                                   // abduction (left = +x in Unity mirror)
                float hipTw   = EstimateAxialRotation(thighDir, -Vector3.up);

                m[21] = ApplyLimit(21, hipFB);
                m[22] = ApplyLimit(22, hipAb);
                m[23] = ApplyLimit(23, hipTw);

                if (Valid(JointName.AnkleLeft))
                {
                    var shinDir = (ankleL - kneeL).normalized;
                    float kneeBend = ComputeKneeBend(thighDir, shinDir);
                    m[24] = ApplyLimit(24, kneeBend);

                    // ── Left Ankle  (26-27) ────────────────────────────────
                    if (Valid(JointName.FootLeft))
                    {
                        var footDir = (footL - ankleL).normalized;
                        float anklePF = Vector3.Dot(footDir, shinDir);         // plantarflex > 0
                        float ankleEv = Vector3.Dot(footDir,
                                        Vector3.Cross(shinDir, Vector3.up).normalized);
                        m[26] = ApplyLimit(26, -anklePF);
                        m[27] = ApplyLimit(27, ankleEv);
                        m[28] = ApplyLimit(28, anklePF * 0.5f);               // toes rough estimate
                    }
                }
            }

            // ── RIGHT LEG ────────────────────────────────────────────────────
            if (Valid(JointName.HipRight) && Valid(JointName.KneeRight))
            {
                var thighDir = (kneeR - hipR).normalized;
                float hipFB   = thighDir.z;
                float hipAb   = thighDir.x;
                float hipTw   = EstimateAxialRotation(thighDir, -Vector3.up);

                m[29] = ApplyLimit(29, hipFB);
                m[30] = ApplyLimit(30, hipAb);
                m[31] = ApplyLimit(31, hipTw);

                if (Valid(JointName.AnkleRight))
                {
                    var shinDir = (ankleR - kneeR).normalized;
                    float kneeBend = ComputeKneeBend(thighDir, shinDir);
                    m[32] = ApplyLimit(32, kneeBend);

                    if (Valid(JointName.FootRight))
                    {
                        var footDir = (footR - ankleR).normalized;
                        float anklePF = Vector3.Dot(footDir, shinDir);
                        float ankleEv = Vector3.Dot(footDir,
                                        Vector3.Cross(shinDir, Vector3.up).normalized);
                        m[34] = ApplyLimit(34, -anklePF);
                        m[35] = ApplyLimit(35, -ankleEv);                      // mirrored
                        m[36] = ApplyLimit(36, anklePF * 0.5f);
                    }
                }
            }

            // Fingers muscles (55-94) are not available from Kinect v2 joint data.
            // Hand open/close from hand state is mapped in a second pass below.
            ApplyHandState(frame, m);

            return m;
        }

        // ─────────────────────────────────────────────────────────────────────
        // HAND STATE → FINGER MUSCLES
        // ─────────────────────────────────────────────────────────────────────

        private static void ApplyHandState(KinectBodyFrame frame, float[] m)
        {
            if (frame.Hands == null) return;

            // Open = 0, Closed = +1 (fingers curled), Lasso = partial curl
            float LeftCurl  = HandCurl(frame.Hands.Left);
            float RightCurl = HandCurl(frame.Hands.Right);

            SetFingerCurl(m, leftSide: true,  curl: LeftCurl);
            SetFingerCurl(m, leftSide: false, curl: RightCurl);
        }

        private static float HandCurl(KinectHandData hand)
        {
            if (hand == null) return 0f;
            return hand.HandState switch
            {
                HandState.Closed => 1f,
                HandState.Lasso  => 0.5f,
                HandState.Open   => 0f,
                _                => 0f,
            };
        }

        /// <summary>
        /// Maps a 0..1 curl value onto all four knuckle muscles of each finger.
        /// Thumb and spread channels are left at zero.
        /// Muscle indices:
        ///   Left:  Index(59,61,62) Middle(63,65,66) Ring(67,69,70) Little(71,73,74)
        ///   Right: Index(79,81,82) Middle(83,85,86) Ring(87,89,90) Little(91,93,94)
        /// </summary>
        private static void SetFingerCurl(float[] m, bool leftSide, float curl)
        {
            int offset = leftSide ? 0 : 20;  // Left fingers start at 55, Right at 75.

            // Knuckle (MCP) and middle/tip (PIP/DIP) for each finger.
            // Each finger block is 4 entries: [knuckle, spread, middle, tip].
            // We drive knuckle(+0), middle(+2), tip(+3); leave spread(+1) alone.
            int[] fingerBaseIndices = leftSide
                ? new[] { 59, 63, 67, 71 }   // Index, Middle, Ring, Little (left)
                : new[] { 79, 83, 87, 91 };   // Index, Middle, Ring, Little (right)

            foreach (int baseIdx in fingerBaseIndices)
            {
                m[baseIdx]     = ApplyLimit_S(baseIdx,     curl);             // MCP flex
                m[baseIdx + 2] = ApplyLimit_S(baseIdx + 2, curl);             // PIP flex
                m[baseIdx + 3] = ApplyLimit_S(baseIdx + 3, curl * 0.8f);     // DIP flex (slightly less)
            }

            // Thumb MCP + IP curl (CMC flex already at 0).
            int thumbBase = leftSide ? 57 : 77;
            m[thumbBase]     = ApplyLimit_S(thumbBase,     curl * 0.7f);
            m[thumbBase + 1] = ApplyLimit_S(thumbBase + 1, curl * 0.7f);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GEOMETRIC HELPERS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Elbow bend: 1 = fully bent, 0 = straight arm.
        /// We measure the angle between the upper arm and forearm vectors and
        /// normalise to the anatomical range (0-145°).
        /// </summary>
        private static float ComputeElbowBend(Vector3 upperArmDir, Vector3 forearmDir)
        {
            float dot   = Mathf.Clamp(Vector3.Dot(-upperArmDir, forearmDir), -1f, 1f);
            float angle = Mathf.Acos(dot) * Mathf.Rad2Deg;   // 0 = straight, 180 = fully bent
            return Mathf.InverseLerp(0f, 145f, angle) * 2f - 1f; // map to -1..+1 (straight..bent)
        }

        /// <summary>
        /// Knee bend: Unity Humanoid convention — (-1 = bent, +1 = straight).
        /// </summary>
        private static float ComputeKneeBend(Vector3 thighDir, Vector3 shinDir)
        {
            float dot   = Mathf.Clamp(Vector3.Dot(-thighDir, shinDir), -1f, 1f);
            float angle = Mathf.Acos(dot) * Mathf.Rad2Deg;   // 0 = straight
            float t     = Mathf.InverseLerp(0f, 145f, angle);  // 0..1
            return Mathf.Lerp(1f, -1f, t);                     // straight = +1, bent = -1
        }

        /// <summary>
        /// Estimate the axial (twist) rotation of a bone direction relative to
        /// a reference up/right vector, returning a value in approximately -1..1.
        /// </summary>
        private static float EstimateAxialRotation(Vector3 boneDir, Vector3 referenceDir)
        {
            if (boneDir.sqrMagnitude < 0.001f) return 0f;
            return Mathf.Clamp(Vector3.Dot(boneDir.normalized, referenceDir.normalized), -1f, 1f);
        }

        /// <summary>
        /// Forearm pronation/supination estimate.
        /// Projects the in-plane perpendicular of the forearm against the shoulder-right axis.
        /// </summary>
        private static float EstimateForearmTwist(Vector3 upperArmDir, Vector3 forearmDir, Vector3 sideRef)
        {
            // Arm plane normal.
            var planeNormal = Vector3.Cross(upperArmDir, sideRef).normalized;
            var perp        = Vector3.Cross(forearmDir, planeNormal).normalized;
            return Mathf.Clamp(Vector3.Dot(perp, sideRef), -1f, 1f);
        }

        // ─────────────────────────────────────────────────────────────────────
        // LIMIT HELPERS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Clamp a raw driving value through HumanMuscleJointLimits for muscle <paramref name="idx"/>.</summary>
        private static float ApplyLimit(int idx, float raw)
            => HumanMuscleJointLimits.Clamp(idx, raw);

        // Static version used in static helper methods where 'this' is unavailable.
        private static float ApplyLimit_S(int idx, float raw)
            => HumanMuscleJointLimits.Clamp(idx, raw);
    }
}
