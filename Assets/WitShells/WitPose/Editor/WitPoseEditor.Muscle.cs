using UnityEngine;
using UnityEditor;
using WitShells.WitPose;
using WitShells.WitPose.Editor.Core;

namespace WitShells.WitPose.Editor
{
    /// <summary>
    /// Muscle Editor Logic for WitPoseEditor
    /// Handles muscle sliders, groups, quick poses, and copy/paste functionality.
    /// </summary>
    public partial class WitPoseEditor
    {
        // ===== MIRROR POSE =====
        private bool mirrorPose = false;

        /// <summary>
        /// Get muscle value directly from animation clip at current time
        /// </summary>
        private float GetMuscleValueFromClip(int muscleIndex)
        {
            if (targetAnimationClip == null || targetAnimator == null) return 0f;

            string muscleName = HumanTrait.MuscleName[muscleIndex];
            EditorCurveBinding binding = EditorCurveBinding.FloatCurve("", typeof(Animator), muscleName);
            AnimationCurve curve = AnimationUtility.GetEditorCurve(targetAnimationClip, binding);

            if (curve != null)
            {
                float currentTime = GetCurrentAnimationTime();
                return curve.Evaluate(currentTime);
            }

            return 0f; // Default if no curve exists
        }

        /// <summary>
        /// Set muscle value directly to animation clip at current time
        /// </summary>
        private void SetMuscleValueToClip(int muscleIndex, float value)
        {
            if (targetAnimationClip == null) return;

            string muscleName = HumanTrait.MuscleName[muscleIndex];
            EditorCurveBinding binding = EditorCurveBinding.FloatCurve("", typeof(Animator), muscleName);
            AnimationCurve curve = AnimationUtility.GetEditorCurve(targetAnimationClip, binding);

            if (curve == null)
            {
                curve = new AnimationCurve();
            }

            float currentTime = GetCurrentAnimationTime();

            // Add or update keyframe
            Keyframe newKey = new Keyframe(currentTime, value);
            int keyIndex = -1;

            // Find existing keyframe at current time
            for (int i = 0; i < curve.keys.Length; i++)
            {
                if (Mathf.Approximately(curve.keys[i].time, currentTime))
                {
                    keyIndex = i;
                    break;
                }
            }

            if (keyIndex >= 0)
            {
                // Update existing keyframe
                curve.MoveKey(keyIndex, newKey);
            }
            else
            {
                // Add new keyframe
                curve.AddKey(newKey);
            }

            // Apply curve back to animation clip
            AnimationUtility.SetEditorCurve(targetAnimationClip, binding, curve);
        }

        /// <summary>
        /// Get current animation time from the timeline or animation window
        /// </summary>
        private float GetCurrentAnimationTime()
        {
            // Try to get time from Animation window if available
            var animationWindowType = System.Type.GetType("UnityEditor.AnimationWindow,UnityEditor");
            if (animationWindowType != null)
            {
                var windows = Resources.FindObjectsOfTypeAll(animationWindowType);
                if (windows.Length > 0)
                {
                    var timeProperty = animationWindowType.GetProperty("time");
                    if (timeProperty != null)
                    {
                        return (float)timeProperty.GetValue(windows[0]);
                    }
                }
            }

            return 0f; // Default to start of animation
        }
        private void DrawMuscleEditorTab()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("💪 Advanced Muscle Editor", EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Single scroll view for the entire muscle editor
            muscleScrollPosition = EditorGUILayout.BeginScrollView(muscleScrollPosition, GUILayout.ExpandHeight(true));
            DrawMuscleMode();
            EditorGUILayout.EndScrollView();
        }

