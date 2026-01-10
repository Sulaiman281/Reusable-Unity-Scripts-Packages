using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Animations;
using WitShells.WitPose.Editor.Core;
using WitShells.WitPose.Editor.SceneGizmos;

namespace WitShells.WitPose.Editor
{
    /// <summary>
    /// Unified WitPose Editor - Main Window Class
    /// Handles Lifecycle, State, and UI Tab orchestration.
    /// </summary>
    public partial class WitPoseEditor : EditorWindow
    {
        // ===== TAB SYSTEM =====
        private enum EditorTab
        {
            PoseLibrary,
            RigBuilder,
            MuscleEditor,
            Settings
        }
        private EditorTab currentTab = EditorTab.PoseLibrary;

        // ===== TARGET SETUP =====
        private Animator targetAnimator;
        private bool isPoseModeActive = false;

        // ===== CORE SYSTEMS =====
        private SkeletonCache skeleton;
        private BonePoseSystem bonePoseSystem;
        private MusclePoseSystem musclePoseSystem;
        private BoneGizmoSystem gizmoSystem;

        // ===== UI STATE =====
        private Vector2 scrollPosition;
        private Vector2 muscleScrollPosition;

        private Vector2 poseLibraryScrollPosition;
        private Vector2 rigBuilderScrollPosition;
        private Dictionary<string, bool> muscleGroupFoldouts = new Dictionary<string, bool>();

        // ===== POSE LIBRARY =====
        private string newPoseName = "New Pose";
        private int selectedPoseIndex = 0;
        private string[] poseNames = new string[0];

        // ===== RIG BUILDER STATE =====
        private Transform skeletonRoot;
        private GameObject poseControlsRoot;
        private Transform duplicateRoot;
        private Dictionary<Transform, Transform> originalToProxy = new Dictionary<Transform, Transform>();
        private Dictionary<UnityEngine.Animations.ParentConstraint, Transform> constraintMap = new Dictionary<UnityEngine.Animations.ParentConstraint, Transform>();
        private bool rigBuilt = false;
        private bool autoDetectRig = true;
        private bool showAdvancedRigSettings = false;

        // ===== ANIMATION TRACKING =====
        private bool enableMuscleTracking = false;
        private AnimationClip targetAnimationClip;
        private float manualKeyframeTime = 0f;
        private bool useAnimationWindowTime = true;
        private Dictionary<int, float> lastMuscleValues = new Dictionary<int, float>();
        private bool muscleTrackingInitialized = false;

        // ===== ROOT POSITION CONTROL =====
        private Vector3 rootPosition = Vector3.zero;
        private Quaternion rootRotation = Quaternion.identity;
        private Vector3 lastRootPosition = Vector3.zero;
        private Quaternion lastRootRotation = Quaternion.identity;
        private bool rootPositionFoldout = true;

        // ===== UI COLORS =====
        private Color accentColor = new Color(0.3f, 0.6f, 1f);
        private Color successColor = new Color(0.5f, 1f, 0.5f);
        private Color warningColor = new Color(1f, 0.8f, 0.3f);
        private Color errorColor = new Color(1f, 0.5f, 0.5f);

        // ===== BONE SELECTION & MINI HUD =====
        private HumanBodyBones selectedBone = HumanBodyBones.LastBone;
        private bool showBoneMuscleHUD = false;
        private Vector2 boneMuscleScrollPosition = Vector2.zero;
        private Rect boneMuscleHUDRect = new Rect();
        private Dictionary<HumanBodyBones, int[]> boneToMuscleMapping;

        // ===== CLIPBOARD =====
        private static float[] copiedMuscleValues;

