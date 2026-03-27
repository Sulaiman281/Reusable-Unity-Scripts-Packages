using System.IO;
using UnityEditor;
using UnityEngine;

namespace WitShells.ParticlesPresets
{
    public class TextureSheetAnimatorWindow : EditorWindow
    {
        // ── Target ───────────────────────────────────────────────────────────
        private ParticleSystem _targetPS;

        // ── Texture Sheet ────────────────────────────────────────────────────
        private Texture2D _texture;
        private int _columns = 4;
        private int _rows    = 4;

        // ── Animation ────────────────────────────────────────────────────────
        private ParticleSystemAnimationMode _animMode  = ParticleSystemAnimationMode.Grid;
        private ParticleSystemAnimationType _animType  = ParticleSystemAnimationType.WholeSheet;
        private int   _rowIndex     = 0;
        private int   _startFrame   = 0;
        private int   _frameCount   = 0;   // 0 = all frames
        private int   _cycleCount   = 1;
        private bool  _randomStart  = false;
        private bool  _flipU        = false;
        private bool  _flipV        = false;
        private ParticleSystemAnimationTimeMode _timeMode = ParticleSystemAnimationTimeMode.Lifetime;
        private float _fps          = 24f;

        // ── Renderer ─────────────────────────────────────────────────────────
        private ParticleSystemRenderMode  _renderMode  = ParticleSystemRenderMode.Billboard;
        private ParticleSystemRenderSpace _renderAlign = ParticleSystemRenderSpace.View;

        // ── Render Pipeline Detection ─────────────────────────────────────────
        private enum RenderPipeline { BuiltIn, URP, HDRP }
        private RenderPipeline _detectedPipeline = RenderPipeline.BuiltIn;

        // ── Material ─────────────────────────────────────────────────────────
        private enum ShaderPreset
        {
            // ── URP ──────────────────────────────────────
            URP_Particles_Unlit,
            URP_Particles_Lit,
            URP_Particles_SimpleLit,
            // ── Mobile / Built-In ────────────────────────
            Mobile_Particles_AlphaBlended,
            Mobile_Particles_Additive,
            Mobile_Particles_Multiply,
            Mobile_Particles_VertexLitBlended,
            // ── Built-In Standard ────────────────────────
            BuiltIn_AlphaBlended,
            BuiltIn_Additive,
            BuiltIn_Multiply,
            BuiltIn_Unlit,
        }
        private ShaderPreset _shaderPreset = ShaderPreset.Mobile_Particles_AlphaBlended;
        private Color _colorTint      = Color.white;
        private bool  _softParticles  = false;
        private float _softNearFade   = 0f;
        private float _softFarFade    = 1f;

        // ── Save ─────────────────────────────────────────────────────────────
        private string _savePath     = "Assets/Materials";
        private string _materialName = "ParticleTextureSheet";

        // ── UI State ─────────────────────────────────────────────────────────
        private bool _foldSheet    = true;
        private bool _foldAnim     = true;
        private bool _foldRenderer = true;
        private bool _foldMaterial = true;
        private bool _foldSave     = true;
        private Vector2 _scroll;

        // ─────────────────────────────────────────────────────────────────────

        [MenuItem("WitShells/Particles Presets/Texture Sheet Animator...")]
        public static void Open()
        {
            var w = GetWindow<TextureSheetAnimatorWindow>("Texture Sheet Animator");
            w.minSize = new Vector2(360f, 540f);
            w.Show();
        }

        private void OnEnable()       { DetectPipeline(); TryLoadFromSelection(); }
        private void OnSelectionChange() { TryLoadFromSelection(); Repaint(); }

        private void TryLoadFromSelection()
        {
            var go = Selection.activeGameObject;
            if (go != null)
            {
                var ps = go.GetComponent<ParticleSystem>();
                if (ps != null) _targetPS = ps;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  GUI
        // ─────────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Particle Texture Sheet Animator",
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, alignment = TextAnchor.MiddleCenter },
                GUILayout.Height(22));
            EditorGUILayout.LabelField("Create a material and configure texture-sheet animation on any Particle System.",
                new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, wordWrap = true });

            Separator();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            // ── Target ───────────────────────────────────────────────────────
            SectionLabel("Target Particle System");
            using (new EditorGUILayout.HorizontalScope())
            {
                _targetPS = (ParticleSystem)EditorGUILayout.ObjectField(
                    new GUIContent("Particle System", "The particle system to configure."),
                    _targetPS, typeof(ParticleSystem), true);
                if (GUILayout.Button(new GUIContent("↺", "Load from current scene selection"), GUILayout.Width(26)))
                    TryLoadFromSelection();
            }

