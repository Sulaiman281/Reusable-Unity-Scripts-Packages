namespace WitShells.AnimationRig.Editor
{
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Animations.Rigging;
    using System.Collections.Generic;

    /// <summary>
    /// Editor window for setting up Animation Rigging on humanoid characters.
    /// Automatically detects bones or allows manual assignment.
    /// </summary>
    public class RigSetupWindow : EditorWindow
    {
        // Target actor
        private GameObject targetActor;
        private Animator animator;

        // Bone references
        private Transform head;
        private Transform spine;
        private Transform leftHand;
        private Transform rightHand;
        private Transform leftFoot;
        private Transform rightFoot;

        // UI State
        private Vector2 scrollPosition;
        private bool bonesAutoDetected;
        private int currentStep = 0;
        private bool showAdvancedOptions;

        // Settings
        private bool createHeadConstraint = true;
        private bool createHandConstraints = true;
        private bool createFootConstraints = true;
        private bool addRigReferences = true;
        private bool addConstraintController = true;

        // Styles
        private GUIStyle headerStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle boxStyle;
        private GUIStyle successStyle;
        private GUIStyle warningStyle;
        private bool stylesInitialized;

        // Default bone name patterns for auto-detection
        private static readonly string[] HeadPatterns = { "Head", "head", "HEAD" };
        private static readonly string[] SpinePatterns = { "Spine", "spine", "SPINE", "Chest", "chest" };
        private static readonly string[] LeftHandPatterns = { "LeftHand", "Left_Hand", "L_Hand", "Hand_L", "hand.L", "Left Hand" };
        private static readonly string[] RightHandPatterns = { "RightHand", "Right_Hand", "R_Hand", "Hand_R", "hand.R", "Right Hand" };
        private static readonly string[] LeftFootPatterns = { "LeftFoot", "Left_Foot", "L_Foot", "Foot_L", "foot.L", "Left Foot" };
        private static readonly string[] RightFootPatterns = { "RightFoot", "Right_Foot", "R_Foot", "Foot_R", "foot.R", "Right Foot" };

        [MenuItem("WitShells/Animation Rig/Rig Setup Wizard")]
        public static void ShowWindow()
        {
            var window = GetWindow<RigSetupWindow>("Rig Setup Wizard");
            window.minSize = new Vector2(450, 550);
            window.maxSize = new Vector2(600, 800);
            window.Initialize();
        }

        [MenuItem("WitShells/Animation Rig/Rig Setup Wizard", true)]
        public static bool ValidateShowWindow()
        {
            return Selection.activeGameObject != null;
        }

        private void Initialize()
        {
            targetActor = Selection.activeGameObject;
            currentStep = 0;
            bonesAutoDetected = false;

            if (targetActor != null)
            {
                animator = targetActor.GetComponent<Animator>();
                TryAutoDetectBones();
            }
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(0, 0, 10, 10)
            };

            subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                margin = new RectOffset(0, 0, 5, 5)
            };

            boxStyle = new GUIStyle("box")
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };

            successStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.2f, 0.8f, 0.2f) }
            };

            warningStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.9f, 0.6f, 0.1f) }
            };

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            EditorGUILayout.Space(10);

            // Header
            EditorGUILayout.LabelField("🎭 Animation Rig Setup Wizard", headerStyle);
            DrawSeparator();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (currentStep)
            {
                case 0:
                    DrawStep1_SelectActor();
                    break;
                case 1:
                    DrawStep2_BoneAssignment();
                    break;
                case 2:
                    DrawStep3_Options();
                    break;
                case 3:
                    DrawStep4_Review();
                    break;
            }

            EditorGUILayout.EndScrollView();

            DrawSeparator();
            DrawNavigationButtons();
        }

        private void DrawStep1_SelectActor()
        {
            EditorGUILayout.LabelField("Step 1: Select Target Actor", subHeaderStyle);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(boxStyle);

            EditorGUILayout.HelpBox(
                "Select the character GameObject you want to set up Animation Rigging for.\n" +
                "The character must have an Animator component.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            EditorGUI.BeginChangeCheck();
            targetActor = (GameObject)EditorGUILayout.ObjectField("Target Actor", targetActor, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck() && targetActor != null)
            {
                animator = targetActor.GetComponent<Animator>();
                TryAutoDetectBones();
            }

            EditorGUILayout.Space(5);

            // Validation
            if (targetActor == null)
            {
                EditorGUILayout.HelpBox("Please select a GameObject.", MessageType.Warning);
            }
            else if (animator == null)
            {
                EditorGUILayout.HelpBox("Selected GameObject needs an Animator component!", MessageType.Error);
            }
            else
            {
                EditorGUILayout.LabelField("✓ Animator found", successStyle);

                if (animator.avatar != null && animator.avatar.isHuman)
                {
                    EditorGUILayout.LabelField("✓ Humanoid Avatar detected", successStyle);
                }
                else
                {
                    EditorGUILayout.LabelField("⚠ Non-humanoid or no avatar (manual bone assignment may be needed)", warningStyle);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawStep2_BoneAssignment()
        {
            EditorGUILayout.LabelField("Step 2: Bone Assignment", subHeaderStyle);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(boxStyle);

            if (bonesAutoDetected)
            {
                EditorGUILayout.HelpBox(
                    "✓ Bones were automatically detected!\n" +
                    "You can verify or modify the assignments below.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "⚠ Some bones could not be auto-detected.\n" +
                    "Please assign the missing bone references manually.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(10);

            // Bone fields with status indicators
            DrawBoneField("Head", ref head, HeadPatterns);
            DrawBoneField("Spine/Chest", ref spine, SpinePatterns);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Arms", EditorStyles.boldLabel);
            DrawBoneField("Left Hand", ref leftHand, LeftHandPatterns);
            DrawBoneField("Right Hand", ref rightHand, RightHandPatterns);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Legs", EditorStyles.boldLabel);
            DrawBoneField("Left Foot", ref leftFoot, LeftFootPatterns);
            DrawBoneField("Right Foot", ref rightFoot, RightFootPatterns);

            EditorGUILayout.Space(10);

            // Auto-detect button
            if (GUILayout.Button("🔍 Re-detect Bones", GUILayout.Height(25)))
            {
                TryAutoDetectBones();
            }

            // Try to use Animator's humanoid mapping if available
            if (animator != null && animator.avatar != null && animator.avatar.isHuman)
            {
                if (GUILayout.Button("📋 Use Humanoid Avatar Bones", GUILayout.Height(25)))
                {
                    UseHumanoidBones();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawBoneField(string label, ref Transform bone, string[] patterns)
        {
            EditorGUILayout.BeginHorizontal();

            // Status indicator
            if (bone != null)
            {
                EditorGUILayout.LabelField("✓", successStyle, GUILayout.Width(20));
            }
            else
            {
                EditorGUILayout.LabelField("○", GUILayout.Width(20));
            }

            bone = (Transform)EditorGUILayout.ObjectField(label, bone, typeof(Transform), true);

            // Quick find button
            if (GUILayout.Button("Find", GUILayout.Width(50)))
            {
                bone = FindBoneByPatterns(targetActor.transform, patterns);
                if (bone == null)
                {
                    EditorUtility.DisplayDialog("Not Found",
                        $"Could not find a bone matching patterns for '{label}'.\n" +
                        $"Please assign it manually.", "OK");
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawStep3_Options()
        {
            EditorGUILayout.LabelField("Step 3: Setup Options", subHeaderStyle);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(boxStyle);

            EditorGUILayout.HelpBox(
                "Configure which constraints and components to create.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Constraints to Create", EditorStyles.boldLabel);
            createHeadConstraint = EditorGUILayout.Toggle("Head Look-At (MultiAim)", createHeadConstraint);
            createHandConstraints = EditorGUILayout.Toggle("Hand IK (TwoBoneIK)", createHandConstraints);
            createFootConstraints = EditorGUILayout.Toggle("Foot IK (TwoBoneIK)", createFootConstraints);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Additional Components", EditorStyles.boldLabel);
            addRigReferences = EditorGUILayout.Toggle("Add RigReferences Component", addRigReferences);
            addConstraintController = EditorGUILayout.Toggle("Add ConstraintTargetController", addConstraintController);

            EditorGUILayout.Space(10);

            showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Advanced Options");
            if (showAdvancedOptions)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(
                    "Advanced options allow fine-tuning of the rig setup.\n" +
                    "Default values work well for most humanoid characters.",
                    MessageType.None);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawStep4_Review()
        {
            EditorGUILayout.LabelField("Step 4: Review & Create", subHeaderStyle);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(boxStyle);

            EditorGUILayout.HelpBox(
                "Review your settings before creating the rig setup.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // Summary
            EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Target: {(targetActor != null ? targetActor.name : "None")}");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Bones:", EditorStyles.boldLabel);
            DrawBoneSummary("Head", head);
            DrawBoneSummary("Spine", spine);
            DrawBoneSummary("Left Hand", leftHand);
            DrawBoneSummary("Right Hand", rightHand);
            DrawBoneSummary("Left Foot", leftFoot);
            DrawBoneSummary("Right Foot", rightFoot);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Will Create:", EditorStyles.boldLabel);
            if (createHeadConstraint && head != null)
                EditorGUILayout.LabelField("  • Head MultiAimConstraint");
            if (createHandConstraints)
            {
                if (leftHand != null) EditorGUILayout.LabelField("  • Left Hand TwoBoneIKConstraint");
                if (rightHand != null) EditorGUILayout.LabelField("  • Right Hand TwoBoneIKConstraint");
            }
            if (createFootConstraints)
            {
                if (leftFoot != null) EditorGUILayout.LabelField("  • Left Leg TwoBoneIKConstraint");
                if (rightFoot != null) EditorGUILayout.LabelField("  • Right Leg TwoBoneIKConstraint");
            }
            if (addRigReferences) EditorGUILayout.LabelField("  • RigReferences Component");
            if (addConstraintController) EditorGUILayout.LabelField("  • ConstraintTargetController Component");

            EditorGUILayout.EndVertical();

            // Validation warnings
            var warnings = GetValidationWarnings();
            if (warnings.Count > 0)
            {
                EditorGUILayout.Space(5);
                foreach (var warning in warnings)
                {
                    EditorGUILayout.HelpBox(warning, MessageType.Warning);
                }
            }
        }

        private void DrawBoneSummary(string label, Transform bone)
        {
            if (bone != null)
                EditorGUILayout.LabelField($"  {label}: {bone.name}", successStyle);
            else
                EditorGUILayout.LabelField($"  {label}: Not assigned", warningStyle);
        }

        private List<string> GetValidationWarnings()
        {
            var warnings = new List<string>();

            if (targetActor == null)
                warnings.Add("No target actor selected!");

            if (animator == null)
                warnings.Add("Target has no Animator component!");

            if (createHeadConstraint && head == null)
                warnings.Add("Head bone not assigned - Head constraint will be skipped.");

            if (createHandConstraints && leftHand == null && rightHand == null)
                warnings.Add("No hand bones assigned - Hand constraints will be skipped.");

            if (createFootConstraints && leftFoot == null && rightFoot == null)
                warnings.Add("No foot bones assigned - Foot constraints will be skipped.");

            return warnings;
        }

        private void DrawNavigationButtons()
        {
            EditorGUILayout.BeginHorizontal();

            // Cancel button
            if (GUILayout.Button("Cancel", GUILayout.Height(30), GUILayout.Width(80)))
            {
                if (EditorUtility.DisplayDialog("Cancel Setup",
                    "Are you sure you want to cancel the rig setup?", "Yes", "No"))
                {
                    Close();
                }
            }

            GUILayout.FlexibleSpace();

            // Back button
            EditorGUI.BeginDisabledGroup(currentStep == 0);
            if (GUILayout.Button("◀ Back", GUILayout.Height(30), GUILayout.Width(80)))
            {
                currentStep--;
            }
            EditorGUI.EndDisabledGroup();

            // Next/Create button
            if (currentStep < 3)
            {
                EditorGUI.BeginDisabledGroup(!CanProceedToNextStep());
                if (GUILayout.Button("Next ▶", GUILayout.Height(30), GUILayout.Width(80)))
                {
                    currentStep++;
                }
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
                EditorGUI.BeginDisabledGroup(!CanCreateRig());
                if (GUILayout.Button("✓ Create Rig", GUILayout.Height(30), GUILayout.Width(100)))
                {
                    CreateRigSetup();
                }
                EditorGUI.EndDisabledGroup();
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndHorizontal();
        }

        private bool CanProceedToNextStep()
        {
            switch (currentStep)
            {
                case 0:
                    return targetActor != null && animator != null;
                case 1:
                    return head != null || leftHand != null || rightHand != null ||
                           leftFoot != null || rightFoot != null;
                case 2:
                    return true;
                default:
                    return true;
            }
        }

        private bool CanCreateRig()
        {
            return targetActor != null && animator != null &&
                   (head != null || leftHand != null || rightHand != null ||
                    leftFoot != null || rightFoot != null);
        }

        private void DrawSeparator()
        {
            EditorGUILayout.Space(5);
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            EditorGUILayout.Space(5);
        }

        private void TryAutoDetectBones()
        {
            if (targetActor == null) return;

            head = FindBoneByPatterns(targetActor.transform, HeadPatterns);
            spine = FindBoneByPatterns(targetActor.transform, SpinePatterns);
            leftHand = FindBoneByPatterns(targetActor.transform, LeftHandPatterns);
            rightHand = FindBoneByPatterns(targetActor.transform, RightHandPatterns);
            leftFoot = FindBoneByPatterns(targetActor.transform, LeftFootPatterns);
            rightFoot = FindBoneByPatterns(targetActor.transform, RightFootPatterns);

            bonesAutoDetected = head != null && leftHand != null && rightHand != null &&
                               leftFoot != null && rightFoot != null;
        }

        private void UseHumanoidBones()
        {
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman) return;

            head = animator.GetBoneTransform(HumanBodyBones.Head);
            spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);

            bonesAutoDetected = true;
            Repaint();
        }

        private Transform FindBoneByPatterns(Transform root, string[] patterns)
        {
            foreach (var pattern in patterns)
            {
                var found = FindByContains(root, pattern);
                if (found != null) return found;
            }
            return null;
        }

        private Transform FindByContains(Transform root, string keyword)
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

        private void CreateRigSetup()
        {
            Undo.SetCurrentGroupName("Create Rig Setup");
            int undoGroup = Undo.GetCurrentGroup();

            try
            {
                // Add RigBuilder if missing
                var rigBuilder = targetActor.GetComponent<RigBuilder>();
                if (rigBuilder == null)
                {
                    rigBuilder = Undo.AddComponent<RigBuilder>(targetActor);
                }

                // Create Rig parent
                var rigGO = new GameObject("Rig");
                Undo.RegisterCreatedObjectUndo(rigGO, "Create Rig GameObject");
                rigGO.transform.SetParent(targetActor.transform);
                rigGO.transform.localPosition = Vector3.zero;
                rigGO.transform.localRotation = Quaternion.identity;
                rigGO.transform.localScale = Vector3.one;

                var rig = rigGO.AddComponent<Rig>();

                // Add rig to RigBuilder layers
                Undo.RecordObject(rigBuilder, "Add Rig Layer");
                if (rigBuilder.layers == null)
                    rigBuilder.layers = new List<RigLayer>();
                rigBuilder.layers.Add(new RigLayer(rig));

                // Create constraints
                if (createHeadConstraint && head != null)
                {
                    CreateHeadConstraint(rigGO.transform, head);
                }

                if (createHandConstraints)
                {
                    if (leftHand != null)
                        CreateIKConstraint(rigGO.transform, "LeftHand", leftHand);
                    if (rightHand != null)
                        CreateIKConstraint(rigGO.transform, "RightHand", rightHand);
                }

                if (createFootConstraints)
                {
                    if (leftFoot != null)
                        CreateIKConstraint(rigGO.transform, "LeftLeg", leftFoot);
                    if (rightFoot != null)
                        CreateIKConstraint(rigGO.transform, "RightLeg", rightFoot);
                }

                // Add RigReferences component
                RigReferences rigRefs = null;
                if (addRigReferences)
                {
                    rigRefs = rigGO.AddComponent<RigReferences>();
                }

                // Add ConstraintTargetController component
                if (addConstraintController)
                {
                    var controller = targetActor.GetComponent<ConstraintTargetController>();
                    if (controller == null)
                    {
                        controller = Undo.AddComponent<ConstraintTargetController>(targetActor);
                    }
                }

                Undo.CollapseUndoOperations(undoGroup);

                EditorUtility.DisplayDialog("Success",
                    $"Rig setup created successfully for '{targetActor.name}'!\n\n" +
                    "The rig is ready to use. You can adjust constraint weights and targets in the Inspector.",
                    "OK");

                Selection.activeGameObject = rigGO;
                Close();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error creating rig setup: {e.Message}");
                Undo.CollapseUndoOperations(undoGroup);
                Undo.PerformUndo();

                EditorUtility.DisplayDialog("Error",
                    $"Failed to create rig setup:\n{e.Message}",
                    "OK");
            }
        }

        private void CreateHeadConstraint(Transform rigParent, Transform headBone)
        {
            var headObj = new GameObject("HeadConstraint");
            Undo.RegisterCreatedObjectUndo(headObj, "Create Head Constraint");
            headObj.transform.SetParent(rigParent);
            headObj.transform.position = headBone.position;
            headObj.transform.rotation = headBone.rotation;

            var headTarget = new GameObject("HeadTarget");
            headTarget.transform.SetParent(headObj.transform);
            headTarget.transform.position = headBone.position + headBone.forward * 2f;
            headTarget.transform.rotation = headBone.rotation;

            var aim = headObj.AddComponent<MultiAimConstraint>();
            aim.data.constrainedObject = headBone;
            aim.data.sourceObjects.Add(new WeightedTransform(headTarget.transform, 1f));
            aim.weight = 0f; // Start disabled
        }

        private void CreateIKConstraint(Transform rigParent, string name, Transform tip)
        {
            var ikObj = new GameObject(name + "Constraint");
            Undo.RegisterCreatedObjectUndo(ikObj, $"Create {name} Constraint");
            ikObj.transform.SetParent(rigParent);
            ikObj.transform.position = tip.position;
            ikObj.transform.rotation = tip.rotation;

            var target = new GameObject(name + "Target");
            target.transform.SetParent(ikObj.transform);
            target.transform.position = tip.position;
            target.transform.rotation = tip.rotation;

            var root = tip.parent?.parent;
            Vector3 hintPos = tip.position;
            Vector3 hintOffset = Vector3.zero;

            // Calculate appropriate hint position based on limb type
            if (name.Contains("Hand") || name.Contains("Arm"))
            {
                // Elbows bend backward
                hintOffset = -tip.forward * 0.3f;
            }
            else if (name.Contains("Leg") || name.Contains("Foot"))
            {
                // Knees bend forward
                hintOffset = tip.forward * 0.3f;
            }

            if (root != null)
            {
                hintPos = Vector3.Lerp(tip.position, root.position, 0.5f) + hintOffset;
            }

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
            ik.weight = 0f; // Start disabled
        }

        private void OnSelectionChange()
        {
            if (currentStep == 0 && Selection.activeGameObject != null)
            {
                targetActor = Selection.activeGameObject;
                animator = targetActor.GetComponent<Animator>();
                TryAutoDetectBones();
                Repaint();
            }
        }
    }

    /// <summary>
    /// Quick access menu item
    /// </summary>
    public static class RigSetupQuickMenu
    {
        [MenuItem("WitShells/Animation Rig/Quick Rig Setup (Auto)")]
        public static void QuickSetup()
        {
            if (Selection.activeGameObject == null)
            {
                EditorUtility.DisplayDialog("No Selection",
                    "Please select a GameObject with an Animator component.", "OK");
                return;
            }

            var actor = Selection.activeGameObject;
            var animator = actor.GetComponent<Animator>();

            if (animator == null)
            {
                EditorUtility.DisplayDialog("No Animator",
                    "Selected GameObject must have an Animator component.", "OK");
                return;
            }

            // Try auto-detection first
            if (animator.avatar != null && animator.avatar.isHuman)
            {
                // Use humanoid bones directly
                PerformQuickSetup(actor, animator);
            }
            else
            {
                // Open wizard for manual assignment
                EditorUtility.DisplayDialog("Manual Setup Required",
                    "Could not auto-detect bones. Opening the Rig Setup Wizard for manual configuration.",
                    "OK");
                RigSetupWindow.ShowWindow();
            }
        }

        private static void PerformQuickSetup(GameObject actor, Animator animator)
        {
            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            var leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);

            if (head == null && leftHand == null && rightHand == null && leftFoot == null && rightFoot == null)
            {
                EditorUtility.DisplayDialog("Setup Failed",
                    "Could not find any bones. Please use the Rig Setup Wizard for manual configuration.",
                    "OK");
                RigSetupWindow.ShowWindow();
                return;
            }

            // Perform quick setup
            Undo.SetCurrentGroupName("Quick Rig Setup");
            int undoGroup = Undo.GetCurrentGroup();

            var rigBuilder = actor.GetComponent<RigBuilder>() ?? Undo.AddComponent<RigBuilder>(actor);

            var rigGO = new GameObject("Rig");
            Undo.RegisterCreatedObjectUndo(rigGO, "Create Rig");
            rigGO.transform.SetParent(actor.transform);
            rigGO.transform.localPosition = Vector3.zero;
            rigGO.transform.localRotation = Quaternion.identity;
            rigGO.transform.localScale = Vector3.one;

            var rig = rigGO.AddComponent<Rig>();

            Undo.RecordObject(rigBuilder, "Add Rig Layer");
            if (rigBuilder.layers == null)
                rigBuilder.layers = new List<RigLayer>();
            rigBuilder.layers.Add(new RigLayer(rig));

            // Create constraints
            CreateConstraintsQuick(rigGO.transform, head, leftHand, rightHand, leftFoot, rightFoot);

            // Add components
            rigGO.AddComponent<RigReferences>();
            if (actor.GetComponent<ConstraintTargetController>() == null)
                Undo.AddComponent<ConstraintTargetController>(actor);

            Undo.CollapseUndoOperations(undoGroup);

            EditorUtility.DisplayDialog("Success",
                $"Quick rig setup completed for '{actor.name}'!",
                "OK");

            Selection.activeGameObject = rigGO;
        }

        private static void CreateConstraintsQuick(Transform rigParent, Transform head,
            Transform leftHand, Transform rightHand, Transform leftFoot, Transform rightFoot)
        {
            if (head != null)
            {
                var headObj = new GameObject("HeadConstraint");
                headObj.transform.SetParent(rigParent);
                headObj.transform.position = head.position;

                var headTarget = new GameObject("HeadTarget");
                headTarget.transform.SetParent(headObj.transform);
                headTarget.transform.position = head.position + head.forward * 2f;

                var aim = headObj.AddComponent<MultiAimConstraint>();
                aim.data.constrainedObject = head;
                aim.data.sourceObjects.Add(new WeightedTransform(headTarget.transform, 1f));
                aim.weight = 0f;
            }

            CreateQuickIK(rigParent, "LeftHand", leftHand);
            CreateQuickIK(rigParent, "RightHand", rightHand);
            CreateQuickIK(rigParent, "LeftLeg", leftFoot);
            CreateQuickIK(rigParent, "RightLeg", rightFoot);
        }

        private static void CreateQuickIK(Transform rigParent, string name, Transform tip)
        {
            if (tip == null) return;

            var ikObj = new GameObject(name + "Constraint");
            ikObj.transform.SetParent(rigParent);
            ikObj.transform.position = tip.position;

            var target = new GameObject(name + "Target");
            target.transform.SetParent(ikObj.transform);
            target.transform.position = tip.position;
            target.transform.rotation = tip.rotation;

            var root = tip.parent?.parent;
            var hintPos = root != null ? Vector3.Lerp(tip.position, root.position, 0.5f) : tip.position;

            var hint = new GameObject(name + "Hint");
            hint.transform.SetParent(ikObj.transform);
            hint.transform.position = hintPos;

            var ik = ikObj.AddComponent<TwoBoneIKConstraint>();
            ik.data.tip = tip;
            ik.data.mid = tip.parent;
            ik.data.root = root;
            ik.data.target = target.transform;
            ik.data.hint = hint.transform;
            ik.weight = 0f;
        }
    }
}
