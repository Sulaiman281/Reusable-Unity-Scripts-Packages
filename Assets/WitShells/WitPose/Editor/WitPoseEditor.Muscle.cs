using UnityEngine;
using UnityEditor;
using WitShells.WitPose.Editor.Core;

namespace WitShells.WitPose.Editor
{
    /// <summary>
    /// Muscle Editor Logic for WitPoseEditor
    /// Handles muscle sliders, groups, quick poses, and copy/paste functionality.
    /// </summary>
    public partial class WitPoseEditor
    {
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
            EditorGUILayout.HelpBox("💡 Adjust muscle values to pose your character. Changes are automatically applied to bones and can be keyframed.", MessageType.Info);

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

            // Debug button
            GUI.backgroundColor = new Color(1f, 1f, 0.7f);
            if (GUILayout.Button("🐛 Debug Log All Muscles", GUILayout.Height(30), GUILayout.Width(160)))
            {
                LogAllMusclesWithIndices();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Muscle groups with proper emojis and organization
            DrawMuscleGroup("🦴 Core & Spine", "Body", 0, 8, new Color(1f, 0.9f, 0.8f));
            DrawMuscleGroup("🗣️ Head & Neck", "Head", 9, 14, new Color(1f, 0.8f, 0.9f));
            DrawMuscleGroup("🤲 Left Arm", "Left Arm", 15, 38, new Color(0.8f, 1f, 0.9f));
            DrawMuscleGroup("🫱 Right Arm", "Right Arm", 39, 62, new Color(0.8f, 0.9f, 1f));
            DrawMuscleGroup("🦵 Left Leg", "Left Leg", 63, 78, new Color(0.9f, 0.8f, 1f));
            DrawMuscleGroup("🦵 Right Leg", "Right Leg", 79, 94, new Color(1f, 1f, 0.8f));
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

            // Slider with color coding
            Color sliderColor = GetMuscleValueColor(currentValue);
            GUI.backgroundColor = sliderColor;

            EditorGUI.BeginChangeCheck();
            float newValue = EditorGUILayout.Slider(currentValue, -1f, 1f);
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

            // Reset individual muscle button
            GUI.backgroundColor = new Color(1f, 0.9f, 0.9f);
            if (GUILayout.Button("↺", GUILayout.Width(20), GUILayout.Height(16)))
            {
                // In recording mode, write to both animation clip AND apply directly to muscles
                if (enableMuscleTracking && targetAnimationClip != null)
                {
                    SetMuscleValueToClip(muscleIndex, 0f); // Record keyframe
                    musclePoseSystem.SetMuscle(muscleIndex, 0f, autoKey: false); // Apply directly for immediate feedback
                }
                else
                {
                    // Normal mode - directly control avatar
                    musclePoseSystem.SetMuscle(muscleIndex, 0f, autoKey: bonePoseSystem.AutoKey);
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

            // Value slider with color coding - use GUI.HorizontalSlider for better drag handling
            Color sliderColor = GetMuscleValueColor(currentValue);
            GUI.backgroundColor = sliderColor;

            EditorGUI.BeginChangeCheck();
            float newValue = GUI.HorizontalSlider(sliderRect, currentValue, -1f, 1f);
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

            Debug.Log($"🔄 Reset muscle group: indices {startIndex}-{endIndex}");
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

            Debug.Log($"🎲 Randomized muscle group: indices {startIndex}-{endIndex}");
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

            Debug.Log("🪑 Applied perfect sitting pose");
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
            Debug.Log("📋 Copied all muscle values to clipboard");
        }

        private void PasteAllMuscleValues()
        {
            if (musclePoseSystem == null || copiedMuscleValues == null)
            {
                Debug.LogWarning("⚠️ No muscle values in clipboard to paste");
                return;
            }

            Undo.RecordObject(targetAnimator, "Paste Muscle Values");
            musclePoseSystem.SetAllMuscles(copiedMuscleValues, autoKey: bonePoseSystem.AutoKey);
            Debug.Log("📄 Pasted all muscle values from clipboard");
        }

        /// <summary>
        /// Debug method to log all Unity muscle indices and their names
        /// </summary>
        private void LogAllMusclesWithIndices()
        {
            if (!isPoseModeActive)
            {
                Debug.LogWarning("🚫 Enter Pose Mode to access muscle information");
                return;
            }

            Debug.Log("=== 🐛 Unity HumanTrait Muscle Debug Log ===");
            Debug.Log($"Total Muscle Count: {HumanTrait.MuscleCount}");
            Debug.Log("\n📋 All Muscles with Indices:");

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

            Debug.Log(logOutput);
            Debug.Log("=== 🏁 End Muscle Debug Log ===");
        }
    }
}