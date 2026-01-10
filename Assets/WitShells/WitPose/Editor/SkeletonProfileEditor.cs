using UnityEngine;
using UnityEditor;

namespace WitShells.WitPose.Editor
{
    /// <summary>
    /// Custom editor for SkeletonProfile ScriptableObject
    /// </summary>
    [CustomEditor(typeof(SkeletonProfile))]
    public class SkeletonProfileEditor : UnityEditor.Editor
    {
        private bool showConstraints = true;
        private bool showGlobalSettings = true;

        public override void OnInspectorGUI()
        {
            SkeletonProfile profile = (SkeletonProfile)target;

            EditorGUI.BeginChangeCheck();

            // Profile information
            EditorGUILayout.LabelField("Profile Information", EditorStyles.boldLabel);
            profile.profileName = EditorGUILayout.TextField("Profile Name", profile.profileName);
            profile.description = EditorGUILayout.TextArea(profile.description, GUILayout.Height(60));

            EditorGUILayout.Space();

            // Profile settings
            EditorGUILayout.LabelField("Profile Settings", EditorStyles.boldLabel);
            profile.ageCategory = (SkeletonProfile.AgeCategory)EditorGUILayout.EnumPopup("Age Category", profile.ageCategory);
            profile.bodyType = (SkeletonProfile.BodyType)EditorGUILayout.EnumPopup("Body Type", profile.bodyType);

            EditorGUILayout.Space();

            // Global settings
            showGlobalSettings = EditorGUILayout.Foldout(showGlobalSettings, "Global Settings", true);
            if (showGlobalSettings)
            {
                EditorGUI.indentLevel++;
                profile.defaultStiffness = EditorGUILayout.Slider("Default Stiffness", profile.defaultStiffness, 0f, 1f);
                profile.defaultPropagationFactor = EditorGUILayout.Slider("Default Propagation Factor", profile.defaultPropagationFactor, 0f, 1f);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Bone constraints
            showConstraints = EditorGUILayout.Foldout(showConstraints, $"Bone Constraints ({profile.boneConstraints.Count})", true);
            if (showConstraints)
            {
                EditorGUI.indentLevel++;

                for (int i = 0; i < profile.boneConstraints.Count; i++)
                {
                    DrawBoneConstraint(profile.boneConstraints[i], i);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Utility buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate Profile"))
            {
                profile.ValidateProfile();
            }

            if (GUILayout.Button("Reset to Standard Human"))
            {
                if (EditorUtility.DisplayDialog("Reset Profile", "This will reset all constraints to standard human values. Continue?", "Yes", "No"))
                {
                    profile.ResetToStandardHuman();
                }
            }
            GUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(profile);
            }
        }

        private void DrawBoneConstraint(SkeletonProfile.BoneConstraint constraint, int index)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField(constraint.boneId.ToString(), EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            constraint.minRotation = EditorGUILayout.Vector3Field("Min Rotation", constraint.minRotation);
            constraint.maxRotation = EditorGUILayout.Vector3Field("Max Rotation", constraint.maxRotation);
            constraint.stiffness = EditorGUILayout.Slider("Stiffness", constraint.stiffness, 0f, 1f);
            constraint.propagationFactor = EditorGUILayout.Slider("Propagation Factor", constraint.propagationFactor, 0f, 1f);

            constraint.isPosturalBone = EditorGUILayout.Toggle("Postural Bone", constraint.isPosturalBone);
            constraint.emphasizeInUI = EditorGUILayout.Toggle("Emphasize in UI", constraint.emphasizeInUI);

            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        [MenuItem("Assets/Create/WitPose/Standard Human Profile")]
        public static void CreateStandardHumanProfile()
        {
            SkeletonProfile profile = ScriptableObject.CreateInstance<SkeletonProfile>();
            profile.profileName = "Standard Adult Human";
            profile.description = "Standard anatomical constraints for an adult human skeleton with realistic joint limits.";
            profile.ResetToStandardHuman();

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (path == "")
            {
                path = "Assets";
            }
            else if (System.IO.Path.GetExtension(path) != "")
            {
                path = path.Replace(System.IO.Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
            }

            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/StandardHumanProfile.asset");
            AssetDatabase.CreateAsset(profile, assetPathAndName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = profile;
        }
    }
}