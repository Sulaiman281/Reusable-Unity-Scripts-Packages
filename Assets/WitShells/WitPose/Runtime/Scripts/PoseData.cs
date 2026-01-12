using System;
using UnityEngine;

namespace WitShells.WitPose
{
    /// <summary>
    /// Data structure for storing and managing human poses.
    /// Uses Unity's HumanPose muscle space for rig-agnostic storage.
    /// </summary>
    [System.Serializable]
    public class PoseData
    {
        [Header("Pose Metadata")]
        public string poseName = "New Pose";
        public string category = "Uncategorized";
        public string author = "";
        public string description = "";
        public string version = "1.0";
        public long timestamp;

        [Header("Pose Data")]
        [SerializeField] private Vector3 bodyPosition;
        [SerializeField] private Quaternion bodyRotation;
        [SerializeField] private float[] muscles;

        [Header("Thumbnails")]
        public Texture2D thumbnail;
        public string thumbnailPath;

        /// <summary>
        /// Properties for accessing pose data
        /// </summary>
        public Vector3 BodyPosition => bodyPosition;
        public Quaternion BodyRotation => bodyRotation;
        public float[] Muscles => muscles;

        /// <summary>
        /// Create a new pose data from HumanPose
        /// </summary>
        public PoseData(HumanPose humanPose, string name = "New Pose")
        {
            poseName = name;
            bodyPosition = humanPose.bodyPosition;
            bodyRotation = humanPose.bodyRotation;
            muscles = (float[])humanPose.muscles.Clone();
            timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
        }

        /// <summary>
        /// Create empty pose data
        /// </summary>
        public PoseData()
        {
            bodyPosition = Vector3.zero;
            bodyRotation = Quaternion.identity;
            muscles = new float[95]; // HumanTrait.MuscleCount is 95, but can't call during serialization
            timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
        }

        /// <summary>
        /// Convert this pose data to HumanPose
        /// </summary>
        public HumanPose ToHumanPose()
        {
            HumanPose pose = new HumanPose();
            pose.bodyPosition = bodyPosition;
            pose.bodyRotation = bodyRotation;
            pose.muscles = (float[])muscles.Clone();
            return pose;
        }

        /// <summary>
        /// Update pose data from HumanPose
        /// </summary>
        public void UpdateFromHumanPose(HumanPose humanPose)
        {
            bodyPosition = humanPose.bodyPosition;
            bodyRotation = humanPose.bodyRotation;
            muscles = (float[])humanPose.muscles.Clone();
            timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
        }

        /// <summary>
        /// Create a copy of this pose data
        /// </summary>
        public PoseData Clone()
        {
            PoseData clone = new PoseData();
            clone.poseName = poseName + " (Copy)";
            clone.category = category;
            clone.author = author;
            clone.description = description;
            clone.version = version;
            clone.bodyPosition = bodyPosition;
            clone.bodyRotation = bodyRotation;
            clone.muscles = (float[])muscles.Clone();
            clone.timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
            return clone;
        }

        /// <summary>
        /// Blend this pose with another pose
        /// </summary>
        public PoseData BlendWith(PoseData otherPose, float t)
        {
            PoseData blended = new PoseData();
            blended.poseName = $"{poseName} + {otherPose.poseName}";
            blended.category = category;
            blended.author = author;
            blended.description = $"Blend of {poseName} and {otherPose.poseName}";

            // Blend positions and rotations
            blended.bodyPosition = Vector3.Lerp(bodyPosition, otherPose.bodyPosition, t);
            blended.bodyRotation = Quaternion.Slerp(bodyRotation, otherPose.bodyRotation, t);

            // Blend muscles
            blended.muscles = new float[muscles.Length];
            for (int i = 0; i < muscles.Length; i++)
            {
                blended.muscles[i] = Mathf.Lerp(muscles[i], otherPose.muscles[i], t);
            }

            return blended;
        }

        /// <summary>
        /// Mirror this pose (left-right flip)
        /// </summary>
        public PoseData CreateMirrored()
        {
            PoseData mirrored = Clone();
            mirrored.poseName = poseName + " (Mirrored)";

            // Mirror muscle values based on left-right pairs
            for (int i = 0; i < muscles.Length; i++)
            {
                int mirrorIndex = HumanTrait.GetMuscleDefaultMax(i) != 0 ? GetMirrorMuscleIndex(i) : i;
                if (mirrorIndex != i && mirrorIndex < muscles.Length)
                {
                    float temp = mirrored.muscles[i];
                    mirrored.muscles[i] = mirrored.muscles[mirrorIndex];
                    mirrored.muscles[mirrorIndex] = temp;
                }
            }

            return mirrored;
        }

