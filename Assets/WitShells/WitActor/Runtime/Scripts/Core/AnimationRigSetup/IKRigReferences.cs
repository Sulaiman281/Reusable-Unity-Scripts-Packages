namespace WitShells.WitActor
{
    using UnityEngine;
    using UnityEngine.Animations.Rigging;

    public class IKRigReferences : MonoBehaviour
    {
        [Header("Aim Constraints")]
        [SerializeField] private MultiAimConstraint head;
        [SerializeField] private MultiAimConstraint leftHand;
        [SerializeField] private MultiAimConstraint rightHand;


        [Header("Two Bone IK Constraints")]
        [SerializeField] private TwoBoneIKConstraint leftArm;
        [SerializeField] private TwoBoneIKConstraint rightArm;
        [SerializeField] private TwoBoneIKConstraint leftLeg;
        [SerializeField]
        private TwoBoneIKConstraint rightLeg;

#if UNITY_EDITOR
        private void OnValidate()
        {

            {
                // Auto-assign constraints if any are null
                if (head == null)
                    head = FindConstraint<MultiAimConstraint>("Head");
                if (leftHand == null)
                    leftHand = FindConstraint<MultiAimConstraint>("LeftHand");
                if (rightHand == null)
                    rightHand = FindConstraint<MultiAimConstraint>("RightHand");
                if (leftArm == null)
                    leftArm = FindConstraint<TwoBoneIKConstraint>("LeftArm");
                if (rightArm == null)
                    rightArm = FindConstraint<TwoBoneIKConstraint>("RightArm");
                if (leftLeg == null)
                    leftLeg = FindConstraint<TwoBoneIKConstraint>("LeftLeg");
                if (rightLeg == null)
                    rightLeg = FindConstraint<TwoBoneIKConstraint>("RightLeg");
            }
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

        // Weight properties
        public float HeadWeight
        {
            get => head.weight;
            set => head.weight = value;
        }

        public float LeftHandWeight
        {
            get => leftHand.weight;
            set => leftHand.weight = value;
        }

        public float RightHandWeight
        {
            get => rightHand.weight;
            set => rightHand.weight = value;
        }

        public float LeftArmWeight
        {
            get => leftArm.weight;
            set => leftArm.weight = value;
        }

        public float RightArmWeight
        {
            get => rightArm.weight;
            set => rightArm.weight = value;
        }

        public float LeftLegWeight
        {
            get => leftLeg.weight;
            set => leftLeg.weight = value;
        }

        public float RightLegWeight
        {
            get => rightLeg.weight;
            set => rightLeg.weight = value;
        }

        public Transform Head => head.data.sourceObjects.GetTransform(0);
        public Transform LeftHand => leftHand.data.sourceObjects.GetTransform(0);
        public Transform RightHand => rightHand.data.sourceObjects.GetTransform(0);

        public Transform LeftArmTarget => leftArm.data.target;
        public Transform LeftArmHint => leftArm.data.hint;
        public Transform RightArmTarget => rightArm.data.target;
        public Transform RightArmHint => rightArm.data.hint;
        public Transform LeftLegTarget => leftLeg.data.target;
        public Transform LeftLegHint => leftLeg.data.hint;
        public Transform RightLegTarget => rightLeg.data.target;
        public Transform RightLegHint => rightLeg.data.hint;

        public Transform LeftFeet => leftLeg.data.tip;
        public Transform RightFeet => rightLeg.data.tip;
    }

}