using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Animations;
using WitShells.WitPose.Editor.Core;

namespace WitShells.WitPose.Editor
{
    /// <summary>
    /// Animation Tracking Logic for WitPoseEditor
    /// Handles keyframing, timeline integration, and root motion recording.
    /// </summary>
    public partial class WitPoseEditor
    {
        // ===== ANIMATION TRACKING METHODS =====

        private void InitializeMuscleTracking()
        {
            if (musclePoseSystem == null) return;

            lastMuscleValues.Clear();

            // Store initial muscle values
            for (int i = 0; i < HumanTrait.MuscleCount; i++)
            {
                lastMuscleValues[i] = musclePoseSystem.MuscleValues[i];
            }

            muscleTrackingInitialized = true;
        }

        private void InitializeRootPositionTracking()
        {
            if (targetAnimator != null)
            {
                lastRootPosition = targetAnimator.transform.position;
                lastRootRotation = targetAnimator.transform.rotation;
            }
        }

        private void CenterRootPosition()
        {
            if (targetAnimator != null)
            {
                Undo.RecordObject(targetAnimator.transform, "Center Root Position");
                Vector3 pos = targetAnimator.transform.position;
                pos.x = 0;
                pos.z = 0;
                targetAnimator.transform.position = pos;

                if (enableMuscleTracking && targetAnimationClip != null)
                {
                    RecordRootPositionKeyframe(pos);
                }

                SceneView.RepaintAll();
                Logger.Log("🎯 Root position centered (X=0, Z=0)");
            }
        }

        private void ResetRootTransform()
        {
            if (targetAnimator != null)
            {
                Undo.RecordObject(targetAnimator.transform, "Reset Root Transform");
                targetAnimator.transform.position = Vector3.zero;
                targetAnimator.transform.rotation = Quaternion.identity;

                if (enableMuscleTracking && targetAnimationClip != null)
                {
                    RecordRootTransformAtCurrentTime();
                }

                SceneView.RepaintAll();
                Logger.Log("🔄 Root transform reset to origin");
            }
        }

        private void RecordRootPositionKeyframe(Vector3 position)
        {
            if (targetAnimationClip == null) return;

            float keyframeTime = GetKeyframeTime();

            // Record position curves
            RecordTransformCurve("m_LocalPosition.x", position.x, keyframeTime);
            RecordTransformCurve("m_LocalPosition.y", position.y, keyframeTime);
            RecordTransformCurve("m_LocalPosition.z", position.z, keyframeTime);
        }

        private void RecordRootRotationKeyframe(Quaternion rotation)
        {
            if (targetAnimationClip == null) return;

            float keyframeTime = GetKeyframeTime();

            // Record rotation curves
            RecordTransformCurve("m_LocalRotation.x", rotation.x, keyframeTime);
            RecordTransformCurve("m_LocalRotation.y", rotation.y, keyframeTime);
            RecordTransformCurve("m_LocalRotation.z", rotation.z, keyframeTime);
            RecordTransformCurve("m_LocalRotation.w", rotation.w, keyframeTime);
        }

        private void RecordRootTransformAtCurrentTime()
        {
            if (targetAnimator == null || targetAnimationClip == null) return;

            Transform rootTransform = targetAnimator.transform;
            RecordRootPositionKeyframe(rootTransform.position);
            RecordRootRotationKeyframe(rootTransform.rotation);

            Logger.Log($"💾 Root transform recorded at time {GetKeyframeTime():F2}s");
        }

        private void RecordTransformCurve(string propertyName, float value, float time)
        {
            // Create binding for transform property
            EditorCurveBinding binding = EditorCurveBinding.FloatCurve("", typeof(Transform), propertyName);

            // Get or create animation curve
            AnimationCurve curve = AnimationUtility.GetEditorCurve(targetAnimationClip, binding);

            if (curve == null)
            {
                curve = new AnimationCurve();
            }

            // Add keyframe
            curve.AddKey(time, value);

            // Apply curve back to animation clip
            AnimationUtility.SetEditorCurve(targetAnimationClip, binding, curve);
        }

