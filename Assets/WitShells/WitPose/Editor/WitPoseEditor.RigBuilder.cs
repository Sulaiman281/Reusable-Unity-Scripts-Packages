using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Animations;
using WitShells.WitPose.Editor.Core;

namespace WitShells.WitPose.Editor
{
    /// <summary>
    /// Rig Builder Logic for WitPoseEditor
    /// Handles creating proxy skeletons, constraints, and mapping for rigging.
    /// </summary>
    public partial class WitPoseEditor
    {
        private void DrawRigBuilderTab()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🎭 Constraint Rig Builder", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Build constraint-driven posing rig with duplicate skeleton and ParentConstraints", MessageType.Info);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            rigBuilderScrollPosition = EditorGUILayout.BeginScrollView(rigBuilderScrollPosition);
            DrawRigBuilderSetup();
            EditorGUILayout.Space(10);
            DrawRigBuilderActions();
            EditorGUILayout.Space(10);
            DrawRigBuilderConstraintControls();
            EditorGUILayout.EndScrollView();
        }

        private void DrawConstraintRigSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🎭 Constraint Rig Mode", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Enable to work with constraint-driven proxy bones.\n" +
                "Switch to the Rig Builder tab to create and manage your constraint rig.",
                MessageType.Info
            );

            bool useConstraintRig = rigBuilt && poseControlsRoot != null;
            EditorGUILayout.LabelField("Constraint Rig Status: " + (useConstraintRig ? "✓ Active" : "○ Inactive"));

            if (useConstraintRig && gizmoSystem != null)
            {
                // Build mapping from original bones to proxy bones
                Dictionary<Transform, Transform> mapping = BuildProxyMapping();
                gizmoSystem.SetProxyBoneMapping(mapping);
                gizmoSystem.UseProxyBones = true;
                EditorGUILayout.HelpBox($"Using {mapping.Count} proxy bones for posing", MessageType.Info);
            }
            else if (gizmoSystem != null)
            {
                gizmoSystem.UseProxyBones = false;
            }

            // Switch to rig builder tab button
            if (GUILayout.Button("Go to Rig Builder Tab", GUILayout.Height(25)))
            {
                currentTab = EditorTab.RigBuilder;
            }

            EditorGUILayout.EndVertical();
        }

        private Dictionary<Transform, Transform> BuildProxyMapping()
        {
            var mapping = new Dictionary<Transform, Transform>();

            if (poseControlsRoot == null || skeleton == null)
                return mapping;

            foreach (var bone in skeleton.AllBones)
            {
                if (bone.transform == null)
                    continue;

                // Find corresponding proxy bone by name
                string proxyName = bone.transform.name + "_CTRL";
                Transform proxy = FindChildRecursive(poseControlsRoot.transform, proxyName);

                if (proxy != null)
                {
                    mapping[bone.transform] = proxy;
                }
            }

            return mapping;
        }

        private Transform FindChildRecursive(Transform parent, string childName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == childName)
                    return child;

                Transform found = FindChildRecursive(child, childName);
                if (found != null)
                    return found;
            }
            return null;
        }

        private void DrawRigBuilderSetup()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("📋 Rig Setup", EditorStyles.boldLabel);

            // Auto-detect skeleton root from current animator
            if (skeletonRoot == null && targetAnimator != null && skeleton != null)
            {
                // Try to find a good skeleton root (usually Hips)
                foreach (var bone in skeleton.AllBones)
                {
                    if (bone.transform != null && (bone.transform.name.ToLower().Contains("hips") || bone.transform.name.ToLower().Contains("pelvis")))
                    {
                        skeletonRoot = bone.transform;
                        break;
                    }
                }
            }

            EditorGUI.BeginChangeCheck();
            skeletonRoot = (Transform)EditorGUILayout.ObjectField("Skeleton Root", skeletonRoot, typeof(Transform), true);
            if (EditorGUI.EndChangeCheck())
            {
                if (skeletonRoot != null && autoDetectRig)
                {
                    AutoDetectExistingRig();
                }
            }

            if (skeletonRoot == null)
            {
                EditorGUILayout.HelpBox("Assign the root bone of your skeleton (e.g., 'Hips')", MessageType.Warning);
            }
            else
            {
                int boneCount = CountBones(skeletonRoot);
                EditorGUILayout.LabelField($"Bones Found: {boneCount}", EditorStyles.miniLabel);

                EditorGUILayout.Space(5);

                // Advanced settings
                showAdvancedRigSettings = EditorGUILayout.Foldout(showAdvancedRigSettings, "Advanced Settings", true);
                if (showAdvancedRigSettings)
                {
                    EditorGUI.indentLevel++;

                    // Auto-detect toggle
                    EditorGUILayout.BeginHorizontal();
                    autoDetectRig = EditorGUILayout.Toggle("Auto-Detect Existing Rig", autoDetectRig);
                    if (GUILayout.Button("🔍 Detect Now", GUILayout.Width(100)))
                    {
                        AutoDetectExistingRig();
                    }
                    EditorGUILayout.EndHorizontal();

                    // Manual duplicate root
                    EditorGUILayout.Space(3);
                    EditorGUI.BeginChangeCheck();
                    duplicateRoot = (Transform)EditorGUILayout.ObjectField("Duplicate Root (Manual)", duplicateRoot, typeof(Transform), true);
                    if (EditorGUI.EndChangeCheck() && duplicateRoot != null)
                    {
                        RebuildRigMappings();
                    }

                    if (duplicateRoot != null)
                    {
                        GUI.backgroundColor = accentColor;
                        EditorGUILayout.HelpBox($"Using duplicate root: {duplicateRoot.name}\nMapped bones: {originalToProxy.Count}", MessageType.Info);
                        GUI.backgroundColor = Color.white;
                    }
                    else if (poseControlsRoot != null)
                    {
                        GUI.backgroundColor = warningColor;
                        EditorGUILayout.HelpBox("PoseControls found but duplicate root not detected.\nManually assign the duplicate root above.", MessageType.Warning);
                        GUI.backgroundColor = Color.white;
                    }

                    EditorGUI.indentLevel--;
                }

                // Status
                EditorGUILayout.Space(5);
                if (poseControlsRoot != null)
                {
                    GUI.backgroundColor = rigBuilt ? successColor : warningColor;
                    string status = rigBuilt ? $"✅ Rig detected: {constraintMap.Count} constraints" : "⚠️ PoseControls found but rig not fully mapped";
                    EditorGUILayout.HelpBox(status, MessageType.Info);
                    GUI.backgroundColor = Color.white;
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawRigBuilderActions()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🛠️ Rig Actions", EditorStyles.boldLabel);

            GUI.enabled = skeletonRoot != null && !rigBuilt;
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("🎭 Create Pose Rig", GUILayout.Height(40)))
            {
                CreatePoseRig();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(5);

            GUI.enabled = rigBuilt && poseControlsRoot != null;
            GUI.backgroundColor = errorColor;
            if (GUILayout.Button("🗑️ Delete Pose Rig", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Delete Pose Rig", "Are you sure? This will remove all constraints and control bones.", "Delete", "Cancel"))
                {
                    DeletePoseRig();
                }
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            EditorGUILayout.EndVertical();
        }

        private void DrawRigBuilderConstraintControls()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🎮 Constraint Controls", EditorStyles.boldLabel);

            if (!rigBuilt || constraintMap.Count == 0)
            {
                EditorGUILayout.HelpBox("Create a pose rig first to control constraints", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            // Status
            bool allEnabled = AreConstraintsEnabled();
            GUI.backgroundColor = allEnabled ? successColor : errorColor;
            EditorGUILayout.LabelField(allEnabled ? "✓ Constraints Active" : "○ Constraints Inactive", EditorStyles.helpBox);
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = successColor;
            if (GUILayout.Button("✓ Enable Constraints", GUILayout.Height(35)))
            {
                EnableConstraints();
            }

            GUI.backgroundColor = errorColor;
            if (GUILayout.Button("○ Disable Constraints", GUILayout.Height(35)))
            {
                DisableConstraints();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"Active Constraints: {constraintMap.Count}", EditorStyles.miniLabel);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Pose Transfer", EditorStyles.boldLabel);

            GUI.backgroundColor = accentColor;
            if (GUILayout.Button("📋 Copy Original Pose to Proxies", GUILayout.Height(30)))
            {
                CopyOriginalPoseToProxies();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        private void AutoDetectExistingRig()
        {
            if (skeletonRoot == null) return;

            Logger.Log($"🔍 Auto-detecting existing rig for skeleton root: {skeletonRoot.name}");

            var existingPoseControls = FindExistingPoseControls();
            if (existingPoseControls != null)
            {
                poseControlsRoot = existingPoseControls;
                Logger.Log($"📋 Found existing PoseControls: {existingPoseControls.name}");

                if (duplicateRoot == null)
                {
                    duplicateRoot = FindDuplicateRootInPoseControls(existingPoseControls);
                }

                if (duplicateRoot != null)
                {
                    Logger.Log($"🎭 Found duplicate root: {duplicateRoot.name}");
                    RebuildRigMappings();
                    Logger.Log($"✅ Auto-detected existing rig: {constraintMap.Count} constraints found");
                }
                else
                {
                    Logger.LogWarning($"⚠️ PoseControls found but could not locate duplicate root for '{skeletonRoot.name}'");
                }
            }
            else
            {
                Logger.Log($"ℹ️ No existing PoseControls found - you may need to create a new pose rig");
            }
        }

        private GameObject FindExistingPoseControls()
        {
            if (skeletonRoot == null) return null;

            // First, check the parent (where CreatePoseRig puts it)
            var parent = skeletonRoot.parent;
            if (parent != null)
            {
                var poseControls = parent.Find("PoseControls");
                if (poseControls != null) return poseControls.gameObject;
            }

            // If not found in parent, search the entire scene
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (GameObject obj in allObjects)
            {
                if (obj.name == "PoseControls")
                {
                    return obj;
                }
            }

            return null;
        }

        private Transform FindDuplicateRootInPoseControls(GameObject poseControls)
        {
            if (poseControls == null || skeletonRoot == null) return null;

            // Look for direct child with same name as skeleton root
            string rootName = skeletonRoot.name;
            string expectedCtrlName = rootName + "_CTRL";

            Logger.Log($"🔍 Searching for duplicate root. Original: '{rootName}', Expected CTRL: '{expectedCtrlName}'");

            for (int i = 0; i < poseControls.transform.childCount; i++)
            {
                var child = poseControls.transform.GetChild(i);
                Logger.Log($"  - Checking child: '{child.name}'");

                // Check exact matches first
                if (child.name == expectedCtrlName || child.name == rootName)
                {
                    Logger.Log($"✅ Found duplicate root: {child.name}");
                    return child;
                }

                // Check case-insensitive matches
                if (string.Equals(child.name, expectedCtrlName, System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(child.name, rootName, System.StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Log($"✅ Found duplicate root (case-insensitive): {child.name}");
                    return child;
                }

                // Check cleaned name matches
                string cleanedChildName = WitPoseUtils.CleanBoneName(child.name);
                string cleanedRootName = WitPoseUtils.CleanBoneName(rootName);
                if (cleanedChildName == cleanedRootName)
                {
                    Logger.Log($"✅ Found duplicate root (cleaned names): {child.name}");
                    return child;
                }
            }

            Logger.LogWarning($"⚠️ Could not find duplicate root for '{rootName}' in PoseControls");
            return null;
        }

        private void RebuildRigMappings()
        {
            originalToProxy.Clear();
            constraintMap.Clear();

            if (skeletonRoot == null || duplicateRoot == null)
            {
                rigBuilt = false;
                return;
            }

            int originalBoneCount = CountBones(skeletonRoot);
            Logger.Log($"🔍 Rebuilding rig mappings... Original skeleton has {originalBoneCount} bones");

            BuildMappingRecursive(skeletonRoot, duplicateRoot);

            int mappedBoneCount = originalToProxy.Count;
            rigBuilt = mappedBoneCount > 0;

            Logger.Log($"✅ Rig mapping complete: {mappedBoneCount}/{originalBoneCount} bones mapped, {constraintMap.Count} constraints found");

            if (mappedBoneCount < originalBoneCount)
            {
                Logger.LogWarning($"⚠️ Only {mappedBoneCount} out of {originalBoneCount} bones were mapped. Some bones may not have matching proxy controls.");
            }
        }

        private void BuildMappingRecursive(Transform original, Transform duplicate)
        {
            if (original == null || duplicate == null) return;

            originalToProxy[original] = duplicate;

            var constraint = original.GetComponent<ParentConstraint>();
            if (constraint != null)
            {
                constraintMap[constraint] = original;
            }

            // Process all child bones, trying multiple matching strategies
            for (int i = 0; i < original.childCount; i++)
            {
                var originalChild = original.GetChild(i);
                var duplicateChild = FindMatchingBone(duplicate, originalChild.name);
                if (duplicateChild != null)
                {
                    BuildMappingRecursive(originalChild, duplicateChild);
                }
                else
                {
                    // Logger warning but continue with other bones
                    Logger.LogWarning($"Could not find matching proxy bone for '{originalChild.name}' under '{duplicate.name}'");
                }
            }
        }

        private Transform FindMatchingBone(Transform parent, string originalName)
        {
            if (parent == null || string.IsNullOrEmpty(originalName)) return null;

            // Strategy 1: Direct name match
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == originalName) return child;
            }

            // Strategy 2: Match with _CTRL suffix
            string nameWithCtrl = originalName + "_CTRL";
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == nameWithCtrl) return child;
            }

            // Strategy 3: Match by removing common prefixes/suffixes
            string cleanedOriginalName = WitPoseUtils.CleanBoneName(originalName);
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                string cleanedChildName = WitPoseUtils.CleanBoneName(child.name);
                if (cleanedChildName == cleanedOriginalName) return child;
            }

            // Strategy 4: Case-insensitive match
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (string.Equals(child.name, originalName, System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(child.name, nameWithCtrl, System.StringComparison.OrdinalIgnoreCase))
                    return child;
            }

            return null;
        }

        private void CreatePoseRig()
        {
            if (skeletonRoot == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a skeleton root first", "OK");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Create Pose Rig");

            try
            {
                CreatePoseControlsRoot();
                duplicateRoot = DuplicateSkeleton(skeletonRoot, poseControlsRoot.transform);
                AddConstraintsToOriginalBones();
                rigBuilt = true;

                Logger.Log($"✅ Pose rig created: {originalToProxy.Count} bones, {constraintMap.Count} constraints");
                EditorUtility.DisplayDialog("Success", $"Pose rig created!\n\n{originalToProxy.Count} control bones\n{constraintMap.Count} constraints", "OK");
            }
            catch (System.Exception e)
            {
                Logger.LogError($"Failed to create pose rig: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to create pose rig:\n{e.Message}", "OK");
                Undo.RevertAllInCurrentGroup();
            }
        }

        private void CreatePoseControlsRoot()
        {
            Transform parent = skeletonRoot.parent;
            poseControlsRoot = new GameObject("PoseControls");
            Undo.RegisterCreatedObjectUndo(poseControlsRoot, "Create PoseControls");

            if (parent != null)
            {
                poseControlsRoot.transform.SetParent(parent, false);
            }

            poseControlsRoot.transform.position = skeletonRoot.position;
            poseControlsRoot.transform.rotation = skeletonRoot.rotation;
        }

        private Transform DuplicateSkeleton(Transform original, Transform parent)
        {
            GameObject proxyObj = new GameObject(original.name + "_CTRL");
            Undo.RegisterCreatedObjectUndo(proxyObj, "Duplicate Bone");

            Transform proxy = proxyObj.transform;
            proxy.SetParent(parent, false);
            proxy.localPosition = original.localPosition;
            proxy.localRotation = original.localRotation;
            proxy.localScale = original.localScale;

            originalToProxy[original] = proxy;

            for (int i = 0; i < original.childCount; i++)
            {
                DuplicateSkeleton(original.GetChild(i), proxy);
            }

            return proxy;
        }

        private void AddConstraintsToOriginalBones()
        {
            foreach (var kvp in originalToProxy)
            {
                Transform originalBone = kvp.Key;
                Transform proxyBone = kvp.Value;

                ParentConstraint constraint = Undo.AddComponent<ParentConstraint>(originalBone.gameObject);

                ConstraintSource source = new ConstraintSource
                {
                    sourceTransform = proxyBone,
                    weight = 1f
                };
                constraint.AddSource(source);

                constraint.constraintActive = true;
                constraint.locked = true;
                constraint.weight = 1f;

                constraintMap[constraint] = originalBone;
            }

            Logger.Log($"✅ Added {constraintMap.Count} ParentConstraints");
        }

        private void DeletePoseRig()
        {
            if (poseControlsRoot == null) return;

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Delete Pose Rig");

            foreach (var kvp in constraintMap)
            {
                if (kvp.Key != null)
                {
                    Undo.DestroyObjectImmediate(kvp.Key);
                }
            }

            Undo.DestroyObjectImmediate(poseControlsRoot);

            originalToProxy.Clear();
            constraintMap.Clear();
            rigBuilt = false;
            poseControlsRoot = null;
            duplicateRoot = null;

            Logger.Log("🗑️ Pose rig deleted");
        }

        private void EnableConstraints()
        {
            if (constraintMap.Count == 0) return;

            foreach (var kvp in constraintMap)
            {
                if (kvp.Key != null)
                {
                    Undo.RecordObject(kvp.Key, "Enable Constraint");
                    kvp.Key.enabled = true;
                }
            }

            Logger.Log("✓ Constraints enabled");
        }

        private void DisableConstraints()
        {
            if (constraintMap.Count == 0) return;

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Disable Constraints");

            foreach (var kvp in constraintMap)
            {
                ParentConstraint constraint = kvp.Key;
                Transform originalBone = kvp.Value;

                if (originalBone != null && constraint != null && constraint.enabled)
                {
                    Undo.RecordObject(originalBone, "Preserve Bone Pose");
                    Undo.RecordObject(constraint, "Disable Constraint");

                    Vector3 worldPos = originalBone.position;
                    Quaternion worldRot = originalBone.rotation;

                    constraint.enabled = false;

                    originalBone.position = worldPos;
                    originalBone.rotation = worldRot;
                }
            }

            Logger.Log("○ Constraints disabled (pose preserved)");
        }

        private bool AreConstraintsEnabled()
        {
            if (constraintMap.Count == 0) return false;

            foreach (var kvp in constraintMap)
            {
                if (kvp.Key != null && kvp.Key.enabled)
                    return true;
            }
            return false;
        }

        private void CopyOriginalPoseToProxies()
        {
            if (originalToProxy.Count == 0) return;

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Copy Pose to Proxies");

            int copiedCount = 0;
            foreach (var kvp in originalToProxy)
            {
                Transform originalBone = kvp.Key;
                Transform proxyBone = kvp.Value;

                if (originalBone != null && proxyBone != null)
                {
                    Undo.RecordObject(proxyBone, "Copy Bone Transform");
                    proxyBone.localPosition = originalBone.localPosition;
                    proxyBone.localRotation = originalBone.localRotation;
                    proxyBone.localScale = originalBone.localScale;
                    copiedCount++;
                }
            }

            Logger.Log($"📋 Copied pose from {copiedCount} original bones to proxy bones");
        }

        private int CountBones(Transform root)
        {
            int count = 1;
            for (int i = 0; i < root.childCount; i++)
            {
                count += CountBones(root.GetChild(i));
            }
            return count;
        }
    }
}