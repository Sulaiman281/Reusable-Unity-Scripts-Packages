using UnityEditor;
using UnityEngine;
using Unity.Cinemachine;
using System.IO;

namespace WitShells.ThirdPersonControl
{
    /// <summary>
    /// Editor utility to set up a complete third-person character controller.
    /// Creates all necessary components, settings, and camera setup.
    /// </summary>
    public class SetupThirdPerson : MonoBehaviour
    {
        private const string RESOURCES_PATH = "Assets/Resources/ThirdPerson";
        private const string SETTINGS_ASSET_NAME = "ThirdPersonSettings.asset";
        private const string SOUND_ASSET_NAME = "SoundSfxObject.asset";

        [MenuItem("WitShells/ThirdPersonSetup/Setup ThirdPerson Character")]
        private static void SetupThirdPersonCharacter()
        {
            if (Selection.activeGameObject == null)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select a GameObject with an Animator in the hierarchy.", "OK");
                return;
            }

            GameObject selectedCharacter = Selection.activeGameObject;

            // Check for Animator
            Animator animator = selectedCharacter.GetComponent<Animator>();
            if (animator == null)
            {
                EditorUtility.DisplayDialog("Missing Animator", "Selected GameObject must have an Animator component.", "OK");
                return;
            }

            // Create settings assets
            ThirdPersonSettings settings = CreateOrLoadSettings();
            SoundSfxObject soundEffects = CreateOrLoadSoundEffects();

            // Assign sound effects to settings if available
            if (settings != null && soundEffects != null)
            {
                var settingsSo = new SerializedObject(settings);
                var soundProp = settingsSo.FindProperty("soundEffects");
                if (soundProp != null)
                {
                    soundProp.objectReferenceValue = soundEffects;
                    settingsSo.ApplyModifiedProperties();
                }
            }

            // Create root ThirdPersonSetup object
            GameObject rootSetup = new GameObject("ThirdPersonSetup");
            Undo.RegisterCreatedObjectUndo(rootSetup, "Create ThirdPersonSetup Root");
            rootSetup.transform.position = selectedCharacter.transform.position;

            // Parent the selected character to root as first child
            Undo.SetTransformParent(selectedCharacter.transform, rootSetup.transform, "Parent Character to Setup");
            selectedCharacter.transform.localPosition = Vector3.zero;
            selectedCharacter.transform.localRotation = Quaternion.identity;

            // Setup character components
            SetupCharacterComponents(selectedCharacter, settings);

            // Create CameraTarget
            Transform cameraTarget = CreateCameraTarget(selectedCharacter);

            // Get ThirdPersonControl and assign camera target
            var tpc = selectedCharacter.GetComponent<ThirdPersonControl>();
            if (tpc != null)
            {
                var tpcSo = new SerializedObject(tpc);
                var camTargetProp = tpcSo.FindProperty("cameraTarget");
                if (camTargetProp != null)
                {
                    camTargetProp.objectReferenceValue = cameraTarget;
                    tpcSo.ApplyModifiedProperties();
                }
            }

            // Create and setup camera
            GameObject cameraObj = CreateCameraSetup(rootSetup.transform, cameraTarget, settings);

            // Setup ThirdPersonInput with camera reference
            var input = selectedCharacter.GetComponent<ThirdPersonInput>();
            if (input != null)
            {
                var inputSo = new SerializedObject(input);
                
                // Assign target controller
                var targetProp = inputSo.FindProperty("targetController");
                if (targetProp != null)
                {
                    targetProp.objectReferenceValue = tpc;
                }

                // Assign camera controller
                var camLookInput = cameraObj.GetComponent<CinemachineCamLookInput>();
                var camControllerProp = inputSo.FindProperty("cameraController");
                if (camControllerProp != null && camLookInput != null)
                {
                    camControllerProp.objectReferenceValue = camLookInput;
                }

                inputSo.ApplyModifiedProperties();
            }

            // Ensure Main Camera has CinemachineBrain
            EnsureCinemachineBrain();

            // Select the root setup object
            Selection.activeGameObject = rootSetup;

            EditorUtility.DisplayDialog("Setup Complete", 
                "Third Person Character setup is complete!\n\n" +
                "Structure created:\n" +
                "• ThirdPersonSetup (Root)\n" +
                "  └─ " + selectedCharacter.name + " (Character)\n" +
                "     └─ CameraTarget\n" +
                "  └─ ThirdPersonCamera\n\n" +
                "Settings created in Resources/ThirdPerson/\n\n" +
                "Press Play to test!", "OK");
        }

