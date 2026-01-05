namespace WitShells.AnimationRig
{
    using UnityEngine;

    /// <summary>
    /// Main controller for managing multiple constraint targets.
    /// Attach this to your character and assign transform references to make constraints follow targets.
    /// </summary>
    public class ConstraintTargetController : MonoBehaviour
    {
        [Header("Rig References")]
        [SerializeField] private RigReferences rigReferences;

        [Header("Head Constraint")]
        [SerializeField] private ConstraintTarget headTarget = new ConstraintTarget();

        [Header("Hand Constraints")]
        [SerializeField] private ConstraintTarget leftHandTarget = new ConstraintTarget();
        [SerializeField] private ConstraintTarget rightHandTarget = new ConstraintTarget();

        [Header("Leg Constraints")]
        [SerializeField] private ConstraintTarget leftLegTarget = new ConstraintTarget();
        [SerializeField] private ConstraintTarget rightLegTarget = new ConstraintTarget();

        [Header("Settings")]
        [Tooltip("Master enable/disable for all constraints")]
        [SerializeField] private bool enableConstraints = true;

        [Tooltip("Master weight multiplier")]
        [Range(0f, 1f)]
        [SerializeField] private float masterWeight = 1f;

        [Tooltip("When to update constraints")]
        [SerializeField] private UpdateMode updateMode = UpdateMode.LateUpdate;

        public enum UpdateMode
        {
            Update,
            LateUpdate,
            FixedUpdate,
            Manual
        }

        #region Properties

        public bool EnableConstraints
        {
            get => enableConstraints;
            set => enableConstraints = value;
        }

        public float MasterWeight
        {
            get => masterWeight;
            set => masterWeight = Mathf.Clamp01(value);
        }

        public ConstraintTarget HeadTarget => headTarget;
        public ConstraintTarget LeftHandTarget => leftHandTarget;
        public ConstraintTarget RightHandTarget => rightHandTarget;
        public ConstraintTarget LeftLegTarget => leftLegTarget;
        public ConstraintTarget RightLegTarget => rightLegTarget;

        public RigReferences RigReferences => rigReferences;

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

            // Auto-assign targets from rig references
            if (rigReferences != null)
            {
                AutoAssignTargets();
            }
        }

        private void AutoAssignTargets()
        {
            if (headTarget.Target == null && rigReferences.HeadTarget != null)
                headTarget.Target = rigReferences.HeadTarget;

            if (leftHandTarget.Target == null && rigReferences.LeftHandTarget != null)
                leftHandTarget.Target = rigReferences.LeftHandTarget;

            if (rightHandTarget.Target == null && rigReferences.RightHandTarget != null)
                rightHandTarget.Target = rigReferences.RightHandTarget;

            if (leftLegTarget.Target == null && rigReferences.LeftLegTarget != null)
                leftLegTarget.Target = rigReferences.LeftLegTarget;

            if (rightLegTarget.Target == null && rigReferences.RightLegTarget != null)
                rightLegTarget.Target = rigReferences.RightLegTarget;
        }
#endif

        private void Update()
        {
            if (updateMode == UpdateMode.Update)
                UpdateAllConstraints();
        }

        private void LateUpdate()
        {
            if (updateMode == UpdateMode.LateUpdate)
                UpdateAllConstraints();
        }

        private void FixedUpdate()
        {
            if (updateMode == UpdateMode.FixedUpdate)
                UpdateAllConstraints();
        }

        /// <summary>
        /// Call this manually when UpdateMode is set to Manual.
        /// </summary>
        public void UpdateAllConstraints()
        {
            if (!enableConstraints || masterWeight <= 0f) return;

            float deltaTime = GetDeltaTime();

            // Store original weights and apply master weight
            float originalWeight;

            // Update head
            if (headTarget.IsValid)
            {
                originalWeight = headTarget.Weight;
                headTarget.Weight = originalWeight * masterWeight;
                headTarget.UpdateConstraint(deltaTime);
                headTarget.Weight = originalWeight;
                UpdateRigWeight(rigReferences, BodyPart.Head, headTarget);
            }

            // Update hands
            if (leftHandTarget.IsValid)
            {
                originalWeight = leftHandTarget.Weight;
                leftHandTarget.Weight = originalWeight * masterWeight;
                leftHandTarget.UpdateConstraint(deltaTime);
                leftHandTarget.Weight = originalWeight;
                UpdateRigWeight(rigReferences, BodyPart.LeftHand, leftHandTarget);
            }

            if (rightHandTarget.IsValid)
            {
                originalWeight = rightHandTarget.Weight;
                rightHandTarget.Weight = originalWeight * masterWeight;
                rightHandTarget.UpdateConstraint(deltaTime);
                rightHandTarget.Weight = originalWeight;
                UpdateRigWeight(rigReferences, BodyPart.RightHand, rightHandTarget);
            }

            // Update legs
            if (leftLegTarget.IsValid)
            {
                originalWeight = leftLegTarget.Weight;
                leftLegTarget.Weight = originalWeight * masterWeight;
                leftLegTarget.UpdateConstraint(deltaTime);
                leftLegTarget.Weight = originalWeight;
                UpdateRigWeight(rigReferences, BodyPart.LeftLeg, leftLegTarget);
            }

            if (rightLegTarget.IsValid)
            {
                originalWeight = rightLegTarget.Weight;
                rightLegTarget.Weight = originalWeight * masterWeight;
                rightLegTarget.UpdateConstraint(deltaTime);
                rightLegTarget.Weight = originalWeight;
                UpdateRigWeight(rigReferences, BodyPart.RightLeg, rightLegTarget);
            }
        }

        private float GetDeltaTime()
        {
            return updateMode switch
            {
                UpdateMode.FixedUpdate => Time.fixedDeltaTime,
                _ => Time.deltaTime
            };
        }

        public enum BodyPart
        {
            Head,
            LeftHand,
            RightHand,
            LeftLeg,
            RightLeg
        }

        private void UpdateRigWeight(RigReferences refs, BodyPart part, ConstraintTarget constraint)
        {
            if (refs == null) return;

            float weight = constraint.IsValid ? constraint.Weight * masterWeight : 0f;

            switch (part)
            {
                case BodyPart.Head:
                    refs.HeadWeight = weight;
                    break;
                case BodyPart.LeftHand:
                    refs.LeftHandWeight = weight;
                    break;
                case BodyPart.RightHand:
                    refs.RightHandWeight = weight;
                    break;
                case BodyPart.LeftLeg:
                    refs.LeftLegWeight = weight;
                    break;
                case BodyPart.RightLeg:
                    refs.RightLegWeight = weight;
                    break;
            }
        }

        #region Public API

        /// <summary>
        /// Sets the source transform for the head to look at.
        /// </summary>
        public void SetHeadLookAt(Transform lookAtTarget)
        {
            headTarget.Source = lookAtTarget;
        }

        /// <summary>
        /// Sets the source transforms for both hands.
        /// </summary>
        public void SetHandTargets(Transform leftHand, Transform rightHand)
        {
            leftHandTarget.Source = leftHand;
            rightHandTarget.Source = rightHand;
        }

        /// <summary>
        /// Sets the source transforms for both legs/feet.
        /// </summary>
        public void SetLegTargets(Transform leftLeg, Transform rightLeg)
        {
            leftLegTarget.Source = leftLeg;
            rightLegTarget.Source = rightLeg;
        }

        /// <summary>
        /// Enables/disables position constraint for a specific body part.
        /// </summary>
        public void SetPositionConstraint(BodyPart part, bool enabled)
        {
            GetConstraintTarget(part).ConstrainPosition = enabled;
        }

        /// <summary>
        /// Enables/disables rotation constraint for a specific body part.
        /// </summary>
        public void SetRotationConstraint(BodyPart part, bool enabled)
        {
            GetConstraintTarget(part).ConstrainRotation = enabled;
        }

        /// <summary>
        /// Sets the weight for a specific body part constraint.
        /// </summary>
        public void SetWeight(BodyPart part, float weight)
        {
            GetConstraintTarget(part).Weight = weight;
        }

        /// <summary>
        /// Snaps all constraints instantly to their sources.
        /// </summary>
        public void SnapAll()
        {
            headTarget.Snap();
            leftHandTarget.Snap();
            rightHandTarget.Snap();
            leftLegTarget.Snap();
            rightLegTarget.Snap();
        }

        /// <summary>
        /// Resets all constraints.
        /// </summary>
        public void ResetAll()
        {
            headTarget.Reset();
            leftHandTarget.Reset();
            rightHandTarget.Reset();
            leftLegTarget.Reset();
            rightLegTarget.Reset();
        }

        /// <summary>
        /// Clears all source references.
        /// </summary>
        public void ClearAllSources()
        {
            headTarget.Source = null;
            leftHandTarget.Source = null;
            rightHandTarget.Source = null;
            leftLegTarget.Source = null;
            rightLegTarget.Source = null;
        }

        private ConstraintTarget GetConstraintTarget(BodyPart part)
        {
            return part switch
            {
                BodyPart.Head => headTarget,
                BodyPart.LeftHand => leftHandTarget,
                BodyPart.RightHand => rightHandTarget,
                BodyPart.LeftLeg => leftLegTarget,
                BodyPart.RightLeg => rightLegTarget,
                _ => headTarget
            };
        }

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            DrawConstraintGizmo(headTarget, Color.yellow);
            DrawConstraintGizmo(leftHandTarget, Color.green);
            DrawConstraintGizmo(rightHandTarget, Color.green);
            DrawConstraintGizmo(leftLegTarget, Color.cyan);
            DrawConstraintGizmo(rightLegTarget, Color.cyan);
        }

        private void DrawConstraintGizmo(ConstraintTarget constraint, Color color)
        {
            if (!constraint.IsValid) return;

            Gizmos.color = color;
            Gizmos.DrawLine(constraint.Source.position, constraint.Target.position);
            Gizmos.DrawWireSphere(constraint.Target.position, 0.03f);

            // Draw offset indicator
            if (constraint.ConstrainPosition && constraint.PositionOffset != Vector3.zero)
            {
                Gizmos.color = Color.white;
                Vector3 offsetPos = constraint.Source.position + constraint.Source.TransformDirection(constraint.PositionOffset);
                Gizmos.DrawWireCube(offsetPos, Vector3.one * 0.02f);
            }
        }
#endif
    }
}