        /// <summary>
        /// Get mirror muscle index for left-right swapping
        /// </summary>
        private int GetMirrorMuscleIndex(int muscleIndex)
        {
            // This would need a lookup table for muscle mirroring
            // For now, return the same index (no mirroring)
            // TODO: Implement proper muscle mirroring lookup
            return muscleIndex;
        }

        /// <summary>
        /// Validate pose data integrity
        /// </summary>
        public bool IsValid()
        {
            if (muscles == null) return false;
            
            // Ensure muscle array has correct size (95 muscles for humanoid)
            int expectedMuscleCount = 95; // HumanTrait.MuscleCount constant
            if (muscles.Length != expectedMuscleCount) return false;

            // Check for NaN or infinite values
            foreach (float muscle in muscles)
            {
                if (float.IsNaN(muscle) || float.IsInfinity(muscle))
                    return false;
            }

            return true;
        
        }
        // Ensure muscle array has correct size - call this after deserialization
        /// </summary>
        public void ValidateMuscleArraySize()
        {
            if (muscles == null || muscles.Length != 95) // HumanTrait.MuscleCount = 95
            {
                var oldMuscles = muscles;
                muscles = new float[95];
                
                // Copy existing values if any
                if (oldMuscles != null)
                {
                    int copyCount = Mathf.Min(oldMuscles.Length, muscles.Length);
                    for (int i = 0; i < copyCount; i++)
                    {
                        muscles[i] = oldMuscles[i];
                    }
                }
            }
        }
        /// <summary>
        /// Clamp all muscle values to valid range
        /// </summary>
        public void ClampMuscleValues()
        {
            for (int i = 0; i < muscles.Length; i++)
            {
                float min = HumanTrait.GetMuscleDefaultMin(i);
                float max = HumanTrait.GetMuscleDefaultMax(i);
                muscles[i] = Mathf.Clamp(muscles[i], min, max);
            }
        }

        /// <summary>
        /// Get pose as JSON string
        /// </summary>
        public string ToJson(bool prettyPrint = false)
        {
            var jsonData = new JsonPoseData
            {
                meta = new JsonPoseData.MetaData
                {
                    name = poseName,
                    category = category,
                    author = author,
                    description = description,
                    version = version,
                    timestamp = timestamp
                },
                pose = new JsonPoseData.PoseDataJson
                {
                    bodyPosition = new JsonVector3(bodyPosition),
                    bodyRotation = new JsonQuaternion(bodyRotation),
                    muscles = muscles
                }
            };

            return JsonUtility.ToJson(jsonData, prettyPrint);
        }

        /// <summary>
        /// Create pose from JSON string
        /// </summary>
        public static PoseData FromJson(string json)
        {
            try
            {
                var jsonData = JsonUtility.FromJson<JsonPoseData>(json);

                PoseData pose = new PoseData();
                pose.poseName = jsonData.meta.name;
                pose.category = jsonData.meta.category;
                pose.author = jsonData.meta.author;
                pose.description = jsonData.meta.description;
                pose.version = jsonData.meta.version;
                pose.timestamp = jsonData.meta.timestamp;

                pose.bodyPosition = jsonData.pose.bodyPosition.ToVector3();
                pose.bodyRotation = jsonData.pose.bodyRotation.ToQuaternion();
                pose.muscles = jsonData.pose.muscles;

                return pose;
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to parse pose JSON: {e.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// JSON serializable version of pose data
    /// </summary>
    [System.Serializable]
    internal class JsonPoseData
    {
        public MetaData meta;
        public PoseDataJson pose;

        [System.Serializable]
        public class MetaData
        {
            public string name;
            public string category;
            public string author;
            public string description;
            public string version;
            public long timestamp;
        }

        [System.Serializable]
        public class PoseDataJson
        {
            public JsonVector3 bodyPosition;
            public JsonQuaternion bodyRotation;
            public float[] muscles;
        }
    }

    /// <summary>
    /// JSON serializable Vector3
    /// </summary>
    [System.Serializable]
    internal struct JsonVector3
    {
        public float x, y, z;

        public JsonVector3(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    /// <summary>
    /// JSON serializable Quaternion
    /// </summary>
    [System.Serializable]
    internal struct JsonQuaternion
    {
        public float x, y, z, w;

        public JsonQuaternion(Quaternion quaternion)
        {
            x = quaternion.x;
            y = quaternion.y;
            z = quaternion.z;
            w = quaternion.w;
        }

        public Quaternion ToQuaternion()
        {
            return new Quaternion(x, y, z, w);
        }
    }
}