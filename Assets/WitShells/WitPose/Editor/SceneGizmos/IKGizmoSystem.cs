using UnityEngine;
using UnityEditor;
using WitShells.WitPose.Editor.Core;

namespace WitShells.WitPose.Editor.SceneGizmos
{
    public class IKGizmoSystem
    {
        private Animator animator;
        private BonePoseSystem bonePoseSystem;
        private MusclePoseSystem musclePoseSystem;
        private WitPoseEditor editorWindow;

        private bool isActive = false;
        private Color ikColor      = new Color(0.2f, 1f,   0.2f, 0.9f); // Green – inactive handle
        private Color activeColor  = new Color(1f,   0.8f, 0.1f, 1f);   // Yellow – active / dragging
        private float gizmoSize    = 0.06f;

        // ── IK end-effector targets ──────────────────────────────────────────
        private Vector3 leftHandTarget, rightHandTarget;
        private Vector3 leftFootTarget, rightFootTarget;
        private bool isLeftHandActive, isRightHandActive;
        private bool isLeftFootActive, isRightFootActive;

        // ── Persistent pole/hint targets (elbow/knee) ────────────────────────
        // Stored once when the chain is activated; user can move them freely.
        private Vector3 leftHandHint, rightHandHint;
        private Vector3 leftFootHint, rightFootHint;

        public bool IsActive => isActive;

        public IKGizmoSystem(Animator animator, BonePoseSystem bonePoseSystem,
                             MusclePoseSystem musclePoseSystem, WitPoseEditor editorWindow)
        {
            this.animator         = animator;
            this.bonePoseSystem   = bonePoseSystem;
            this.musclePoseSystem = musclePoseSystem;
            this.editorWindow     = editorWindow;
        }

        public void Activate()
        {
            if (isActive) return;
            isActive = true;
            SceneView.duringSceneGui += OnSceneGUI;
            SyncTargetsFromSkeleton();
        }

