using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace WitShells.WitPose.Editor
{
    /// <summary>
    /// Advanced Animation to Humanoid Converter
    /// Converts constraint-based animations to humanoid muscle-based animation clips
    /// </summary>
    public class AnimationToHumanoidConverter : EditorWindow
    {
        // ===== CONVERSION SETTINGS =====
        private AnimationClip sourceClip;
        private Animator targetAnimator;
        private GameObject sourceRig;
        private string outputClipName = "Converted_Humanoid_Clip";
        private float samplingRate = 30f;
        private bool useOriginalFrameRate = true;
        private bool generateInPlace = false;
        private bool previewMode = false;

        // ===== CONVERSION STATE =====
        private HumanPoseHandler poseHandler;
        private AnimationClip outputClip;
        private bool isConverting = false;
        private float conversionProgress = 0f;
        private string currentStatus = "Ready to convert";
        private int totalFrames = 0;
        private int currentFrame = 0;

        // ===== UI STATE =====
        private Vector2 scrollPosition;
        private bool showAdvancedSettings = false;
        private bool showPreviewSettings = false;
        private GUIStyle headerStyle;
        private Color accentColor = new Color(0.2f, 0.8f, 0.2f);
        private Color warningColor = new Color(1f, 0.8f, 0.3f);
        private Color errorColor = new Color(1f, 0.5f, 0.5f);

        // ===== PREVIEW SYSTEM =====
        private float previewTime = 0f;
        private bool isPlaying = false;
        private HumanPose previewPose;
        private List<HumanPose> sampledPoses = new List<HumanPose>();

        // ===== PROCESSING STATE =====
        private bool isProcessing = false;
        private System.Action currentProcessingAction;
        private float lastUpdateTime;

        [MenuItem("Window/WitPose/Animation to Humanoid Converter")]
        public static void OpenWindow()
        {
            var window = GetWindow<AnimationToHumanoidConverter>("Animation → Humanoid Converter");
            window.minSize = new Vector2(450, 600);
            window.Show();
        }

        private void OnEnable()
        {
            InitializeStyles();
            EditorApplication.update += OnEditorUpdate;
            lastUpdateTime = (float)EditorApplication.timeSinceStartup;
        }

        private void OnEditorUpdate()
        {
            if (isProcessing && currentProcessingAction != null)
            {
                currentProcessingAction.Invoke();
                Repaint();
            }

            if (isPlaying && sampledPoses.Count > 0)
            {
                UpdatePreview();
                Repaint();
            }
        }

        private void InitializeStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel);
                headerStyle.fontSize = 14;
                headerStyle.normal.textColor = Color.white;
            }
        }

        private void OnGUI()
        {
            InitializeStyles();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            EditorGUILayout.Space(10);

            DrawSourceSettings();
            EditorGUILayout.Space(10);

            DrawTargetSettings();
            EditorGUILayout.Space(10);

            DrawConversionSettings();
            EditorGUILayout.Space(10);

            DrawPreviewSection();
            EditorGUILayout.Space(10);

            DrawConversionControls();
            EditorGUILayout.Space(10);

            DrawProgressSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical("box");

            GUILayout.Space(5);
            GUI.backgroundColor = accentColor;
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = Color.white;

            EditorGUILayout.LabelField("🎬 Animation → Humanoid Converter", headerStyle, GUILayout.Height(25));
            EditorGUILayout.LabelField("Convert constraint-based animations to muscle-based humanoid clips", EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
            GUILayout.Space(5);

            EditorGUILayout.EndVertical();
        }

        private void DrawSourceSettings()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("📥 Source Animation", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            sourceClip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", sourceClip, typeof(AnimationClip), false);
            sourceRig = (GameObject)EditorGUILayout.ObjectField("Source Rig", sourceRig, typeof(GameObject), true);

            if (EditorGUI.EndChangeCheck())
            {
                ValidateSourceSettings();
            }

            if (sourceClip != null)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"📊 Clip Info:", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"   Duration: {sourceClip.length:F2}s", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"   Frame Rate: {sourceClip.frameRate:F1} fps", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"   Legacy: {sourceClip.legacy}", EditorStyles.miniLabel);

                if (sourceClip.legacy)
                {
                    GUI.backgroundColor = warningColor;
                    EditorGUILayout.HelpBox("⚠️ Legacy animation detected. Make sure your source rig is properly set up.", MessageType.Warning);
                    GUI.backgroundColor = Color.white;
                }
            }
            else
            {
                GUI.backgroundColor = errorColor;
                EditorGUILayout.HelpBox("❌ Please select a source animation clip to convert", MessageType.Error);
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTargetSettings()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🎯 Target Humanoid", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            targetAnimator = (Animator)EditorGUILayout.ObjectField("Target Animator", targetAnimator, typeof(Animator), true);

            if (EditorGUI.EndChangeCheck())
            {
                ValidateTargetSettings();
            }

            if (targetAnimator != null)
            {
                if (targetAnimator.isHuman && targetAnimator.avatar != null)
                {
                    EditorGUILayout.Space(5);
                    GUI.backgroundColor = accentColor;
                    EditorGUILayout.HelpBox($"✅ Valid humanoid animator: {targetAnimator.avatar.name}", MessageType.Info);
                    GUI.backgroundColor = Color.white;

                    EditorGUILayout.LabelField($"📋 Avatar Info:", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"   Muscles: {HumanTrait.MuscleCount}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"   Bones: {HumanTrait.BoneCount}", EditorStyles.miniLabel);
                }
                else
                {
                    GUI.backgroundColor = errorColor;
                    EditorGUILayout.HelpBox("❌ Target animator must be humanoid with a valid avatar", MessageType.Error);
                    GUI.backgroundColor = Color.white;
                }
            }
            else
            {
                GUI.backgroundColor = errorColor;
                EditorGUILayout.HelpBox("❌ Please assign a target humanoid animator", MessageType.Error);
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawConversionSettings()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("⚙️ Conversion Settings", EditorStyles.boldLabel);
            showAdvancedSettings = EditorGUILayout.Toggle("Advanced", showAdvancedSettings, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            outputClipName = EditorGUILayout.TextField("Output Clip Name", outputClipName);

            useOriginalFrameRate = EditorGUILayout.Toggle("Use Original Frame Rate", useOriginalFrameRate);

            if (!useOriginalFrameRate)
            {
                samplingRate = EditorGUILayout.FloatField("Custom Sampling Rate", samplingRate);
                samplingRate = Mathf.Clamp(samplingRate, 1f, 120f);
            }

            if (showAdvancedSettings)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("🔧 Advanced Options", EditorStyles.boldLabel);

                generateInPlace = EditorGUILayout.Toggle("Generate In-Place", generateInPlace);
                if (generateInPlace)
                {
                    EditorGUILayout.HelpBox("💡 In-place generation removes root motion from the animation", MessageType.Info);
                }

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField($"Estimated keyframes: {GetEstimatedKeyframes()}", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPreviewSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("👁️ Preview", EditorStyles.boldLabel);
            showPreviewSettings = EditorGUILayout.Toggle("Show Controls", showPreviewSettings, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            if (showPreviewSettings && CanPreview())
            {
                EditorGUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(isPlaying ? "⏸️ Pause" : "▶️ Play", GUILayout.Width(80)))
                {
                    TogglePreview();
                }

                if (GUILayout.Button("⏹️ Stop", GUILayout.Width(80)))
                {
                    StopPreview();
                }

                if (GUILayout.Button("🔄 Sample", GUILayout.Width(80)))
                {
                    StartSamplingForPreview();
                }
                EditorGUILayout.EndHorizontal();

                if (sourceClip != null)
                {
                    EditorGUILayout.Space(3);
                    previewTime = EditorGUILayout.Slider("Time", previewTime, 0f, sourceClip.length);
                    EditorGUILayout.LabelField($"Frame: {Mathf.FloorToInt(previewTime * sourceClip.frameRate)}", EditorStyles.miniLabel);
                }
            }
            else if (!CanPreview())
            {
                GUI.backgroundColor = warningColor;
                EditorGUILayout.HelpBox("⚠️ Preview requires both source clip and target animator", MessageType.Warning);
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawConversionControls()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🚀 Conversion", EditorStyles.boldLabel);

            bool canConvert = CanConvert();

            GUI.enabled = canConvert && !isConverting;
            GUI.backgroundColor = accentColor;
            if (GUILayout.Button("🎬 Convert Animation to Humanoid", GUILayout.Height(40)))
            {
                StartConversion();
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            if (isConverting)
            {
                EditorGUILayout.Space(5);
                GUI.backgroundColor = warningColor;
                if (GUILayout.Button("⏹️ Cancel Conversion", GUILayout.Height(30)))
                {
                    CancelConversion();
                }
                GUI.backgroundColor = Color.white;
            }

            if (!canConvert && !isConverting)
            {
                EditorGUILayout.Space(5);
                DrawConversionRequirements();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawProgressSection()
        {
            if (isConverting || !string.IsNullOrEmpty(currentStatus))
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("📊 Progress", EditorStyles.boldLabel);

                if (isConverting)
                {
                    EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), conversionProgress,
                        $"Frame {currentFrame}/{totalFrames} ({conversionProgress:P0})");

                    EditorGUILayout.Space(3);
                    EditorGUILayout.LabelField($"Status: {currentStatus}", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField($"Status: {currentStatus}", EditorStyles.miniLabel);
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawConversionRequirements()
        {
            EditorGUILayout.LabelField("Requirements:", EditorStyles.boldLabel);

            DrawRequirement("Source Animation Clip", sourceClip != null);
            DrawRequirement("Source Rig GameObject", sourceRig != null);
            DrawRequirement("Target Humanoid Animator", targetAnimator != null && targetAnimator.isHuman);
            DrawRequirement("Valid Avatar", targetAnimator != null && targetAnimator.avatar != null);
            DrawRequirement("Valid Output Name", !string.IsNullOrWhiteSpace(outputClipName));
        }

        private void DrawRequirement(string requirement, bool satisfied)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(satisfied ? "✅" : "❌", GUILayout.Width(20));
            EditorGUILayout.LabelField(requirement, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void ValidateSourceSettings()
        {
            if (sourceClip != null && sourceRig == null)
            {
                // Try to auto-detect source rig from selection
                if (Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<Animator>() != null)
                {
                    sourceRig = Selection.activeGameObject;
                }
            }
        }

        private void ValidateTargetSettings()
        {
            if (targetAnimator != null && targetAnimator.isHuman && targetAnimator.avatar != null)
            {
                // Initialize pose handler for preview
                if (poseHandler != null)
                {
                    poseHandler.Dispose();
                }
                poseHandler = new HumanPoseHandler(targetAnimator.avatar, targetAnimator.transform);
            }
        }

        private bool CanConvert()
        {
            return sourceClip != null &&
                   sourceRig != null &&
                   targetAnimator != null &&
                   targetAnimator.isHuman &&
                   targetAnimator.avatar != null &&
                   !string.IsNullOrWhiteSpace(outputClipName);
        }

        private bool CanPreview()
        {
            return sourceClip != null && targetAnimator != null && targetAnimator.isHuman;
        }

        private int GetEstimatedKeyframes()
        {
            if (sourceClip == null) return 0;

            float frameRate = useOriginalFrameRate ? sourceClip.frameRate : samplingRate;
            return Mathf.CeilToInt(sourceClip.length * frameRate);
        }

        private void StartConversion()
        {
            if (!CanConvert()) return;

            isConverting = true;
            conversionProgress = 0f;
            currentStatus = "Initializing conversion...";
            currentFrame = 0;

            StartConversionProcess();
        }

        private void CancelConversion()
        {
            isConverting = false;
            isProcessing = false;
            currentProcessingAction = null;
            currentStatus = "Conversion cancelled";
            conversionProgress = 0f;
        }

        private void StartConversionProcess()
        {
            // Calculate frame settings
            float frameRate = useOriginalFrameRate ? sourceClip.frameRate : samplingRate;
            totalFrames = Mathf.CeilToInt(sourceClip.length * frameRate);
            float timeStep = 1f / frameRate;

            currentStatus = $"Creating output clip ({totalFrames} frames)...";

            // Create output clip
            outputClip = new AnimationClip();
            outputClip.name = outputClipName;
            outputClip.frameRate = frameRate;
            outputClip.legacy = false;

            // Prepare animation curves for muscle values
            AnimationCurve[] muscleCurves = new AnimationCurve[HumanTrait.MuscleCount];
            for (int i = 0; i < HumanTrait.MuscleCount; i++)
            {
                muscleCurves[i] = new AnimationCurve();
            }

            // Body transform curves
            AnimationCurve bodyPosX = new AnimationCurve();
            AnimationCurve bodyPosY = new AnimationCurve();
            AnimationCurve bodyPosZ = new AnimationCurve();
            AnimationCurve bodyRotX = new AnimationCurve();
            AnimationCurve bodyRotY = new AnimationCurve();
            AnimationCurve bodyRotZ = new AnimationCurve();
            AnimationCurve bodyRotW = new AnimationCurve();

            currentStatus = "Sampling animation frames...";
            currentFrame = 0;

            // Set up processing action
            isProcessing = true;
            currentProcessingAction = () =>
            {
                if (currentFrame >= totalFrames)
                {
                    FinalizeConversion(muscleCurves, bodyPosX, bodyPosY, bodyPosZ, bodyRotX, bodyRotY, bodyRotZ, bodyRotW);
                    return;
                }

                // Process a few frames at a time
                int framesToProcess = Mathf.Min(3, totalFrames - currentFrame);
                for (int i = 0; i < framesToProcess; i++)
                {
                    int frame = currentFrame + i;
                    conversionProgress = (float)frame / totalFrames;

                    float time = frame * timeStep;
                    if (time > sourceClip.length) time = sourceClip.length;

                    // Sample the source animation at this time
                    sourceClip.SampleAnimation(sourceRig, time);

                    // Get human pose from current bone positions
                    HumanPose humanPose = new HumanPose();
                    poseHandler.GetHumanPose(ref humanPose);

                    // Add keyframes for muscles
                    for (int j = 0; j < HumanTrait.MuscleCount; j++)
                    {
                        muscleCurves[j].AddKey(time, humanPose.muscles[j]);
                    }

                    // Add keyframes for body transform
                    if (!generateInPlace)
                    {
                        bodyPosX.AddKey(time, humanPose.bodyPosition.x);
                        bodyPosY.AddKey(time, humanPose.bodyPosition.y);
                        bodyPosZ.AddKey(time, humanPose.bodyPosition.z);
                    }

                    bodyRotX.AddKey(time, humanPose.bodyRotation.x);
                    bodyRotY.AddKey(time, humanPose.bodyRotation.y);
                    bodyRotZ.AddKey(time, humanPose.bodyRotation.z);
                    bodyRotW.AddKey(time, humanPose.bodyRotation.w);
                }

                currentFrame += framesToProcess;
                currentStatus = $"Processing frame {currentFrame}/{totalFrames}...";

                if (!isConverting)
                {
                    isProcessing = false;
                    currentProcessingAction = null;
                }
            };
        }

        private void FinalizeConversion(AnimationCurve[] muscleCurves, AnimationCurve bodyPosX, AnimationCurve bodyPosY, AnimationCurve bodyPosZ,
                                       AnimationCurve bodyRotX, AnimationCurve bodyRotY, AnimationCurve bodyRotZ, AnimationCurve bodyRotW)
        {
            isProcessing = false;
            currentProcessingAction = null;

            currentStatus = "Finalizing animation clip...";

            // Apply curves to the output clip
            for (int i = 0; i < HumanTrait.MuscleCount; i++)
            {
                string muscleName = HumanTrait.MuscleName[i];
                outputClip.SetCurve("", typeof(Animator), muscleName, muscleCurves[i]);
            }

            // Apply body transform curves
            if (!generateInPlace)
            {
                outputClip.SetCurve("", typeof(Animator), "RootT.x", bodyPosX);
                outputClip.SetCurve("", typeof(Animator), "RootT.y", bodyPosY);
                outputClip.SetCurve("", typeof(Animator), "RootT.z", bodyPosZ);
            }

            outputClip.SetCurve("", typeof(Animator), "RootQ.x", bodyRotX);
            outputClip.SetCurve("", typeof(Animator), "RootQ.y", bodyRotY);
            outputClip.SetCurve("", typeof(Animator), "RootQ.z", bodyRotZ);
            outputClip.SetCurve("", typeof(Animator), "RootQ.w", bodyRotW);

            currentStatus = "Saving animation clip...";

            // Save the clip
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Humanoid Animation Clip",
                outputClipName + ".anim",
                "anim",
                "Save the converted humanoid animation clip");

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(outputClip, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                currentStatus = $"✅ Conversion completed! Saved to: {path}";

                // Select the created clip
                Selection.activeObject = outputClip;
                EditorGUIUtility.PingObject(outputClip);
            }
            else
            {
                currentStatus = "❌ Conversion cancelled - no save location selected";
            }

            conversionProgress = 1f;
            isConverting = false;
        }

        private void TogglePreview()
        {
            isPlaying = !isPlaying;
            if (isPlaying && sampledPoses.Count == 0)
            {
                StartSamplingForPreview();
            }
        }

        private void StopPreview()
        {
            isPlaying = false;
            previewTime = 0f;
        }

        private void StartSamplingForPreview()
        {
            if (!CanPreview()) return;

            sampledPoses.Clear();
            int sampleFrames = Mathf.Min(60, Mathf.CeilToInt(sourceClip.length * 30f));

            for (int i = 0; i < sampleFrames; i++)
            {
                float time = (float)i / (sampleFrames - 1) * sourceClip.length;
                sourceClip.SampleAnimation(sourceRig, time);

                HumanPose pose = new HumanPose();
                poseHandler.GetHumanPose(ref pose);
                sampledPoses.Add(pose);
            }

            currentStatus = $"Preview ready ({sampledPoses.Count} poses sampled)";
        }

        private void UpdatePreview()
        {
            if (!isPlaying || sampledPoses.Count == 0) return;

            float currentTime = (float)EditorApplication.timeSinceStartup;
            float deltaTime = currentTime - lastUpdateTime;
            lastUpdateTime = currentTime;

            previewTime += deltaTime;
            if (previewTime >= sourceClip.length)
            {
                previewTime = 0f;
            }

            // Find the appropriate pose
            float normalizedTime = previewTime / sourceClip.length;
            int poseIndex = Mathf.FloorToInt(normalizedTime * (sampledPoses.Count - 1));
            poseIndex = Mathf.Clamp(poseIndex, 0, sampledPoses.Count - 1);

            // Apply the pose to the target animator
            HumanPose currentPose = sampledPoses[poseIndex];
            poseHandler.SetHumanPose(ref currentPose);
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;

            if (poseHandler != null)
            {
                poseHandler.Dispose();
                poseHandler = null;
            }

            isConverting = false;
            isProcessing = false;
            isPlaying = false;
            currentProcessingAction = null;
        }
    }
}