using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace WitShells.WitPose.Editor.Core
{
    /// <summary>
    /// Authoritative bone registry for humanoid skeletons
    /// Caches all bone data for fast lookup by gizmos, IK, and pose systems
    /// </summary>
    public class SkeletonCache
    {
        public class BoneData
        {
            public HumanBodyBones boneType;
            public Transform transform;
            public string path;
            public BoneData parent;
            public List<BoneData> children = new List<BoneData>();
            public Quaternion bindRotation;
            public Vector3 bindPosition;
            
            // Runtime state
            public bool isSelected;
            public Color gizmoColor = Color.white;
        }

        private Animator animator;
        private Dictionary<HumanBodyBones, BoneData> boneCache = new Dictionary<HumanBodyBones, BoneData>();
        private Dictionary<Transform, BoneData> transformLookup = new Dictionary<Transform, BoneData>();

        public Animator Animator => animator;
        public IEnumerable<BoneData> AllBones => boneCache.Values;

        public SkeletonCache(Animator animator)
        {
            this.animator = animator;
            BuildCache();
        }

        private void BuildCache()
        {
            if (animator == null || !animator.isHuman)
            {
                Logger.LogError("SkeletonCache requires a Humanoid Animator!");
                return;
            }

            boneCache.Clear();
            transformLookup.Clear();

            // Cache all bones
            foreach (HumanBodyBones bone in System.Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (bone == HumanBodyBones.LastBone)
                    continue;

                Transform boneTransform = animator.GetBoneTransform(bone);
                if (boneTransform == null)
                    continue;

                var boneData = new BoneData
                {
                    boneType = bone,
                    transform = boneTransform,
                    path = AnimationUtility.CalculateTransformPath(boneTransform, animator.transform),
                    bindRotation = boneTransform.localRotation,
                    bindPosition = boneTransform.localPosition
                };

                boneCache[bone] = boneData;
                transformLookup[boneTransform] = boneData;
            }

            // Build parent-child relationships
            foreach (var kvp in boneCache)
            {
                var boneData = kvp.Value;
                if (boneData.transform.parent != null && transformLookup.TryGetValue(boneData.transform.parent, out var parentData))
                {
                    boneData.parent = parentData;
                    parentData.children.Add(boneData);
                }
            }

            Logger.Log($"SkeletonCache: Cached {boneCache.Count} bones");
        }

        public BoneData GetBone(HumanBodyBones bone)
        {
            return boneCache.TryGetValue(bone, out var data) ? data : null;
        }

        public BoneData GetBone(Transform transform)
        {
            return transformLookup.TryGetValue(transform, out var data) ? data : null;
        }

        public bool HasBone(HumanBodyBones bone)
        {
            return boneCache.ContainsKey(bone);
        }

        public void ResetToBindPose()
        {
            foreach (var boneData in boneCache.Values)
            {
                boneData.transform.localRotation = boneData.bindRotation;
                boneData.transform.localPosition = boneData.bindPosition;
            }
        }

        public void ClearSelection()
        {
            foreach (var bone in boneCache.Values)
            {
                bone.isSelected = false;
            }
        }

        public void SelectBone(HumanBodyBones bone, bool exclusive = true)
        {
            if (exclusive)
                ClearSelection();

            if (boneCache.TryGetValue(bone, out var data))
            {
                data.isSelected = true;
            }
        }

        public BoneData GetSelectedBone()
        {
            foreach (var bone in boneCache.Values)
            {
                if (bone.isSelected)
                    return bone;
            }
            return null;
        }
    }
}
