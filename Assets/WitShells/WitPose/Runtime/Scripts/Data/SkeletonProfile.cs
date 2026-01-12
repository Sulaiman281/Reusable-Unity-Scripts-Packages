using System.Collections.Generic;
using UnityEngine;

namespace WitShells.WitPose
{
    /// <summary>
    /// ScriptableObject database storing standard human anatomical limits.
    /// Provides default constraint values for different body types and age groups.
    /// </summary>
    [CreateAssetMenu(fileName = "SkeletonProfile", menuName = "WitPose/Skeleton Profile", order = 1)]
    public class SkeletonProfile : ScriptableObject
    {
        [Header("Profile Information")]
        [Tooltip("Name of this skeleton profile")]
        public string profileName = "Standard Adult Human";

        [Tooltip("Description of this profile's characteristics")]
        [TextArea(3, 5)]
        public string description = "Standard anatomical constraints for an adult human skeleton.";

        [Header("Profile Settings")]
        [Tooltip("Age category this profile represents")]
        public AgeCategory ageCategory = AgeCategory.Adult;

        [Tooltip("Body type this profile represents")]
        public BodyType bodyType = BodyType.Average;

        [Header("Bone Constraints")]
        [Tooltip("Anatomical constraints for each bone type")]
        public List<BoneConstraint> boneConstraints = new List<BoneConstraint>();

        [Header("Global Settings")]
        [Range(0f, 1f)]
        [Tooltip("Default stiffness for all bones")]
        public float defaultStiffness = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("Default propagation factor for all bones")]
        public float defaultPropagationFactor = 0.3f;

        /// <summary>
        /// Age categories for different constraint profiles
        /// </summary>
        public enum AgeCategory
        {
            Child,
            Teenager,
            Adult,
            Elderly
        }

        /// <summary>
        /// Body types for different constraint profiles
        /// </summary>
        public enum BodyType
        {
            Slim,
            Average,
            Athletic,
            Heavy
        }

        /// <summary>
        /// Constraint data for a specific bone
        /// </summary>
        [System.Serializable]
        public class BoneConstraint
        {
            [Header("Bone Identification")]
            public HumanBodyBones boneId;
            public string boneName;

            [Header("Rotation Constraints (Degrees)")]
            public Vector3 minRotation = new Vector3(-30, -30, -30);
            public Vector3 maxRotation = new Vector3(30, 30, 30);

            [Header("Position Constraints (Units)")]
            [Tooltip("Minimum position offset from original bone position")]
            public Vector3 minPosition = new Vector3(-0.02f, -0.02f, -0.02f);

            [Tooltip("Maximum position offset from original bone position")]
            public Vector3 maxPosition = new Vector3(0.02f, 0.02f, 0.02f);

            [Header("Behavior Settings")]
            [Range(0f, 1f)]
            public float stiffness = 0.5f;

            [Range(0f, 1f)]
            public float propagationFactor = 0.3f;

            [Header("Special Properties")]
            [Tooltip("This bone is critical for posture")]
            public bool isPosturalBone = false;

            [Tooltip("This bone should have enhanced visual feedback")]
            public bool emphasizeInUI = false;
        }

        /// <summary>
        /// Get constraint data for a specific bone
        /// </summary>
        public BoneConstraint GetConstraintForBone(HumanBodyBones boneId)
        {
            foreach (var constraint in boneConstraints)
            {
                if (constraint.boneId == boneId)
                    return constraint;
            }

            // Return default constraint if not found
            return CreateDefaultConstraint(boneId);
        }

        /// <summary>
        /// Create a default constraint for a bone not in the database
        /// </summary>
        private BoneConstraint CreateDefaultConstraint(HumanBodyBones boneId)
        {
            return new BoneConstraint
            {
                boneId = boneId,
                boneName = boneId.ToString(),
                minRotation = new Vector3(-30, -30, -30),
                maxRotation = new Vector3(30, 30, 30),
                stiffness = defaultStiffness,
                propagationFactor = defaultPropagationFactor
            };
        }

        /// <summary>
        /// Apply this profile's constraints to editor bone data
        /// </summary>
        public void ApplyConstraints(HumanBodyBones boneId, out Vector3 minRotation, out Vector3 maxRotation, out float stiffness, out float propagationFactor)
        {
            var constraint = GetConstraintForBone(boneId);

            minRotation = constraint.minRotation;
            maxRotation = constraint.maxRotation;
            stiffness = constraint.stiffness;
            propagationFactor = constraint.propagationFactor;
        }