        private void DrawMuscleMode()
        {
            if (musclePoseSystem == null)
            {
                EditorGUILayout.HelpBox("🚫 Enter Pose Mode to access Muscle Editor", MessageType.Warning);
                return;
            }

            // Animation Tracking Section
            DrawAnimationTrackingSection();

            EditorGUILayout.Space(10);

            // Selected Bone Section
            DrawSelectedBoneSection();

            EditorGUILayout.Space(10);

            // Root Position Control Section
            DrawRootPositionSection();

            EditorGUILayout.Space(10);

            // Header with controls
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🏋️ Muscle Control System", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("💡 Sliders follow realistic human joint limits. Arms: straight→bent, fist→open. Legs: straight→bent, feet anatomically correct.", MessageType.Info);

            EditorGUILayout.BeginHorizontal();

            // Reset button
            GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
            if (GUILayout.Button("🔄 Reset to T-Pose", GUILayout.Height(30), GUILayout.Width(150)))
            {
                musclePoseSystem.ResetToTPose(autoKey: bonePoseSystem.AutoKey);
            }

            GUILayout.Space(10);

            // Quick pose buttons
            GUI.backgroundColor = new Color(0.7f, 0.9f, 1f);
            if (GUILayout.Button("🤸 Quick Poses ▼", GUILayout.Height(30), GUILayout.Width(120)))
            {
                ShowQuickPoseMenu();
            }

            GUILayout.FlexibleSpace();

            // Global muscle controls
            GUI.backgroundColor = new Color(0.8f, 1f, 0.8f);
            if (GUILayout.Button("📋 Copy All", GUILayout.Height(30), GUILayout.Width(100)))
            {
                CopyAllMuscleValues();
            }

            if (GUILayout.Button("📄 Paste All", GUILayout.Height(30), GUILayout.Width(100)))
            {
                PasteAllMuscleValues();
            }

            GUILayout.Space(10);

            // Logger button
            GUI.backgroundColor = new Color(1f, 1f, 0.7f);
            if (GUILayout.Button("🐛 Logger Log All Muscles", GUILayout.Height(30), GUILayout.Width(160)))
            {
                LogAllMusclesWithIndices();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // ── Core & Spine ─────────────────────────────────────────────────
            DrawMuscleGroup("🦴 Core & Spine", "Body", 0, 8, new Color(1f, 0.9f, 0.8f));

            // ── Head & Neck (kept as original) ────────────────────────────────
            DrawMuscleGroup("🗣️ Head & Neck", "Head", 9, 14, new Color(1f, 0.8f, 0.9f));

            // ── Mirror Pose Toggle ────────────────────────────────────────────
            DrawMirrorToggle();

            // ── Left Arm (anatomical sub-groups) ─────────────────────────────
            DrawArmGroup("🤲 Left Arm", "LeftArm", isLeft: true);

            // ── Right Arm (anatomical sub-groups) ────────────────────────────
            DrawArmGroup("🫱 Right Arm", "RightArm", isLeft: false);

            // ── Left Leg (anatomical sub-groups) ─────────────────────────────
            DrawLegGroup("🦵 Left Leg", "LeftLeg", isLeft: true);

            // ── Right Leg (anatomical sub-groups) ────────────────────────────
            DrawLegGroup("🦵 Right Leg", "RightLeg", isLeft: false);
        }

        // ─────────────────────────────────────────────────────────────────────
        // ARM GROUP — absolute indices sourced from InitializeBoneToMuscleMapping
        //
        // LEFT ARM:
        //   Shoulder:   37=Down-Up  38=Front-Back
        //   Upper Arm:  39=Down-Up  40=Front-Back  41=Twist In-Out
        //   Forearm:    42=Stretch(Elbow Bend)  43=Twist(Pronation-Supination)
        //   Hand/Wrist: 44=Down-Up  45=In-Out
        //   Thumb:      55=1Stretched  56=Spread  57=2Stretched  58=3Stretched
        //   Index:      59=1Stretched  60=Spread  61=2Stretched  62=3Stretched
        //   Middle:     63=1Stretched  64=Spread  65=2Stretched  66=3Stretched
        //   Ring:       67=1Stretched  68=Spread  69=2Stretched  70=3Stretched
        //   Little:     71=1Stretched  72=Spread  73=2Stretched  74=3Stretched
        //
        // RIGHT ARM:
        //   Shoulder:   46=Down-Up  47=Front-Back
        //   Upper Arm:  48=Down-Up  49=Front-Back  50=Twist In-Out
        //   Forearm:    51=Stretch(Elbow Bend)  52=Twist(Pronation-Supination)
        //   Hand/Wrist: 53=Down-Up  54=In-Out
        //   Thumb:      75=1Stretched  76=Spread  77=2Stretched  78=3Stretched
        //   Index:      79=1Stretched  80=Spread  81=2Stretched  82=3Stretched
        //   Middle:     83=1Stretched  84=Spread  85=2Stretched  86=3Stretched
        //   Ring:       87=1Stretched  88=Spread  89=2Stretched  90=3Stretched
        //   Little:     91=1Stretched  92=Spread  93=2Stretched  94=3Stretched
        // ─────────────────────────────────────────────────────────────────────
        private void DrawArmGroup(string displayName, string groupKey, bool isLeft)
        {
            // Absolute shoulder/arm/wrist base: left=37, right=46
            int sh0 = isLeft ? 37 : 46; // Shoulder Down-Up
            int sh1 = isLeft ? 38 : 47; // Shoulder Front-Back
            int ua0 = isLeft ? 39 : 48; // Upper Arm Down-Up
            int ua1 = isLeft ? 40 : 49; // Upper Arm Front-Back
            int ua2 = isLeft ? 41 : 50; // Upper Arm Twist In-Out
            int fa0 = isLeft ? 42 : 51; // Forearm Stretch (Elbow Bend)
            int fa1 = isLeft ? 43 : 52; // Forearm Twist (Pronation-Supination)
            int wr0 = isLeft ? 44 : 53; // Wrist Down-Up
            int wr1 = isLeft ? 45 : 54; // Wrist In-Out

            // Absolute finger base: left thumb=55, right thumb=75  (each finger = 4 muscles)
            int tb = isLeft ? 55 : 75; // Thumb base
            int ib = isLeft ? 59 : 79; // Index base
            int mb = isLeft ? 63 : 83; // Middle base
            int rb = isLeft ? 67 : 87; // Ring base
            int lb = isLeft ? 71 : 91; // Little base

            // All arm muscles (non-contiguous: 37-45 + 55-74  /  46-54 + 75-94)
            int[] allArmMuscles = isLeft
                ? new[] { 37,38,39,40,41,42,43,44,45, 55,56,57,58, 59,60,61,62, 63,64,65,66, 67,68,69,70, 71,72,73,74 }
                : new[] { 46,47,48,49,50,51,52,53,54, 75,76,77,78, 79,80,81,82, 83,84,85,86, 87,88,89,90, 91,92,93,94 };

            if (!muscleGroupFoldouts.ContainsKey(groupKey))
                muscleGroupFoldouts[groupKey] = false;

            EditorGUILayout.Space(5);
            Color armColor = isLeft ? new Color(0.8f, 1f, 0.9f) : new Color(0.8f, 0.9f, 1f);
            GUI.backgroundColor = armColor;
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = Color.white;

            EditorGUILayout.BeginHorizontal();
            muscleGroupFoldouts[groupKey] = EditorGUILayout.Foldout(muscleGroupFoldouts[groupKey], displayName, true, EditorStyles.foldoutHeader);
            if (muscleGroupFoldouts[groupKey])
            {
                GUILayout.FlexibleSpace();
                GUI.backgroundColor = new Color(1f, 0.8f, 0.8f);
                if (GUILayout.Button("🔄", GUILayout.Width(25), GUILayout.Height(18)))
                    ResetMuscleIndices(allArmMuscles);
                GUI.backgroundColor = new Color(0.8f, 0.8f, 1f);
                if (GUILayout.Button("🎲", GUILayout.Width(25), GUILayout.Height(18)))
                    RandomizeMuscleIndices(allArmMuscles);
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            if (!muscleGroupFoldouts[groupKey])
            {
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUI.indentLevel++;

            // ── Shoulder ──────────────────────────────────────────────────────
            DrawMuscleSubGroup("💪 Shoulder", groupKey + "_shoulder",
                new Color(0.95f, 1f, 0.9f),
                () =>
                {
                    DrawLabeledMuscleSlider(sh0, "↕ Shoulder Down / Up  (-1=Down → 1=Up/Raised)");
                    DrawLabeledMuscleSlider(sh1, "↔ Shoulder Front / Back  (-1=Back → 1=Forward)");
                });

            // ── Upper Arm & Elbow ─────────────────────────────────────────────
            DrawMuscleSubGroup("🦾 Upper Arm & Elbow", groupKey + "_elbow",
                new Color(0.9f, 1f, 0.9f),
                () =>
                {
                    DrawLabeledMuscleSlider(ua0, "↕ Arm Down / Up  (-1=Lower → 1=Raise)");
                    DrawLabeledMuscleSlider(ua1, "↔ Arm Front / Back  (-1=Back → 1=Forward)");
                    DrawLabeledMuscleSlider(ua2, "🔄 Upper Arm Twist  (-1=Inward → 1=Outward)");
                    DrawLabeledMuscleSlider(fa0, "📐 Elbow Bend  (-1=Straight → 1=Fully Bent)");
                    DrawLabeledMuscleSlider(fa1, "🔄 Forearm Pronation / Supination  (-1=Pronate → 1=Supinate)");
                });

            // ── Wrist ─────────────────────────────────────────────────────────
            DrawMuscleSubGroup("🤝 Wrist", groupKey + "_wrist",
                new Color(0.85f, 1f, 0.85f),
                () =>
                {
                    DrawLabeledMuscleSlider(wr0, "↕ Wrist Down / Up  (-1=Flex Down → 1=Extend Up)");
                    DrawLabeledMuscleSlider(wr1, "↔ Wrist In / Out  (-1=Ulnar → 1=Radial Deviation)");
                });

            // ── All Fingers master slider ─────────────────────────────────────
            DrawAllFingersSlider(tb, ib, mb, rb, lb);

            // ── Thumb ─────────────────────────────────────────────────────────
            DrawMuscleSubGroup("👍 Thumb", groupKey + "_thumb",
                new Color(1f, 1f, 0.85f),
                () =>
                {
                    DrawLabeledMuscleSlider(tb + 0, "📐 Thumb Proximal Stretch  (-1=Curl → 1=Extend)");
                    DrawLabeledMuscleSlider(tb + 1, "↔ Thumb Spread  (-1=Adduct → 1=Abduct)");
                    DrawLabeledMuscleSlider(tb + 2, "📐 Thumb Middle Stretch  (-1=Curl → 1=Extend)");
                    DrawLabeledMuscleSlider(tb + 3, "📐 Thumb Distal Stretch  (-1=Curl → 1=Extend)");
                });

            // ── Index Finger ──────────────────────────────────────────────────
            DrawMuscleSubGroup("☝️ Index Finger", groupKey + "_index",
                new Color(1f, 0.95f, 0.85f),
                () =>
                {
                    DrawLabeledMuscleSlider(ib + 0, "📐 Index Knuckle  (-1=Fist → 1=Open)");
                    DrawLabeledMuscleSlider(ib + 1, "↔ Index Spread  (-1=Adduct → 1=Abduct)");
                    DrawLabeledMuscleSlider(ib + 2, "📐 Index Middle Joint  (-1=Fist → 1=Open)");
                    DrawLabeledMuscleSlider(ib + 3, "📐 Index Tip Joint  (-1=Fist → 1=Open)");
                });

            // ── Middle Finger ─────────────────────────────────────────────────
            DrawMuscleSubGroup("🖕 Middle Finger", groupKey + "_middle",
                new Color(1f, 0.9f, 0.85f),
                () =>
                {
                    DrawLabeledMuscleSlider(mb + 0, "📐 Middle Knuckle  (-1=Fist → 1=Open)");
                    DrawLabeledMuscleSlider(mb + 1, "↔ Middle Spread  (-1=Adduct → 1=Abduct)");
                    DrawLabeledMuscleSlider(mb + 2, "📐 Middle Middle Joint  (-1=Fist → 1=Open)");
                    DrawLabeledMuscleSlider(mb + 3, "📐 Middle Tip Joint  (-1=Fist → 1=Open)");
                });

            // ── Ring Finger ───────────────────────────────────────────────────
            DrawMuscleSubGroup("💍 Ring Finger", groupKey + "_ring",
                new Color(0.95f, 0.9f, 1f),
                () =>
                {
                    DrawLabeledMuscleSlider(rb + 0, "📐 Ring Knuckle  (-1=Fist → 1=Open)");
                    DrawLabeledMuscleSlider(rb + 1, "↔ Ring Spread  (-1=Adduct → 1=Abduct)");
                    DrawLabeledMuscleSlider(rb + 2, "📐 Ring Middle Joint  (-1=Fist → 1=Open)");
                    DrawLabeledMuscleSlider(rb + 3, "📐 Ring Tip Joint  (-1=Fist → 1=Open)");
                });

            // ── Little (Pinky) Finger ─────────────────────────────────────────
            DrawMuscleSubGroup("🤙 Little Finger (Pinky)", groupKey + "_little",
                new Color(0.9f, 0.9f, 1f),
                () =>
                {
                    DrawLabeledMuscleSlider(lb + 0, "📐 Little Knuckle  (-1=Fist → 1=Open)");
                    DrawLabeledMuscleSlider(lb + 1, "↔ Little Spread  (-1=Adduct → 1=Abduct)");
                    DrawLabeledMuscleSlider(lb + 2, "📐 Little Middle Joint  (-1=Fist → 1=Open)");
                    DrawLabeledMuscleSlider(lb + 3, "📐 Little Tip Joint  (-1=Fist → 1=Open)");
                });

            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Master slider that drives all finger curl/stretch muscles at once.
        /// Spread muscles (index +1 in each finger) are excluded so fingers stay
        /// naturally together. -1 = full fist, 1 = fully open / extended hand.
        /// Respects mirror mode and recording mode just like individual sliders.
        /// </summary>
        private void DrawAllFingersSlider(int tb, int ib, int mb, int rb, int lb)
        {
            // Curl/stretch muscles only — spread muscles intentionally skipped
            int[] stretchMuscles =
            {
                tb+0, tb+2, tb+3,
                ib+0, ib+2, ib+3,
                mb+0, mb+2, mb+3,
                rb+0, rb+2, rb+3,
                lb+0, lb+2, lb+3
            };

            // Compute average of all curl muscles for the current slider position
            float avg = 0f;
            foreach (int idx in stretchMuscles)
                avg += (enableMuscleTracking && targetAnimationClip != null)
                    ? GetMuscleValueFromClip(idx)
                    : musclePoseSystem.MuscleValues[idx];
            avg /= stretchMuscles.Length;

            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(1f, 0.94f, 0.75f);
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = Color.white;

            EditorGUILayout.BeginHorizontal();

            GUIStyle boldLabel = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };
            EditorGUILayout.LabelField("🖐  All Fingers   👊 ←  Fist  |  Open  → ✋", boldLabel, GUILayout.MinWidth(220));

            EditorGUILayout.LabelField($"{avg:F3}", EditorStyles.miniLabel, GUILayout.Width(40));

            GUI.backgroundColor = GetMuscleValueColor(avg);
            EditorGUI.BeginChangeCheck();
            float newAvg = EditorGUILayout.Slider(avg, -1f, 1f);
            GUI.backgroundColor = Color.white;

            if (EditorGUI.EndChangeCheck())
            {
                foreach (int idx in stretchMuscles)
                    ApplyMuscleValue(idx, newAvg);
            }

            GUI.backgroundColor = new Color(1f, 0.9f, 0.9f);
            if (GUILayout.Button("↺", GUILayout.Width(20), GUILayout.Height(16)))
            {
                foreach (int idx in stretchMuscles)
                    ApplyMuscleValue(idx, 0f, useAutoKey: false);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        // ─────────────────────────────────────────────────────────────────────
        // LEG GROUP — absolute indices sourced from InitializeBoneToMuscleMapping
        //
        // LEFT LEG:
        //   Upper Leg:  21=Front-Back(Hip Flex)  22=In-Out(Abduction)  23=Twist
        //   Lower Leg:  24=Stretch(Knee Bend)    25=Twist
        //   Foot:       26=Up-Down(Ankle)         27=Twist(Inversion)
        //   Toes:       28=Up-Down
        //
        // RIGHT LEG:
        //   Upper Leg:  29=Front-Back(Hip Flex)  30=In-Out(Abduction)  31=Twist
        //   Lower Leg:  32=Stretch(Knee Bend)    33=Twist
        //   Foot:       34=Up-Down(Ankle)         35=Twist(Inversion)
        //   Toes:       36=Up-Down
        // ─────────────────────────────────────────────────────────────────────
        private void DrawLegGroup(string displayName, string groupKey, bool isLeft)
        {
            // Absolute leg base: left=21, right=29 (8 consecutive muscles each)
            int b = isLeft ? 21 : 29;

            if (!muscleGroupFoldouts.ContainsKey(groupKey))
                muscleGroupFoldouts[groupKey] = false;

            EditorGUILayout.Space(5);
            Color legColor = isLeft ? new Color(0.9f, 0.8f, 1f) : new Color(1f, 1f, 0.8f);
            GUI.backgroundColor = legColor;
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = Color.white;

            EditorGUILayout.BeginHorizontal();
            muscleGroupFoldouts[groupKey] = EditorGUILayout.Foldout(muscleGroupFoldouts[groupKey], displayName, true, EditorStyles.foldoutHeader);
            if (muscleGroupFoldouts[groupKey])
            {
                GUILayout.FlexibleSpace();
                GUI.backgroundColor = new Color(1f, 0.8f, 0.8f);
                if (GUILayout.Button("🔄", GUILayout.Width(25), GUILayout.Height(18)))
                    ResetMuscleGroup(b, b + 7);
                GUI.backgroundColor = new Color(0.8f, 0.8f, 1f);
                if (GUILayout.Button("🎲", GUILayout.Width(25), GUILayout.Height(18)))
                    RandomizeMuscleGroup(b, b + 7);
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            if (!muscleGroupFoldouts[groupKey])
            {
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUI.indentLevel++;

            // ── Hip / Upper Leg ───────────────────────────────────────────────
            DrawMuscleSubGroup("🦴 Hip", groupKey + "_hip",
                new Color(0.95f, 0.9f, 1f),
                () =>
                {
                    DrawLabeledMuscleSlider(b + 0, "↕ Hip Flex / Extend  (-1=Leg Back → 1=Leg Forward)");
                    DrawLabeledMuscleSlider(b + 1, "↔ Hip Abduct / Adduct  (-1=Legs Together → 1=Spread)");
                    DrawLabeledMuscleSlider(b + 2, "🔄 Hip Rotation  (-1=External → 1=Internal)");
                });

            // ── Knee / Lower Leg ──────────────────────────────────────────────
            DrawMuscleSubGroup("🦵 Knee", groupKey + "_knee",
                new Color(0.9f, 0.85f, 1f),
                () =>
                {
                    DrawLabeledMuscleSlider(b + 3, "📐 Knee Bend  (-1=Fully Bent → 1=Straight)");
                    DrawLabeledMuscleSlider(b + 4, "🔄 Shin Rotation  (-1=Inward → 1=Outward)");
                });

            // ── Ankle / Foot ──────────────────────────────────────────────────
            DrawMuscleSubGroup("🦶 Ankle & Foot", groupKey + "_foot",
                new Color(0.85f, 0.85f, 1f),
                () =>
                {
                    DrawLabeledMuscleSlider(b + 5, "↕ Ankle Up / Down  (-1=Plantarflex(Toes Down) → 1=Dorsiflex(Toes Up))");
                    DrawLabeledMuscleSlider(b + 6, "↔ Foot Twist  (-1=Inversion(Roll In) → 1=Eversion(Roll Out))");
                });

            // ── Toes ──────────────────────────────────────────────────────────
            DrawMuscleSubGroup("👣 Toes", groupKey + "_toes",
                new Color(0.8f, 0.8f, 1f),
                () =>
                {
                    DrawLabeledMuscleSlider(b + 7, "↕ Toes Up / Down  (-1=Curl Down → 1=Extend Up)");
                });

            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws a collapsible sub-group within an arm or leg group.
        /// </summary>
        private void DrawMuscleSubGroup(string label, string key, Color bgColor, System.Action drawContent)
        {
            if (!muscleGroupFoldouts.ContainsKey(key))
                muscleGroupFoldouts[key] = true; // sub-groups default open

            EditorGUILayout.Space(3);
            GUI.backgroundColor = bgColor;
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = Color.white;

            muscleGroupFoldouts[key] = EditorGUILayout.Foldout(muscleGroupFoldouts[key], label, true);
            if (muscleGroupFoldouts[key])
            {
                EditorGUI.indentLevel++;
                drawContent?.Invoke();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws the Mirror Pose toggle bar shown between Head/Neck and the arm groups.
        /// </summary>
        private void DrawMirrorToggle()
        {
            EditorGUILayout.Space(4);

            Color onColor  = new Color(0.3f, 0.9f, 1f);   // cyan-ish when ON
            Color offColor = new Color(0.75f, 0.75f, 0.75f); // grey when OFF

            GUI.backgroundColor = mirrorPose ? onColor : offColor;
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = Color.white;

            EditorGUILayout.BeginHorizontal();

            // Big, clear toggle button
            string icon  = mirrorPose ? "🔗" : "🔓";
            string state = mirrorPose ? "ON" : "OFF";
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize  = 12
            };
            GUI.backgroundColor = mirrorPose ? onColor : offColor;
            if (GUILayout.Button($"{icon}  Mirror Pose  [{state}]", btnStyle, GUILayout.Height(30), GUILayout.ExpandWidth(true)))
                mirrorPose = !mirrorPose;
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            // Description line
            string desc = mirrorPose
                ? "✅ Moving any arm or leg slider will also move its opposite side automatically."
                : "💡 Enable to sync both arms and both legs symmetrically when adjusting sliders.";
            EditorGUILayout.LabelField(desc, EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        /// <summary>
        /// Returns the mirrored muscle index for arm/leg muscles, or -1 if none.
        /// Mapping is sourced from InitializeBoneToMuscleMapping:
        ///   Left arm  (37-45)  ↔  Right arm  (46-54)  : offset ±9
        ///   Left fingers (55-74) ↔ Right fingers (75-94): offset ±20
        ///   Left leg  (21-28)  ↔  Right leg  (29-36)  : offset ±8
        /// </summary>
        private int GetMirrorMuscleIndex(int muscleIndex)
        {
            if (muscleIndex >= 37 && muscleIndex <= 45) return muscleIndex + 9;  // Left arm → Right arm
            if (muscleIndex >= 46 && muscleIndex <= 54) return muscleIndex - 9;  // Right arm → Left arm
            if (muscleIndex >= 55 && muscleIndex <= 74) return muscleIndex + 20; // Left fingers → Right fingers
            if (muscleIndex >= 75 && muscleIndex <= 94) return muscleIndex - 20; // Right fingers → Left fingers
            if (muscleIndex >= 21 && muscleIndex <= 28) return muscleIndex + 8;  // Left leg → Right leg
            if (muscleIndex >= 29 && muscleIndex <= 36) return muscleIndex - 8;  // Right leg → Left leg
            return -1;
        }

        /// <summary>
        /// Applies a muscle value (and its mirror when mirrorPose is enabled).
        /// Handles both recording mode and normal pose mode.
        /// </summary>
        private void ApplyMuscleValue(int muscleIndex, float value, bool useAutoKey = true)
        {
            if (enableMuscleTracking && targetAnimationClip != null)
            {
                SetMuscleValueToClip(muscleIndex, value);
                musclePoseSystem.SetMuscle(muscleIndex, value, autoKey: false);
            }
            else
            {
                musclePoseSystem.SetMuscle(muscleIndex, value, autoKey: useAutoKey && bonePoseSystem.AutoKey);
            }

            if (mirrorPose)
            {
                int mirror = GetMirrorMuscleIndex(muscleIndex);
                if (mirror >= 0 && mirror < HumanTrait.MuscleCount)
                {
                    if (enableMuscleTracking && targetAnimationClip != null)
                    {
                        SetMuscleValueToClip(mirror, value);
                        musclePoseSystem.SetMuscle(mirror, value, autoKey: false);
                    }
                    else
                    {
                        musclePoseSystem.SetMuscle(mirror, value, autoKey: false);
                    }
                }
            }
        }

        /// <summary>
        /// Draws a muscle slider with a custom semantic label instead of the raw muscle name.
        /// Falls back gracefully if muscleIndex is out of range.
        /// </summary>
        private void DrawLabeledMuscleSlider(int muscleIndex, string semanticLabel)
        {
            if (muscleIndex < 0 || muscleIndex >= HumanTrait.MuscleCount) return;

            float currentValue;
            if (enableMuscleTracking && targetAnimationClip != null)
                currentValue = GetMuscleValueFromClip(muscleIndex);
            else
                currentValue = musclePoseSystem.MuscleValues[muscleIndex];

            // Show a mirror indicator badge when this slider has an active mirror counterpart
            bool hasMirror = mirrorPose && GetMirrorMuscleIndex(muscleIndex) >= 0;

            EditorGUILayout.BeginHorizontal();

            // Index badge (with mirror icon when active)
            string indexLabel = hasMirror ? $"🔗({muscleIndex})" : $"({muscleIndex})";
            EditorGUILayout.LabelField(indexLabel, GUILayout.Width(hasMirror ? 55 : 35));

            // Semantic label
            EditorGUILayout.LabelField(semanticLabel, GUILayout.MinWidth(160));

            // Numeric readout
            EditorGUILayout.LabelField($"{currentValue:F3}", EditorStyles.miniLabel, GUILayout.Width(40));

            // Colour-coded slider — range comes from anatomical joint limits
            var limit = HumanMuscleJointLimits.Get(muscleIndex);
            GUI.backgroundColor = GetMuscleValueColor(currentValue);
            EditorGUI.BeginChangeCheck();
            float newValue = EditorGUILayout.Slider(currentValue, limit.Min, limit.Max);
            GUI.backgroundColor = Color.white;

            if (EditorGUI.EndChangeCheck())
                ApplyMuscleValue(muscleIndex, newValue);

            // Reset button — resets to the anatomical neutral (almost always 0)
            GUI.backgroundColor = new Color(1f, 0.9f, 0.9f);
            if (GUILayout.Button("↺", GUILayout.Width(20), GUILayout.Height(16)))
                ApplyMuscleValue(muscleIndex, limit.Neutral, useAutoKey: false);
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawMuscleGroup(string displayName, string groupKey, int startIndex, int endIndex, Color groupColor)
        {
            if (!muscleGroupFoldouts.ContainsKey(groupKey))
            {
                muscleGroupFoldouts[groupKey] = false;
            }

            EditorGUILayout.Space(5);

            // Group header with colored background
            GUI.backgroundColor = groupColor;
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = Color.white;

            EditorGUILayout.BeginHorizontal();
            muscleGroupFoldouts[groupKey] = EditorGUILayout.Foldout(muscleGroupFoldouts[groupKey], displayName, true, EditorStyles.foldoutHeader);

            // Group controls
            if (muscleGroupFoldouts[groupKey])
            {
                GUILayout.FlexibleSpace();

                // Reset group button
                GUI.backgroundColor = new Color(1f, 0.8f, 0.8f);
                if (GUILayout.Button("🔄", GUILayout.Width(25), GUILayout.Height(18)))
                {
                    ResetMuscleGroup(startIndex, endIndex);
                }

                // Random pose button
                GUI.backgroundColor = new Color(0.8f, 0.8f, 1f);
                if (GUILayout.Button("🎲", GUILayout.Width(25), GUILayout.Height(18)))
                {
                    RandomizeMuscleGroup(startIndex, endIndex);
                }

                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            if (muscleGroupFoldouts[groupKey])
            {
                EditorGUILayout.Space(5);
                EditorGUI.indentLevel++;

                for (int i = startIndex; i <= endIndex && i < HumanTrait.MuscleCount; i++)
                {
                    DrawMuscleSlider(i);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawMuscleSlider(int muscleIndex)
        {
            string muscleName = musclePoseSystem.GetMuscleName(muscleIndex);
            string muscleEmoji = WitPoseUtils.GetMuscleEmoji(muscleName);

            // Get current value - in recording mode, read from animation clip; in normal mode, get from muscle system
            float currentValue;
            if (enableMuscleTracking && targetAnimationClip != null)
            {
                // In recording mode, read directly from animation clip
                currentValue = GetMuscleValueFromClip(muscleIndex);
            }
            else
            {
                // In normal mode, get from muscle pose system
                currentValue = musclePoseSystem.MuscleValues[muscleIndex];
            }

            EditorGUILayout.BeginHorizontal();

            // Show muscle index at start
            EditorGUILayout.LabelField($"({muscleIndex})", GUILayout.Width(35));

            // Muscle emoji and name
            EditorGUILayout.LabelField(muscleEmoji, GUILayout.Width(20));
            EditorGUILayout.LabelField(WitPoseUtils.CleanMuscleName(muscleName), GUILayout.MinWidth(120));

            // Value display
            EditorGUILayout.LabelField($"{currentValue:F3}", EditorStyles.miniLabel, GUILayout.Width(40));

            // Slider with color coding — range comes from anatomical joint limits
            var limit = HumanMuscleJointLimits.Get(muscleIndex);
            Color sliderColor = GetMuscleValueColor(currentValue);
            GUI.backgroundColor = sliderColor;

            EditorGUI.BeginChangeCheck();
            float newValue = EditorGUILayout.Slider(currentValue, limit.Min, limit.Max);
            GUI.backgroundColor = Color.white;

            if (EditorGUI.EndChangeCheck())
            {
                // In recording mode, write directly to animation clip
                if (enableMuscleTracking && targetAnimationClip != null)
                {
                    SetMuscleValueToClip(muscleIndex, newValue);
                }
                else
                {
                    // Normal mode - directly control avatar
                    musclePoseSystem.SetMuscle(muscleIndex, newValue, autoKey: bonePoseSystem.AutoKey);
                }
            }

            // Reset individual muscle — resets to the anatomical neutral (almost always 0)
            GUI.backgroundColor = new Color(1f, 0.9f, 0.9f);
            if (GUILayout.Button("↺", GUILayout.Width(20), GUILayout.Height(16)))
            {
                float neutral = limit.Neutral;
                if (enableMuscleTracking && targetAnimationClip != null)
                {
                    SetMuscleValueToClip(muscleIndex, neutral);
                    musclePoseSystem.SetMuscle(muscleIndex, neutral, autoKey: false);
                }
                else
                {
                    musclePoseSystem.SetMuscle(muscleIndex, neutral, autoKey: bonePoseSystem.AutoKey);
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private Color GetMuscleValueColor(float value)
        {
            // Color code based on muscle value intensity
            float absValue = Mathf.Abs(value);

            if (absValue < 0.1f)
                return new Color(0.9f, 0.9f, 0.9f); // Nearly neutral - light gray
            else if (absValue < 0.5f)
                return new Color(0.8f, 1f, 0.8f); // Mild - light green
            else if (absValue < 0.8f)
                return new Color(1f, 1f, 0.8f); // Moderate - light yellow
            else
                return new Color(1f, 0.8f, 0.8f); // Intense - light red
        }

        // Scene view HUD version of muscle slider with manual rect positioning
        private void DrawCompactMuscleSliderInRect(int muscleIndex, Rect containerRect)
        {
            string muscleName = musclePoseSystem.GetMuscleName(muscleIndex);
            string muscleEmoji = WitPoseUtils.GetMuscleEmoji(muscleName);

            // Get current value - in recording mode, read from animation clip; in normal mode, get from muscle system
            float currentValue;
            if (enableMuscleTracking && targetAnimationClip != null)
            {
                // In recording mode, read directly from animation clip
                currentValue = GetMuscleValueFromClip(muscleIndex);
            }
            else
            {
                // In normal mode, get from muscle pose system
                currentValue = musclePoseSystem.MuscleValues[muscleIndex];
            }

            // Divide the container rect into sections
            Rect labelRect = new Rect(containerRect.x + 2, containerRect.y + 2, 160, 16);
            Rect sliderRect = new Rect(containerRect.x + 165, containerRect.y + 2, containerRect.width - 240, 16);
            Rect valueRect = new Rect(containerRect.x + containerRect.width - 70, containerRect.y + 2, 30, 16);
            Rect resetRect = new Rect(containerRect.x + containerRect.width - 35, containerRect.y + 2, 18, 16);

            // Compact muscle name with emoji and index
            string displayName = $"({muscleIndex}) {muscleEmoji} {WitPoseUtils.CleanMuscleName(muscleName)}";
            GUI.Label(labelRect, displayName);

            // Value slider with color coding — range from anatomical joint limits
            var limit = HumanMuscleJointLimits.Get(muscleIndex);
            Color sliderColor = GetMuscleValueColor(currentValue);
            GUI.backgroundColor = sliderColor;

            EditorGUI.BeginChangeCheck();
            float newValue = GUI.HorizontalSlider(sliderRect, currentValue, limit.Min, limit.Max);
            GUI.backgroundColor = Color.white;

            if (EditorGUI.EndChangeCheck())
            {
                // In recording mode, write to both animation clip AND apply directly to muscles
                if (enableMuscleTracking && targetAnimationClip != null)
                {
                    SetMuscleValueToClip(muscleIndex, newValue); // Record keyframe
                    musclePoseSystem.SetMuscle(muscleIndex, newValue, autoKey: false); // Apply directly for immediate feedback
                }
                else
                {
                    // Normal mode - directly control avatar
                    musclePoseSystem.SetMuscle(muscleIndex, newValue, autoKey: bonePoseSystem.AutoKey);
                }
            }

            // Value display and reset button
            GUI.Label(valueRect, $"{newValue:F2}");

            GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f);
            if (GUI.Button(resetRect, "↺"))
            {
                // In recording mode, write directly to animation clip
                if (enableMuscleTracking && targetAnimationClip != null)
                {
                    SetMuscleValueToClip(muscleIndex, 0f);
                }
                else
                {
                    // Normal mode - directly control avatar
                    musclePoseSystem.SetMuscle(muscleIndex, 0f, autoKey: bonePoseSystem.AutoKey);
                }
            }
            GUI.backgroundColor = Color.white;
        }

        private void ResetMuscleGroup(int startIndex, int endIndex)
        {
            if (musclePoseSystem == null) return;

            Undo.RecordObject(targetAnimator, "Reset Muscle Group");

            for (int i = startIndex; i <= endIndex && i < HumanTrait.MuscleCount; i++)
            {
                // In recording mode, write to both animation clip AND apply directly to muscles
                if (enableMuscleTracking && targetAnimationClip != null)
                {
                    SetMuscleValueToClip(i, 0f); // Record keyframe
                    musclePoseSystem.SetMuscle(i, 0f, autoKey: false); // Apply directly for immediate feedback
                }
                else
                {
                    // Normal mode - directly control avatar
                    musclePoseSystem.SetMuscle(i, 0f, autoKey: false);
                }
            }

            if (bonePoseSystem.AutoKey)
            {
                musclePoseSystem.SetMuscle(startIndex, 0f, autoKey: true); // Trigger one autokey for the group
            }

            Logger.Log($"🔄 Reset muscle group: indices {startIndex}-{endIndex}");
        }

        private void RandomizeMuscleGroup(int startIndex, int endIndex)
        {
            if (musclePoseSystem == null) return;

            Undo.RecordObject(targetAnimator, "Randomize Muscle Group");

            for (int i = startIndex; i <= endIndex && i < HumanTrait.MuscleCount; i++)
            {
                float randomValue = UnityEngine.Random.Range(-0.3f, 0.3f); // Conservative randomization

                // In recording mode, write to both animation clip AND apply directly to muscles
                if (enableMuscleTracking && targetAnimationClip != null)
                {
                    SetMuscleValueToClip(i, randomValue); // Record keyframe
                    musclePoseSystem.SetMuscle(i, randomValue, autoKey: false); // Apply directly for immediate feedback
                }
                else
                {
                    // Normal mode - directly control avatar
                    musclePoseSystem.SetMuscle(i, randomValue, autoKey: false);
                }
            }

            if (bonePoseSystem.AutoKey)
            {
                musclePoseSystem.SetMuscle(startIndex, musclePoseSystem.MuscleValues[startIndex], autoKey: true);
            }

            Logger.Log($"🎲 Randomized muscle group: indices {startIndex}-{endIndex}");
        }

        /// <summary>Reset a specific array of muscle indices (handles non-contiguous ranges such as arm+fingers).</summary>
        private void ResetMuscleIndices(int[] indices)
        {
            if (musclePoseSystem == null) return;
            Undo.RecordObject(targetAnimator, "Reset Muscles");
            foreach (int i in indices)
            {
                if (i < 0 || i >= HumanTrait.MuscleCount) continue;
                if (enableMuscleTracking && targetAnimationClip != null)
                {
                    SetMuscleValueToClip(i, 0f);
                    musclePoseSystem.SetMuscle(i, 0f, autoKey: false);
                }
                else
                {
                    musclePoseSystem.SetMuscle(i, 0f, autoKey: false);
                }
            }
            if (bonePoseSystem.AutoKey)
                bonePoseSystem.CommitPose();
        }

        /// <summary>Randomize a specific array of muscle indices (handles non-contiguous ranges such as arm+fingers).</summary>
        private void RandomizeMuscleIndices(int[] indices)
        {
            if (musclePoseSystem == null) return;
            Undo.RecordObject(targetAnimator, "Randomize Muscles");
            foreach (int i in indices)
            {
                if (i < 0 || i >= HumanTrait.MuscleCount) continue;
                float v = UnityEngine.Random.Range(-0.3f, 0.3f);
                if (enableMuscleTracking && targetAnimationClip != null)
                {
                    SetMuscleValueToClip(i, v);
                    musclePoseSystem.SetMuscle(i, v, autoKey: false);
                }
                else
                {
                    musclePoseSystem.SetMuscle(i, v, autoKey: false);
                }
            }
            if (bonePoseSystem.AutoKey)
                bonePoseSystem.CommitPose();
        }

        private void DrawSelectedBoneSection()
        {
            if (selectedBone == HumanBodyBones.LastBone || !boneToMuscleMapping.ContainsKey(selectedBone))
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("🎯 Selected Bone Muscles", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("💡 Click on a bone gizmo in the scene view to see its muscle controls here.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            // Selected bone muscle group with special highlight
            GUI.backgroundColor = new Color(1f, 0.95f, 0.8f); // Warm highlight color
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = Color.white;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"🎯 Selected: {GetBoneDisplayName(selectedBone)}", EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            // Quick actions for selected bone
            GUI.backgroundColor = new Color(1f, 0.8f, 0.8f);
            if (GUILayout.Button("🔄 Reset", GUILayout.Width(60), GUILayout.Height(20)))
            {
                ResetSelectedBoneMuscles();
            }

            GUI.backgroundColor = new Color(0.8f, 0.8f, 1f);
            if (GUILayout.Button("🎲 Random", GUILayout.Width(70), GUILayout.Height(20)))
            {
                RandomizeSelectedBoneMuscles();
            }

            GUI.backgroundColor = new Color(0.8f, 1f, 0.8f);
            if (GUILayout.Button("❌ Deselect", GUILayout.Width(80), GUILayout.Height(20)))
            {
                selectedBone = HumanBodyBones.LastBone;
                showBoneMuscleHUD = false;
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            int[] muscleIndices = boneToMuscleMapping[selectedBone];
            EditorGUILayout.LabelField($"Muscles: {muscleIndices.Length}", EditorStyles.miniLabel);

            // Show actual muscle indices and names for verification
            EditorGUILayout.LabelField("Mapped Muscle Indices:", EditorStyles.boldLabel);
            string muscleInfo = "";
            foreach (int index in muscleIndices)
            {
                if (index < HumanTrait.MuscleCount)
                {
                    string muscleName = HumanTrait.MuscleName[index];
                    muscleInfo += $"({index}) {muscleName}\n";
                }
            }
            EditorGUILayout.TextArea(muscleInfo, GUILayout.Height(60));

            EditorGUILayout.Space(5);

            // Draw muscle sliders for selected bone
            EditorGUI.indentLevel++;
            foreach (int muscleIndex in muscleIndices)
            {
                if (muscleIndex < HumanTrait.MuscleCount)
                {
                    DrawMuscleSlider(muscleIndex);
                }
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
        }

        private void ShowQuickPoseMenu()
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("🧘 Relaxed Pose"), false, () => ApplyQuickPose("relaxed"));
            menu.AddItem(new GUIContent("💪 Power Pose"), false, () => ApplyQuickPose("power"));
            menu.AddItem(new GUIContent("🤸 Dynamic Pose"), false, () => ApplyQuickPose("dynamic"));
            menu.AddItem(new GUIContent("🦸 Hero Pose"), false, () => ApplyQuickPose("hero"));
            menu.AddItem(new GUIContent("🤔 Thinking Pose"), false, () => ApplyQuickPose("thinking"));
            menu.AddItem(new GUIContent("🪑 Perfect Sit Pose"), false, () => ApplyQuickPose("sitting"));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("🎲 Random Pose"), false, () => ApplyQuickPose("random"));

            menu.ShowAsContext();
        }

        private void ApplyQuickPose(string poseType)
        {
            if (musclePoseSystem == null) return;

            Undo.RecordObject(targetAnimator, $"Apply Quick Pose: {poseType}");

            // Store if we should record keyframes for this pose
            bool shouldRecord = enableMuscleTracking;

            switch (poseType)
            {
                case "relaxed":
                    ApplyRelaxedPose(shouldRecord);
                    break;
                case "power":
                    ApplyPowerPose(shouldRecord);
                    break;
                case "dynamic":
                    ApplyDynamicPose(shouldRecord);
                    break;
                case "hero":
                    ApplyHeroPose(shouldRecord);
                    break;
                case "thinking":
                    ApplyThinkingPose(shouldRecord);
                    break;
                case "sitting":
                    ApplySittingPose(shouldRecord);
                    break;
                case "random":
                    ApplyRandomPose(shouldRecord);
                    break;
            }
        }

        private void ApplyRelaxedPose(bool recordKeyframes = false)
        {
            // Slight bend in arms and legs, relaxed shoulders
            musclePoseSystem.SetMuscle(15, 0.2f, false); // Left arm bend
            musclePoseSystem.SetMuscle(39, 0.2f, false); // Right arm bend
            musclePoseSystem.SetMuscle(63, 0.1f, false); // Left leg slight bend
            musclePoseSystem.SetMuscle(79, 0.1f, true);  // Right leg slight bend + autokey

            if (recordKeyframes)
            {
                RecordMuscleKeyframe(15, 0.2f);
                RecordMuscleKeyframe(39, 0.2f);
                RecordMuscleKeyframe(63, 0.1f);
                RecordMuscleKeyframe(79, 0.1f);
            }
        }

        private void ApplyPowerPose(bool recordKeyframes = false)
        {
            // Chest out, shoulders back, confident stance
            musclePoseSystem.SetMuscle(2, 0.3f, false);  // Chest forward
            musclePoseSystem.SetMuscle(15, -0.2f, false); // Left shoulder back
            musclePoseSystem.SetMuscle(39, -0.2f, true);  // Right shoulder back + autokey

            if (recordKeyframes)
            {
                RecordMuscleKeyframe(2, 0.3f);
                RecordMuscleKeyframe(15, -0.2f);
                RecordMuscleKeyframe(39, -0.2f);
            }
        }

        private void ApplyDynamicPose(bool recordKeyframes = false)
        {
            // Asymmetrical, action-ready pose
            musclePoseSystem.SetMuscle(15, 0.4f, false);  // Left arm more bent
            musclePoseSystem.SetMuscle(39, -0.2f, false); // Right arm extended
            musclePoseSystem.SetMuscle(63, 0.3f, false);  // Left leg forward
            musclePoseSystem.SetMuscle(79, -0.1f, true);  // Right leg back + autokey

            if (recordKeyframes)
            {
                RecordMuscleKeyframe(15, 0.4f);
                RecordMuscleKeyframe(39, -0.2f);
                RecordMuscleKeyframe(63, 0.3f);
                RecordMuscleKeyframe(79, -0.1f);
            }
        }

        private void ApplyHeroPose(bool recordKeyframes = false)
        {
            // Classic superhero pose
            musclePoseSystem.SetMuscle(2, 0.4f, false);   // Chest out
            musclePoseSystem.SetMuscle(15, -0.3f, false); // Left arm out
            musclePoseSystem.SetMuscle(39, -0.3f, false); // Right arm out
            musclePoseSystem.SetMuscle(1, -0.2f, true);   // Spine straight + autokey

            if (recordKeyframes)
            {
                RecordMuscleKeyframe(2, 0.4f);
                RecordMuscleKeyframe(15, -0.3f);
                RecordMuscleKeyframe(39, -0.3f);
                RecordMuscleKeyframe(1, -0.2f);
            }
        }

        private void ApplyThinkingPose(bool recordKeyframes = false)
        {
            // Hand to chin, contemplative
            musclePoseSystem.SetMuscle(15, 0.6f, false);  // Left arm bent
            musclePoseSystem.SetMuscle(9, 0.2f, false);   // Head slightly tilted
            musclePoseSystem.SetMuscle(63, 0.1f, true);   // Slight weight shift + autokey

            if (recordKeyframes)
            {
                RecordMuscleKeyframe(15, 0.6f);
                RecordMuscleKeyframe(9, 0.2f);
                RecordMuscleKeyframe(63, 0.1f);
            }
        }

        private void ApplySittingPose(bool recordKeyframes = false)
        {
            // Perfect sitting pose - natural and comfortable chair sitting position

            // Upper legs (thighs) - bend forward significantly for sitting
            musclePoseSystem.SetMuscle(63, 0.85f, false);  // Left upper leg forward bend
            musclePoseSystem.SetMuscle(79, 0.85f, false);  // Right upper leg forward bend

            // Lower legs (knees) - bend backward to create natural sitting angle
            musclePoseSystem.SetMuscle(67, -0.7f, false);  // Left lower leg back
            musclePoseSystem.SetMuscle(83, -0.7f, false);  // Right lower leg back

            // Spine - slight forward lean for natural sitting posture
            musclePoseSystem.SetMuscle(0, 0.15f, false);   // Spine front-back tilt
            musclePoseSystem.SetMuscle(1, 0.0f, false);    // Spine left-right straight
            musclePoseSystem.SetMuscle(2, 0.1f, false);    // Upper chest slightly forward

            // Arms - relaxed position for sitting
            musclePoseSystem.SetMuscle(15, 0.1f, false);   // Left arm slight bend
            musclePoseSystem.SetMuscle(39, 0.1f, false);   // Right arm slight bend
            musclePoseSystem.SetMuscle(19, -0.2f, false);  // Left forearm down
            musclePoseSystem.SetMuscle(43, -0.2f, false);  // Right forearm down

            // Shoulders - relaxed and natural
            musclePoseSystem.SetMuscle(16, -0.1f, false);  // Left shoulder down
            musclePoseSystem.SetMuscle(40, -0.1f, false);  // Right shoulder down

            // Feet - flat on ground position
            musclePoseSystem.SetMuscle(68, -0.2f, false);  // Left foot forward tilt
            musclePoseSystem.SetMuscle(84, -0.2f, false);  // Right foot forward tilt

            // Head - neutral and relaxed
            musclePoseSystem.SetMuscle(9, 0.0f, false);    // Head neutral
            musclePoseSystem.SetMuscle(10, 0.0f, true);    // Neck neutral + autokey

            // Record keyframes if animation tracking is enabled
            if (recordKeyframes)
            {
                int[] muscleIndices = { 63, 79, 67, 83, 0, 1, 2, 15, 39, 19, 43, 16, 40, 68, 84, 9, 10 };
                float[] muscleValues = { 0.85f, 0.85f, -0.7f, -0.7f, 0.15f, 0.0f, 0.1f, 0.1f, 0.1f, -0.2f, -0.2f, -0.1f, -0.1f, -0.2f, -0.2f, 0.0f, 0.0f };

                for (int i = 0; i < muscleIndices.Length; i++)
                {
                    RecordMuscleKeyframe(muscleIndices[i], muscleValues[i]);
                }
            }

            Logger.Log("🪑 Applied perfect sitting pose");
        }

        private void ApplyRandomPose(bool recordKeyframes = false)
        {
            // Apply random values to multiple muscle groups
            for (int i = 0; i < HumanTrait.MuscleCount; i++)
            {
                if (UnityEngine.Random.value < 0.3f) // 30% chance to modify each muscle
                {
                    float randomValue = UnityEngine.Random.Range(-0.4f, 0.4f);
                    musclePoseSystem.SetMuscle(i, randomValue, false);

                    if (recordKeyframes)
                    {
                        RecordMuscleKeyframe(i, randomValue);
                    }
                }
            }

            if (bonePoseSystem.AutoKey)
            {
                musclePoseSystem.SetMuscle(0, musclePoseSystem.MuscleValues[0], true);
            }
        }

        private void CopyAllMuscleValues()
        {
            if (musclePoseSystem == null) return;

            copiedMuscleValues = (float[])musclePoseSystem.MuscleValues.Clone();
            Logger.Log("📋 Copied all muscle values to clipboard");
        }

        private void PasteAllMuscleValues()
        {
            if (musclePoseSystem == null || copiedMuscleValues == null)
            {
                Logger.LogWarning("⚠️ No muscle values in clipboard to paste");
                return;
            }

            Undo.RecordObject(targetAnimator, "Paste Muscle Values");
            musclePoseSystem.SetAllMuscles(copiedMuscleValues, autoKey: bonePoseSystem.AutoKey);
            Logger.Log("📄 Pasted all muscle values from clipboard");
        }

        /// <summary>
        /// Logger method to log all Unity muscle indices and their names
        /// </summary>
        private void LogAllMusclesWithIndices()
        {
            if (!isPoseModeActive)
            {
                Logger.LogWarning("🚫 Enter Pose Mode to access muscle information");
                return;
            }

            Logger.Log("=== 🐛 Unity HumanTrait Muscle Logger Log ===");
            Logger.Log($"Total Muscle Count: {HumanTrait.MuscleCount}");
            Logger.Log("\n📋 All Muscles with Indices:");

            string logOutput = "";

            // Group muscles by type for better readability
            logOutput += "\n🦴 SPINE & CORE (0-8):\n";
            for (int i = 0; i <= 8; i++)
            {
                if (i < HumanTrait.MuscleCount)
                {
                    logOutput += $"  [{i:D2}] {HumanTrait.MuscleName[i]}\n";
                }
            }

            logOutput += "\n🗣️ HEAD & NECK (9-14):\n";
            for (int i = 9; i <= 14; i++)
            {
                if (i < HumanTrait.MuscleCount)
                {
                    logOutput += $"  [{i:D2}] {HumanTrait.MuscleName[i]}\n";
                }
            }

            logOutput += "\n🤲 LEFT ARM (15-38):\n";
            for (int i = 15; i <= 38; i++)
            {
                if (i < HumanTrait.MuscleCount)
                {
                    logOutput += $"  [{i:D2}] {HumanTrait.MuscleName[i]}\n";
                }
            }

            logOutput += "\n🫱 RIGHT ARM (39-62):\n";
            for (int i = 39; i <= 62; i++)
            {
                if (i < HumanTrait.MuscleCount)
                {
                    logOutput += $"  [{i:D2}] {HumanTrait.MuscleName[i]}\n";
                }
            }

            logOutput += "\n🦵 LEFT LEG (63-78):\n";
            for (int i = 63; i <= 78; i++)
            {
                if (i < HumanTrait.MuscleCount)
                {
                    logOutput += $"  [{i:D2}] {HumanTrait.MuscleName[i]}\n";
                }
            }

            logOutput += "\n🦵 RIGHT LEG (79-94):\n";
            for (int i = 79; i <= 94; i++)
            {
                if (i < HumanTrait.MuscleCount)
                {
                    logOutput += $"  [{i:D2}] {HumanTrait.MuscleName[i]}\n";
                }
            }

            Logger.Log(logOutput);
            Logger.Log("=== 🏁 End Muscle Logger Log ===");
        }
    }
}