        [MenuItem("WitShells/ThirdPersonSetup/Create Settings Only")]
        private static void CreateSettingsOnly()
        {
            ThirdPersonSettings settings = CreateOrLoadSettings();
            SoundSfxObject soundEffects = CreateOrLoadSoundEffects();

            if (settings != null && soundEffects != null)
            {
                var settingsSo = new SerializedObject(settings);
                var soundProp = settingsSo.FindProperty("soundEffects");
                if (soundProp != null)
                {
                    soundProp.objectReferenceValue = soundEffects;
                    settingsSo.ApplyModifiedProperties();
                }
            }

            EditorUtility.DisplayDialog("Settings Created", 
                "Settings assets created in Resources/ThirdPerson/\n\n" +
                "• ThirdPersonSettings.asset\n" +
                "• SoundSfxObject.asset", "OK");
        }

        private static void SetupCharacterComponents(GameObject character, ThirdPersonSettings settings)
        {
            // Add CharacterController if not present
            CharacterController cc = character.GetComponent<CharacterController>();
            if (cc == null)
                cc = Undo.AddComponent<CharacterController>(character);

            // Configure CharacterController
            Undo.RecordObject(cc, "Configure CharacterController");
            cc.radius = 0.3f;
            cc.center = new Vector3(0, 1f, 0);
            cc.height = 2f;
            cc.skinWidth = 0.02f;
            cc.minMoveDistance = 0.001f;

            // Add ThirdPersonControl if not present
            var tpc = character.GetComponent<ThirdPersonControl>();
            if (tpc == null)
                tpc = Undo.AddComponent<ThirdPersonControl>(character);

            // Assign settings to ThirdPersonControl
            if (settings != null)
            {
                var tpcSo = new SerializedObject(tpc);
                var settingsProp = tpcSo.FindProperty("settings");
                if (settingsProp != null)
                {
                    settingsProp.objectReferenceValue = settings;
                    tpcSo.ApplyModifiedProperties();
                }
            }

            // Add ThirdPersonInput if not present
            var input = character.GetComponent<ThirdPersonInput>();
            if (input == null)
                input = Undo.AddComponent<ThirdPersonInput>(character);

            // Add AnimationEventHandler if not present
            var animHandler = character.GetComponentInChildren<AnimationEventHandler>();
            if (animHandler == null)
            {
                // Add to the object with the Animator
                Animator animator = character.GetComponent<Animator>();
                if (animator == null)
                    animator = character.GetComponentInChildren<Animator>();

                if (animator != null)
                {
                    animHandler = Undo.AddComponent<AnimationEventHandler>(animator.gameObject);
                    
                    // Assign settings to AnimationEventHandler
                    if (settings != null)
                    {
                        var handlerSo = new SerializedObject(animHandler);
                        var handlerSettingsProp = handlerSo.FindProperty("settings");
                        if (handlerSettingsProp != null)
                        {
                            handlerSettingsProp.objectReferenceValue = settings;
                            handlerSo.ApplyModifiedProperties();
                        }
                    }
                }
            }

            // Set ground layers (Default + Ground layers)
            var tpcSerializedObj = new SerializedObject(tpc);
            var groundLayersProp = tpcSerializedObj.FindProperty("groundLayers");
            if (groundLayersProp != null)
            {
                // Set to "Default" and "Ground" layers
                groundLayersProp.intValue = LayerMask.GetMask("Default", "Ground");
                tpcSerializedObj.ApplyModifiedProperties();
            }
        }

        private static Transform CreateCameraTarget(GameObject character)
        {
            Transform cameraTarget = character.transform.Find("CameraTarget");
            if (cameraTarget == null)
            {
                GameObject camTargetObj = new GameObject("CameraTarget");
                Undo.RegisterCreatedObjectUndo(camTargetObj, "Create CameraTarget");
                camTargetObj.transform.SetParent(character.transform);
                camTargetObj.transform.localPosition = new Vector3(0, 1.6f, 0);
                camTargetObj.transform.localRotation = Quaternion.identity;
                cameraTarget = camTargetObj.transform;
            }
            return cameraTarget;
        }

        private static GameObject CreateCameraSetup(Transform parent, Transform trackingTarget, ThirdPersonSettings settings)
        {
            // Create camera GameObject
            GameObject cameraObj = new GameObject("ThirdPersonCamera");
            Undo.RegisterCreatedObjectUndo(cameraObj, "Create ThirdPersonCamera");
            cameraObj.transform.SetParent(parent);
            cameraObj.transform.localPosition = new Vector3(0, 2f, -4f);
            cameraObj.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);

            // Add CinemachineCamera
            CinemachineCamera cinemachineCam = Undo.AddComponent<CinemachineCamera>(cameraObj);
            
            // Configure CinemachineCamera
            var cineSo = new SerializedObject(cinemachineCam);
            
            // Set tracking target
            cinemachineCam.Target.TrackingTarget = trackingTarget;
            
            // Set priority
            cinemachineCam.Priority = 10;

            cineSo.ApplyModifiedProperties();

            // Add CinemachineThirdPersonFollow as body component
            CinemachineThirdPersonFollow thirdPersonFollow = cameraObj.AddComponent<CinemachineThirdPersonFollow>();
            
