using UnityEngine;

namespace WitShells.WitPose.Editor.Core
{
    /// <summary>
    /// Two-Bone IK solver that mirrors Unity Animation Rigging behaviour.
    /// Solves entirely from first principles each frame — no incremental delta
    /// accumulation — so it never drifts or glitches.
    ///
    /// Algorithm:
    ///   1. Compute desired mid-joint position via Law of Cosines.
    ///   2. Use the hint (pole vector) to define the bend plane.
    ///   3. RotateRootToward desiredMid, then RotateMidToward target.
    ///   4. Blend by weight.
    /// </summary>
    public static class TwoBoneIKSolver
    {
        public static void Solve(
            Transform root,
            Transform mid,
            Transform tip,
            Vector3 targetPosition,
            Vector3 hintPosition,
            float weight = 1f)
        {
            if (root == null || mid == null || tip == null) return;
            if (weight <= 0f) return;

            // ── Cache source rotations for weight blending ──────────────────
            Quaternion srcRoot = root.rotation;
            Quaternion srcMid  = mid.rotation;

            // ── Bone-chain lengths (from current rest positions) ─────────────
            Vector3 aPos = root.position;
            Vector3 bPos = mid.position;
            Vector3 cPos = tip.position;

            float lenAB = (bPos - aPos).magnitude;
            float lenBC = (cPos - bPos).magnitude;

            if (lenAB < 0.00001f || lenBC < 0.00001f) return;

            // ── Clamp target inside reach ────────────────────────────────────
            Vector3 aToTarget = targetPosition - aPos;
            float   distAT    = aToTarget.magnitude;

            if (distAT < 0.00001f) return;

            float   maxReach  = lenAB + lenBC - 0.0001f;
            Vector3 targetDir = aToTarget.normalized;

            if (distAT > maxReach)
            {
                targetPosition = aPos + targetDir * maxReach;
                distAT = maxReach;
            }

            // ── Law of Cosines: angle at root joint ──────────────────────────
            // Triangle sides:  c = lenAB (root→mid),
            //                  a = lenBC (mid→tip),
            //                  b = distAT (root→target)
            // cos(A) = (b² + c² - a²) / (2·b·c)
            float cosA   = (distAT * distAT + lenAB * lenAB - lenBC * lenBC)
                           / (2f * distAT * lenAB);
            float angleA  = Mathf.Acos(Mathf.Clamp(cosA, -1f, 1f)) * Mathf.Rad2Deg;

            // ── Bend plane defined by pole/hint vector ───────────────────────
            Vector3 hintDir = (hintPosition - aPos);
            if (hintDir.sqrMagnitude < 0.0001f) hintDir = root.up;
            hintDir.Normalize();

            // Normal to the bend plane
            Vector3 bendNormal = Vector3.Cross(targetDir, hintDir);
            if (bendNormal.sqrMagnitude < 0.0001f)
            {
                bendNormal = Vector3.Cross(targetDir, root.up);
                if (bendNormal.sqrMagnitude < 0.0001f)
                    bendNormal = Vector3.Cross(targetDir, Vector3.right);
            }
            bendNormal.Normalize();

            // In-plane axis perpendicular to target direction
            Vector3 bendAxis = Vector3.Cross(bendNormal, targetDir).normalized;

            // ── Desired mid-joint world position ─────────────────────────────
            // Rotate targetDir by -(angleA) around bendAxis to get direction of AB
            Vector3 desiredMidDir = Quaternion.AngleAxis(-angleA, bendAxis) * targetDir;
            Vector3 desiredMidPos = aPos + desiredMidDir.normalized * lenAB;

            // ── Rotate root so AB points toward desiredMidPos ────────────────
            Vector3 curMidDir  = (bPos - aPos).normalized;
            Vector3 wantMidDir = (desiredMidPos - aPos).normalized;

            if (Vector3.Dot(curMidDir, wantMidDir) < 0.99999f)
            {
                root.rotation = Quaternion.FromToRotation(curMidDir, wantMidDir) * root.rotation;
            }

            // ── Rotate mid so BC points toward target ────────────────────────
            // Read mid-bone tip position AFTER root was rotated
            Vector3 curTipDir  = (tip.position - mid.position).normalized;
            Vector3 wantTipDir = (targetPosition - mid.position).normalized;

            if (Vector3.Dot(curTipDir, wantTipDir) < 0.99999f)
            {
                mid.rotation = Quaternion.FromToRotation(curTipDir, wantTipDir) * mid.rotation;
            }

            // ── Apply weight blend ───────────────────────────────────────────
            if (weight < 1f)
            {
                root.rotation = Quaternion.Slerp(srcRoot, root.rotation, weight);
                mid.rotation  = Quaternion.Slerp(srcMid,  mid.rotation,  weight);
            }
        }
    }
}