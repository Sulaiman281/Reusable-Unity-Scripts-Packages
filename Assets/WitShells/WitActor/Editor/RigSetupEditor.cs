namespace WitShells.WitActor
{
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Animations.Rigging;

    public class RigSetupEditor
    {
        [MenuItem("WitShells/Actor/RigSetup")]
        public static void CreateRigSetup()
        {
            if (Selection.activeGameObject == null)
            {
                Debug.LogWarning("No GameObject selected.");
                return;
            }

            var actor = Selection.activeGameObject;

            // Check for Animator
            var animator = actor.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("Selected GameObject must have an Animator component.");
                return;
            }

            // Add RigBuilder if missing
            var rigBuilder = actor.GetComponent<RigBuilder>();
            if (rigBuilder == null)
                rigBuilder = actor.AddComponent<RigBuilder>();

            // Create Rig parent
            var rigGO = new GameObject("Rig");
            rigGO.transform.SetParent(actor.transform);
            rigGO.transform.localPosition = Vector3.zero;
            rigGO.transform.localRotation = Quaternion.identity;
            rigGO.transform.localScale = Vector3.one;

            var rig = rigGO.AddComponent<Rig>();

            // Add rig to RigBuilder layers
            if (rigBuilder.layers == null)
                rigBuilder.layers = new System.Collections.Generic.List<RigLayer>();
            rigBuilder.layers.Add(new RigLayer(rig));

            // Find bones by name
            var head = FindByContains(actor.transform, "Head");
            var leftHand = FindByContains(actor.transform, "LeftHand");
            var rightHand = FindByContains(actor.transform, "RightHand");
            var leftLeg = FindByContains(actor.transform, "LeftFoot");
            var rightLeg = FindByContains(actor.transform, "RightFoot");

            // Head constraint
            if (head != null)
            {
                var headObj = new GameObject("HeadConstraint");
                headObj.transform.SetParent(rigGO.transform);
                headObj.transform.position = head.position;
                headObj.transform.rotation = head.rotation;

                // Create target child
                var headTarget = new GameObject("HeadTarget");
                headTarget.transform.SetParent(headObj.transform);
                headTarget.transform.position = head.position;
                headTarget.transform.rotation = head.rotation;

                var aim = headObj.AddComponent<MultiAimConstraint>();
                aim.data.constrainedObject = head;
                aim.data.sourceObjects.Add(new WeightedTransform(headTarget.transform, 1f));
            }

            // Helper for IK constraints
            void CreateIKConstraint(string name, Transform tip)
            {
                if (tip == null) return;

                var ikObj = new GameObject(name + "Constraint");
                ikObj.transform.SetParent(rigGO.transform);
                ikObj.transform.position = tip.position;
                ikObj.transform.rotation = tip.rotation;

                // Target child
                var target = new GameObject(name + "Target");
                target.transform.SetParent(ikObj.transform);
                target.transform.position = tip.position;
                target.transform.rotation = tip.rotation;

                // Hint child (placed between tip and root for demo)
                var root = tip.parent != null ? tip.parent.parent : null;
                Vector3 hintPos = tip.position;
                if (root != null)
                    hintPos = Vector3.Lerp(tip.position, root.position, 0.5f);

                var hint = new GameObject(name + "Hint");
                hint.transform.SetParent(ikObj.transform);
                hint.transform.position = hintPos;
                hint.transform.rotation = tip.rotation;

                var ik = ikObj.AddComponent<TwoBoneIKConstraint>();
                ik.data.tip = tip;
                ik.data.mid = tip.parent;
                ik.data.root = root;
                ik.data.target = target.transform;
                ik.data.hint = hint.transform;
            }

            // Create IK constraints for hands and legs
            CreateIKConstraint("LeftHand", leftHand);
            CreateIKConstraint("RightHand", rightHand);
            CreateIKConstraint("LeftLeg", leftLeg);
            CreateIKConstraint("RightLeg", rightLeg);

            Debug.Log("Rig setup created with targets and hints under: " + actor.name);
        }

        private static Transform FindByContains(Transform root, string keyword)
        {
            if (root.name.Contains(keyword))
                return root;

            foreach (Transform child in root)
            {
                var found = FindByContains(child, keyword);
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}