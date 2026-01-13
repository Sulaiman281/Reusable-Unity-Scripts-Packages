using UnityEngine;
using UnityEditor;
using WitShells.ShootingSystem;

namespace WitShells.ShootingSystem.Editor
{
    public static class WeaponSetupMenu
    {
        [MenuItem("GameObject/WitShells/ShootingSystem/Setup Weapon", false, 10)]
        static void SetupWeapon(MenuCommand menuCommand)
        {
            GameObject selectedObject = Selection.activeGameObject;
            if (selectedObject == null)
            {
                EditorUtility.DisplayDialog("Setup Weapon", "Please select a GameObject to setup as a weapon.", "OK");
                return;
            }

            SetupWeaponHierarchy(selectedObject);
        }

        [MenuItem("GameObject/WitShells/ShootingSystem/Setup Weapon", true)]
        static bool ValidateSetupWeapon()
        {
            return Selection.activeGameObject != null;
        }

        public static GameObject SetupWeaponHierarchy(GameObject selectedObject)
        {
            // Create weapon root object
            GameObject weaponRoot = new GameObject(selectedObject.name + "_Weapon");
            weaponRoot.transform.position = selectedObject.transform.position;
            weaponRoot.transform.rotation = selectedObject.transform.rotation;

            // Set up parent hierarchy
            Transform originalParent = selectedObject.transform.parent;
            if (originalParent != null)
            {
                weaponRoot.transform.SetParent(originalParent);
            }

            // Create visuals container
            GameObject visualsContainer = new GameObject("Visuals");
            visualsContainer.transform.SetParent(weaponRoot.transform);
            visualsContainer.transform.localPosition = Vector3.zero;
            visualsContainer.transform.localRotation = Quaternion.identity;

            // Move original model to visuals container
            selectedObject.transform.SetParent(visualsContainer.transform);
            selectedObject.transform.localPosition = Vector3.zero;
            selectedObject.transform.localRotation = Quaternion.identity;

            // Create muzzle transform
            GameObject muzzleTransform = new GameObject("Muzzle");
            muzzleTransform.transform.SetParent(selectedObject.transform);
            
            // Try to position muzzle at a reasonable location (front of the model)
            Renderer renderer = selectedObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                Bounds bounds = renderer.bounds;
                Vector3 muzzlePos = bounds.center + selectedObject.transform.forward * (bounds.size.z * 0.5f);
                muzzleTransform.transform.position = muzzlePos;
            }
            else
            {
                muzzleTransform.transform.localPosition = Vector3.forward;
            }

            // Add Weapon component to root
            Weapon weaponComponent = weaponRoot.GetComponent<Weapon>();
            if (weaponComponent == null)
            {
                weaponComponent = weaponRoot.AddComponent<Weapon>();
            }

            // Set up AudioSource with 3D settings
            AudioSource audioSource = weaponRoot.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = weaponRoot.AddComponent<AudioSource>();
            }
            
            // Configure AudioSource for 3D spatial audio
            audioSource.spatialBlend = 1.0f; // 3D
            audioSource.volume = 0.8f;
            audioSource.pitch = 1.0f;
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.dopplerLevel = 1.0f;
            audioSource.spread = 0.0f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.minDistance = 1.0f;
            audioSource.maxDistance = 50.0f;

            // Set muzzle transform reference using reflection (since it's private)
            var muzzleField = typeof(Weapon).GetField("muzzleTransform", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (muzzleField != null)
            {
                muzzleField.SetValue(weaponComponent, muzzleTransform.transform);
            }

            // Mark objects as dirty for undo system
            Undo.RegisterCreatedObjectUndo(weaponRoot, "Setup Weapon");
            Selection.activeGameObject = weaponRoot;

            // Open the weapon setup window
            WeaponSetupWindow.ShowWindow(weaponComponent);

            Debug.Log($"Weapon setup complete for '{selectedObject.name}'. Use the Weapon Setup Window to complete configuration.");
            
            return weaponRoot;
        }
    }
}