        private float GetAnimationWindowTime()
        {
            try
            {
                // Try to get Animation Window through reflection
                var animationWindowType = System.Type.GetType("UnityEditor.AnimationWindow,UnityEditor");
                if (animationWindowType != null)
                {
                    var windows = Resources.FindObjectsOfTypeAll(animationWindowType);
                    if (windows != null && windows.Length > 0)
                    {
                        var animWindow = windows[0];
                        var timeProperty = animationWindowType.GetProperty("time", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (timeProperty != null)
                        {
                            var timeValue = timeProperty.GetValue(animWindow);
                            if (timeValue is float time)
                            {
                                return time;
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Logger.LogWarning($"Could not access Animation Window time: {e.Message}");
            }

            return -1f; // Indicates Animation Window time not available
        }

        private void RecordMuscleKeyframe(int muscleIndex, float value)
        {
            if (targetAnimationClip == null || musclePoseSystem == null)
                return;

            float keyframeTime = GetKeyframeTime();
            if (keyframeTime < 0f)
                return;

            // Get the muscle name for the animation curve
            string muscleName = HumanTrait.MuscleName[muscleIndex];

            // Create binding for the muscle curve
            EditorCurveBinding binding = EditorCurveBinding.FloatCurve("", typeof(Animator), muscleName);

            // Get or create animation curve for this muscle
            AnimationCurve muscleCurve = AnimationUtility.GetEditorCurve(targetAnimationClip, binding);

            if (muscleCurve == null)
            {
                muscleCurve = new AnimationCurve();
            }

            // Add keyframe
            muscleCurve.AddKey(keyframeTime, value);

            // Apply curve back to animation clip
            AnimationUtility.SetEditorCurve(targetAnimationClip, binding, muscleCurve);

            // Mark the clip as dirty
            EditorUtility.SetDirty(targetAnimationClip);

            Logger.Log($"🎯 Recorded keyframe: {muscleName} = {value:F3} at {keyframeTime:F3}s");
        }

        private void RecordAllMusclesAtCurrentTime()
        {
            if (targetAnimationClip == null || musclePoseSystem == null)
            {
                Logger.LogWarning("Cannot record: No target animation clip or muscle pose system");
                return;
            }

            float keyframeTime = GetKeyframeTime();
            if (keyframeTime < 0f)
            {
                Logger.LogWarning("Cannot record: Invalid keyframe time");
                return;
            }

            Undo.RecordObject(targetAnimationClip, "Record All Muscle Keyframes");

            int recordedCount = 0;

            for (int i = 0; i < HumanTrait.MuscleCount; i++)
            {
                float muscleValue = musclePoseSystem.MuscleValues[i];
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
                muscleCurve.AddKey(keyframeTime, muscleValue);

                // Apply curve back to animation clip
                AnimationUtility.SetEditorCurve(targetAnimationClip, binding, muscleCurve);

                recordedCount++;
            }

            // Mark the clip as dirty
            EditorUtility.SetDirty(targetAnimationClip);

            Logger.Log($"💾 Recorded {recordedCount} muscle keyframes at {keyframeTime:F3}s");
        }

        private void ClearKeyframesAtCurrentTime()
        {
            if (targetAnimationClip == null)
            {
                Logger.LogWarning("Cannot clear keyframes: No target animation clip");
                return;
            }

            float keyframeTime = GetKeyframeTime();
            if (keyframeTime < 0f)
            {
                Logger.LogWarning("Cannot clear keyframes: Invalid keyframe time");
                return;
            }

            Undo.RecordObject(targetAnimationClip, "Clear Muscle Keyframes");

            int clearedCount = 0;

            for (int i = 0; i < HumanTrait.MuscleCount; i++)
            {
                string muscleName = HumanTrait.MuscleName[i];

                // Create binding for the muscle curve
                EditorCurveBinding binding = EditorCurveBinding.FloatCurve("", typeof(Animator), muscleName);

                // Get animation curve for this muscle
                AnimationCurve muscleCurve = AnimationUtility.GetEditorCurve(targetAnimationClip, binding);

                if (muscleCurve != null)
                {
                    // Find and remove keyframes at the current time (within tolerance)
                    for (int k = muscleCurve.length - 1; k >= 0; k--)
                    {
                        if (Mathf.Abs(muscleCurve.keys[k].time - keyframeTime) < 0.01f)
                        {
                            muscleCurve.RemoveKey(k);
                            clearedCount++;
                        }
                    }

                    // Apply curve back to animation clip (even if empty)
                    AnimationUtility.SetEditorCurve(targetAnimationClip, binding, muscleCurve.length > 0 ? muscleCurve : null);
                }
            }

            // Mark the clip as dirty
            EditorUtility.SetDirty(targetAnimationClip);

            Logger.Log($"🗑️ Cleared {clearedCount} muscle keyframes at {keyframeTime:F3}s");
        }

        private float GetKeyframeTime()
        {
            if (useAnimationWindowTime)
            {
                float animWindowTime = GetAnimationWindowTime();
                if (animWindowTime >= 0f)
                {
                    return animWindowTime;
                }
            }

            // Fallback to manual time
            return manualKeyframeTime;
        }

        private void DrawAnimationTrackingSection()
        {
            EditorGUILayout.BeginVertical("box");

            // Header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("� Animation Recording", EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            // Recording button with visual effects
            string buttonText = enableMuscleTracking ? "🔴 STOP Recording" : "⚪ START Recording";

            if (enableMuscleTracking)
            {
                // Pulsing red effect when recording
                float pulse = Mathf.PingPong(Time.realtimeSinceStartup * 2f, 1f);
                GUI.backgroundColor = Color.Lerp(new Color(1f, 0.3f, 0.3f), new Color(1f, 0.5f, 0.5f), pulse);
            }
            else
            {
                GUI.backgroundColor = new Color(0.7f, 1f, 0.7f); // Light green for start
            }

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.fontSize = 12;

            if (GUILayout.Button(buttonText, buttonStyle, GUILayout.Height(30), GUILayout.Width(150)))
            {
                enableMuscleTracking = !enableMuscleTracking;

                // Automatically control animation preview when recording state changes
                if (enableMuscleTracking)
                {
                    // Entering recording mode - disable animation preview for direct control
                    if (AnimationMode.InAnimationMode())
                    {
                        AnimationMode.StopAnimationMode();
                        Logger.Log("🎬 Disabled Animation Preview for direct muscle control");
                    }

                    if (!muscleTrackingInitialized)
                    {
                        InitializeMuscleTracking();
                        InitializeRootPositionTracking();
                    }
                }

                // Force repaint for visual effects
                if (enableMuscleTracking)
                {
                    EditorApplication.update += RepaintWhileRecording;
                }
                else
                {
                    EditorApplication.update -= RepaintWhileRecording;
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            if (enableMuscleTracking)
            {
                EditorGUILayout.Space(5);
                EditorGUI.indentLevel++;

                // Target animation clip
                EditorGUI.BeginChangeCheck();
                targetAnimationClip = (AnimationClip)EditorGUILayout.ObjectField("Target Clip", targetAnimationClip, typeof(AnimationClip), false);
                if (EditorGUI.EndChangeCheck())
                {
                    if (targetAnimationClip != null)
                    {
                        Logger.Log($"🎥 Animation tracking target set: {targetAnimationClip.name}");
                    }
                }

                if (targetAnimationClip == null)
                {
                    GUI.backgroundColor = errorColor;
                    EditorGUILayout.HelpBox("⚠️ Please assign an animation clip to record keyframes", MessageType.Warning);
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    // Show clip info
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField($"📋 Clip: {targetAnimationClip.name}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"   Duration: {targetAnimationClip.length:F2}s | Frame Rate: {targetAnimationClip.frameRate:F1} fps", EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();

                    // Automatic animation preview control info
                    EditorGUILayout.HelpBox("🎬 Animation Preview is automatically disabled during recording for direct muscle control", MessageType.Info);

                    EditorGUILayout.Space(5);

                    // Time source selection
                    EditorGUILayout.LabelField("🕰️ Keyframe Timing", EditorStyles.boldLabel);

                    useAnimationWindowTime = EditorGUILayout.Toggle("Use Animation Window Time", useAnimationWindowTime);

                    if (!useAnimationWindowTime)
                    {
                        EditorGUI.indentLevel++;
                        manualKeyframeTime = EditorGUILayout.FloatField("Manual Time (seconds)", manualKeyframeTime);
                        manualKeyframeTime = Mathf.Clamp(manualKeyframeTime, 0f, targetAnimationClip.length);

                        // Time slider
                        manualKeyframeTime = EditorGUILayout.Slider(manualKeyframeTime, 0f, targetAnimationClip.length);

                        EditorGUILayout.LabelField($"Frame: {Mathf.RoundToInt(manualKeyframeTime * targetAnimationClip.frameRate)}", EditorStyles.miniLabel);
                        EditorGUI.indentLevel--;
                    }
                    else
                    {
                        float animWindowTime = GetAnimationWindowTime();
                        if (animWindowTime >= 0f)
                        {
                            GUI.backgroundColor = successColor;
                            EditorGUILayout.LabelField($"✅ Animation Window Time: {animWindowTime:F3}s (Frame {Mathf.RoundToInt(animWindowTime * targetAnimationClip.frameRate)})", EditorStyles.helpBox);
                            GUI.backgroundColor = Color.white;
                        }
                        else
                        {
                            GUI.backgroundColor = warningColor;
                            EditorGUILayout.LabelField("⚠️ Animation Window not detected - using manual time", EditorStyles.helpBox);
                            GUI.backgroundColor = Color.white;

                            EditorGUI.indentLevel++;
                            manualKeyframeTime = EditorGUILayout.FloatField("Fallback Time (seconds)", manualKeyframeTime);
                            manualKeyframeTime = Mathf.Clamp(manualKeyframeTime, 0f, targetAnimationClip.length);
                            EditorGUI.indentLevel--;
                        }
                    }

                    EditorGUILayout.Space(5);

                    // Recording status and controls
                    EditorGUILayout.BeginVertical("box");

                    // Animated recording indicator
                    if (enableMuscleTracking)
                    {
                        float pulse = Mathf.PingPong(Time.realtimeSinceStartup * 3f, 1f);
                        GUI.backgroundColor = Color.Lerp(new Color(1f, 0.4f, 0.4f), new Color(1f, 0.7f, 0.7f), pulse);
                        EditorGUILayout.LabelField("🔴 RECORDING ACTIVE - Muscle changes write to animation clip only", EditorStyles.helpBox);
                        GUI.backgroundColor = Color.white;

                        EditorGUILayout.Space(2);
                        GUI.backgroundColor = new Color(0.9f, 0.9f, 1f);
                        EditorGUILayout.LabelField("💡 Animation preview controls character - sliders record keyframes", EditorStyles.helpBox);
                        GUI.backgroundColor = Color.white;
                    }
                    else
                    {
                        GUI.backgroundColor = successColor;
                        EditorGUILayout.LabelField("✅ Normal mode - sliders directly control character", EditorStyles.helpBox);
                        GUI.backgroundColor = Color.white;
                    }

                    EditorGUILayout.BeginHorizontal();

                    // Clear all keyframes at current time
                    GUI.backgroundColor = errorColor;
                    if (GUILayout.Button("🗑️ Clear Current Frame", GUILayout.Height(25)))
                    {
                        ClearKeyframesAtCurrentTime();
                    }
                    // � Enable recording to write muscle changes to animation clip
                    // Record all current muscle values
                    GUI.backgroundColor = accentColor;
                    if (GUILayout.Button("💾 Record All Muscles", GUILayout.Height(25)))
                    {
                        RecordAllMusclesAtCurrentTime();
                    }

                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                }

                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.HelpBox("� Enable recording to write muscle changes to animation clips", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawRootPositionSection()
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            rootPositionFoldout = EditorGUILayout.Foldout(rootPositionFoldout, "🎯 Root Transform Control", true, EditorStyles.foldoutHeader);

            if (rootPositionFoldout)
            {
                GUILayout.FlexibleSpace();

                // Center button
                GUI.backgroundColor = Color.cyan;
                if (GUILayout.Button("🎯 Center", GUILayout.Width(60), GUILayout.Height(18)))
                {
                    CenterRootPosition();
                }

                // Reset button
                GUI.backgroundColor = new Color(1f, 0.8f, 0.8f);
                if (GUILayout.Button("🔄 Reset", GUILayout.Width(60), GUILayout.Height(18)))
                {
                    ResetRootTransform();
                }

                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            if (rootPositionFoldout)
            {
                EditorGUILayout.Space(5);

                if (targetAnimator != null)
                {
                    // Get current root transform
                    Transform rootTransform = targetAnimator.transform;
                    Vector3 currentPos = rootTransform.position;
                    Vector3 currentEuler = rootTransform.eulerAngles;

                    // Position controls
                    EditorGUILayout.LabelField("📍 Position", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;

                    Vector3 newPosition = EditorGUILayout.Vector3Field("", currentPos);
                    if (newPosition != currentPos)
                    {
                        Undo.RecordObject(rootTransform, "Root Position Change");
                        rootTransform.position = newPosition;

                        // Record keyframes if tracking is enabled
                        if (enableMuscleTracking && targetAnimationClip != null)
                        {
                            RecordRootPositionKeyframe(newPosition);
                        }

                        SceneView.RepaintAll();
                    }

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("X = 0", GUILayout.Height(20)))
                    {
                        Vector3 pos = rootTransform.position;
                        pos.x = 0;
                        Undo.RecordObject(rootTransform, "Reset Root X");
                        rootTransform.position = pos;
                        if (enableMuscleTracking && targetAnimationClip != null)
                            RecordRootPositionKeyframe(pos);
                        SceneView.RepaintAll();
                    }
                    if (GUILayout.Button("Y = 0", GUILayout.Height(20)))
                    {
                        Vector3 pos = rootTransform.position;
                        pos.y = 0;
                        Undo.RecordObject(rootTransform, "Reset Root Y");
                        rootTransform.position = pos;
                        if (enableMuscleTracking && targetAnimationClip != null)
                            RecordRootPositionKeyframe(pos);
                        SceneView.RepaintAll();
                    }
                    if (GUILayout.Button("Z = 0", GUILayout.Height(20)))
                    {
                        Vector3 pos = rootTransform.position;
                        pos.z = 0;
                        Undo.RecordObject(rootTransform, "Reset Root Z");
                        rootTransform.position = pos;
                        if (enableMuscleTracking && targetAnimationClip != null)
                            RecordRootPositionKeyframe(pos);
                        SceneView.RepaintAll();
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space(5);

                    // Rotation controls
                    EditorGUILayout.LabelField("🔄 Rotation", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;

                    Vector3 newEuler = EditorGUILayout.Vector3Field("", currentEuler);
                    if (newEuler != currentEuler)
                    {
                        Undo.RecordObject(rootTransform, "Root Rotation Change");
                        rootTransform.eulerAngles = newEuler;

                        // Record keyframes if tracking is enabled
                        if (enableMuscleTracking && targetAnimationClip != null)
                        {
                            RecordRootRotationKeyframe(Quaternion.Euler(newEuler));
                        }

                        SceneView.RepaintAll();
                    }

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Reset Rotation", GUILayout.Height(20)))
                    {
                        Undo.RecordObject(rootTransform, "Reset Root Rotation");
                        rootTransform.rotation = Quaternion.identity;
                        if (enableMuscleTracking && targetAnimationClip != null)
                            RecordRootRotationKeyframe(Quaternion.identity);
                        SceneView.RepaintAll();
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUI.indentLevel--;

                    if (enableMuscleTracking && targetAnimationClip != null)
                    {
                        EditorGUILayout.Space(5);
                        EditorGUILayout.BeginHorizontal();

                        GUI.backgroundColor = Color.yellow;
                        if (GUILayout.Button("💾 Record Root Transform", GUILayout.Height(25)))
                        {
                            RecordRootTransformAtCurrentTime();
                        }

                        GUI.backgroundColor = Color.white;
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("🚫 Select an Animator to control root transform", MessageType.Info);
                }
            }

            EditorGUILayout.EndVertical();
        }
    }
}