            // Configure third person follow settings
            var followSo = new SerializedObject(thirdPersonFollow);
            
            // Set shoulder offset
            var shoulderOffsetProp = followSo.FindProperty("ShoulderOffset");
            if (shoulderOffsetProp != null)
            {
                shoulderOffsetProp.vector3Value = new Vector3(0.5f, -0.4f, 0f);
            }
            
            // Set camera distance
            var distanceProp = followSo.FindProperty("CameraDistance");
            if (distanceProp != null)
            {
                distanceProp.floatValue = 4f;
            }

            // Set vertical arm length
            var verticalArmProp = followSo.FindProperty("VerticalArmLength");
            if (verticalArmProp != null)
            {
                verticalArmProp.floatValue = 0.4f;
            }

            // Set camera side (0.5 = center, 1 = right, 0 = left)
            var cameraSideProp = followSo.FindProperty("CameraSide");
            if (cameraSideProp != null)
            {
                cameraSideProp.floatValue = 1f;
            }

            // Set damping
            var dampingProp = followSo.FindProperty("Damping");
            if (dampingProp != null)
            {
                dampingProp.vector3Value = new Vector3(0.1f, 0.5f, 0.3f);
            }

            followSo.ApplyModifiedProperties();

            // Add CinemachineHardLookAt as aim component
            CinemachineHardLookAt hardLookAt = cameraObj.AddComponent<CinemachineHardLookAt>();

            // Add CinemachineCamLookInput for camera rotation
            CinemachineCamLookInput camLookInput = Undo.AddComponent<CinemachineCamLookInput>(cameraObj);
            
            // Configure CinemachineCamLookInput
            var camLookSo = new SerializedObject(camLookInput);
            
            var cinemachineCamProp = camLookSo.FindProperty("cinemachineCamera");
            if (cinemachineCamProp != null)
            {
                cinemachineCamProp.objectReferenceValue = cinemachineCam;
            }

            // Assign settings if available
            if (settings != null)
            {
                var camSettingsProp = camLookSo.FindProperty("settings");
                if (camSettingsProp != null)
                {
                    camSettingsProp.objectReferenceValue = settings;
                }
            }

            camLookSo.ApplyModifiedProperties();

            return cameraObj;
        }

        private static void EnsureCinemachineBrain()
        {
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
                // Create a new main camera if none exists
                GameObject camObj = new GameObject("Main Camera");
                Undo.RegisterCreatedObjectUndo(camObj, "Create Main Camera");
                camObj.tag = "MainCamera";
                Camera cam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
                Undo.AddComponent<CinemachineBrain>(camObj);
                Debug.Log("Main Camera with CinemachineBrain created.");
            }
        }

        private static ThirdPersonSettings CreateOrLoadSettings()
        {
            // Ensure Resources/ThirdPerson directory exists
            EnsureDirectoryExists(RESOURCES_PATH);

            string settingsPath = Path.Combine(RESOURCES_PATH, SETTINGS_ASSET_NAME);

            // Check if settings already exist
            ThirdPersonSettings settings = AssetDatabase.LoadAssetAtPath<ThirdPersonSettings>(settingsPath);
            
            if (settings == null)
            {
                // Create new settings asset
                settings = ScriptableObject.CreateInstance<ThirdPersonSettings>();
                AssetDatabase.CreateAsset(settings, settingsPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"ThirdPersonSettings created at: {settingsPath}");
            }

            return settings;
        }

        private static SoundSfxObject CreateOrLoadSoundEffects()
        {
            // Ensure Resources/ThirdPerson directory exists
            EnsureDirectoryExists(RESOURCES_PATH);

            string soundPath = Path.Combine(RESOURCES_PATH, SOUND_ASSET_NAME);

            // Check if sound effects already exist
            SoundSfxObject soundEffects = AssetDatabase.LoadAssetAtPath<SoundSfxObject>(soundPath);
            
            if (soundEffects == null)
            {
                // Create new sound effects asset
                soundEffects = ScriptableObject.CreateInstance<SoundSfxObject>();
                AssetDatabase.CreateAsset(soundEffects, soundPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"SoundSfxObject created at: {soundPath}");
            }

            return soundEffects;
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string[] folders = path.Split('/');
                string currentPath = folders[0];
                
                for (int i = 1; i < folders.Length; i++)
                {
                    string nextPath = currentPath + "/" + folders[i];
                    if (!AssetDatabase.IsValidFolder(nextPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, folders[i]);
                    }
                    currentPath = nextPath;
                }
            }
        }

        [MenuItem("WitShells/ThirdPersonSetup/Setup ThirdPerson Character", true)]
        private static bool ValidateSetupThirdPersonCharacter()
        {
            // Only enable if a GameObject with Animator is selected
            if (Selection.activeGameObject == null)
                return false;
            
            return Selection.activeGameObject.GetComponent<Animator>() != null;
        }
    }
}