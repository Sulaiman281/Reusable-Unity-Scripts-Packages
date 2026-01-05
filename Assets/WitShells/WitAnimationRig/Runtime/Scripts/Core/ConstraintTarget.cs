namespace WitShells.AnimationRig
{
    using UnityEngine;
    using System;

    /// <summary>
    /// Represents a single constraint target that can be assigned to follow a source transform.
    /// Provides fine-grained control over position and rotation constraints.
    /// </summary>
    [Serializable]
    public class ConstraintTarget
    {
        [Header("Target Configuration")]
        [Tooltip("The IK target transform to control")]
        [SerializeField] private Transform target;

        [Tooltip("The source transform to follow")]
        [SerializeField] private Transform source;

        [Header("Position Constraint")]
        [SerializeField] private bool constrainPosition = true;
        [SerializeField] private bool constrainPositionX = true;
        [SerializeField] private bool constrainPositionY = true;
        [SerializeField] private bool constrainPositionZ = true;
        [SerializeField] private Vector3 positionOffset;

        [Header("Rotation Constraint")]
        [SerializeField] private bool constrainRotation = true;
        [SerializeField] private bool constrainRotationX = true;
        [SerializeField] private bool constrainRotationY = true;
        [SerializeField] private bool constrainRotationZ = true;
        [SerializeField] private Vector3 rotationOffset;

        [Header("Interpolation")]
        [Range(0f, 1f)]
        [SerializeField] private float weight = 1f;
        [Range(0f, 50f)]
        [SerializeField] private float positionSmoothSpeed = 15f;
        [Range(0f, 50f)]
        [SerializeField] private float rotationSmoothSpeed = 15f;

        // Runtime cached values
        private Vector3 smoothedPosition;
        private Quaternion smoothedRotation;
        private bool initialized;

        #region Properties

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        public Transform Source
        {
            get => source;
            set => source = value;
        }

        public float Weight
        {
            get => weight;
            set => weight = Mathf.Clamp01(value);
        }

        public bool ConstrainPosition
        {
            get => constrainPosition;
            set => constrainPosition = value;
        }

        public bool ConstrainRotation
        {
            get => constrainRotation;
            set => constrainRotation = value;
        }

        public Vector3 PositionOffset
        {
            get => positionOffset;
            set => positionOffset = value;
        }

        public Vector3 RotationOffset
        {
            get => rotationOffset;
            set => rotationOffset = value;
        }

        public bool IsValid => target != null && source != null && weight > 0f;

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the constraint target to follow the source.
        /// Call this in Update or LateUpdate.
        /// </summary>
        public void UpdateConstraint(float deltaTime)
        {
            if (!IsValid) return;

            if (!initialized)
            {
                Initialize();
            }

            // Calculate target position
            if (constrainPosition)
            {
                Vector3 targetPos = source.position + source.TransformDirection(positionOffset);
                Vector3 currentPos = target.position;

                // Apply axis constraints
                if (!constrainPositionX) targetPos.x = currentPos.x;
                if (!constrainPositionY) targetPos.y = currentPos.y;
                if (!constrainPositionZ) targetPos.z = currentPos.z;

                // Smooth interpolation
                if (positionSmoothSpeed > 0f)
                {
                    smoothedPosition = Vector3.Lerp(smoothedPosition, targetPos, deltaTime * positionSmoothSpeed);
                }
                else
                {
                    smoothedPosition = targetPos;
                }

                // Apply with weight
                target.position = Vector3.Lerp(target.position, smoothedPosition, weight);
            }

            // Calculate target rotation
            if (constrainRotation)
            {
                Quaternion targetRot = source.rotation * Quaternion.Euler(rotationOffset);
                Vector3 currentEuler = target.eulerAngles;
                Vector3 targetEuler = targetRot.eulerAngles;

                // Apply axis constraints
                if (!constrainRotationX) targetEuler.x = currentEuler.x;
                if (!constrainRotationY) targetEuler.y = currentEuler.y;
                if (!constrainRotationZ) targetEuler.z = currentEuler.z;

                targetRot = Quaternion.Euler(targetEuler);

                // Smooth interpolation
                if (rotationSmoothSpeed > 0f)
                {
                    smoothedRotation = Quaternion.Slerp(smoothedRotation, targetRot, deltaTime * rotationSmoothSpeed);
                }
                else
                {
                    smoothedRotation = targetRot;
                }

                // Apply with weight
                target.rotation = Quaternion.Slerp(target.rotation, smoothedRotation, weight);
            }
        }

        /// <summary>
        /// Instantly snaps the target to the source position/rotation.
        /// </summary>
        public void Snap()
        {
            if (!IsValid) return;

            if (constrainPosition)
            {
                Vector3 targetPos = source.position + source.TransformDirection(positionOffset);
                Vector3 currentPos = target.position;

                if (!constrainPositionX) targetPos.x = currentPos.x;
                if (!constrainPositionY) targetPos.y = currentPos.y;
                if (!constrainPositionZ) targetPos.z = currentPos.z;

                target.position = targetPos;
                smoothedPosition = targetPos;
            }

            if (constrainRotation)
            {
                Quaternion targetRot = source.rotation * Quaternion.Euler(rotationOffset);
                Vector3 currentEuler = target.eulerAngles;
                Vector3 targetEuler = targetRot.eulerAngles;

                if (!constrainRotationX) targetEuler.x = currentEuler.x;
                if (!constrainRotationY) targetEuler.y = currentEuler.y;
                if (!constrainRotationZ) targetEuler.z = currentEuler.z;

                target.rotation = Quaternion.Euler(targetEuler);
                smoothedRotation = target.rotation;
            }

            initialized = true;
        }

        /// <summary>
        /// Resets the constraint to default state.
        /// </summary>
        public void Reset()
        {
            initialized = false;
        }

        private void Initialize()
        {
            if (target != null)
            {
                smoothedPosition = target.position;
                smoothedRotation = target.rotation;
            }
            initialized = true;
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a position-only constraint.
        /// </summary>
        public static ConstraintTarget CreatePositionConstraint(Transform target, Transform source, Vector3 offset = default)
        {
            return new ConstraintTarget
            {
                target = target,
                source = source,
                constrainPosition = true,
                constrainRotation = false,
                positionOffset = offset
            };
        }

        /// <summary>
        /// Creates a rotation-only constraint.
        /// </summary>
        public static ConstraintTarget CreateRotationConstraint(Transform target, Transform source, Vector3 offset = default)
        {
            return new ConstraintTarget
            {
                target = target,
                source = source,
                constrainPosition = false,
                constrainRotation = true,
                rotationOffset = offset
            };
        }

        /// <summary>
        /// Creates a full position and rotation constraint.
        /// </summary>
        public static ConstraintTarget CreateFullConstraint(Transform target, Transform source)
        {
            return new ConstraintTarget
            {
                target = target,
                source = source,
                constrainPosition = true,
                constrainRotation = true
            };
        }

        #endregion
    }
}
