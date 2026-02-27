using UnityEngine;
using UnityEditor;
using WitShells.WitPose.Editor.SceneGizmos;

namespace WitShells.WitPose.Editor
{
    /// <summary>
    /// Settings and Gizmo logic for WitPoseEditor
    /// </summary>
    public partial class WitPoseEditor
    {
        private void DrawSettingsTab()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("⚙️ Settings & Gizmos", EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);
            Vector2 settingsScrollPos = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandWidth(true));
            DrawGizmoSettings();
            EditorGUILayout.Space(10);
            DrawBoneSelectionIntegration();
            EditorGUILayout.Space(10);
            // General settings can be added here later
            EditorGUILayout.EndScrollView();
        }

        private void DrawGizmoSettings()
        {
            EditorGUILayout.LabelField("🎨 Gizmo Settings", EditorStyles.boldLabel);

            // Gizmo Mode Toggle
            if (isPoseModeActive)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("🔄 Gizmo Mode", EditorStyles.boldLabel);
                
                EditorGUI.BeginChangeCheck();
                GizmoMode newMode = (GizmoMode)EditorGUILayout.EnumPopup("Active Gizmos", currentGizmoMode);
                if (EditorGUI.EndChangeCheck())
                {
                    SwitchGizmoMode(newMode);
                }

                // Display current mode info
                switch (currentGizmoMode)
                {
                    case GizmoMode.BoneGizmos:
                        GUI.backgroundColor = new Color(0.8f, 1f, 0.8f);
                        EditorGUILayout.LabelField("🦴 Bone Gizmos: Click bones to edit muscles", EditorStyles.helpBox);
                        break;
                    case GizmoMode.IKGizmos:
                        GUI.backgroundColor = new Color(0.8f, 0.8f, 1f);
                        EditorGUILayout.LabelField("🤖 IK Gizmos: Drag green cubes for IK poses", EditorStyles.helpBox);
                        break;
                }
                GUI.backgroundColor = Color.white;
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Pose Mode to access gizmo controls", MessageType.Info);
            }

            if (gizmoSystem != null && currentGizmoMode == GizmoMode.BoneGizmos)
            {
                EditorGUILayout.LabelField("🦴 Bone Gizmo Options", EditorStyles.boldLabel);
                gizmoSystem.ShowConnections = EditorGUILayout.Toggle("Show Connections", gizmoSystem.ShowConnections);
                gizmoSystem.ShowRotationHandles = EditorGUILayout.Toggle("Show Rotation Handles", gizmoSystem.ShowRotationHandles);
            }
            else if (ikGizmoSystem != null && currentGizmoMode == GizmoMode.IKGizmos)
            {
                EditorGUILayout.LabelField("🤖 IK Gizmo Options", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("• Click green cubes to activate IK targets", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("• Drag position handles to pose limbs", EditorStyles.miniLabel);
            }
        }

        private void DrawBoneSelectionIntegration()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🦴 Bone Selection Integration", EditorStyles.boldLabel);
            
            if (currentGizmoMode != GizmoMode.BoneGizmos)
            {
                EditorGUILayout.HelpBox("Bone selection is only available in Bone Gizmos mode. Switch to Bone Gizmos above to use this feature.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }
            
            EditorGUILayout.HelpBox(
                "Integration Instructions:\n\n" +
                "1. In your BoneGizmoSystem, when a bone gizmo is clicked, call:\n" +
                "   editorWindow.SelectBone(HumanBodyBones.BoneName);\n\n" +
                "2. Pass the editor window reference to your gizmo system during initialization.\n\n" +
                "3. The mini HUD window will automatically appear in the bottom-right corner with relevant muscle sliders.",
                MessageType.Info);

            EditorGUILayout.Space(5);

            // Show current bone selection status
            if (selectedBone != HumanBodyBones.LastBone)
            {
                GUI.backgroundColor = successColor;
                EditorGUILayout.LabelField($"✅ Selected: {GetBoneDisplayName(selectedBone)}", EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;

                if (boneToMuscleMapping != null && boneToMuscleMapping.ContainsKey(selectedBone))
                {
                    EditorGUILayout.LabelField($"   Muscles: {boneToMuscleMapping[selectedBone].Length}", EditorStyles.miniLabel);
                }

                if (GUILayout.Button("Clear Selection"))
                {
                    selectedBone = HumanBodyBones.LastBone;
                    showBoneMuscleHUD = false;
                }
            }
            else
            {
                EditorGUILayout.LabelField("No bone selected", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(10);

            // Test bone selection buttons
            if (isPoseModeActive && boneToMuscleMapping != null)
            {
                EditorGUILayout.LabelField("🧪 Test Bone Selection:", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Spine", GUILayout.Height(25)))
                {
                    SelectBone(HumanBodyBones.Spine);
                }
                if (GUILayout.Button("Head", GUILayout.Height(25)))
                {
                    SelectBone(HumanBodyBones.Head);
                }
                if (GUILayout.Button("L.Shoulder", GUILayout.Height(25)))
                {
                    SelectBone(HumanBodyBones.LeftShoulder);
                }
                if (GUILayout.Button("R.Hand", GUILayout.Height(25)))
                {
                    SelectBone(HumanBodyBones.RightHand);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("L.UpperLeg", GUILayout.Height(25)))
                {
                    SelectBone(HumanBodyBones.LeftUpperLeg);
                }
                if (GUILayout.Button("R.Foot", GUILayout.Height(25)))
                {
                    SelectBone(HumanBodyBones.RightFoot);
                }
                if (GUILayout.Button("L.Thumb", GUILayout.Height(25)))
                {
                    SelectBone(HumanBodyBones.LeftThumbProximal);
                }
                if (GUILayout.Button("Neck", GUILayout.Height(25)))
                {
                    SelectBone(HumanBodyBones.Neck);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("💡 Click any test button to see the mini HUD window appear in the bottom-right corner!", EditorStyles.wordWrappedMiniLabel);
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Pose Mode to test bone selection", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }
    }
}