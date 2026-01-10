using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace WitShells.WitPose.Editor.Core
{
    /// <summary>
    /// AUTHORITATIVE animation writer - the ONLY system allowed to write keyframes
    /// All pose edits (FK, IK, Muscle) flow through this system
    /// </summary>
    public class BonePoseSystem
    {
        private SkeletonCache skeleton;
        private AnimationClip activeClip;
        private float currentTime;
        private bool autoKey = true;

        // Sparse keying optimization
        private HashSet<HumanBodyBones> modifiedBones = new HashSet<HumanBodyBones>();

        public AnimationClip ActiveClip
        {
            get => activeClip;
            set => activeClip = value;
        }

        public float CurrentTime
        {
            get => currentTime;
            set => currentTime = Mathf.Max(0, value);
        }

        public bool AutoKey
        {
            get => autoKey;
            set => autoKey = value;
        }

        public BonePoseSystem(SkeletonCache skeleton)
        {
            this.skeleton = skeleton;
        }

        /// <summary>
        /// Set bone rotation - called from gizmos, IK, or muscle system
        /// </summary>
        public void SetBoneRotation(HumanBodyBones bone, Quaternion localRotation, bool recordUndo = true)
        {
            var boneData = skeleton.GetBone(bone);
            if (boneData == null)
                return;

            if (recordUndo)
            {
                Undo.RecordObject(boneData.transform, "Rotate Bone");
            }

            boneData.transform.localRotation = localRotation;
            modifiedBones.Add(bone);

            if (autoKey && activeClip != null)
            {
                WriteBoneRotationKey(bone, localRotation, currentTime);
            }
        }

        /// <summary>
        /// Set bone position - only for root (Hips)
        /// </summary>
        public void SetBonePosition(HumanBodyBones bone, Vector3 localPosition, bool recordUndo = true)
        {
            var boneData = skeleton.GetBone(bone);
            if (boneData == null)
                return;

            if (recordUndo)
            {
                Undo.RecordObject(boneData.transform, "Move Bone");
            }

            boneData.transform.localPosition = localPosition;
            modifiedBones.Add(bone);

            if (autoKey && activeClip != null && bone == HumanBodyBones.Hips)
            {
                WriteBonePositionKey(bone, localPosition, currentTime);
            }
        }

        /// <summary>
        /// Commit all modified bones to animation clip at current time
        /// Called explicitly when batch editing (e.g., IK solve)
        /// </summary>
        public void CommitPose()
        {
            if (activeClip == null || modifiedBones.Count == 0)
                return;

            Undo.RecordObject(activeClip, "Commit Pose");

            foreach (var bone in modifiedBones)
            {
                var boneData = skeleton.GetBone(bone);
                if (boneData == null)
                    continue;

                WriteBoneRotationKey(bone, boneData.transform.localRotation, currentTime);

                if (bone == HumanBodyBones.Hips)
                {
                    WriteBonePositionKey(bone, boneData.transform.localPosition, currentTime);
                }
            }

            EditorUtility.SetDirty(activeClip);
            modifiedBones.Clear();

            Debug.Log($"✅ Committed pose at {currentTime:F3}s");
        }

        /// <summary>
        /// Write rotation keyframe directly to AnimationClip
        /// </summary>
        private void WriteBoneRotationKey(HumanBodyBones bone, Quaternion rotation, float time)
        {
            var boneData = skeleton.GetBone(bone);
            if (boneData == null || activeClip == null)
                return;

            string path = boneData.path;

            // Write quaternion components
            SetKeyframe(path, "localRotation.x", time, rotation.x);
            SetKeyframe(path, "localRotation.y", time, rotation.y);
            SetKeyframe(path, "localRotation.z", time, rotation.z);
            SetKeyframe(path, "localRotation.w", time, rotation.w);
        }

        /// <summary>
        /// Write position keyframe directly to AnimationClip
        /// </summary>
        private void WriteBonePositionKey(HumanBodyBones bone, Vector3 position, float time)
        {
            var boneData = skeleton.GetBone(bone);
            if (boneData == null || activeClip == null)
                return;

            string path = boneData.path;

            SetKeyframe(path, "localPosition.x", time, position.x);
            SetKeyframe(path, "localPosition.y", time, position.y);
            SetKeyframe(path, "localPosition.z", time, position.z);
        }

        /// <summary>
        /// Core keyframe writer - directly manipulates AnimationClip curves
        /// </summary>
        private void SetKeyframe(string path, string propertyName, float time, float value)
        {
            if (activeClip == null)
                return;

            EditorCurveBinding binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName);
            AnimationCurve curve = AnimationUtility.GetEditorCurve(activeClip, binding);

            if (curve == null)
            {
                curve = new AnimationCurve();
            }

            // Remove existing keyframe at this time
            for (int i = curve.keys.Length - 1; i >= 0; i--)
            {
                if (Mathf.Approximately(curve.keys[i].time, time))
                {
                    curve.RemoveKey(i);
                }
            }

            // Add new keyframe
            curve.AddKey(new Keyframe(time, value));

            AnimationUtility.SetEditorCurve(activeClip, binding, curve);
        }

        /// <summary>
        /// Sample animation at current time and apply to skeleton
        /// </summary>
        public void SampleAnimation(float time)
        {
            if (activeClip == null || skeleton == null)
                return;

            currentTime = time;

            // Sample all bone transforms from the clip
            activeClip.SampleAnimation(skeleton.Animator.gameObject, time);
        }

        /// <summary>
        /// Clear all keyframes at current time
        /// </summary>
        public void DeleteKeyframesAtTime(float time)
        {
            if (activeClip == null)
                return;

            Undo.RecordObject(activeClip, "Delete Keyframes");

            foreach (var boneData in skeleton.AllBones)
            {
                DeleteKeyframeAtTime(boneData.path, "localRotation.x", time);
                DeleteKeyframeAtTime(boneData.path, "localRotation.y", time);
                DeleteKeyframeAtTime(boneData.path, "localRotation.z", time);
                DeleteKeyframeAtTime(boneData.path, "localRotation.w", time);

                if (boneData.boneType == HumanBodyBones.Hips)
                {
                    DeleteKeyframeAtTime(boneData.path, "localPosition.x", time);
                    DeleteKeyframeAtTime(boneData.path, "localPosition.y", time);
                    DeleteKeyframeAtTime(boneData.path, "localPosition.z", time);
                }
            }

            EditorUtility.SetDirty(activeClip);
        }

        private void DeleteKeyframeAtTime(string path, string propertyName, float time)
        {
            EditorCurveBinding binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName);
            AnimationCurve curve = AnimationUtility.GetEditorCurve(activeClip, binding);

            if (curve == null)
                return;

            for (int i = curve.keys.Length - 1; i >= 0; i--)
            {
                if (Mathf.Approximately(curve.keys[i].time, time))
                {
                    curve.RemoveKey(i);
                }
            }

            AnimationUtility.SetEditorCurve(activeClip, binding, curve);
        }
    }
}
