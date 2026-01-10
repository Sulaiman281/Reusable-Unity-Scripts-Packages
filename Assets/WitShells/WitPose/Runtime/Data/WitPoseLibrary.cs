using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace WitShells.WitPose
{
    /// <summary>
    /// Singleton ScriptableObject that stores and manages all saved poses
    /// Automatically loads from Resources and persists between sessions
    /// </summary>
    [CreateAssetMenu(fileName = "WitPoseLibrary", menuName = "WitPose/Pose Library", order = 1)]
    public class WitPoseLibrary : ScriptableObject
    {
        private static WitPoseLibrary _instance;

        [Header("Saved Poses")]
        [SerializeField]
        private List<PoseData> savedPoses = new List<PoseData>();

        [Header("Library Settings")]
        [SerializeField]
        private bool autoBackup = true;

        [SerializeField]
        private int maxPoses = 100;

        /// <summary>
        /// Get singleton instance, loading from Resources or creating if needed
        /// </summary>
        public static WitPoseLibrary Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Try to load from Resources first
                    _instance = Resources.Load<WitPoseLibrary>("WitPoseLibrary");

                    if (_instance == null)
                    {
                        // Create new instance if not found
                        _instance = CreateInstance<WitPoseLibrary>();

#if UNITY_EDITOR
                        // Save to Resources folder in editor
                        string resourcesPath = "Assets/WitShells/WitPose/Resources";
                        if (!UnityEditor.AssetDatabase.IsValidFolder(resourcesPath))
                        {
                            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/WitShells/WitPose"))
                            {
                                UnityEditor.AssetDatabase.CreateFolder("Assets/WitShells", "WitPose");
                            }
                            UnityEditor.AssetDatabase.CreateFolder("Assets/WitShells/WitPose", "Resources");
                        }

                        string assetPath = resourcesPath + "/WitPoseLibrary.asset";
                        UnityEditor.AssetDatabase.CreateAsset(_instance, assetPath);
                        UnityEditor.AssetDatabase.SaveAssets();

                        Debug.Log($"WitPose: Created pose library at {assetPath}");
#endif
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Get all saved poses
        /// </summary>
        public List<PoseData> GetAllPoses() => new List<PoseData>(savedPoses);

        /// <summary>
        /// Save a new pose to the library
        /// </summary>
        public bool SavePose(PoseData poseData)
        {
            if (poseData == null)
            {
                Debug.LogWarning("WitPose: Cannot save null pose data");
                return false;
            }

            // Check for duplicate names
            if (savedPoses.Any(p => p.poseName == poseData.poseName))
            {
                Debug.LogWarning($"WitPose: Pose with name '{poseData.poseName}' already exists. Use UpdatePose() or choose a different name.");
                return false;
            }

            // Check pose limit
            if (savedPoses.Count >= maxPoses)
            {
                Debug.LogWarning($"WitPose: Maximum pose limit ({maxPoses}) reached. Remove some poses first.");
                return false;
            }

            // Add timestamp
            poseData.timestamp = System.DateTimeOffset.Now.ToUnixTimeSeconds();

            savedPoses.Add(poseData);
            SaveLibrary();

            Debug.Log($"WitPose: Saved pose '{poseData.poseName}' to library. Total poses: {savedPoses.Count}");
            return true;
        }

        /// <summary>
        /// Update an existing pose
        /// </summary>
        public bool UpdatePose(string poseName, PoseData newPoseData)
        {
            var existingPose = savedPoses.FirstOrDefault(p => p.poseName == poseName);
            if (existingPose == null)
            {
                Debug.LogWarning($"WitPose: Pose '{poseName}' not found for update");
                return false;
            }

            // Update the pose data
            int index = savedPoses.IndexOf(existingPose);
            newPoseData.poseName = poseName; // Keep the same name
            newPoseData.timestamp = System.DateTimeOffset.Now.ToUnixTimeSeconds();
            savedPoses[index] = newPoseData;

            SaveLibrary();
            Debug.Log($"WitPose: Updated pose '{poseName}'");
            return true;
        }

        /// <summary>
        /// Get a pose by name
        /// </summary>
        public PoseData GetPose(string poseName)
        {
            return savedPoses.FirstOrDefault(p => p.poseName == poseName);
        }

        /// <summary>
        /// Remove a pose from the library
        /// </summary>
        public bool RemovePose(string poseName)
        {
            var pose = savedPoses.FirstOrDefault(p => p.poseName == poseName);
            if (pose == null)
            {
                Debug.LogWarning($"WitPose: Pose '{poseName}' not found for removal");
                return false;
            }

            savedPoses.Remove(pose);
            SaveLibrary();

            Debug.Log($"WitPose: Removed pose '{poseName}' from library");
            return true;
        }

        /// <summary>
        /// Clear all poses from the library
        /// </summary>
        public void ClearAllPoses()
        {
            int count = savedPoses.Count;
            savedPoses.Clear();
            SaveLibrary();

            Debug.Log($"WitPose: Cleared all {count} poses from library");
        }

        /// <summary>
        /// Get pose names for dropdown/selection
        /// </summary>
        public string[] GetPoseNames()
        {
            return savedPoses.Select(p => p.poseName).ToArray();
        }

        /// <summary>
        /// Get pose count
        /// </summary>
        public int GetPoseCount() => savedPoses.Count;

        /// <summary>
        /// Save library to disk (Editor only)
        /// </summary>
        private void SaveLibrary()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        /// <summary>
        /// Export all poses to JSON file
        /// </summary>
        public void ExportPosesToJSON(string filePath)
        {
#if UNITY_EDITOR
            var exportData = new PoseLibraryExport
            {
                poses = savedPoses.ToArray(),
                exportDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                version = "1.0"
            };

            string json = JsonUtility.ToJson(exportData, true);
            System.IO.File.WriteAllText(filePath, json);
            Debug.Log($"WitPose: Exported {savedPoses.Count} poses to {filePath}");
#endif
        }

        /// <summary>
        /// Import poses from JSON file
        /// </summary>
        public void ImportPosesFromJSON(string filePath)
        {
#if UNITY_EDITOR
            if (!System.IO.File.Exists(filePath))
            {
                Debug.LogError($"WitPose: File not found: {filePath}");
                return;
            }

            string json = System.IO.File.ReadAllText(filePath);
            var importData = JsonUtility.FromJson<PoseLibraryExport>(json);

            if (importData?.poses != null)
            {
                int importedCount = 0;
                foreach (var pose in importData.poses)
                {
                    if (!savedPoses.Any(p => p.poseName == pose.poseName))
                    {
                        savedPoses.Add(pose);
                        importedCount++;
                    }
                }

                SaveLibrary();
                Debug.Log($"WitPose: Imported {importedCount} new poses from {filePath}");
            }
#endif
        }

        [System.Serializable]
        private class PoseLibraryExport
        {
            public PoseData[] poses;
            public string exportDate;
            public string version;
        }
    }
}