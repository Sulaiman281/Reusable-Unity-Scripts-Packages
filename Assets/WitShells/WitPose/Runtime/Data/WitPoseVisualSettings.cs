using UnityEngine;

namespace WitShells.WitPose
{
    /// <summary>
    /// Singleton ScriptableObject that stores visual settings for WitPose editor
    /// Automatically loads from Resources and persists between sessions
    /// </summary>
    [CreateAssetMenu(fileName = "WitPoseVisualSettings", menuName = "WitPose/Visual Settings", order = 2)]
    public class WitPoseVisualSettings : ScriptableObject
    {
        private static WitPoseVisualSettings _instance;
        
        [Header("Gizmo Colors")]
        [Tooltip("Color for selected bones")]
        public Color selectedColor = Color.green;
        
        [Tooltip("Color for bones with constraints")]
        public Color constrainedColor = Color.red;
        
        [Tooltip("Color for normal bones")]
        public Color normalColor = Color.yellow;
        
        [Tooltip("Color for bone connections")]
        public Color connectionColor = Color.gray;
        
        [Tooltip("Color for free mode selected bones")]
        public Color freeModeColor = Color.cyan;
        
        [Tooltip("Color for knee bend gizmo")]
        public Color kneeBendColor = Color.magenta;
        
        [Header("Gizmo Display")]
        [Range(0.01f, 0.5f)]
        [Tooltip("Size of bone gizmos")]
        public float gizmoSize = 0.1f;
        
        [Tooltip("Show bone names as labels")]
        public bool showBoneLabels = true;
        
        [Tooltip("Show connections between bones")]
        public bool showConnections = true;
        
        [Tooltip("Show constraint visualizations")]
        public bool showConstraintVisuals = true;
        
        [Header("Knee Bend Settings")]
        [Range(0.1f, 2.0f)]
        [Tooltip("Height range for knee bending (in units)")]
        public float kneeBendRange = 1.0f;
        
        [Range(0f, 140f)]
        [Tooltip("Maximum knee bend angle")]
        public float maxKneeBendAngle = 120f;
        
        [Range(0.01f, 0.2f)]
        [Tooltip("Size of knee bend gizmo handle")]
        public float kneeBendGizmoSize = 0.05f;
        
        /// <summary>
        /// Get singleton instance, loading from Resources or creating if needed
        /// </summary>
        public static WitPoseVisualSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Try to load from Resources first
                    _instance = Resources.Load<WitPoseVisualSettings>("WitPoseVisualSettings");
                    
                    if (_instance == null)
                    {
                        // Create new instance if not found
                        _instance = CreateInstance<WitPoseVisualSettings>();
                        
#if UNITY_EDITOR
                        // Save to Resources folder in editor
                        string resourcesPath = "Assets/WitShells/WitPose/Resources";
                        if (!UnityEditor.AssetDatabase.IsValidFolder(resourcesPath))
                        {
                            UnityEditor.AssetDatabase.CreateFolder("Assets/WitShells/WitPose", "Resources");
                        }
                        
                        string assetPath = resourcesPath + "/WitPoseVisualSettings.asset";
                        UnityEditor.AssetDatabase.CreateAsset(_instance, assetPath);
                        UnityEditor.AssetDatabase.SaveAssets();
                        
                        Debug.Log($"WitPose: Created visual settings at {assetPath}");
#endif
                    }
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// Save settings to disk (Editor only)
        /// </summary>
        public void SaveSettings()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }
        
        /// <summary>
        /// Reset to default values
        /// </summary>
        [ContextMenu("Reset to Defaults")]
        public void ResetToDefaults()
        {
            selectedColor = Color.green;
            constrainedColor = Color.red;
            normalColor = Color.yellow;
            connectionColor = Color.gray;
            freeModeColor = Color.cyan;
            kneeBendColor = Color.magenta;
            
            gizmoSize = 0.1f;
            showBoneLabels = true;
            showConnections = true;
            showConstraintVisuals = true;
            
            kneeBendRange = 1.0f;
            maxKneeBendAngle = 120f;
            kneeBendGizmoSize = 0.05f;
            
            SaveSettings();
        }
    }
}