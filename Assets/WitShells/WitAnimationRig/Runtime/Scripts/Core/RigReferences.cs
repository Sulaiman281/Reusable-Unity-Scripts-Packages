namespace WitShells.AnimationRig
{
    using UnityEngine;
    using UnityEngine.Animations.Rigging;

    /// <summary>
    /// Holds references to all IK constraints and provides easy access to targets and weights.
    /// Attach this component to the Rig GameObject.
    /// </summary>
    public class RigReferences : MonoBehaviour
    {
        [Header("Head Constraint")]
        [SerializeField] private MultiAimConstraint headAim;

        [Header("Hand Constraints")]
        [SerializeField] private TwoBoneIKConstraint leftHandIK;
        [SerializeField] private TwoBoneIKConstraint rightHandIK;

        [Header("Leg Constraints")]
        [SerializeField] private TwoBoneIKConstraint leftLegIK;
        [SerializeField] private TwoBoneIKConstraint rightLegIK;

#if UNITY_EDITOR
        private void OnValidate()
        {
            AutoAssignConstraints();
        }

        private void AutoAssignConstraints()
        {
            if (headAim == null)
                headAim = FindConstraint<MultiAimConstraint>("Head");
            if (leftHandIK == null)
                leftHandIK = FindConstraint<TwoBoneIKConstraint>("LeftHand");
            if (rightHandIK == null)
                rightHandIK = FindConstraint<TwoBoneIKConstraint>("RightHand");
            if (leftLegIK == null)
                leftLegIK = FindConstraint<TwoBoneIKConstraint>("LeftLeg");
            if (rightLegIK == null)
                rightLegIK = FindConstraint<TwoBoneIKConstraint>("RightLeg");
        }

        private T FindConstraint<T>(string prefix) where T : Component
        {
            foreach (var c in GetComponentsInChildren<T>(true))
            {
                if (c.name.StartsWith(prefix))
                    return c;
            }
            return null;
        }
#endif

        #region Constraint Accessors

        public MultiAimConstraint HeadAimConstraint => headAim;
        public TwoBoneIKConstraint LeftHandIKConstraint => leftHandIK;
        public TwoBoneIKConstraint RightHandIKConstraint => rightHandIK;
        public TwoBoneIKConstraint LeftLegIKConstraint => leftLegIK;
        public TwoBoneIKConstraint RightLegIKConstraint => rightLegIK;

        #endregion

        #region Weight Properties

        public float HeadWeight
        {
            get => headAim != null ? headAim.weight : 0f;
            set { if (headAim != null) headAim.weight = value; }
        }

        public float LeftHandWeight
        {
            get => leftHandIK != null ? leftHandIK.weight : 0f;
            set { if (leftHandIK != null) leftHandIK.weight = value; }
        }

        public float RightHandWeight
        {
            get => rightHandIK != null ? rightHandIK.weight : 0f;
            set { if (rightHandIK != null) rightHandIK.weight = value; }
        }

        public float LeftLegWeight
        {
            get => leftLegIK != null ? leftLegIK.weight : 0f;
            set { if (leftLegIK != null) leftLegIK.weight = value; }
        }

        public float RightLegWeight
        {
            get => rightLegIK != null ? rightLegIK.weight : 0f;
            set { if (rightLegIK != null) rightLegIK.weight = value; }
        }

        #endregion

        #region Target Transforms

        public Transform HeadTarget => headAim?.data.sourceObjects.Count > 0 
            ? headAim.data.sourceObjects.GetTransform(0) : null;

        public Transform LeftHandTarget => leftHandIK?.data.target;
        public Transform LeftHandHint => leftHandIK?.data.hint;
        public Transform LeftHandTip => leftHandIK?.data.tip;

        public Transform RightHandTarget => rightHandIK?.data.target;
        public Transform RightHandHint => rightHandIK?.data.hint;
        public Transform RightHandTip => rightHandIK?.data.tip;

        public Transform LeftLegTarget => leftLegIK?.data.target;
        public Transform LeftLegHint => leftLegIK?.data.hint;
        public Transform LeftFootTip => leftLegIK?.data.tip;

        public Transform RightLegTarget => rightLegIK?.data.target;
        public Transform RightLegHint => rightLegIK?.data.hint;
        public Transform RightFootTip => rightLegIK?.data.tip;

        #endregion

        #region Utility Methods

        /// <summary>
        /// Sets all constraint weights to the specified value.
        /// </summary>
        public void SetAllWeights(float weight)
        {
            HeadWeight = weight;
            LeftHandWeight = weight;
            RightHandWeight = weight;
            LeftLegWeight = weight;
            RightLegWeight = weight;
        }

        /// <summary>
        /// Resets all constraint weights to zero.
        /// </summary>
        public void ResetAllWeights()
        {
            SetAllWeights(0f);
        }

        /// <summary>
        /// Checks if all required constraints are assigned.
        /// </summary>
        public bool IsValid => headAim != null || leftHandIK != null || rightHandIK != null ||
                               leftLegIK != null || rightLegIK != null;

        #endregion
    }
}