        public void Deactivate()
        {
            if (!isActive) return;
            isActive = false;
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        public void SyncTargetsFromSkeleton()
        {
            if (animator == null) return;

            CacheTarget(HumanBodyBones.LeftHand,  ref leftHandTarget,  HumanBodyBones.LeftLowerArm,  "left_hand");
            CacheTarget(HumanBodyBones.RightHand, ref rightHandTarget, HumanBodyBones.RightLowerArm, "right_hand");
            CacheTarget(HumanBodyBones.LeftFoot,  ref leftFootTarget,  HumanBodyBones.LeftLowerLeg,  "left_foot");
            CacheTarget(HumanBodyBones.RightFoot, ref rightFootTarget, HumanBodyBones.RightLowerLeg, "right_foot");

            // Initialise hint (pole) positions from actual mid-bone positions
            // Left/right arms get lateral offsets so elbows bend outward correctly
            SetHintFromBone(HumanBodyBones.LeftLowerArm,  ref leftHandHint,  isArm: true,  isLeft: true);
            SetHintFromBone(HumanBodyBones.RightLowerArm, ref rightHandHint, isArm: true,  isLeft: false);
            SetHintFromBone(HumanBodyBones.LeftLowerLeg,  ref leftFootHint,  isArm: false, isLeft: true);
            SetHintFromBone(HumanBodyBones.RightLowerLeg, ref rightFootHint, isArm: false, isLeft: false);
        }

        private void CacheTarget(HumanBodyBones tipBone, ref Vector3 target,
                                  HumanBodyBones fallback, string tag)
        {
            Transform t = animator.GetBoneTransform(tipBone) ??
                          animator.GetBoneTransform(fallback);
            if (t != null) target = t.position;
        }

        /// <summary>
        /// Seeds the pole/hint position from the mid-bone world position with a
        /// limb-appropriate offset so the elbow/knee starts pointing the right way.
        ///
        /// Arms:  offset outward (left = -right, right = +right) + slightly backward
        /// Legs:  offset forward (knees naturally face forward)
        /// </summary>
        private void SetHintFromBone(HumanBodyBones midBone, ref Vector3 hint, bool isArm, bool isLeft)
        {
            Transform t = animator.GetBoneTransform(midBone);
            if (t == null) return;

            Transform tr = animator.transform;
            Vector3 offset;

            if (isArm)
            {
                // Lateral: left elbow goes left (-right), right elbow goes right (+right)
                Vector3 lateral  = isLeft ? -tr.right : tr.right;
                // Backward: elbows point slightly behind the character
                Vector3 backward = -tr.forward;
                offset = lateral * 0.5f + backward * 0.3f;
            }
            else
            {
                // Knees point forward for both legs
                offset = tr.forward * 0.5f;
            }

            hint = t.position + offset;
        }

        // ─────────────────────────────────────────────────────────────────────
        private void OnSceneGUI(SceneView sceneView)
        {
            if (animator == null) return;

            DrawIKChain(HumanBodyBones.LeftUpperArm,  HumanBodyBones.LeftLowerArm,  HumanBodyBones.LeftHand,
                        ref leftHandTarget,  ref leftHandHint,  ref isLeftHandActive,
                        "Left Hand IK", "L.Elbow", isLeft: true,  isArm: true);
            DrawIKChain(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand,
                        ref rightHandTarget, ref rightHandHint, ref isRightHandActive,
                        "Right Hand IK", "R.Elbow", isLeft: false, isArm: true);
            DrawIKChain(HumanBodyBones.LeftUpperLeg,  HumanBodyBones.LeftLowerLeg,  HumanBodyBones.LeftFoot,
                        ref leftFootTarget,  ref leftFootHint,  ref isLeftFootActive,
                        "Left Foot IK",  "L.Knee",  isLeft: true,  isArm: false);
            DrawIKChain(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot,
                        ref rightFootTarget, ref rightFootHint, ref isRightFootActive,
                        "Right Foot IK", "R.Knee",  isLeft: false, isArm: false);
        }

        private void DrawIKChain(
            HumanBodyBones rootBone, HumanBodyBones midBone, HumanBodyBones tipBone,
            ref Vector3 targetPos, ref Vector3 hintPos,
            ref bool isActive, string label, string hintLabel, bool isLeft, bool isArm)
        {
            Transform root = animator.GetBoneTransform(rootBone);
            Transform mid  = animator.GetBoneTransform(midBone);
            Transform tip  = animator.GetBoneTransform(tipBone);
            if (root == null || mid == null || tip == null) return;

            // If chain is inactive keep target snapped to current tip world pos
            if (!isActive)
            {
                targetPos = tip.position;
            }

            // ── Pole/hint handle — ALWAYS visible so user can fix bending any time ──
            DrawHintHandle(ref hintPos, mid, root, tip, targetPos,
                           rootBone, midBone, tipBone,
                           hintLabel, isActive, midBone, isArm, isLeft);

            // ── End-effector sphere ──────────────────────────────────────────
            float handleSize = HandleUtility.GetHandleSize(targetPos) * gizmoSize;
            Handles.color = isActive ? activeColor : ikColor;
            if (Handles.Button(targetPos, Quaternion.identity,
                               handleSize, handleSize, Handles.SphereHandleCap))
            {
                isActive = !isActive;
                if (isActive)
                {
                    targetPos = tip.position;
                    // Re-seed hint using correct side offset
                    SetHintFromBone(midBone, ref hintPos, isArm, isLeft);
                }
            }

            if (!isActive) return;

            Handles.Label(targetPos + Vector3.up * handleSize * 2.5f, label, EditorStyles.boldLabel);

            // ── Draggable end-effector position handle ───────────────────────
            EditorGUI.BeginChangeCheck();
            Vector3 newTarget = Handles.PositionHandle(targetPos, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(new Object[] { root, mid, tip }, "IK Move");
                targetPos = newTarget;
                ApplyIK(root, mid, tip, targetPos, hintPos, rootBone, midBone, tipBone);
            }
        }

        private void DrawHintHandle(
            ref Vector3 hintPos, Transform mid,
            Transform root, Transform tip, Vector3 targetPos,
            HumanBodyBones rootBone, HumanBodyBones midBone, HumanBodyBones tipBone,
            string hintLabel, bool chainActive,
            HumanBodyBones seedBone, bool isArm, bool isLeft)
        {
            float hintSize = HandleUtility.GetHandleSize(hintPos) * gizmoSize * 0.55f;

            // Cyan when chain active, dimmer when inactive but still grabbable
            Color hintColor = chainActive
                ? new Color(0.3f, 0.9f, 1f, 1f)
                : new Color(0.3f, 0.9f, 1f, 0.45f);

            Handles.color = hintColor;

            // Dotted line from mid-bone to hint at all times
            Handles.DrawDottedLine(mid.position, hintPos, 4f);

            // Label next to the hint dot
            Handles.Label(hintPos + Vector3.up * hintSize * 1.5f,
                          hintLabel, EditorStyles.miniLabel);

            EditorGUI.BeginChangeCheck();
            Vector3 newHint = Handles.FreeMoveHandle(
                hintPos, hintSize, Vector3.zero, Handles.SphereHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(new Object[] { root, mid, tip }, $"IK Hint Move ({hintLabel})");
                hintPos = newHint;
                if (chainActive)
                    ApplyIK(root, mid, tip, targetPos, hintPos, rootBone, midBone, tipBone);
            }
        }

        private void ApplyIK(Transform root, Transform mid, Transform tip,
                             Vector3 targetPos, Vector3 hintPos,
                             HumanBodyBones rootBone, HumanBodyBones midBone, HumanBodyBones tipBone)
        {
            // Solve at full weight — the rewritten solver is stable each frame
            TwoBoneIKSolver.Solve(root, mid, tip, targetPos, hintPos, weight: 1f);

            // Push result back into the pose systems
            bonePoseSystem.SetBoneRotation(rootBone, root.localRotation, false);
            bonePoseSystem.SetBoneRotation(midBone,  mid.localRotation,  false);
            bonePoseSystem.SetBoneRotation(tipBone,  tip.localRotation,  false);
            bonePoseSystem.CommitPose();

            musclePoseSystem.SyncFromSkeleton();

            if (editorWindow != null && editorWindow.EnableMuscleTracking)
                editorWindow.RecordAllMusclesAtCurrentTime();

            SceneView.RepaintAll();
        }
    }
}