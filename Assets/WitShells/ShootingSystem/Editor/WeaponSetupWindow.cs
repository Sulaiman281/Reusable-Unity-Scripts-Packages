using UnityEngine;
using UnityEditor;
using WitShells.ShootingSystem;
using System.Collections.Generic;

namespace WitShells.ShootingSystem.Editor
{
    public class WeaponSetupWindow : EditorWindow
    {
        private Weapon targetWeapon;
        private Vector2 scrollPosition;
        
        // Checklist states
        private Dictionary<string, bool> checklistStates = new Dictionary<string, bool>();
        
        // Preset selection
        private int selectedPresetIndex = 0;
        private WeaponPreset[] presets;
        private string[] presetNames;
        
        // UI Styles
        private GUIStyle headerStyle;
        private GUIStyle checklistItemStyle;
        private GUIStyle completedItemStyle;
        private GUIStyle sectionHeaderStyle;
        
        private bool stylesInitialized = false;

        public static void ShowWindow(Weapon weapon)
        {
            WeaponSetupWindow window = GetWindow<WeaponSetupWindow>("Weapon Setup");
            window.targetWeapon = weapon;
            window.Initialize();
            window.Show();
        }

        private void Initialize()
        {
            presets = WeaponPresets.GetPresets();
            presetNames = new string[presets.Length + 1];
            presetNames[0] = "Select Preset...";
            for (int i = 0; i < presets.Length; i++)
            {
                presetNames[i + 1] = presets[i].name;
            }

            InitializeChecklist();
        }

        private void InitializeChecklist()
        {
            checklistStates.Clear();
            checklistStates["muzzleTransform"] = false;
            checklistStates["shootSound"] = false;
            checklistStates["reloadSound"] = false;
            checklistStates["weaponClickSound"] = false;
            checklistStates["muzzleEffects"] = false;
            checklistStates["weaponSettings"] = false;
            checklistStates["modelPosition"] = false;
        }

        private void InitializeStyles()
        {
            if (stylesInitialized) return;

            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };

            sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
            };

            checklistItemStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                wordWrap = true
            };

            completedItemStyle = new GUIStyle(checklistItemStyle)
            {
                normal = { textColor = Color.green }
            };

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitializeStyles();

            if (targetWeapon == null)
            {
                EditorGUILayout.HelpBox("No weapon selected. Please select a weapon to setup.", MessageType.Warning);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Header
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("🔫 WEAPON SETUP WIZARD", headerStyle);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"Setting up: {targetWeapon.name}", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space(15);

            // Weapon Presets Section
            DrawPresetSection();
            EditorGUILayout.Space(10);

            // Checklist Section  
            DrawChecklistSection();
            EditorGUILayout.Space(10);

            // Quick Actions Section
            DrawQuickActionsSection();

            EditorGUILayout.EndScrollView();

            UpdateChecklist();
        }

        private void DrawPresetSection()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("⚙️ WEAPON PRESETS", sectionHeaderStyle);
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox("Apply a preset to quickly configure your weapon with balanced settings.", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            int newPresetIndex = EditorGUILayout.Popup("Preset:", selectedPresetIndex, presetNames);
            
            GUI.enabled = newPresetIndex > 0;
            if (GUILayout.Button("Apply Preset", GUILayout.Width(100)))
            {
                ApplySelectedPreset(newPresetIndex - 1);
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();

            if (newPresetIndex != selectedPresetIndex && newPresetIndex > 0)
            {
                selectedPresetIndex = newPresetIndex;
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(presets[newPresetIndex - 1].description, MessageType.None);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawChecklistSection()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("✅ SETUP CHECKLIST", sectionHeaderStyle);
            EditorGUILayout.Space(5);

            float progress = GetOverallProgress();
            EditorGUILayout.LabelField($"Progress: {progress:F0}%");
            
            Rect progressRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(progressRect, progress / 100f, "");

            EditorGUILayout.Space(10);

            // Checklist items
            DrawChecklistItem("muzzleTransform", "🎯 Muzzle Transform Position", 
                "Position the muzzle transform at the barrel tip where bullets/effects should spawn.");
            
            DrawChecklistItem("shootSound", "🔊 Shoot Sound Effect", 
                "Add a gunshot sound effect for firing feedback.");
            
            DrawChecklistItem("reloadSound", "🔄 Reload Sound Effect", 
                "Add a reload sound effect for reload feedback.");
            
            DrawChecklistItem("weaponClickSound", "🖱️ Empty Chamber Click Sound", 
                "Add a click sound for when trying to fire with no ammo.");
            
            DrawChecklistItem("muzzleEffects", "💥 Muzzle Effects", 
                "Add muzzle flash particles or muzzle prefab for visual effects.");
            
            DrawChecklistItem("modelPosition", "📐 Model Position & Pivot", 
                "Adjust weapon model position and pivot point for proper placement.");
            
            DrawChecklistItem("weaponSettings", "⚙️ Weapon Configuration", 
                "Configure damage, fire rate, range, and other weapon parameters.");

            EditorGUILayout.EndVertical();
        }

        private void DrawChecklistItem(string key, string title, string description)
        {
            EditorGUILayout.BeginHorizontal();
            
            bool isCompleted = checklistStates.ContainsKey(key) && checklistStates[key];
            string statusIcon = isCompleted ? "✅" : "⭕";
            
            GUIStyle itemStyle = isCompleted ? completedItemStyle : checklistItemStyle;
            
            EditorGUILayout.LabelField(statusIcon, GUILayout.Width(25));
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(title, itemStyle);
            EditorGUILayout.LabelField(description, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }

        private void DrawQuickActionsSection()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("🛠️ QUICK ACTIONS", sectionHeaderStyle);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select Weapon"))
            {
                Selection.activeGameObject = targetWeapon.gameObject;
            }
            if (GUILayout.Button("Select Muzzle Transform"))
            {
                Transform muzzle = GetMuzzleTransform();
                if (muzzle != null)
                    Selection.activeGameObject = muzzle.gameObject;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Audio Source"))
            {
                AudioSource audioSource = targetWeapon.GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    Selection.activeObject = audioSource;
                    EditorGUIUtility.PingObject(audioSource);
                }
            }
           
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void ApplySelectedPreset(int presetIndex)
        {
            if (presetIndex >= 0 && presetIndex < presets.Length)
            {
                WeaponPresets.ApplyPreset(targetWeapon, presets[presetIndex]);
                EditorUtility.SetDirty(targetWeapon);
                Debug.Log($"Applied preset '{presets[presetIndex].name}' to {targetWeapon.name}");
            }
        }

        private void UpdateChecklist()
        {
            if (targetWeapon == null) return;

            // Check muzzle transform
            Transform muzzleTransform = GetMuzzleTransform();
            checklistStates["muzzleTransform"] = muzzleTransform != null;

            // Check audio clips using reflection
            checklistStates["shootSound"] = GetAudioClip("shootSound") != null;
            checklistStates["reloadSound"] = GetAudioClip("reloadSound") != null;
            checklistStates["weaponClickSound"] = GetAudioClip("weaponClickSound") != null;

            // Check muzzle effects
            ParticleSystem muzzleFlash = GetMuzzleFlash();
            GameObject muzzlePrefab = GetMuzzlePrefab();
            checklistStates["muzzleEffects"] = muzzleFlash != null || muzzlePrefab != null;

            // Check model position (assume completed if weapon has children)
            checklistStates["modelPosition"] = targetWeapon.transform.childCount > 0;

            // Check weapon settings (assume completed if not default values)
            var damageField = typeof(Weapon).GetField("damage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            float damage = damageField != null ? (float)damageField.GetValue(targetWeapon) : 25f;
            checklistStates["weaponSettings"] = damage != 25f; // Not default value
        }

        private float GetOverallProgress()
        {
            int completed = 0;
            int total = checklistStates.Count;
            
            foreach (var state in checklistStates.Values)
            {
                if (state) completed++;
            }
            
            return total > 0 ? (float)completed / total * 100f : 0f;
        }

        private Transform GetMuzzleTransform()
        {
            var field = typeof(Weapon).GetField("muzzleTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(targetWeapon) as Transform;
        }

        private AudioClip GetAudioClip(string fieldName)
        {
            var field = typeof(Weapon).GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(targetWeapon) as AudioClip;
        }

        private ParticleSystem GetMuzzleFlash()
        {
            var field = typeof(Weapon).GetField("muzzleFlash", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(targetWeapon) as ParticleSystem;
        }

        private GameObject GetMuzzlePrefab()
        {
            var field = typeof(Weapon).GetField("muzzlePrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(targetWeapon) as GameObject;
        }

        
    }
}