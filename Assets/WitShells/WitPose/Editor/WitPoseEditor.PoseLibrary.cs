using UnityEngine;
using UnityEditor;
using WitShells.WitPose.Editor.Core;

namespace WitShells.WitPose.Editor
{
    /// <summary>
    /// Pose Library Logic for WitPoseEditor
    /// Handles displaying, saving, loading, and deleting poses from the library.
    /// </summary>
    public partial class WitPoseEditor
    {
        private void DrawPoseControls()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🎯 Direct Pose Control", EditorStyles.boldLabel);

            if (bonePoseSystem == null)
            {
                EditorGUILayout.HelpBox("Bone pose system not available", MessageType.Warning);
                return;
            }

            // Reset to T-Pose
            if (GUILayout.Button("Reset to T-Pose", GUILayout.Height(30)))
            {
                musclePoseSystem?.ResetToTPose();
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔑 Commit Current Pose", GUILayout.Height(25)))
            {
                bonePoseSystem?.CommitPose();
                Debug.Log("✅ Pose committed");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Show constraint rig status if available
            if (rigBuilt && duplicateRoot != null)
            {
                EditorGUILayout.BeginVertical("box");
                GUI.backgroundColor = successColor;
                EditorGUILayout.LabelField("✅ Constraint Rig Active", EditorStyles.boldLabel);
                GUI.backgroundColor = Color.white;

                EditorGUILayout.LabelField($"Duplicate Root: {duplicateRoot.name}");
                EditorGUILayout.LabelField($"Constraint Count: {constraintMap.Count}");

                if (GUILayout.Button("Switch to Rig Builder Tab", GUILayout.Height(25)))
                {
                    currentTab = EditorTab.RigBuilder;
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawPoseLibraryTab()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("📚 Pose Library", EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);
            poseLibraryScrollPosition = EditorGUILayout.BeginScrollView(poseLibraryScrollPosition, GUILayout.ExpandWidth(true));
            DrawPoseLibrary();
            EditorGUILayout.EndScrollView();
        }

        private void DrawPoseLibrary()
        {
            EditorGUILayout.LabelField("💾 Pose Library", EditorStyles.boldLabel);

            if (musclePoseSystem == null)
            {
                EditorGUILayout.HelpBox("Enter Pose Mode to use Pose Library", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginVertical("box");

            // Save current pose
            EditorGUILayout.LabelField("Save Current Pose", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            newPoseName = EditorGUILayout.TextField("Pose Name", newPoseName);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("💾 Save", GUILayout.Width(80), GUILayout.Height(25)))
            {
                SaveCurrentPose();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Load saved poses
            EditorGUILayout.LabelField("Load Saved Pose", EditorStyles.boldLabel);

            RefreshPoseNames();

            if (poseNames.Length == 0)
            {
                EditorGUILayout.HelpBox("No saved poses. Save a pose to see it here.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                selectedPoseIndex = EditorGUILayout.Popup("Select Pose", selectedPoseIndex, poseNames);

                GUI.backgroundColor = Color.cyan;
                if (GUILayout.Button("📂 Load", GUILayout.Width(80), GUILayout.Height(25)))
                {
                    LoadSelectedPose(false);
                }

                // Show Load + Record button only if animation tracking is available
                if (enableMuscleTracking && targetAnimationClip != null)
                {
                    GUI.backgroundColor = Color.yellow;
                    if (GUILayout.Button("🎬 Load + Record", GUILayout.Width(100), GUILayout.Height(25)))
                    {
                        LoadSelectedPose(true);
                    }
                }

                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("🗑️ Delete", GUILayout.Height(25)))
                {
                    DeleteSelectedPose();
                }

                if (GUILayout.Button("📋 Duplicate", GUILayout.Height(25)))
                {
                    DuplicateSelectedPose();
                }

                EditorGUILayout.EndHorizontal();

                // Show pose count
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"Total Poses: {poseNames.Length}", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void SaveCurrentPose()
        {
            if (musclePoseSystem == null || targetAnimator == null)
            {
                Debug.LogWarning("Cannot save pose: No active pose system");
                return;
            }

            if (string.IsNullOrWhiteSpace(newPoseName))
            {
                EditorUtility.DisplayDialog("Invalid Name", "Please enter a valid pose name", "OK");
                return;
            }

            // IMPORTANT: Sync from skeleton to capture any manual bone adjustments
            // This ensures all current bone transforms are captured, not just muscle values
            musclePoseSystem.SyncFromSkeleton();

            // Get the complete current pose from the animator (includes body position/rotation)
            HumanPose currentPose = new HumanPose();
            HumanPoseHandler handler = new HumanPoseHandler(targetAnimator.avatar, targetAnimator.transform);
            handler.GetHumanPose(ref currentPose);

            // Now currentPose contains the actual bone transforms converted to muscle space
            // No need to override muscles since GetHumanPose already captured them

            // Create PoseData with the complete pose
            PoseData poseData = new PoseData(currentPose, newPoseName);
            poseData.author = System.Environment.UserName;
            poseData.description = $"Saved from WitPose Editor on {System.DateTime.Now:yyyy-MM-dd HH:mm} - Bones: {HumanTrait.MuscleCount} muscles captured";

            // Save to library
            if (WitPoseLibrary.Instance.SavePose(poseData))
            {
                Debug.Log($"✅ Saved pose: {newPoseName} with {HumanTrait.MuscleCount} muscles and body transform");
                newPoseName = "New Pose"; // Reset name
                RefreshPoseNames();
            }
            newPoseName = "New Pose"; // Reset name
            RefreshPoseNames();
        }

        private void LoadSelectedPose(bool recordKeyframes = false)
        {
            if (musclePoseSystem == null || poseNames.Length == 0)
                return;

            if (selectedPoseIndex < 0 || selectedPoseIndex >= poseNames.Length)
                return;

            string poseName = poseNames[selectedPoseIndex];
            PoseData poseData = WitPoseLibrary.Instance.GetPose(poseName);

            if (poseData == null)
            {
                Debug.LogWarning($"Pose '{poseName}' not found");
                return;
            }

            // Convert to HumanPose and apply
            HumanPose pose = poseData.ToHumanPose();

            // Apply to muscle system (don't auto-key on load, let user decide)
            musclePoseSystem.SetAllMuscles(pose.muscles, autoKey: false);

            // Record keyframes if requested and animation tracking is enabled
            if (recordKeyframes && enableMuscleTracking && targetAnimationClip != null)
            {
                float keyframeTime = GetAnimationWindowTime();
                int recordedCount = 0;

                for (int i = 0; i < pose.muscles.Length && i < HumanTrait.MuscleCount; i++)
                {
                    string muscleName = HumanTrait.MuscleName[i];

                    // Create binding for the muscle curve
                    EditorCurveBinding binding = EditorCurveBinding.FloatCurve("", typeof(Animator), muscleName);

                    // Get or create animation curve for this muscle
                    AnimationCurve muscleCurve = AnimationUtility.GetEditorCurve(targetAnimationClip, binding);

                    if (muscleCurve == null)
                    {
                        muscleCurve = new AnimationCurve();
                    }

                    // Add keyframe
                    muscleCurve.AddKey(keyframeTime, pose.muscles[i]);

                    // Apply curve back to animation clip
                    AnimationUtility.SetEditorCurve(targetAnimationClip, binding, muscleCurve);

                    recordedCount++;
                }

                Debug.Log($"🎬 Loaded pose '{poseName}' and recorded {recordedCount} muscle keyframes at time {keyframeTime:F2}s");
            }
            else
            {
                Debug.Log($"📂 Loaded pose: {poseName}");
            }

            // Optionally commit to animation if in recording mode or auto-key enabled
            if (bonePoseSystem.AutoKey)
            {
                bonePoseSystem.CommitPose();
            }

            SceneView.RepaintAll();
        }

        private void DeleteSelectedPose()
        {
            if (poseNames.Length == 0 || selectedPoseIndex < 0 || selectedPoseIndex >= poseNames.Length)
                return;

            string poseName = poseNames[selectedPoseIndex];

            if (EditorUtility.DisplayDialog("Delete Pose",
                $"Are you sure you want to delete '{poseName}'?", "Delete", "Cancel"))
            {
                if (WitPoseLibrary.Instance.RemovePose(poseName))
                {
                    selectedPoseIndex = Mathf.Max(0, selectedPoseIndex - 1);
                    RefreshPoseNames();
                }
            }
        }

        private void DuplicateSelectedPose()
        {
            if (poseNames.Length == 0 || selectedPoseIndex < 0 || selectedPoseIndex >= poseNames.Length)
                return;

            string poseName = poseNames[selectedPoseIndex];
            PoseData originalPose = WitPoseLibrary.Instance.GetPose(poseName);

            if (originalPose != null)
            {
                PoseData duplicate = originalPose.Clone();
                if (WitPoseLibrary.Instance.SavePose(duplicate))
                {
                    RefreshPoseNames();
                }
            }
        }

        private void RefreshPoseNames()
        {
            poseNames = WitPoseLibrary.Instance.GetPoseNames();
            selectedPoseIndex = Mathf.Clamp(selectedPoseIndex, 0, Mathf.Max(0, poseNames.Length - 1));
        }
    }
}