using UnityEngine;

namespace WitShells.WitPose
{
    /// <summary>
    /// Anatomical joint-limit data for all 95 Unity Humanoid muscle channels (indices 0-94).
    ///
    /// Each entry maps a muscle index to:
    ///   • Min / Max  — realistic anatomical range in Unity's normalized muscle space (-1 to 1)
    ///   • Neutral    — natural rest-pose value (almost always 0)
    ///   • Label      — compact human-readable description of what the muscle controls
    ///   • Group      — anatomical group name (e.g. "Spine", "Left Arm", "Left Fingers")
    ///
    /// Index → muscle mapping (sourced from Unity HumanTrait / InitializeBoneToMuscleMapping):
    ///   0-2   Spine       3-5   Chest       6-8   Upper Chest
    ///   9-11  Neck        12-14 Head        15-20 Face (eyes, jaw)
    ///   21-28 Left Leg    29-36 Right Leg
    ///   37-38 L Shoulder  39-41 L Upper Arm  42-43 L Forearm  44-45 L Wrist
    ///   46-47 R Shoulder  48-50 R Upper Arm  51-52 R Forearm  53-54 R Wrist
    ///   55-74 Left Fingers (Thumb 55-58, Index 59-62, Middle 63-66, Ring 67-70, Little 71-74)
    ///   75-94 Right Fingers (Thumb 75-78, Index 79-82, Middle 83-86, Ring 87-90, Little 91-94)
    ///
    /// Available at runtime and in the editor — just add "using WitShells.WitPose;" to any file.
    /// </summary>
    public static class HumanMuscleJointLimits
    {
        // ─────────────────────────────────────────────────────────────────────
        // Data structure
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Anatomical limit definition for a single Unity Humanoid muscle channel.
        /// </summary>
        public readonly struct MuscleLimit
        {
            /// <summary>Anatomical minimum in Unity's normalized muscle space (-1..1).</summary>
            public readonly float Min;

            /// <summary>Anatomical maximum in Unity's normalized muscle space (-1..1).</summary>
            public readonly float Max;

            /// <summary>Natural rest / neutral value (0 for almost all muscles).</summary>
            public readonly float Neutral;

            /// <summary>Short semantic label describing what the muscle controls.</summary>
            public readonly string Label;

            /// <summary>Anatomical group name (e.g. "Spine", "Left Arm", "Left Fingers").</summary>
            public readonly string Group;

            public MuscleLimit(float min, float max, float neutral, string label, string group = "")
            {
                Min     = min;
                Max     = max;
                Neutral = neutral;
                Label   = label;
                Group   = group;
            }

            /// <summary>Clamp <paramref name="value"/> to this joint's anatomical range.</summary>
            public float Clamp(float value) => Mathf.Clamp(value, Min, Max);

            /// <summary>Remap <paramref name="value"/> from [Min..Max] to [0..1].</summary>
            public float Normalize(float value) =>
                Max > Min ? Mathf.InverseLerp(Min, Max, value) : 0f;

            /// <summary>Remap a [0..1] t-value back to [Min..Max].</summary>
            public float Lerp(float t) => Mathf.Lerp(Min, Max, t);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Constants & lookup
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Total number of entries — matches Unity's HumanTrait.MuscleCount (95).
        /// </summary>
        public const int MuscleCount = 95;

        private static readonly MuscleLimit[] s_limits = BuildLimits();

        // ─────────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the <see cref="MuscleLimit"/> for a muscle index.
        /// Out-of-range indices return a full-range fallback (-1..1).
        /// </summary>
        public static MuscleLimit Get(int muscleIndex)
        {
            if (muscleIndex < 0 || muscleIndex >= s_limits.Length)
                return new MuscleLimit(-1f, 1f, 0f, $"Muscle {muscleIndex}", "Unknown");
            return s_limits[muscleIndex];
        }

        /// <summary>Anatomical minimum for this muscle (normalized -1..1 space).</summary>
        public static float GetMin(int muscleIndex)     => Get(muscleIndex).Min;

        /// <summary>Anatomical maximum for this muscle (normalized -1..1 space).</summary>
        public static float GetMax(int muscleIndex)     => Get(muscleIndex).Max;

        /// <summary>Natural rest / neutral value for this muscle (usually 0).</summary>
        public static float GetNeutral(int muscleIndex) => Get(muscleIndex).Neutral;

        /// <summary>Compact semantic label describing what this muscle controls.</summary>
        public static string GetLabel(int muscleIndex)  => Get(muscleIndex).Label;

        /// <summary>Anatomical group this muscle belongs to (e.g. "Left Arm").</summary>
        public static string GetGroup(int muscleIndex)  => Get(muscleIndex).Group;

        /// <summary>Clamp a value to the anatomical range of this muscle.</summary>
        public static float Clamp(int muscleIndex, float value) => Get(muscleIndex).Clamp(value);

        /// <summary>Normalize a value within this muscle's anatomical range → 0..1.</summary>
        public static float Normalize(int muscleIndex, float value) => Get(muscleIndex).Normalize(value);

        /// <summary>True if <paramref name="muscleIndex"/> is within the valid range (0-94).</summary>
        public static bool IsValid(int muscleIndex) =>
            muscleIndex >= 0 && muscleIndex < s_limits.Length;

        // ─────────────────────────────────────────────────────────────────────
        // Initialization — all 95 muscles
        // ─────────────────────────────────────────────────────────────────────

        private static MuscleLimit[] BuildLimits()
        {
            var arr = new MuscleLimit[MuscleCount];

            // ── Spine  (0-2) ──────────────────────────────────────────────────
            // Human thoracolumbar spine: ~40° forward flex, ~25° extension, ~25° lateral, ~35° rotation.
            // ROM is distributed across three segments (Spine, Chest, UpperChest).
            arr[ 0] = L(-0.50f,  0.70f, 0f, "Spine Front-Back (Flex / Extend)",          "Spine");
            arr[ 1] = L(-0.60f,  0.60f, 0f, "Spine Left-Right (Lateral Bend)",           "Spine");
            arr[ 2] = L(-0.60f,  0.60f, 0f, "Spine Rotate Left-Right",                   "Spine");

            // ── Chest  (3-5) ──────────────────────────────────────────────────
            arr[ 3] = L(-0.40f,  0.60f, 0f, "Chest Front-Back (Flex / Extend)",          "Chest");
            arr[ 4] = L(-0.50f,  0.50f, 0f, "Chest Left-Right (Lateral Bend)",           "Chest");
            arr[ 5] = L(-0.50f,  0.50f, 0f, "Chest Rotate Left-Right",                   "Chest");

            // ── Upper Chest  (6-8) ────────────────────────────────────────────
            arr[ 6] = L(-0.40f,  0.50f, 0f, "Upper Chest Front-Back (Flex / Extend)",    "Upper Chest");
            arr[ 7] = L(-0.50f,  0.50f, 0f, "Upper Chest Left-Right (Lateral Bend)",     "Upper Chest");
            arr[ 8] = L(-0.50f,  0.50f, 0f, "Upper Chest Rotate Left-Right",             "Upper Chest");

            // ── Neck  (9-11) ──────────────────────────────────────────────────
            // Cervical: ~50° flex forward, ~70° extension, ~45° lateral, ~80° rotation.
            arr[ 9] = L(-0.40f,  0.70f, 0f, "Neck Nod Down-Up (Flex / Extend)",          "Neck");
            arr[10] = L(-0.60f,  0.60f, 0f, "Neck Tilt Left-Right",                      "Neck");
            arr[11] = L(-0.70f,  0.70f, 0f, "Neck Turn Left-Right",                      "Neck");

            // ── Head  (12-14) ─────────────────────────────────────────────────
            arr[12] = L(-0.50f,  0.80f, 0f, "Head Nod Down-Up (Flex / Extend)",          "Head");
            arr[13] = L(-0.50f,  0.50f, 0f, "Head Tilt Left-Right",                      "Head");
            arr[14] = L(-0.80f,  0.80f, 0f, "Head Turn Left-Right",                      "Head");

            // ── Face  (15-20) ─────────────────────────────────────────────────
            // Only present when the avatar has facial bones.
            arr[15] = L(-1.00f,  1.00f, 0f, "Left Eye Down-Up",                          "Face");
            arr[16] = L(-1.00f,  1.00f, 0f, "Left Eye In-Out",                           "Face");
            arr[17] = L(-1.00f,  1.00f, 0f, "Right Eye Down-Up",                         "Face");
            arr[18] = L(-1.00f,  1.00f, 0f, "Right Eye In-Out",                          "Face");
            arr[19] = L(-1.00f,  1.00f, 0f, "Jaw Close",                                 "Face");
            arr[20] = L(-0.50f,  0.50f, 0f, "Jaw Left-Right",                            "Face");

            // ── Left Leg  (21-28) ─────────────────────────────────────────────
            // Hip: ~120° flex, ~15° ext, ~45° abduct, ~20° adduct, ~40° ext rotation, ~35° int rotation.
            // Knee: 0-145° flex, virtually no hyperextension.
            // Ankle (true ROM): ~50° plantarflex, ~20° dorsiflex. Inversion ~35°, eversion ~15°.
            arr[21] = L(-0.40f,  1.00f, 0f, "L Hip Front-Back (Flex / Extend)",          "Left Leg");
            arr[22] = L(-0.20f,  0.90f, 0f, "L Hip In-Out (Abduction)",                  "Left Leg");
            arr[23] = L(-0.60f,  0.50f, 0f, "L Hip Twist In-Out (Rotation)",             "Left Leg");
            arr[24] = L(-1.00f,  1.00f, 0f, "L Knee Bend  (-1=Bent → 1=Straight)",        "Left Leg");
            arr[25] = L(-0.30f,  0.30f, 0f, "L Shin Twist In-Out",                       "Left Leg");
            arr[26] = L(-0.80f,  0.60f, 0f, "L Ankle Up-Down (Plantar / Dorsi-flex)",    "Left Leg");
            arr[27] = L(-0.50f,  0.40f, 0f, "L Foot Twist (Inversion / Eversion)",       "Left Leg");
            arr[28] = L(-0.60f,  1.00f, 0f, "L Toes Up-Down",                            "Left Leg");

            // ── Right Leg  (29-36) ────────────────────────────────────────────
            arr[29] = L(-0.40f,  1.00f, 0f, "R Hip Front-Back (Flex / Extend)",          "Right Leg");
            arr[30] = L(-0.20f,  0.90f, 0f, "R Hip In-Out (Abduction)",                  "Right Leg");
            arr[31] = L(-0.60f,  0.50f, 0f, "R Hip Twist In-Out (Rotation)",             "Right Leg");
            arr[32] = L(-1.00f,  1.00f, 0f, "R Knee Bend  (-1=Bent → 1=Straight)",        "Right Leg");
            arr[33] = L(-0.30f,  0.30f, 0f, "R Shin Twist In-Out",                       "Right Leg");
            arr[34] = L(-0.80f,  0.60f, 0f, "R Ankle Up-Down (Plantar / Dorsi-flex)",    "Right Leg");
            arr[35] = L(-0.50f,  0.40f, 0f, "R Foot Twist (Inversion / Eversion)",       "Right Leg");
            arr[36] = L(-0.60f,  1.00f, 0f, "R Toes Up-Down",                            "Right Leg");

            // ── Left Shoulder  (37-38) ────────────────────────────────────────
            // Scapulothoracic: elevation ~30°, depression ~10°, protraction ~25°, retraction ~25°.
            arr[37] = L(-0.70f,  1.00f, 0f, "L Shoulder Down-Up",                        "Left Arm");
            arr[38] = L(-0.50f,  0.70f, 0f, "L Shoulder Front-Back",                     "Left Arm");

            // ── Left Upper Arm  (39-41) ──────────────────────────────────────
            // GH joint: abduction/flexion ~180°, extension ~50°, internal/external rotation ~90° each.
            arr[39] = L(-0.90f,  1.00f, 0f, "L Arm Down-Up (Abduction / Adduction)",    "Left Arm");
            arr[40] = L(-0.80f,  1.00f, 0f, "L Arm Front-Back (Flex / Extend)",         "Left Arm");
            arr[41] = L(-0.80f,  0.80f, 0f, "L Arm Twist In-Out (Int / Ext Rotation)",  "Left Arm");

            // ── Left Forearm  (42-43) ────────────────────────────────────────
            // Elbow: 0-145° flex; virtually no hyperextension (small value for rig tolerance).
            // Forearm rotation: ~90° pronation, ~90° supination.
            arr[42] = L(-0.10f,  1.00f, 0f, "L Elbow Bend  (-0.1=Straight → 1=Bent)",   "Left Arm");
            arr[43] = L(-1.00f,  1.00f, 0f, "L Forearm Twist (Pronate / Supinate)",      "Left Arm");

            // ── Left Wrist / Hand  (44-45) ────────────────────────────────────
            // Wrist flex ~60°, extension ~60°, ulnar deviation ~30°, radial ~20°.
            arr[44] = L(-0.70f,  0.70f, 0f, "L Wrist Down-Up (Flex / Extend)",           "Left Arm");
            arr[45] = L(-0.40f,  0.40f, 0f, "L Wrist In-Out (Ulnar / Radial Deviation)", "Left Arm");

            // ── Right Shoulder  (46-47) ──────────────────────────────────────
            arr[46] = L(-0.70f,  1.00f, 0f, "R Shoulder Down-Up",                        "Right Arm");
            arr[47] = L(-0.50f,  0.70f, 0f, "R Shoulder Front-Back",                     "Right Arm");

            // ── Right Upper Arm  (48-50) ─────────────────────────────────────
            arr[48] = L(-0.90f,  1.00f, 0f, "R Arm Down-Up (Abduction / Adduction)",    "Right Arm");
            arr[49] = L(-0.80f,  1.00f, 0f, "R Arm Front-Back (Flex / Extend)",         "Right Arm");
            arr[50] = L(-0.80f,  0.80f, 0f, "R Arm Twist In-Out (Int / Ext Rotation)",  "Right Arm");

            // ── Right Forearm  (51-52) ────────────────────────────────────────
            arr[51] = L(-0.10f,  1.00f, 0f, "R Elbow Bend  (-0.1=Straight → 1=Bent)",   "Right Arm");
            arr[52] = L(-1.00f,  1.00f, 0f, "R Forearm Twist (Pronate / Supinate)",      "Right Arm");

            // ── Right Wrist / Hand  (53-54) ───────────────────────────────────
            arr[53] = L(-0.70f,  0.70f, 0f, "R Wrist Down-Up (Flex / Extend)",           "Right Arm");
            arr[54] = L(-0.40f,  0.40f, 0f, "R Wrist In-Out (Ulnar / Radial Deviation)", "Right Arm");

            // ── Left Fingers — Thumb  (55-58) ────────────────────────────────
            // CMC: wide abduction range. MCP & IP: flex ~80°, slight hyperextension.
            arr[55] = L(-1.00f,  1.00f, 0f, "L Thumb Proximal Stretch (CMC Flex)",       "Left Fingers");
            arr[56] = L(-0.30f,  1.00f, 0f, "L Thumb Spread (CMC Abduction)",            "Left Fingers");
            arr[57] = L(-1.00f,  1.00f, 0f, "L Thumb Middle Stretch (MCP Flex)",         "Left Fingers");
            arr[58] = L(-1.00f,  1.00f, 0f, "L Thumb Distal Stretch (IP Flex)",          "Left Fingers");

            // ── Left Fingers — Index  (59-62) ────────────────────────────────
            // MCP: 0-90° flex, 20° hyper-ext. PIP: 0-100° flex. DIP: 0-70° flex.
            arr[59] = L(-1.00f,  1.00f, 0f, "L Index Knuckle (MCP Flex)",                "Left Fingers");
            arr[60] = L(-0.40f,  0.80f, 0f, "L Index Spread (MCP Abduction)",            "Left Fingers");
            arr[61] = L(-1.00f,  1.00f, 0f, "L Index Middle Joint (PIP Flex)",           "Left Fingers");
            arr[62] = L(-1.00f,  1.00f, 0f, "L Index Tip Joint (DIP Flex)",              "Left Fingers");

            // ── Left Fingers — Middle  (63-66) ────────────────────────────────
            arr[63] = L(-1.00f,  1.00f, 0f, "L Middle Knuckle (MCP Flex)",               "Left Fingers");
            arr[64] = L(-0.40f,  0.80f, 0f, "L Middle Spread (MCP Abduction)",           "Left Fingers");
            arr[65] = L(-1.00f,  1.00f, 0f, "L Middle Middle Joint (PIP Flex)",          "Left Fingers");
            arr[66] = L(-1.00f,  1.00f, 0f, "L Middle Tip Joint (DIP Flex)",             "Left Fingers");

            // ── Left Fingers — Ring  (67-70) ──────────────────────────────────
            arr[67] = L(-1.00f,  1.00f, 0f, "L Ring Knuckle (MCP Flex)",                 "Left Fingers");
            arr[68] = L(-0.40f,  0.80f, 0f, "L Ring Spread (MCP Abduction)",             "Left Fingers");
            arr[69] = L(-1.00f,  1.00f, 0f, "L Ring Middle Joint (PIP Flex)",            "Left Fingers");
            arr[70] = L(-1.00f,  1.00f, 0f, "L Ring Tip Joint (DIP Flex)",               "Left Fingers");

            // ── Left Fingers — Little (Pinky)  (71-74) ────────────────────────
            arr[71] = L(-1.00f,  1.00f, 0f, "L Little (Pinky) Knuckle (MCP Flex)",       "Left Fingers");
            arr[72] = L(-0.40f,  0.80f, 0f, "L Little (Pinky) Spread (MCP Abduction)",  "Left Fingers");
            arr[73] = L(-1.00f,  1.00f, 0f, "L Little Middle Joint (PIP Flex)",          "Left Fingers");
            arr[74] = L(-1.00f,  1.00f, 0f, "L Little Tip Joint (DIP Flex)",             "Left Fingers");

            // ── Right Fingers — Thumb  (75-78) ────────────────────────────────
            arr[75] = L(-1.00f,  1.00f, 0f, "R Thumb Proximal Stretch (CMC Flex)",       "Right Fingers");
            arr[76] = L(-0.30f,  1.00f, 0f, "R Thumb Spread (CMC Abduction)",            "Right Fingers");
            arr[77] = L(-1.00f,  1.00f, 0f, "R Thumb Middle Stretch (MCP Flex)",         "Right Fingers");
            arr[78] = L(-1.00f,  1.00f, 0f, "R Thumb Distal Stretch (IP Flex)",          "Right Fingers");

            // ── Right Fingers — Index  (79-82) ───────────────────────────────
            arr[79] = L(-1.00f,  1.00f, 0f, "R Index Knuckle (MCP Flex)",                "Right Fingers");
            arr[80] = L(-0.40f,  0.80f, 0f, "R Index Spread (MCP Abduction)",            "Right Fingers");
            arr[81] = L(-1.00f,  1.00f, 0f, "R Index Middle Joint (PIP Flex)",           "Right Fingers");
            arr[82] = L(-1.00f,  1.00f, 0f, "R Index Tip Joint (DIP Flex)",              "Right Fingers");

            // ── Right Fingers — Middle  (83-86) ──────────────────────────────
            arr[83] = L(-1.00f,  1.00f, 0f, "R Middle Knuckle (MCP Flex)",               "Right Fingers");
            arr[84] = L(-0.40f,  0.80f, 0f, "R Middle Spread (MCP Abduction)",           "Right Fingers");
            arr[85] = L(-1.00f,  1.00f, 0f, "R Middle Middle Joint (PIP Flex)",          "Right Fingers");
            arr[86] = L(-1.00f,  1.00f, 0f, "R Middle Tip Joint (DIP Flex)",             "Right Fingers");

            // ── Right Fingers — Ring  (87-90) ────────────────────────────────
            arr[87] = L(-1.00f,  1.00f, 0f, "R Ring Knuckle (MCP Flex)",                 "Right Fingers");
            arr[88] = L(-0.40f,  0.80f, 0f, "R Ring Spread (MCP Abduction)",             "Right Fingers");
            arr[89] = L(-1.00f,  1.00f, 0f, "R Ring Middle Joint (PIP Flex)",            "Right Fingers");
            arr[90] = L(-1.00f,  1.00f, 0f, "R Ring Tip Joint (DIP Flex)",               "Right Fingers");

            // ── Right Fingers — Little (Pinky)  (91-94) ──────────────────────
            arr[91] = L(-1.00f,  1.00f, 0f, "R Little (Pinky) Knuckle (MCP Flex)",       "Right Fingers");
            arr[92] = L(-0.40f,  0.80f, 0f, "R Little (Pinky) Spread (MCP Abduction)",  "Right Fingers");
            arr[93] = L(-1.00f,  1.00f, 0f, "R Little Middle Joint (PIP Flex)",          "Right Fingers");
            arr[94] = L(-1.00f,  1.00f, 0f, "R Little Tip Joint (DIP Flex)",             "Right Fingers");

            return arr;
        }

        // Shorthand to keep BuildLimits() readable.
        private static MuscleLimit L(float min, float max, float neutral, string label, string group)
            => new MuscleLimit(min, max, neutral, label, group);
    }
}