            Separator();

            // ── Texture Sheet ────────────────────────────────────────────────
            _foldSheet = EditorGUILayout.Foldout(_foldSheet, "  Texture Sheet", true, EditorStyles.foldoutHeader);
            if (_foldSheet)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    _texture = (Texture2D)EditorGUILayout.ObjectField(
                        new GUIContent("Sprite Sheet", "A single texture containing all animation frames in a grid."),
                        _texture, typeof(Texture2D), false);

                    _columns = EditorGUILayout.IntSlider(
                        new GUIContent("Columns (X Tiles)", "How many frames are laid out horizontally."),
                        _columns, 1, 64);

                    _rows = EditorGUILayout.IntSlider(
                        new GUIContent("Rows    (Y Tiles)", "How many frames are laid out vertically."),
                        _rows, 1, 64);

                    using (new EditorGUI.DisabledGroupScope(true))
                        EditorGUILayout.IntField(new GUIContent("Total Frames", "Columns × Rows"), _columns * _rows);
                }
            }

            Separator();

            // ── Animation ────────────────────────────────────────────────────
            _foldAnim = EditorGUILayout.Foldout(_foldAnim, "  Animation", true, EditorStyles.foldoutHeader);
            if (_foldAnim)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    _animMode = (ParticleSystemAnimationMode)EditorGUILayout.EnumPopup(
                        new GUIContent("Mode", "Grid: uniform tile grid.\nSprites: uses Sprite assets assigned in the module."),
                        _animMode);

                    _animType = (ParticleSystemAnimationType)EditorGUILayout.EnumPopup(
                        new GUIContent("Animation Type", "WholeSheet: cycles through every row.\nSingleRow: plays one row only."),
                        _animType);

                    if (_animType == ParticleSystemAnimationType.SingleRow)
                        _rowIndex = EditorGUILayout.IntSlider(
                            new GUIContent("Row Index", "Which row (0-based) to animate."),
                            _rowIndex, 0, Mathf.Max(0, _rows - 1));

                    _timeMode = (ParticleSystemAnimationTimeMode)EditorGUILayout.EnumPopup(
                        new GUIContent("Time Mode", "Lifetime: ties playback to particle age.\nSpeed: ties playback to particle speed.\nFPS: fixed frames-per-second rate."),
                        _timeMode);

                    if (_timeMode == ParticleSystemAnimationTimeMode.FPS)
                        _fps = EditorGUILayout.FloatField(
                            new GUIContent("FPS", "Frames per second to play."),
                            Mathf.Max(1f, _fps));

                    _cycleCount = EditorGUILayout.IntSlider(
                        new GUIContent("Cycles", "How many full loops to play over a particle's lifetime."),
                        _cycleCount, 1, 16);

                    int maxFrame = Mathf.Max(0, _columns * _rows - 1);
                    _startFrame = EditorGUILayout.IntSlider(
                        new GUIContent("Start Frame", "First frame of the animated range."),
                        _startFrame, 0, maxFrame);

                    _frameCount = EditorGUILayout.IntSlider(
                        new GUIContent("Frame Count", "Number of frames to cycle through. 0 = use all frames from Start Frame to end."),
                        _frameCount, 0, maxFrame + 1);

                    if (_frameCount > 0)
                    {
                        using (new EditorGUI.DisabledGroupScope(true))
                            EditorGUILayout.IntField(
                                new GUIContent("Last Frame", "Resolved last frame index."),
                                Mathf.Min(_startFrame + _frameCount, _columns * _rows) - 1);
                    }

                    _randomStart = EditorGUILayout.Toggle(
                        new GUIContent("Random Start Frame", "Each particle begins at a random frame within the range."),
                        _randomStart);

                    _flipU = EditorGUILayout.Toggle(
                        new GUIContent("Flip U (Horizontal)", "50% chance to mirror each frame horizontally."),
                        _flipU);

                    _flipV = EditorGUILayout.Toggle(
                        new GUIContent("Flip V (Vertical)", "50% chance to mirror each frame vertically."),
                        _flipV);
                }
            }

            Separator();

            // ── Renderer ─────────────────────────────────────────────────────
            _foldRenderer = EditorGUILayout.Foldout(_foldRenderer, "  Renderer", true, EditorStyles.foldoutHeader);
            if (_foldRenderer)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    _renderMode = (ParticleSystemRenderMode)EditorGUILayout.EnumPopup(
                        new GUIContent("Render Mode", "Billboard: always faces camera.\nStretch: stretch along velocity.\nMesh: render as mesh, etc."),
                        _renderMode);

                    _renderAlign = (ParticleSystemRenderSpace)EditorGUILayout.EnumPopup(
                        new GUIContent("Alignment", "How the billboard is oriented in 3D space."),
                        _renderAlign);
                }
            }

            Separator();

            // ── Material ─────────────────────────────────────────────────────
            _foldMaterial = EditorGUILayout.Foldout(_foldMaterial, "  Material", true, EditorStyles.foldoutHeader);
            if (_foldMaterial)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    // Pipeline banner
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        string pipelineLabel = _detectedPipeline switch
                        {
                            RenderPipeline.URP  => "Universal Render Pipeline (URP)",
                            RenderPipeline.HDRP => "High Definition Render Pipeline (HDRP)",
                            _                   => "Built-In Render Pipeline",
                        };
                        MessageType msgType = _detectedPipeline == RenderPipeline.BuiltIn
                            ? MessageType.None : MessageType.Info;
                        EditorGUILayout.HelpBox($"Detected: {pipelineLabel}", msgType);
                        if (GUILayout.Button(new GUIContent("↺", "Re-detect render pipeline and reset shader to recommended default."), GUILayout.Width(26), GUILayout.Height(38)))
                            DetectPipeline();
                    }

                    _shaderPreset = (ShaderPreset)EditorGUILayout.EnumPopup(
                        new GUIContent("Shader",
                            "URP_Particles_Unlit      — URP, no lighting, best mobile performance.\n" +
                            "URP_Particles_Lit        — URP, physically-based lighting.\n" +
                            "URP_Particles_SimpleLit  — URP, lightweight lighting.\n" +
                            "Mobile_AlphaBlended      — Built-In mobile, transparent blend.\n" +
                            "Mobile_Additive          — Built-In mobile, additive glow.\n" +
                            "Mobile_Multiply          — Built-In mobile, darkening blend.\n" +
                            "Mobile_VertexLitBlended  — Built-In mobile, vertex lit + blend.\n" +
                            "BuiltIn_AlphaBlended     — Built-In standard transparent.\n" +
                            "BuiltIn_Additive         — Built-In standard additive.\n" +
                            "BuiltIn_Multiply         — Built-In standard multiply.\n" +
                            "BuiltIn_Unlit            — Built-In unlit surface."),
                        _shaderPreset);

                    _colorTint = EditorGUILayout.ColorField(
                        new GUIContent("Color Tint", "Multiplied over the texture. Use alpha to control overall opacity."),
                        _colorTint);

                    _softParticles = EditorGUILayout.Toggle(
                        new GUIContent("Soft Particles", "Fade particles near opaque geometry. Requires Camera depth texture."),
                        _softParticles);

                    if (_softParticles)
                    {
                        using (new EditorGUI.IndentLevelScope())
                        {
                            _softNearFade = EditorGUILayout.FloatField(
                                new GUIContent("Near Fade Distance", "Distance at which soft fade starts (world units)."),
                                Mathf.Max(0f, _softNearFade));
                            _softFarFade = EditorGUILayout.FloatField(
                                new GUIContent("Far Fade Distance", "Distance at which soft fade is fully opaque."),
                                Mathf.Max(_softNearFade + 0.01f, _softFarFade));
                        }
                    }
                }
            }

            Separator();

            // ── Save ─────────────────────────────────────────────────────────
            _foldSave = EditorGUILayout.Foldout(_foldSave, "  Save Path", true, EditorStyles.foldoutHeader);
            if (_foldSave)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _savePath = EditorGUILayout.TextField(
                            new GUIContent("Folder", "Project-relative folder where the material asset will be saved (e.g. Assets/Materials)."),
                            _savePath);

                        if (GUILayout.Button("Browse", GUILayout.Width(60)))
                        {
                            string abs = EditorUtility.OpenFolderPanel("Select Save Folder", Application.dataPath, "");
                            if (!string.IsNullOrEmpty(abs))
                                _savePath = "Assets" + abs.Replace(Application.dataPath, "").Replace("\\", "/");
                        }
                    }

                    _materialName = EditorGUILayout.TextField(
                        new GUIContent("Material Name", "File name (without extension) for the generated .mat asset."),
                        _materialName);

                    string preview = $"{_savePath}/{_materialName}.mat";
                    EditorGUILayout.LabelField(new GUIContent("Asset Path", "Full path that will be written."),
                        new GUIContent(preview, preview), EditorStyles.miniLabel);
                }
            }

            Separator();

            // ── Validation & Apply ───────────────────────────────────────────
            bool missingPS  = _targetPS == null;
            bool missingTex = _texture  == null;

            if (missingPS || missingTex)
            {
                string msg = missingPS && missingTex
                    ? "Assign a Particle System and a Sprite Sheet Texture to continue."
                    : missingPS
                        ? "Assign a Particle System to continue."
                        : "Assign a Sprite Sheet Texture to continue.";
                EditorGUILayout.HelpBox(msg, MessageType.Info);
            }

            using (new EditorGUI.DisabledGroupScope(missingPS || missingTex))
            {
                if (GUILayout.Button("Create Material & Apply to Particle System", GUILayout.Height(34)))
                    Apply();
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.EndScrollView();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Apply Logic
        // ─────────────────────────────────────────────────────────────────────

        private void Apply()
        {
            Material mat = CreateOrUpdateMaterial();
            if (mat == null)
            {
                Debug.LogError("[TextureSheetAnimator] Could not create material — shader not found.");
                return;
            }

            // ── Renderer ─────────────────────────────────────────────────────
            var psr = _targetPS.GetComponent<ParticleSystemRenderer>();
            Undo.RecordObject(psr, "Apply Texture Sheet Animator");
            psr.renderMode     = _renderMode;
            psr.alignment      = _renderAlign;
            psr.sharedMaterial = mat;
            psr.trailMaterial  = null;

            // ── Texture Sheet Animation module ────────────────────────────────
            Undo.RecordObject(_targetPS, "Apply Texture Sheet Animator");
            var tsa = _targetPS.textureSheetAnimation;
            tsa.enabled   = true;
            tsa.mode      = _animMode;
            tsa.numTilesX = _columns;
            tsa.numTilesY = _rows;
            tsa.animation = _animType;

            if (_animType == ParticleSystemAnimationType.SingleRow)
            {
                tsa.rowMode  = ParticleSystemAnimationRowMode.Custom;
                tsa.rowIndex = _rowIndex;
            }
            else
            {
                tsa.rowMode = ParticleSystemAnimationRowMode.Random;
            }

            tsa.timeMode   = _timeMode;
            if (_timeMode == ParticleSystemAnimationTimeMode.FPS)
                tsa.fps = _fps;

            tsa.cycleCount = _cycleCount;

            int   total      = _columns * _rows;
            int   start      = _startFrame;
            int   end        = (_frameCount > 0) ? Mathf.Min(start + _frameCount, total) : total;
            float startNorm  = (float)start / total;
            float endNorm    = (float)end   / total;

            tsa.frameOverTime = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, startNorm),
                    new Keyframe(1f, endNorm)
                )
            );

            tsa.startFrame = _randomStart
                ? new ParticleSystem.MinMaxCurve(startNorm, endNorm)
                : new ParticleSystem.MinMaxCurve(startNorm);

            tsa.flipU = _flipU ? 0.5f : 0f;
            tsa.flipV = _flipV ? 0.5f : 0f;

            EditorUtility.SetDirty(_targetPS);
            Debug.Log($"[TextureSheetAnimator] Applied to '{_targetPS.name}'. Material: '{_savePath}/{_materialName}.mat'.");
        }

        private Material CreateOrUpdateMaterial()
        {
            Shader shader = ResolveShader();
            if (shader == null) return null;

            EnsureFolderExists(_savePath);

            string path = $"{_savePath}/{_materialName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

            mat.mainTexture = _texture;
            mat.color       = _colorTint;

            ApplySoftParticles(mat);

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return mat;
        }

        private void ApplySoftParticles(Material mat)
        {
            if (_softParticles)
            {
                mat.EnableKeyword("SOFTPARTICLES_ON");
                if (mat.HasProperty("_SoftParticlesNearFadeDistance"))
                    mat.SetFloat("_SoftParticlesNearFadeDistance", _softNearFade);
                if (mat.HasProperty("_SoftParticlesFarFadeDistance"))
                    mat.SetFloat("_SoftParticlesFarFadeDistance", _softFarFade);
                if (mat.HasProperty("_SoftParticleFadeParams"))
                    mat.SetVector("_SoftParticleFadeParams", new Vector4(_softNearFade, _softFarFade, 0f, 0f));
            }
            else
            {
                mat.DisableKeyword("SOFTPARTICLES_ON");
            }
        }

        private Shader ResolveShader()
        {
            // Each entry is an ordered fallback chain — first found wins.
            string[][] chains = _shaderPreset switch
            {
                // ── URP ──────────────────────────────────────────────────────
                ShaderPreset.URP_Particles_Unlit => new[]
                {
                    new[] { "Universal Render Pipeline/Particles/Unlit",
                            "Universal Render Pipeline/Unlit",
                            "Mobile/Particles/Alpha Blended",
                            "Particles/Alpha Blended" }
                },
                ShaderPreset.URP_Particles_Lit => new[]
                {
                    new[] { "Universal Render Pipeline/Particles/Lit",
                            "Universal Render Pipeline/Particles/Simple Lit",
                            "Universal Render Pipeline/Particles/Unlit",
                            "Particles/Alpha Blended" }
                },
                ShaderPreset.URP_Particles_SimpleLit => new[]
                {
                    new[] { "Universal Render Pipeline/Particles/Simple Lit",
                            "Universal Render Pipeline/Particles/Lit",
                            "Universal Render Pipeline/Particles/Unlit",
                            "Particles/Alpha Blended" }
                },
                // ── Mobile Built-In ──────────────────────────────────────────
                ShaderPreset.Mobile_Particles_AlphaBlended => new[]
                {
                    new[] { "Mobile/Particles/Alpha Blended",
                            "Particles/Alpha Blended Premultiply",
                            "Particles/Alpha Blended",
                            "Universal Render Pipeline/Particles/Unlit" }
                },
                ShaderPreset.Mobile_Particles_Additive => new[]
                {
                    new[] { "Mobile/Particles/Additive",
                            "Particles/Additive",
                            "Universal Render Pipeline/Particles/Unlit" }
                },
                ShaderPreset.Mobile_Particles_Multiply => new[]
                {
                    new[] { "Mobile/Particles/Multiply",
                            "Particles/Multiply",
                            "Particles/Alpha Blended" }
                },
                ShaderPreset.Mobile_Particles_VertexLitBlended => new[]
                {
                    new[] { "Mobile/Particles/VertexLit Blended",
                            "Mobile/Particles/Alpha Blended",
                            "Particles/Alpha Blended" }
                },
                // ── Built-In Standard ────────────────────────────────────────
                ShaderPreset.BuiltIn_AlphaBlended => new[]
                {
                    new[] { "Particles/Alpha Blended",
                            "Particles/Alpha Blended Premultiply",
                            "Mobile/Particles/Alpha Blended" }
                },
                ShaderPreset.BuiltIn_Additive => new[]
                {
                    new[] { "Particles/Additive",
                            "Mobile/Particles/Additive" }
                },
                ShaderPreset.BuiltIn_Multiply => new[]
                {
                    new[] { "Particles/Multiply",
                            "Mobile/Particles/Multiply",
                            "Particles/Alpha Blended" }
                },
                ShaderPreset.BuiltIn_Unlit => new[]
                {
                    new[] { "Particles/Standard Unlit",
                            "Particles/Alpha Blended" }
                },
                _ => new[] { new[] { "Particles/Alpha Blended" } }
            };

            foreach (var chain in chains)
            foreach (var name   in chain)
            {
                Shader s = Shader.Find(name);
                if (s != null)
                {
                    if (name != chain[0])
                        Debug.LogWarning($"[TextureSheetAnimator] Primary shader not found — using fallback '{name}'.");
                    return s;
                }
            }

            Debug.LogWarning("[TextureSheetAnimator] No matching shader found — falling back to 'Standard'.");
            return Shader.Find("Standard");
        }

        private void DetectPipeline()
        {
            var rpa = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            if (rpa == null)
                _detectedPipeline = RenderPipeline.BuiltIn;
            else if (rpa.GetType().FullName.Contains("Universal"))
                _detectedPipeline = RenderPipeline.URP;
            else if (rpa.GetType().FullName.Contains("HighDefinition") || rpa.GetType().FullName.Contains("HDRenderPipeline"))
                _detectedPipeline = RenderPipeline.HDRP;
            else
                _detectedPipeline = RenderPipeline.BuiltIn;

            // Auto-select pipeline-appropriate default shader
            _shaderPreset = _detectedPipeline switch
            {
                RenderPipeline.URP  => ShaderPreset.URP_Particles_Unlit,
                RenderPipeline.HDRP => ShaderPreset.BuiltIn_AlphaBlended, // HDRP uses custom VFX Graph; BuiltIn as safe default
                _                   => ShaderPreset.Mobile_Particles_AlphaBlended,
            };
        }

        private static void EnsureFolderExists(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string[] parts   = path.Split('/');
            string   current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────────────

        private static void Separator()
        {
            EditorGUILayout.Space(3);
            Rect r = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(r, new Color(0.35f, 0.35f, 0.35f, 1f));
            EditorGUILayout.Space(3);
        }

        private static void SectionLabel(string label)
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        }
    }
}