        [MenuItem("Window/WitPose/Animation Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<WitPoseEditor>("WitPose Studio");
            window.minSize = new Vector2(500, 700);
            window.Show();
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
            EditorApplication.update += OnEditorUpdate;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.update -= OnEditorUpdate;
            SceneView.duringSceneGui -= OnSceneGUI;

            // Stop recording repaint updates
            EditorApplication.update -= RepaintWhileRecording;

            ExitPoseMode();
        }

        private void OnEditorUpdate()
        {
            // Monitor animation mode state and automatically disable if needed
            if (enableMuscleTracking && targetAnimationClip != null && AnimationMode.InAnimationMode())
            {
                // If recording mode is active but animation preview got enabled again, disable it
                AnimationMode.StopAnimationMode();
                Debug.Log("🎬 Animation Preview automatically disabled to maintain direct muscle control");
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            // Draw mini bone muscle HUD in scene view
            if (showBoneMuscleHUD && selectedBone != HumanBodyBones.LastBone && isPoseModeActive)
            {
                DrawSceneViewBoneMuscleHUD();
            }
        }

        private void OnUndoRedo()
        {
            if (isPoseModeActive)
            {
                musclePoseSystem?.SyncFromSkeleton();
                Repaint();
            }
        }

        private void OnGUI()
        {
            DrawTabBar();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            DrawTargetSetup();

            if (isPoseModeActive && targetAnimator != null)
            {
                EditorGUILayout.Space(5);

                // Draw content based on selected tab
                switch (currentTab)
                {
                    case EditorTab.PoseLibrary:
                        DrawPoseLibraryTab();
                        break;
                    case EditorTab.RigBuilder:
                        DrawRigBuilderTab();
                        break;
                    case EditorTab.MuscleEditor:
                        DrawMuscleEditorTab();
                        break;
                    case EditorTab.Settings:
                        DrawSettingsTab();
                        break;
                }
            }

            EditorGUILayout.EndScrollView();
        }

        // ===== TAB BAR =====

        private void DrawTabBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(30), GUILayout.ExpandWidth(true));

            // Title
            GUIStyle titleStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };
            GUILayout.Label("🎭 WitPose Studio", titleStyle, GUILayout.Width(150));

            GUILayout.FlexibleSpace();

            if (DrawTabButton("📚 Pose Library", currentTab == EditorTab.PoseLibrary))
                currentTab = EditorTab.PoseLibrary;

            if (DrawTabButton("🎭 Rig Builder", currentTab == EditorTab.RigBuilder))
                currentTab = EditorTab.RigBuilder;

            if (DrawTabButton("💪 Muscle Editor", currentTab == EditorTab.MuscleEditor))
                currentTab = EditorTab.MuscleEditor;

            if (DrawTabButton("⚙️ Settings", currentTab == EditorTab.Settings))
                currentTab = EditorTab.Settings;

            EditorGUILayout.EndHorizontal();
        }

        private bool DrawTabButton(string label, bool isActive)
        {
            GUIStyle style = new GUIStyle(EditorStyles.toolbarButton);
            if (isActive)
            {
                style.normal.background = style.active.background;
                style.fontStyle = FontStyle.Bold;
            }
            return GUILayout.Button(label, style, GUILayout.MinWidth(120));
        }

