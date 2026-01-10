using UnityEngine;
using UnityEditor;

namespace WitShells.WitPose.Editor.Core
{
    /// <summary>
    /// Muscle-based pose system - HELPER layer only
    /// Never writes to AnimationClip directly
    /// Forwards all bone transforms to BonePoseSystem
    /// </summary>
    public class MusclePoseSystem
    {
        private Animator animator;
        private HumanPoseHandler poseHandler;
        private HumanPose currentPose;
        private float[] muscleValues;
        private BonePoseSystem bonePoseSystem;
        private SkeletonCache skeleton;

        public float[] MuscleValues => muscleValues;

        public MusclePoseSystem(Animator animator, SkeletonCache skeleton, BonePoseSystem bonePoseSystem)
        {
            this.animator = animator;
            this.skeleton = skeleton;
            this.bonePoseSystem = bonePoseSystem;

            if (!animator.isHuman)
            {
                Debug.LogError("MusclePoseSystem requires Humanoid rig!");
                return;
            }

            // Initialize pose handler
            poseHandler = new HumanPoseHandler(animator.avatar, animator.transform);
            currentPose = new HumanPose();
            muscleValues = new float[HumanTrait.MuscleCount];

            // Read current pose
            SyncFromSkeleton();
        }

        /// <summary>
        /// Read current skeleton state into muscle values
        /// </summary>
        public void SyncFromSkeleton()
        {
            if (poseHandler == null)
                return;

            poseHandler.GetHumanPose(ref currentPose);

            for (int i = 0; i < HumanTrait.MuscleCount; i++)
            {
                muscleValues[i] = currentPose.muscles[i];
            }
        }

        /// <summary>
        /// Set a specific muscle value and apply to skeleton
        /// Forwards resulting bone rotations to BonePoseSystem
        /// </summary>
        public void SetMuscle(int muscleIndex, float value, bool autoKey = true)
        {
            if (muscleIndex < 0 || muscleIndex >= muscleValues.Length)
                return;

            muscleValues[muscleIndex] = Mathf.Clamp(value, -1f, 1f);
            ApplyMusclesToSkeleton(autoKey);
        }

        /// <summary>
        /// Set all muscle values at once
        /// </summary>
        public void SetAllMuscles(float[] values, bool autoKey = true)
        {
            if (values == null || values.Length != muscleValues.Length)
                return;

            System.Array.Copy(values, muscleValues, muscleValues.Length);
            ApplyMusclesToSkeleton(autoKey);
        }

        /// <summary>
        /// Reset all muscles to T-Pose (0 values)
        /// </summary>
        public void ResetToTPose(bool autoKey = true)
        {
            for (int i = 0; i < muscleValues.Length; i++)
            {
                muscleValues[i] = 0f;
            }
            ApplyMusclesToSkeleton(autoKey);
        }

        /// <summary>
        /// Apply muscle values to skeleton and forward to BonePoseSystem
        /// </summary>
        private void ApplyMusclesToSkeleton(bool autoKey)
        {
            if (poseHandler == null)
                return;

            // Update pose with current muscle values
            currentPose.muscles = muscleValues;

            // Apply to skeleton transforms
            poseHandler.SetHumanPose(ref currentPose);

            // Forward all bone rotations to BonePoseSystem
            // This is the ONLY way muscles affect animation
            foreach (HumanBodyBones bone in System.Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (bone == HumanBodyBones.LastBone)
                    continue;

                var boneData = skeleton.GetBone(bone);
                if (boneData == null)
                    continue;

                // Forward to authoritative system
                bonePoseSystem.SetBoneRotation(
                    bone,
                    boneData.transform.localRotation,
                    recordUndo: false // Already handled by muscle system
                );

                // Forward root position
                if (bone == HumanBodyBones.Hips)
                {
                    bonePoseSystem.SetBonePosition(
                        bone,
                        boneData.transform.localPosition,
                        recordUndo: false
                    );
                }
            }

            // Commit if auto-keying
            if (autoKey)
            {
                bonePoseSystem.CommitPose();
            }
        }

        /// <summary>
        /// Get muscle name for UI display
        /// </summary>
        public string GetMuscleName(int index)
        {
            if (index < 0 || index >= HumanTrait.MuscleCount)
                return "Invalid";

            return HumanTrait.MuscleName[index];
        }

        /// <summary>
        /// Get muscle limits
        /// </summary>
        public Vector2 GetMuscleLimits(int index)
        {
            // Unity muscles are typically -1 to 1, but some have different ranges
            // For now, return standard range
            return new Vector2(-1f, 1f);
        }

        /// <summary>
        /// Get the current HumanPose with latest muscle values
        /// </summary>
        public HumanPose GetHumanPose()
        {
            if (poseHandler != null)
            {
                // Sync to get latest body position/rotation
                poseHandler.GetHumanPose(ref currentPose);
            }

            // Update with current muscle values
            currentPose.muscles = (float[])muscleValues.Clone();
            return currentPose;
        }
    }
}
