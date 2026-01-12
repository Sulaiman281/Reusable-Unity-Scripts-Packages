using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using WitShells.WitPose.Editor.Core;

namespace WitShells.WitPose.Editor.SceneGizmos
{
    /// <summary>
    /// Interactive bone gizmos in Scene View
    /// Primary UX for bone manipulation
    /// </summary>
    public class BoneGizmoSystem
    {
        private SkeletonCache skeleton;
        private BonePoseSystem poseSystem;
        private WitPoseEditor editorWindow; // Reference to editor window for bone selection
        
        private Color normalColor = new Color(0.5f, 0.8f, 1f, 0.7f);
        private Color selectedColor = new Color(1f, 0.6f, 0f, 1f);
        private Color hoverColor = new Color(0.8f, 1f, 0.8f, 0.9f);
        
        private float gizmoSize = 0.03f;
        private bool showConnections = true;
        private bool showRotationHandles = true;
        
        private Tool previousTool;
        private bool isActive = false;

        // Proxy bone support for constraint-driven rigs
        private Dictionary<Transform, Transform> proxyBoneMapping; // Original -> Proxy
        private bool useProxyBones = false;

        public bool IsActive => isActive;
        public bool ShowConnections { get => showConnections; set => showConnections = value; }
        public bool ShowRotationHandles { get => showRotationHandles; set => showRotationHandles = value; }
        public bool UseProxyBones 
        { 
            get => useProxyBones; 
            set 
            { 
                useProxyBones = value;
                Logger.Log($"Gizmos now targeting: {(useProxyBones ? "PROXY bones" : "ORIGINAL bones")}");
            } 
        }

        public BoneGizmoSystem(SkeletonCache skeleton, BonePoseSystem poseSystem, WitPoseEditor editorWindow = null)
        {
            this.skeleton = skeleton;
            this.poseSystem = poseSystem;
            this.editorWindow = editorWindow;
            this.proxyBoneMapping = new Dictionary<Transform, Transform>();
        }

        /// <summary>
        /// Set proxy bone mapping for constraint-driven rigs
        /// </summary>
        public void SetProxyBoneMapping(Dictionary<Transform, Transform> mapping)
        {
            proxyBoneMapping = mapping ?? new Dictionary<Transform, Transform>();
            Logger.Log($"Proxy bone mapping set: {proxyBoneMapping.Count} bones");
        }

        /// <summary>
        /// Get the transform to manipulate (proxy if enabled, otherwise original)
        /// </summary>
        private Transform GetTargetTransform(SkeletonCache.BoneData bone)
        {
            if (useProxyBones && proxyBoneMapping.TryGetValue(bone.transform, out Transform proxy))
            {
                return proxy;
            }
            return bone.transform;
        }

        public void Activate()
        {
            if (isActive)
                return;

            isActive = true;
            SceneView.duringSceneGui += OnSceneGUI;
            previousTool = Tools.current;
            Tools.current = Tool.None; // Hide Unity's transform tools
            
            Logger.Log("🎨 Bone Gizmos Activated");
        }

        public void Deactivate()
        {
            if (!isActive)
                return;

            isActive = false;
            SceneView.duringSceneGui -= OnSceneGUI;
            Tools.current = previousTool;
            
            Logger.Log("Bone Gizmos Deactivated");
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (skeleton == null)
                return;

            // Draw all bone connections first (background)
            if (showConnections)
            {
                DrawBoneConnections();
            }

            // Draw bone gizmos
            DrawBoneGizmos();

            // Draw rotation handles for selected bone
            if (showRotationHandles)
            {
                DrawRotationHandles();
            }

            // Handle bone selection
            HandleBoneSelection();
        }

        private void DrawBoneConnections()
        {
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);

            foreach (var bone in skeleton.AllBones)
            {
                if (bone.parent != null && bone.parent.transform != null)
                {
                    Transform targetTransform = GetTargetTransform(bone);
                    Transform parentTargetTransform = GetTargetTransform(bone.parent);
                    
                    if (targetTransform != null && parentTargetTransform != null)
                    {
                        Handles.DrawLine(parentTargetTransform.position, targetTransform.position);
                    }
                }
            }
        }