        private void DrawTargetSetup()
        {
            EditorGUILayout.LabelField("Target Setup", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            targetAnimator = (Animator)EditorGUILayout.ObjectField("Target Animator", targetAnimator, typeof(Animator), true);

            if (EditorGUI.EndChangeCheck())
            {
                if (targetAnimator != null && !targetAnimator.isHuman)
                {
                    Debug.LogError("Selected Animator must be a Humanoid rig!");
                    targetAnimator = null;
                }
            }

            EditorGUILayout.Space();

            if (targetAnimator == null)
            {
                EditorGUILayout.HelpBox("Assign a Humanoid Animator to begin.", MessageType.Warning);
                return;
            }

            // Pose mode toggle
            GUI.backgroundColor = isPoseModeActive ? Color.red : Color.green;
            string buttonText = isPoseModeActive ? "Exit Pose Mode" : "Enter Pose Mode";

            if (GUILayout.Button(buttonText, GUILayout.Height(40)))
            {
                if (isPoseModeActive)
                {
                    ExitPoseMode();
                }
                else
                {
                    EnterPoseMode();
                }
            }
            GUI.backgroundColor = Color.white;
        }

        private void EnterPoseMode()
        {
            if (targetAnimator == null || !targetAnimator.isHuman)
            {
                Debug.LogError("Cannot enter pose mode: Animator must be humanoid!");
                return;
            }

            isPoseModeActive = true;

            // Initialize core systems
            skeleton = new SkeletonCache(targetAnimator);
            bonePoseSystem = new BonePoseSystem(skeleton);
            musclePoseSystem = new MusclePoseSystem(targetAnimator, skeleton, bonePoseSystem);
            gizmoSystem = new BoneGizmoSystem(skeleton, bonePoseSystem, this);

            // Activate gizmos
            gizmoSystem.Activate();

            // Initialize muscle group foldouts
            muscleGroupFoldouts.Clear();
            muscleGroupFoldouts["Body"] = true;
            muscleGroupFoldouts["Head"] = false;
            muscleGroupFoldouts["Left Arm"] = false;
            muscleGroupFoldouts["Right Arm"] = false;
            muscleGroupFoldouts["Left Leg"] = false;
            muscleGroupFoldouts["Right Leg"] = false;
            muscleGroupFoldouts["Left Fingers"] = false;
            muscleGroupFoldouts["Right Fingers"] = false;

            // Initialize muscle tracking
            InitializeMuscleTracking();

            // Initialize bone-to-muscle mapping
            InitializeBoneToMuscleMapping();

            Debug.Log("✅ Animation Editor Active - Scene Gizmos Enabled");
        }

        private void ExitPoseMode()
        {
            isPoseModeActive = false;

            // Cleanup
            gizmoSystem?.Deactivate();
            skeleton = null;
            bonePoseSystem = null;
            musclePoseSystem = null;
            gizmoSystem = null;

            // Reset bone selection
            selectedBone = HumanBodyBones.LastBone;
            showBoneMuscleHUD = false;

            Debug.Log("Exited Animation Editor");
        }

        // ===== BONE SELECTION & MUSCLE MAPPING =====

        private void InitializeBoneToMuscleMapping()
        {
            boneToMuscleMapping = new Dictionary<HumanBodyBones, int[]>();

            // Spine and Core (muscles 0-8)
            boneToMuscleMapping[HumanBodyBones.Spine] = new int[] { 0, 1, 2 }; // Spine Front-Back, Left-Right, Twist Left-Right
            boneToMuscleMapping[HumanBodyBones.Chest] = new int[] { 3, 4, 5 }; // Chest Front-Back, Left-Right, Twist Left-Right
            boneToMuscleMapping[HumanBodyBones.UpperChest] = new int[] { 6, 7, 8 }; // Upper Chest Front-Back, Left-Right, Twist Left-Right

            // Head and Neck (muscles 9-14)
            boneToMuscleMapping[HumanBodyBones.Neck] = new int[] { 9, 10, 11 }; // Neck Nod Down-Up, Tilt Left-Right, Turn Left-Right
            boneToMuscleMapping[HumanBodyBones.Head] = new int[] { 12, 13, 14 }; // Head Nod Down-Up, Tilt Left-Right, Turn Left-Right

            // Left Leg (muscles 21-28) - ACTUAL Unity indices from debug log
            boneToMuscleMapping[HumanBodyBones.LeftUpperLeg] = new int[] { 21, 22, 23 }; // Left Upper Leg Front-Back, In-Out, Twist In-Out
            boneToMuscleMapping[HumanBodyBones.LeftLowerLeg] = new int[] { 24, 25 }; // Left Lower Leg Stretch, Twist In-Out
            boneToMuscleMapping[HumanBodyBones.LeftFoot] = new int[] { 26, 27 }; // Left Foot Up-Down, Twist In-Out  
            boneToMuscleMapping[HumanBodyBones.LeftToes] = new int[] { 28 }; // Left Toes Up-Down

            // Right Leg (muscles 29-36) - ACTUAL Unity indices from debug log  
            boneToMuscleMapping[HumanBodyBones.RightUpperLeg] = new int[] { 29, 30, 31 }; // Right Upper Leg Front-Back, In-Out, Twist In-Out
            boneToMuscleMapping[HumanBodyBones.RightLowerLeg] = new int[] { 32, 33 }; // Right Lower Leg Stretch, Twist In-Out
            boneToMuscleMapping[HumanBodyBones.RightFoot] = new int[] { 34, 35 }; // Right Foot Up-Down, Twist In-Out
            boneToMuscleMapping[HumanBodyBones.RightToes] = new int[] { 36 }; // Right Toes Up-Down

            // Left Arm (muscles 37-45) - ACTUAL Unity indices from debug log
            boneToMuscleMapping[HumanBodyBones.LeftShoulder] = new int[] { 37, 38 }; // Left Shoulder Down-Up, Front-Back
            boneToMuscleMapping[HumanBodyBones.LeftUpperArm] = new int[] { 39, 40, 41 }; // Left Arm Down-Up, Front-Back, Twist In-Out
            boneToMuscleMapping[HumanBodyBones.LeftLowerArm] = new int[] { 42, 43 }; // Left Forearm Stretch, Twist In-Out
            boneToMuscleMapping[HumanBodyBones.LeftHand] = new int[] { 44, 45 }; // Left Hand Down-Up, In-Out

            // Right Arm (muscles 46-54) - ACTUAL Unity indices from debug log
            boneToMuscleMapping[HumanBodyBones.RightShoulder] = new int[] { 46, 47 }; // Right Shoulder Down-Up, Front-Back
            boneToMuscleMapping[HumanBodyBones.RightUpperArm] = new int[] { 48, 49, 50 }; // Right Arm Down-Up, Front-Back, Twist In-Out
            boneToMuscleMapping[HumanBodyBones.RightLowerArm] = new int[] { 51, 52 }; // Right Forearm Stretch, Twist In-Out
            boneToMuscleMapping[HumanBodyBones.RightHand] = new int[] { 53, 54 }; // Right Hand Down-Up, In-Out

            // Left Fingers (muscles 55-78) - ACTUAL Unity indices from debug log
            boneToMuscleMapping[HumanBodyBones.LeftThumbProximal] = new int[] { 55, 56 }; // Left Thumb 1 Stretched, Spread
            boneToMuscleMapping[HumanBodyBones.LeftThumbIntermediate] = new int[] { 57 }; // Left Thumb 2 Stretched
            boneToMuscleMapping[HumanBodyBones.LeftThumbDistal] = new int[] { 58 }; // Left Thumb 3 Stretched
            boneToMuscleMapping[HumanBodyBones.LeftIndexProximal] = new int[] { 59, 60 }; // Left Index 1 Stretched, Spread
            boneToMuscleMapping[HumanBodyBones.LeftIndexIntermediate] = new int[] { 61 }; // Left Index 2 Stretched
            boneToMuscleMapping[HumanBodyBones.LeftIndexDistal] = new int[] { 62 }; // Left Index 3 Stretched
            boneToMuscleMapping[HumanBodyBones.LeftMiddleProximal] = new int[] { 63, 64 }; // Left Middle 1 Stretched, Spread
            boneToMuscleMapping[HumanBodyBones.LeftMiddleIntermediate] = new int[] { 65 }; // Left Middle 2 Stretched
            boneToMuscleMapping[HumanBodyBones.LeftMiddleDistal] = new int[] { 66 }; // Left Middle 3 Stretched
            boneToMuscleMapping[HumanBodyBones.LeftRingProximal] = new int[] { 67, 68 }; // Left Ring 1 Stretched, Spread
            boneToMuscleMapping[HumanBodyBones.LeftRingIntermediate] = new int[] { 69 }; // Left Ring 2 Stretched
            boneToMuscleMapping[HumanBodyBones.LeftRingDistal] = new int[] { 70 }; // Left Ring 3 Stretched
            boneToMuscleMapping[HumanBodyBones.LeftLittleProximal] = new int[] { 71, 72 }; // Left Little 1 Stretched, Spread
            boneToMuscleMapping[HumanBodyBones.LeftLittleIntermediate] = new int[] { 73 }; // Left Little 2 Stretched
            boneToMuscleMapping[HumanBodyBones.LeftLittleDistal] = new int[] { 74 }; // Left Little 3 Stretched

            // Right Fingers (muscles 75-94) - ACTUAL Unity indices from debug log  
            boneToMuscleMapping[HumanBodyBones.RightThumbProximal] = new int[] { 75, 76 }; // Right Thumb 1 Stretched, Spread
            boneToMuscleMapping[HumanBodyBones.RightThumbIntermediate] = new int[] { 77 }; // Right Thumb 2 Stretched
            boneToMuscleMapping[HumanBodyBones.RightThumbDistal] = new int[] { 78 }; // Right Thumb 3 Stretched
            boneToMuscleMapping[HumanBodyBones.RightIndexProximal] = new int[] { 79, 80 }; // Right Index 1 Stretched, Spread
            boneToMuscleMapping[HumanBodyBones.RightIndexIntermediate] = new int[] { 81 }; // Right Index 2 Stretched
            boneToMuscleMapping[HumanBodyBones.RightIndexDistal] = new int[] { 82 }; // Right Index 3 Stretched
            boneToMuscleMapping[HumanBodyBones.RightMiddleProximal] = new int[] { 83, 84 }; // Right Middle 1 Stretched, Spread
            boneToMuscleMapping[HumanBodyBones.RightMiddleIntermediate] = new int[] { 85 }; // Right Middle 2 Stretched
            boneToMuscleMapping[HumanBodyBones.RightMiddleDistal] = new int[] { 86 }; // Right Middle 3 Stretched
            boneToMuscleMapping[HumanBodyBones.RightRingProximal] = new int[] { 87, 88 }; // Right Ring 1 Stretched, Spread
            boneToMuscleMapping[HumanBodyBones.RightRingIntermediate] = new int[] { 89 }; // Right Ring 2 Stretched
            boneToMuscleMapping[HumanBodyBones.RightRingDistal] = new int[] { 90 }; // Right Ring 3 Stretched
            boneToMuscleMapping[HumanBodyBones.RightLittleProximal] = new int[] { 91, 92 }; // Right Little 1 Stretched, Spread
            boneToMuscleMapping[HumanBodyBones.RightLittleIntermediate] = new int[] { 93 }; // Right Little 2 Stretched
            boneToMuscleMapping[HumanBodyBones.RightLittleDistal] = new int[] { 94 }; // Right Little 3 Stretched
        }

        /// <summary>
        /// Public method to be called by BoneGizmoSystem when a bone is selected
        /// </summary>
        public void SelectBone(HumanBodyBones bone)
        {
            if (!isPoseModeActive || musclePoseSystem == null)
                return;

            if (bone == selectedBone)
            {
                // Toggle off if same bone is selected again
                selectedBone = HumanBodyBones.LastBone;
                showBoneMuscleHUD = false;
            }
            else
            {
                selectedBone = bone;
                showBoneMuscleHUD = boneToMuscleMapping.ContainsKey(bone);

                if (showBoneMuscleHUD)
                {
                    UpdateHUDPosition();

                    // Debug validation - log the actual muscle names being mapped
                    int[] muscleIndices = boneToMuscleMapping[selectedBone];
                    string muscleNames = "";
                    foreach (int index in muscleIndices)
                    {
                        if (index < HumanTrait.MuscleCount)
                        {
                            string muscleName = HumanTrait.MuscleName[index];
                            muscleNames += $"[{index}]{muscleName}, ";
                        }
                    }

                    Debug.Log($"🦴 Selected bone: {GetBoneDisplayName(selectedBone)} - Muscles: {muscleNames.TrimEnd(',', ' ')}");
                }
            }

            Repaint();
        }

        private void UpdateHUDPosition()
        {
            // Position HUD in bottom-right corner of the editor window
            float hudWidth = 320f;
            float hudHeight = 300f;
            float margin = 10f;

            boneMuscleHUDRect = new Rect(
                position.width - hudWidth - margin,
                position.height - hudHeight - margin - 50f, // Extra margin for status bar
                hudWidth,
                hudHeight
            );
        }

        private void DrawSceneViewBoneMuscleHUD()
        {
            if (musclePoseSystem == null || !boneToMuscleMapping.ContainsKey(selectedBone))
                return;

            // Position HUD in bottom-right corner of scene view
            Handles.BeginGUI();

            float hudWidth = 320f;
            float hudHeight = 300f;
            float margin = 10f;

            Rect hudRect = new Rect(
                SceneView.currentDrawingSceneView.position.width - hudWidth - margin,
                SceneView.currentDrawingSceneView.position.height - hudHeight - margin - 50f,
                hudWidth,
                hudHeight
            );

            // Create a unique window ID
            int windowId = GetHashCode() + (int)selectedBone + 1000;

            // Draw the HUD window
            boneMuscleHUDRect = GUI.Window(windowId, hudRect, DrawBoneMuscleHUDContent,
                $"🦴 {GetBoneDisplayName(selectedBone)}", GUI.skin.window);

            Handles.EndGUI();
        }

        private void DrawBoneMuscleHUDContent(int windowId)
        {
            if (musclePoseSystem == null || !boneToMuscleMapping.ContainsKey(selectedBone))
            {
                GUI.DragWindow();
                return;
            }

            // Header with bone info and close button
            Rect headerRect = new Rect(5, 20, 290, 25);
            Rect closeRect = new Rect(270, 22, 20, 20);

            GUI.Label(new Rect(5, 22, 200, 20), $"🎯 {GetBoneDisplayName(selectedBone)}", EditorStyles.boldLabel);

            GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
            if (GUI.Button(closeRect, "✖"))
            {
                showBoneMuscleHUD = false;
                selectedBone = HumanBodyBones.LastBone;
            }
            GUI.backgroundColor = Color.white;

            // Show muscle count
            int[] muscleIndices = boneToMuscleMapping[selectedBone];
            GUI.Label(new Rect(5, 45, 200, 15), $"Muscles: {muscleIndices.Length}", EditorStyles.miniLabel);

            float currentY = 65;
            
            // Special fist control for hand bones
            if (IsHandBone(selectedBone))
            {
                // Fist control header
                GUI.Label(new Rect(5, currentY, 200, 15), "👊 Fist Control", EditorStyles.boldLabel);
                currentY += 18;
                
                // Fist slider
                float currentFistValue = GetHandFistValue(selectedBone);
                GUI.Label(new Rect(5, currentY, 50, 15), "Fist:", EditorStyles.miniLabel);
                
                EditorGUI.BeginChangeCheck();
                float newFistValue = GUI.HorizontalSlider(new Rect(55, currentY, 180, 15), currentFistValue, -1f, 1f);
                GUI.Label(new Rect(240, currentY, 40, 15), $"{newFistValue:F2}", EditorStyles.miniLabel);
                
                if (EditorGUI.EndChangeCheck())
                {
                    SetHandFist(selectedBone, newFistValue);
                }
                
                currentY += 20;
                
                // Quick fist buttons
                if (GUI.Button(new Rect(5, currentY, 70, 18), "✋ Open"))
                {
                    SetHandFist(selectedBone, -1f);
                }
                if (GUI.Button(new Rect(80, currentY, 70, 18), "👊 Fist"))
                {
                    SetHandFist(selectedBone, 1f);
                }
                if (GUI.Button(new Rect(155, currentY, 70, 18), "🤏 Half"))
                {
                    SetHandFist(selectedBone, 0.5f);
                }
                
                currentY += 25;
                
                // Separator
                GUI.Box(new Rect(5, currentY, 290, 1), "");
                currentY += 8;
                
                GUI.Label(new Rect(5, currentY, 200, 15), "🎚️ Individual Muscles", EditorStyles.boldLabel);
                currentY += 18;
            }

            // Muscle sliders area with scroll view
            float scrollViewHeight = IsHandBone(selectedBone) ? 80f : 180f; // Smaller if we have fist controls
            Rect scrollViewRect = new Rect(5, currentY, 290, scrollViewHeight);
            Rect scrollContentRect = new Rect(0, 0, 270, muscleIndices.Length * 22);

            boneMuscleScrollPosition = GUI.BeginScrollView(scrollViewRect, boneMuscleScrollPosition, scrollContentRect);

            for (int i = 0; i < muscleIndices.Length; i++)
            {
                int muscleIndex = muscleIndices[i];
                if (muscleIndex < HumanTrait.MuscleCount)
                {
                    // Draw slider directly in scroll content coordinates
                    DrawCompactMuscleSliderInRect(muscleIndex, new Rect(0, i * 22, 270, 20));
                }
            }

            GUI.EndScrollView();

            // Quick actions for selected bone (position varies based on content above)
            float buttonY = IsHandBone(selectedBone) ? 255f : 255f; // Same position for now, but could adjust
            Rect resetRect = new Rect(5, buttonY, 140, 25);
            Rect randomRect = new Rect(150, buttonY, 140, 25);

            GUI.backgroundColor = new Color(1f, 0.8f, 0.8f);
            if (GUI.Button(resetRect, "🔄 Reset"))
            {
                ResetSelectedBoneMuscles();
            }

            GUI.backgroundColor = new Color(0.8f, 0.8f, 1f);
            if (GUI.Button(randomRect, "🎲 Random"))
            {
                RandomizeSelectedBoneMuscles();
            }

            GUI.backgroundColor = Color.white;

            // Make window draggable (but avoid the slider areas)
            Rect dragRect = new Rect(0, 0, 300, 20);
            GUI.DragWindow(dragRect);
        }

        private void ResetSelectedBoneMuscles()
        {
            if (musclePoseSystem == null || !boneToMuscleMapping.ContainsKey(selectedBone))
                return;

            Undo.RecordObject(targetAnimator, $"Reset {GetBoneDisplayName(selectedBone)} Muscles");

            int[] muscleIndices = boneToMuscleMapping[selectedBone];

            foreach (int muscleIndex in muscleIndices)
            {
                if (muscleIndex < HumanTrait.MuscleCount)
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
            }

            Debug.Log($"🔄 Reset muscles for {GetBoneDisplayName(selectedBone)}");
        }

        private void RandomizeSelectedBoneMuscles()
        {
            if (musclePoseSystem == null || !boneToMuscleMapping.ContainsKey(selectedBone))
                return;

            Undo.RecordObject(targetAnimator, $"Randomize {GetBoneDisplayName(selectedBone)} Muscles");

            int[] muscleIndices = boneToMuscleMapping[selectedBone];

            foreach (int muscleIndex in muscleIndices)
            {
                if (muscleIndex < HumanTrait.MuscleCount)
                {
                    float randomValue = UnityEngine.Random.Range(-0.6f, 0.6f);

                    // In recording mode, write to both animation clip AND apply directly to muscles
                    if (enableMuscleTracking && targetAnimationClip != null)
                    {
                        SetMuscleValueToClip(muscleIndex, randomValue); // Record keyframe
                        musclePoseSystem.SetMuscle(muscleIndex, randomValue, autoKey: false); // Apply directly for immediate feedback
                    }
                    else
                    {
                        // Normal mode - directly control avatar
                        musclePoseSystem.SetMuscle(muscleIndex, randomValue, autoKey: bonePoseSystem.AutoKey);
                    }
                }
            }

            Debug.Log($"🎲 Randomized muscles for {GetBoneDisplayName(selectedBone)}");
        }

        private string GetBoneDisplayName(HumanBodyBones bone)
        {
            string boneName = bone.ToString();

            // Clean up the bone name for display
            boneName = boneName.Replace("Human", "").Replace("BodyBones", "");

            // Add spaces before capitals (camelCase to Title Case)
            string result = "";
            for (int i = 0; i < boneName.Length; i++)
            {
                if (i > 0 && char.IsUpper(boneName[i]) && !char.IsUpper(boneName[i - 1]))
                {
                    result += " ";
                }
                result += boneName[i];
            }

            return result;
        }

        /// <summary>
        /// Get all finger muscle indices for a specific hand
        /// </summary>
        private int[] GetFingerMusclesForHand(HumanBodyBones handBone)
        {
            if (handBone == HumanBodyBones.LeftHand)
            {
                // Left fingers: muscles 55-74 (all finger joints)
                return new int[] { 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74 };
            }
            else if (handBone == HumanBodyBones.RightHand)
            {
                // Right fingers: muscles 75-94 (all finger joints)
                return new int[] { 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94 };
            }
            return new int[0];
        }

        /// <summary>
        /// Set fist value (-1 = open hand, 1 = closed fist)
        /// </summary>
        private void SetHandFist(HumanBodyBones handBone, float fistValue)
        {
            if (musclePoseSystem == null) return;

            int[] fingerMuscles = GetFingerMusclesForHand(handBone);
            if (fingerMuscles.Length == 0) return;

            Undo.RecordObject(targetAnimator, $"Set {GetBoneDisplayName(handBone)} Fist");

            foreach (int muscleIndex in fingerMuscles)
            {
                if (muscleIndex < HumanTrait.MuscleCount)
                {
                    // Convert fist value to muscle value (fingers close with positive values)
                    float muscleValue = fistValue;

                    // In recording mode, write to both animation clip AND apply directly to muscles
                    if (enableMuscleTracking && targetAnimationClip != null)
                    {
                        SetMuscleValueToClip(muscleIndex, muscleValue); // Record keyframe
                        musclePoseSystem.SetMuscle(muscleIndex, muscleValue, autoKey: false); // Apply directly for immediate feedback
                    }
                    else
                    {
                        // Normal mode - directly control avatar
                        musclePoseSystem.SetMuscle(muscleIndex, muscleValue, autoKey: bonePoseSystem.AutoKey);
                    }
                }
            }
        }

        /// <summary>
        /// Get current average fist value for a hand (-1 = open, 1 = closed)
        /// </summary>
        private float GetHandFistValue(HumanBodyBones handBone)
        {
            if (musclePoseSystem == null) return 0f;

            int[] fingerMuscles = GetFingerMusclesForHand(handBone);
            if (fingerMuscles.Length == 0) return 0f;

            float totalValue = 0f;
            int validMuscles = 0;

            foreach (int muscleIndex in fingerMuscles)
            {
                if (muscleIndex < HumanTrait.MuscleCount)
                {
                    float muscleValue;
                    if (enableMuscleTracking && targetAnimationClip != null)
                    {
                        // In recording mode, read from animation clip
                        muscleValue = GetMuscleValueFromClip(muscleIndex);
                    }
                    else
                    {
                        // Normal mode, get from muscle pose system
                        muscleValue = musclePoseSystem.MuscleValues[muscleIndex];
                    }
                    
                    totalValue += muscleValue;
                    validMuscles++;
                }
            }

            return validMuscles > 0 ? totalValue / validMuscles : 0f;
        }

        /// <summary>
        /// Check if the selected bone is a hand bone
        /// </summary>
        private bool IsHandBone(HumanBodyBones bone)
        {
            return bone == HumanBodyBones.LeftHand || bone == HumanBodyBones.RightHand;
        }

        /// <summary>
        /// Repaint function for recording visual effects
        /// </summary>
        private void RepaintWhileRecording()
        {
            if (enableMuscleTracking)
            {
                Repaint();
            }
        }
    }
}