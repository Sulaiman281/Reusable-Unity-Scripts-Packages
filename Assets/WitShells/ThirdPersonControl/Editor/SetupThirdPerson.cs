using UnityEditor;
using UnityEngine;
using UnityEditor.Animations;
using Unity.Cinemachine;

namespace WitShells.ThirdPersonControl
{
    public class SetupThirdPerson : MonoBehaviour
    {
        [MenuItem("WitShells/ThirdPersonSetup/Setup ThirdPerson Character")]
        private static void SetupThirdPersonCharacter()
        {
            if (Selection.activeGameObject == null)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select a GameObject in the hierarchy.", "OK");
                return;
            }

            GameObject selected = Selection.activeGameObject;

            // Check for Animator
            Animator animator = selected.GetComponent<Animator>();
            if (animator == null)
            {
                EditorUtility.DisplayDialog("Missing Animator", "Selected GameObject must have an Animator component.", "OK");
                return;
            }

            // Add CharacterController if not present
            CharacterController cc = selected.GetComponent<CharacterController>();
            if (cc == null)
                cc = Undo.AddComponent<CharacterController>(selected);

            // Configure CharacterController
            Undo.RecordObject(cc, "Configure CharacterController");
            cc.radius = 0.3f;
            cc.center = new Vector3(0, 1f, 0);
            cc.height = 2f;

            // Add ThirdPersonControl if not present
            var tpc = selected.GetComponent<ThirdPersonControl>();
            if (tpc == null)
                tpc = Undo.AddComponent<ThirdPersonControl>(selected);

            // Assign Animator Controller from Resources/Animation/LocoMotion
            var controller = Resources.Load<RuntimeAnimatorController>("Animations/LocoMotion");
            if (controller != null)
            {
                Undo.RecordObject(animator, "Assign Animator Controller");
                animator.runtimeAnimatorController = controller;
            }
            else
            {
                Debug.LogWarning("LocoMotion Animator Controller not found in Resources/Animation.");
            }

            // Create CameraTarget child
            Transform cameraTarget = selected.transform.Find("CameraTarget");
            if (cameraTarget == null)
            {
                GameObject camTargetObj = new GameObject("CameraTarget");
                Undo.RegisterCreatedObjectUndo(camTargetObj, "Create CameraTarget");
                camTargetObj.transform.SetParent(selected.transform);
                camTargetObj.transform.localPosition = new Vector3(0, 1.75f, 0); // Adjust position as needed
                cameraTarget = camTargetObj.transform;
            }

            // Assign CameraTarget reference in ThirdPersonControl
            var so = new SerializedObject(tpc);
            var prop = so.FindProperty("cameraTarget");
            if (prop != null)
            {
                prop.objectReferenceValue = cameraTarget;
                so.ApplyModifiedProperties();
            }

            // Instantiate ThirdPersonFollow camera prefab from Resources
            var camPrefab = Resources.Load<GameObject>("ThirdPersonFollow");
            if (camPrefab != null)
            {
                GameObject camInstance = (GameObject)PrefabUtility.InstantiatePrefab(camPrefab);
                Undo.RegisterCreatedObjectUndo(camInstance, "Instantiate ThirdPersonFollow Camera");
                camInstance.transform.SetParent(selected.transform.parent);

                // Assign CameraTarget to CinemachineCamera's TrackingTarget
                var cineCam = camInstance.GetComponent<CinemachineCamera>();
                if (cineCam != null)
                {
                    var cineSo = new SerializedObject(cineCam);
                    cineCam.Target.TrackingTarget = cameraTarget;
                    cineSo.Update();
                    var trackingTargetProp = cineSo.FindProperty("m_TrackingTarget");
                    if (trackingTargetProp != null)
                    {
                        trackingTargetProp.objectReferenceValue = cameraTarget;
                        cineSo.ApplyModifiedProperties();
                    }
                }

                // Assign CameraTarget to CinemachineCamLookInput if needed
                var camLookInput = camInstance.GetComponent<CinemachineCamLookInput>();
                if (camLookInput != null)
                {
                    var camSo = new SerializedObject(camLookInput);
                    var camTargetProp = camSo.FindProperty("cinemachineCamera");
                    if (camTargetProp != null)
                    {
                        // If needed, assign the CinemachineCamera reference here
                    }
                    camSo.ApplyModifiedProperties();
                }
            }
            else
            {
                Debug.LogWarning("ThirdPersonFollow prefab not found in Resources.");
            }

            // Ensure Main Camera has CinemachineBrain
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                var brain = mainCam.GetComponent<CinemachineBrain>();
                if (brain == null)
                {
                    Undo.AddComponent<CinemachineBrain>(mainCam.gameObject);
                    Debug.Log("CinemachineBrain added to Main Camera.");
                }
            }
            else
            {
                Debug.LogWarning("No Main Camera found in the scene.");
            }

            EditorUtility.DisplayDialog("Setup Complete", "Third Person Character and Camera setup is complete!", "OK");
        }
    }
}