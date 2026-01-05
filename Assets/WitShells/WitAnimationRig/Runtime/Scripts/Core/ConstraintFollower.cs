namespace WitShells.AnimationRig
{
    using UnityEngine;
    using System;

    /// <summary>
    /// Defines how a constraint should follow a target transform.
    /// </summary>
    [Serializable]
    public class ConstraintBinding
    {
        [Tooltip("The source transform to follow")]
        public Transform source;

        [Tooltip("Enable position constraint")]
        public bool followPosition = true;

        [Tooltip("Enable rotation constraint")]
        public bool followRotation = true;

        [Tooltip("Position offset from source")]
        public Vector3 positionOffset;

        [Tooltip("Rotation offset from source")]
        public Vector3 rotationOffset;

        [Tooltip("Smoothing speed (0 = instant, higher = smoother)")]
        [Range(0f, 50f)]
        public float smoothSpeed = 10f;

        [Tooltip("Weight of this binding (0-1)")]
        [Range(0f, 1f)]
        public float weight = 1f;

        /// <summary>
        /// Whether this binding is valid and active.
        /// </summary>
        public bool IsValid => source != null && weight > 0f && (followPosition || followRotation);
    }

    /// <summary>
    /// Controls IK constraint targets by following assigned transform references.
    /// Allows toggling position/rotation constraints independently for hands, legs, and head.
    /// </summary>
    public class ConstraintFollower : MonoBehaviour
    {
        [Header("Rig References")]
        [SerializeField] private RigReferences rigReferences;

        [Header("Head Binding")]
        [SerializeField] private ConstraintBinding headBinding = new ConstraintBinding();

        [Header("Hand Bindings")]
        [SerializeField] private ConstraintBinding leftHandBinding = new ConstraintBinding();
        [SerializeField] private ConstraintBinding rightHandBinding = new ConstraintBinding();

        [Header("Leg Bindings")]
        [SerializeField] private ConstraintBinding leftLegBinding = new ConstraintBinding();
        [SerializeField] private ConstraintBinding rightLegBinding = new ConstraintBinding();

        [Header("Global Settings")]
        [Tooltip("Master weight multiplier for all constraints")]
        [Range(0f, 1f)]
        [SerializeField] private float masterWeight = 1f;

        [Tooltip("Update mode for constraint following")]
        [SerializeField] private UpdateMode updateMode = UpdateMode.LateUpdate;

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = false;

        public enum UpdateMode
        {
            Update,
            LateUpdate,
            FixedUpdate
        }

        #region Properties

        public RigReferences RigReferences
        {
            get => rigReferences;
            set => rigReferences = value;
        }

        public float MasterWeight
        {
            get => masterWeight;
            set => masterWeight = Mathf.Clamp01(value);
        }

        public ConstraintBinding HeadBinding => headBinding;
        public ConstraintBinding LeftHandBinding => leftHandBinding;
        public ConstraintBinding RightHandBinding => rightHandBinding;
        public ConstraintBinding LeftLegBinding => leftLegBinding;
        public ConstraintBinding RightLegBinding => rightLegBinding;

        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (rigReferences == null)
            {
                rigReferences = GetComponentInChildren<RigReferences>();
                if (rigReferences == null)
                    rigReferences = GetComponentInParent<RigReferences>();
            }
        }
#endif

        private void Update()
        {
            if (updateMode == UpdateMode.Update)
                UpdateConstraints();
        }

        private void LateUpdate()
        {
            if (updateMode == UpdateMode.LateUpdate)
                UpdateConstraints();
        }

        private void FixedUpdate()
        {
            if (updateMode == UpdateMode.FixedUpdate)
                UpdateConstraints();
        }

        private void UpdateConstraints()
        {
            if (rigReferences == null || masterWeight <= 0f) return;

            // Head
            ApplyBinding(headBinding, rigReferences.HeadTarget);
            UpdateWeight(rigReferences, ConstraintType.Head, headBinding);

            // Hands
            ApplyBinding(leftHandBinding, rigReferences.LeftHandTarget);
            UpdateWeight(rigReferences, ConstraintType.LeftHand, leftHandBinding);

            ApplyBinding(rightHandBinding, rigReferences.RightHandTarget);
            UpdateWeight(rigReferences, ConstraintType.RightHand, rightHandBinding);

            // Legs
            ApplyBinding(leftLegBinding, rigReferences.LeftLegTarget);
            UpdateWeight(rigReferences, ConstraintType.LeftLeg, leftLegBinding);

            ApplyBinding(rightLegBinding, rigReferences.RightLegTarget);
            UpdateWeight(rigReferences, ConstraintType.RightLeg, rightLegBinding);
        }

        private void ApplyBinding(ConstraintBinding binding, Transform target)
        {
            if (!binding.IsValid || target == null) return;

            float effectiveWeight = binding.weight * masterWeight;
            if (effectiveWeight <= 0f) return;

            Vector3 targetPosition = binding.source.position + 
                binding.source.TransformDirection(binding.positionOffset);
            Quaternion targetRotation = binding.source.rotation * 
                Quaternion.Euler(binding.rotationOffset);

            if (binding.smoothSpeed > 0f)
            {
                float smoothFactor = Time.deltaTime * binding.smoothSpeed;

                if (binding.followPosition)
                {
                    target.position = Vector3.Lerp(target.position, targetPosition, smoothFactor);
                }

                if (binding.followRotation)
                {
                    target.rotation = Quaternion.Slerp(target.rotation, targetRotation, smoothFactor);
                }
            }
            else
            {
                if (binding.followPosition)
                {
                    target.position = targetPosition;
                }

                if (binding.followRotation)
                {
                    target.rotation = targetRotation;
                }
            }
        }

        public enum ConstraintType
        {
            Head,
            LeftHand,
            RightHand,
            LeftLeg,
            RightLeg
        }

        private void UpdateWeight(RigReferences refs, ConstraintType type, ConstraintBinding binding)
        {
            if (refs == null) return;
            
            float targetWeight = binding.IsValid ? binding.weight * masterWeight : 0f;

            switch (type)
            {
                case ConstraintType.Head:
                    refs.HeadWeight = targetWeight;
                    break;
                case ConstraintType.LeftHand:
                    refs.LeftHandWeight = targetWeight;
                    break;
                case ConstraintType.RightHand:
                    refs.RightHandWeight = targetWeight;
                    break;
                case ConstraintType.LeftLeg:
                    refs.LeftLegWeight = targetWeight;
                    break;
                case ConstraintType.RightLeg:
                    refs.RightLegWeight = targetWeight;
                    break;
            }
        }

        #region Public API

        /// <summary>
        /// Sets the source transform for a specific constraint.
        /// </summary>
        public void SetSource(ConstraintType type, Transform source)
        {
            var binding = GetBinding(type);
            if (binding != null)
                binding.source = source;
        }

        /// <summary>
        /// Enables or disables position following for a specific constraint.
        /// </summary>
        public void SetFollowPosition(ConstraintType type, bool follow)
        {
            var binding = GetBinding(type);
            if (binding != null)
                binding.followPosition = follow;
        }

        /// <summary>
        /// Enables or disables rotation following for a specific constraint.
        /// </summary>
        public void SetFollowRotation(ConstraintType type, bool follow)
        {
            var binding = GetBinding(type);
            if (binding != null)
                binding.followRotation = follow;
        }

        /// <summary>
        /// Sets the weight for a specific constraint binding.
        /// </summary>
        public void SetBindingWeight(ConstraintType type, float weight)
        {
            var binding = GetBinding(type);
            if (binding != null)
                binding.weight = Mathf.Clamp01(weight);
        }

        private ConstraintBinding GetBinding(ConstraintType type)
        {
            return type switch
            {
                ConstraintType.Head => headBinding,
                ConstraintType.LeftHand => leftHandBinding,
                ConstraintType.RightHand => rightHandBinding,
                ConstraintType.LeftLeg => leftLegBinding,
                ConstraintType.RightLeg => rightLegBinding,
                _ => null
            };
        }

        /// <summary>
        /// Clears all source references.
        /// </summary>
        public void ClearAllSources()
        {
            headBinding.source = null;
            leftHandBinding.source = null;
            rightHandBinding.source = null;
            leftLegBinding.source = null;
            rightLegBinding.source = null;
        }

        /// <summary>
        /// Sets all bindings to follow the same target with configurable position/rotation.
        /// </summary>
        public void SetAllBindings(Transform source, bool followPosition = true, bool followRotation = true)
        {
            SetBinding(headBinding, source, followPosition, followRotation);
            SetBinding(leftHandBinding, source, followPosition, followRotation);
            SetBinding(rightHandBinding, source, followPosition, followRotation);
            SetBinding(leftLegBinding, source, followPosition, followRotation);
            SetBinding(rightLegBinding, source, followPosition, followRotation);
        }

        private void SetBinding(ConstraintBinding binding, Transform source, bool position, bool rotation)
        {
            binding.source = source;
            binding.followPosition = position;
            binding.followRotation = rotation;
        }

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos || rigReferences == null) return;

            DrawBindingGizmo(headBinding, rigReferences.HeadTarget, Color.yellow);
            DrawBindingGizmo(leftHandBinding, rigReferences.LeftHandTarget, Color.green);
            DrawBindingGizmo(rightHandBinding, rigReferences.RightHandTarget, Color.green);
            DrawBindingGizmo(leftLegBinding, rigReferences.LeftLegTarget, Color.blue);
            DrawBindingGizmo(rightLegBinding, rigReferences.RightLegTarget, Color.blue);
        }

        private void DrawBindingGizmo(ConstraintBinding binding, Transform target, Color color)
        {
            if (!binding.IsValid || target == null) return;

            Gizmos.color = color;
            Gizmos.DrawLine(binding.source.position, target.position);
            Gizmos.DrawWireSphere(target.position, 0.05f);

            if (binding.followPosition)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(binding.source.position + binding.source.TransformDirection(binding.positionOffset), Vector3.one * 0.03f);
            }
        }
#endif
    }
}