        private void DrawBoneGizmos()
        {
            foreach (var bone in skeleton.AllBones)
            {
                Transform targetTransform = GetTargetTransform(bone);
                if (targetTransform == null)
                    continue;

                Vector3 position = targetTransform.position;
                float size = HandleUtility.GetHandleSize(position) * gizmoSize;

                // Determine color
                Color color = bone.isSelected ? selectedColor : normalColor;
                
                // Tint proxy bones slightly different
                if (useProxyBones && proxyBoneMapping.ContainsKey(bone.transform))
                {
                    color = new Color(color.r * 1.2f, color.g, color.b * 1.2f, color.a);
                }

                // Draw sphere gizmo
                Handles.color = color;
                Handles.SphereHandleCap(0, position, Quaternion.identity, size, EventType.Repaint);

                // Draw bone label for selected
                if (bone.isSelected)
                {
                    string label = bone.boneType.ToString();
                    if (useProxyBones) label += " [CTRL]";
                    Handles.Label(position + Vector3.up * size * 2, label);
                }
            }
        }

        private void HandleBoneSelection()
        {
            Event e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                // Cast ray to find clicked bone
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                float closestDist = float.MaxValue;
                SkeletonCache.BoneData closestBone = null;

                foreach (var bone in skeleton.AllBones)
                {
                    Transform targetTransform = GetTargetTransform(bone);
                    if (targetTransform == null)
                        continue;

                    Vector3 position = targetTransform.position;
                    float size = HandleUtility.GetHandleSize(position) * gizmoSize;
                    float dist = HandleUtility.DistanceToCircle(position, size);

                    if (dist < 20f && dist < closestDist) // 20px threshold
                    {
                        closestDist = dist;
                        closestBone = bone;
                    }
                }

                if (closestBone != null)
                {
                    skeleton.ClearSelection();
                    closestBone.isSelected = true;
                    e.Use();
                    SceneView.RepaintAll();
                    
                    string target = useProxyBones ? " [CTRL]" : "";
                    Logger.Log($"Selected: {closestBone.boneType}{target}");
                    
                    // Notify editor window of bone selection
                    if (editorWindow != null)
                    {
                        editorWindow.SelectBone(closestBone.boneType);
                    }
                }
            }
        }

        private void DrawRotationHandles()
        {
            var selectedBone = skeleton.GetSelectedBone();
            if (selectedBone == null)
                return;

            Transform targetTransform = GetTargetTransform(selectedBone);
            if (targetTransform == null)
                return;

            Vector3 position = targetTransform.position;

            EditorGUI.BeginChangeCheck();

            // Draw rotation handle
            Quaternion newRotation = Handles.RotationHandle(targetTransform.rotation, position);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(targetTransform, "Rotate Bone");
                
                // When using proxy bones, manipulate the proxy directly
                // ParentConstraints will drive the original bones automatically
                if (useProxyBones)
                {
                    // Convert to local rotation for proxy
                    if (targetTransform.parent != null)
                    {
                        Quaternion localRotation = Quaternion.Inverse(targetTransform.parent.rotation) * newRotation;
                        targetTransform.localRotation = localRotation;
                    }
                    else
                    {
                        targetTransform.rotation = newRotation;
                    }
                }
                else
                {
                    // Original behavior: use pose system
                    if (targetTransform.parent != null)
                    {
                        Quaternion localRotation = Quaternion.Inverse(targetTransform.parent.rotation) * newRotation;
                        poseSystem.SetBoneRotation(selectedBone.boneType, localRotation, recordUndo: false);
                    }
                    else
                    {
                        poseSystem.SetBoneRotation(selectedBone.boneType, newRotation, recordUndo: false);
                    }

                    // Commit immediately
                    poseSystem.CommitPose();
                }
                
                SceneView.RepaintAll();
            }
        }

        /// <summary>
        /// Select bone programmatically
        /// </summary>
        public void SelectBone(HumanBodyBones bone)
        {
            skeleton.SelectBone(bone, exclusive: true);
            SceneView.RepaintAll();
        }

        /// <summary>
        /// Clear selection
        /// </summary>
        public void ClearSelection()
        {
            skeleton.ClearSelection();
            SceneView.RepaintAll();
        }
    }
}
