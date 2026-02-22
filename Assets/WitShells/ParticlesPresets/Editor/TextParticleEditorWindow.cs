using UnityEditor;
using UnityEngine;

namespace WitShells.ParticlesPresets
{
    public class TextParticleEditorWindow : EditorWindow
    {
        private string _text = "Hello";
        private float _lifetime = 2f;
        private Vector3 _direction = Vector3.up;
        private float _spawnInterval = 0.5f;
        private GameObject _textPrefab;

        [MenuItem("WitShells/Particles Presets/Text Particle Editor")] 
        public static void ShowWindow()
        {
            var window = GetWindow<TextParticleEditorWindow>(false, "Text Particle Preset", true);
            window.minSize = new Vector2(320, 180);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Text Particle Settings", EditorStyles.boldLabel);

            _text = EditorGUILayout.TextField("Text", _text);
            _lifetime = EditorGUILayout.FloatField("Lifetime (s)", _lifetime);
            _direction = EditorGUILayout.Vector3Field("Direction", _direction);
            _spawnInterval = EditorGUILayout.FloatField("Spawn Interval (s)", _spawnInterval);
            _textPrefab = (GameObject)EditorGUILayout.ObjectField("Text Prefab", _textPrefab, typeof(GameObject), false);

            _lifetime = Mathf.Max(0.1f, _lifetime);
            _spawnInterval = Mathf.Max(0.05f, _spawnInterval);

            GUILayout.Space(10);

            if (GUILayout.Button("Generate Text Particle Prefab"))
            {
                CreatePrefab();
            }

            if (GUILayout.Button("Apply To Selected Particle Systems"))
            {
                ApplyToSelection();
            }
        }

        private void CreatePrefab()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Save Text Particle Prefab",
                "TextParticle_" + _text,
                "prefab",
                "Choose location for the generated text particle prefab.");

            if (string.IsNullOrEmpty(path))
                return;

            TextParticlePresetUtility.CreateTextParticlePrefab(path, _text, _lifetime, _direction, _spawnInterval, _textPrefab);
        }

        private void ApplyToSelection()
        {
            var selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                Debug.LogWarning("[TextParticlePreset] No GameObject selected.");
                return;
            }

            int appliedCount = 0;

            foreach (var go in selected)
            {
                if (go == null) continue;

                var ps = go.GetComponent<ParticleSystem>();
                if (ps == null) continue;

                Undo.RecordObject(ps, "Apply Text Particle Preset");
                TextParticlePresetUtility.ApplyToParticleSystem(ps, _text, _lifetime, _direction, _spawnInterval, _textPrefab);
                appliedCount++;
            }

            if (appliedCount > 0)
            {
                Debug.Log($"[TextParticlePreset] Applied text particle settings to {appliedCount} ParticleSystem(s).");
            }
            else
            {
                Debug.LogWarning("[TextParticlePreset] No ParticleSystem found on selected GameObjects.");
            }
        }
    }
}