        /// <summary>
        /// Validate that all required bones have constraints
        /// </summary>
        [ContextMenu("Validate Profile")]
        public void ValidateProfile()
        {
            var requiredBones = GetRequiredBones();
            var missingBones = new List<HumanBodyBones>();

            foreach (var requiredBone in requiredBones)
            {
                bool found = false;
                foreach (var constraint in boneConstraints)
                {
                    if (constraint.boneId == requiredBone)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    missingBones.Add(requiredBone);
            }

            if (missingBones.Count > 0)
            {
                Logger.LogWarning($"SkeletonProfile '{profileName}' is missing constraints for: {string.Join(", ", missingBones)}");
            }
            else
            {
                Logger.Log($"SkeletonProfile '{profileName}' validation passed!");
            }
        }

        /// <summary>
        /// Get list of bones that should have constraints defined
        /// </summary>
        private List<HumanBodyBones> GetRequiredBones()
        {
            return new List<HumanBodyBones>
            {
                HumanBodyBones.Hips,
                HumanBodyBones.Spine,
                HumanBodyBones.Chest,
                HumanBodyBones.Neck,
                HumanBodyBones.Head,
                HumanBodyBones.LeftShoulder,
                HumanBodyBones.LeftUpperArm,
                HumanBodyBones.LeftLowerArm,
                HumanBodyBones.LeftHand,
                HumanBodyBones.RightShoulder,
                HumanBodyBones.RightUpperArm,
                HumanBodyBones.RightLowerArm,
                HumanBodyBones.RightHand,
                HumanBodyBones.LeftUpperLeg,
                HumanBodyBones.LeftLowerLeg,
                HumanBodyBones.LeftFoot,
                HumanBodyBones.RightUpperLeg,
                HumanBodyBones.RightLowerLeg,
                HumanBodyBones.RightFoot
            };
        }

        /// <summary>
        /// Create default profile with standard human constraints
        /// </summary>
        [ContextMenu("Reset to Standard Human")]
        public void ResetToStandardHuman()
        {
            boneConstraints.Clear();

            // Add standard human constraints
            AddStandardConstraints();

            Logger.Log($"Reset profile '{profileName}' to standard human constraints.");
        }

        /// <summary>
        /// Add standard human anatomical constraints
        /// </summary>
        private void AddStandardConstraints()
        {
            // Spine constraints
            AddConstraint(HumanBodyBones.Hips, new Vector3(-20, -30, -10), new Vector3(20, 30, 10), 0.8f, 0.5f, true);
            AddConstraint(HumanBodyBones.Spine, new Vector3(-30, -20, -20), new Vector3(30, 20, 20), 0.6f, 0.4f, true);
            AddConstraint(HumanBodyBones.Chest, new Vector3(-20, -15, -15), new Vector3(20, 15, 15), 0.5f, 0.3f, true);
            AddConstraint(HumanBodyBones.Neck, new Vector3(-30, -60, -30), new Vector3(30, 60, 30), 0.3f, 0.2f);
            AddConstraint(HumanBodyBones.Head, new Vector3(-20, -45, -20), new Vector3(20, 45, 20), 0.2f, 0.1f);

            // Left arm constraints
            AddConstraint(HumanBodyBones.LeftShoulder, new Vector3(-30, -30, -30), new Vector3(30, 30, 30), 0.4f, 0.3f);
            AddConstraint(HumanBodyBones.LeftUpperArm, new Vector3(-90, -180, -60), new Vector3(90, 60, 120), 0.5f, 0.4f);
            AddConstraint(HumanBodyBones.LeftLowerArm, new Vector3(0, -90, -90), new Vector3(140, 0, 90), 0.4f, 0.2f);
            AddConstraint(HumanBodyBones.LeftHand, new Vector3(-60, -30, -30), new Vector3(60, 30, 30), 0.3f, 0.1f);

            // Right arm constraints (mirrored)
            AddConstraint(HumanBodyBones.RightShoulder, new Vector3(-30, -30, -30), new Vector3(30, 30, 30), 0.4f, 0.3f);
            AddConstraint(HumanBodyBones.RightUpperArm, new Vector3(-90, -60, -120), new Vector3(90, 180, 60), 0.5f, 0.4f);
            AddConstraint(HumanBodyBones.RightLowerArm, new Vector3(0, 0, -90), new Vector3(140, 90, 90), 0.4f, 0.2f);
            AddConstraint(HumanBodyBones.RightHand, new Vector3(-60, -30, -30), new Vector3(60, 30, 30), 0.3f, 0.1f);

            // Left leg constraints
            AddConstraint(HumanBodyBones.LeftUpperLeg, new Vector3(-90, -45, -30), new Vector3(30, 45, 30), 0.6f, 0.4f);
            AddConstraint(HumanBodyBones.LeftLowerLeg, new Vector3(0, -10, -10), new Vector3(140, 10, 10), 0.5f, 0.2f);
            AddConstraint(HumanBodyBones.LeftFoot, new Vector3(-30, -20, -20), new Vector3(30, 20, 20), 0.4f, 0.1f);

            // Right leg constraints (mirrored)
            AddConstraint(HumanBodyBones.RightUpperLeg, new Vector3(-90, -45, -30), new Vector3(30, 45, 30), 0.6f, 0.4f);
            AddConstraint(HumanBodyBones.RightLowerLeg, new Vector3(0, -10, -10), new Vector3(140, 10, 10), 0.5f, 0.2f);
            AddConstraint(HumanBodyBones.RightFoot, new Vector3(-30, -20, -20), new Vector3(30, 20, 20), 0.4f, 0.1f);
        }

        /// <summary>
        /// Helper method to add a constraint
        /// </summary>
        private void AddConstraint(HumanBodyBones boneId, Vector3 minRot, Vector3 maxRot,
            float stiffness, float propagation, bool isPostural = false)
        {
            boneConstraints.Add(new BoneConstraint
            {
                boneId = boneId,
                boneName = boneId.ToString(),
                minRotation = minRot,
                maxRotation = maxRot,
                stiffness = stiffness,
                propagationFactor = propagation,
                isPosturalBone = isPostural
            });
        }
    }
}