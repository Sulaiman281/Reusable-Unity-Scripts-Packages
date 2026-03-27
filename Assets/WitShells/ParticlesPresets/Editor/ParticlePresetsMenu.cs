using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace WitShells.ParticlesPresets
{
    public static class ParticlePresetsMenu
    {
        private const string MenuRoot = "WitShells/Particles Presets/";

        [MenuItem(MenuRoot + "Apply Dark Smoke Up", validate = true)]
        private static bool ValidateApplyDarkSmokeUp()
        {
            return HasSelectedParticleSystem();
        }

        [MenuItem(MenuRoot + "Apply Dark Smoke Up")]
        private static void ApplyDarkSmokeUp()
        {
            ApplyPresetToSelection(DarkSmokePreset.Configure, DarkSmokePreset.ConfigureRenderer, "Dark Smoke Up");
        }

        [MenuItem(MenuRoot + "Apply Fire", validate = true)]
        private static bool ValidateApplyFire()
        {
            return HasSelectedParticleSystem();
        }

        [MenuItem(MenuRoot + "Apply Fire")]
        private static void ApplyFire()
        {
            ApplyPresetToSelection(FirePreset.Configure, FirePreset.ConfigureRenderer, "Fire");
        }

        [MenuItem(MenuRoot + "Apply Rain", validate = true)]
        private static bool ValidateApplyRain()
        {
            return HasSelectedParticleSystem();
        }

        [MenuItem(MenuRoot + "Apply Rain")]
        private static void ApplyRain()
        {
            ApplyPresetToSelection(RainPreset.Configure, RainPreset.ConfigureRenderer, "Rain");
        }

        [MenuItem(MenuRoot + "Apply Ashes", validate = true)]
        private static bool ValidateApplyAshes()
        {
            return HasSelectedParticleSystem();
        }

        [MenuItem(MenuRoot + "Apply Ashes")]
        private static void ApplyAshes()
        {
            ApplyPresetToSelection(AshesPreset.Configure, AshesPreset.ConfigureRenderer, "Ashes");
        }

        [MenuItem(MenuRoot + "Apply Pollen & Floating Micro-Flora", validate = true)]
        private static bool ValidateApplyPollenMicroFlora()
        {
            return HasSelectedParticleSystem();
        }

        [MenuItem(MenuRoot + "Apply Pollen & Floating Micro-Flora")]
        private static void ApplyPollenMicroFlora()
        {
            ApplyPresetToSelection(PollenMicroFloraPreset.Configure, PollenMicroFloraPreset.ConfigureRenderer, "Pollen & Floating Micro-Flora");
        }

        [MenuItem(MenuRoot + "Apply Magic Leaf Fall", validate = true)]
        private static bool ValidateApplyMagicLeafFall()
        {
            return HasSelectedParticleSystem();
        }

        [MenuItem(MenuRoot + "Apply Magic Leaf Fall")]
        private static void ApplyMagicLeafFall()
        {
            ApplyPresetToSelection(MagicLeafFallPreset.Configure, MagicLeafFallPreset.ConfigureRenderer, "Magic Leaf Fall");
        }

        [MenuItem(MenuRoot + "Apply Amber Dust Motes", validate = true)]
        private static bool ValidateApplyAmberDustMotes()
        {
            return HasSelectedParticleSystem();
        }

        [MenuItem(MenuRoot + "Apply Amber Dust Motes")]
        private static void ApplyAmberDustMotes()
        {
            ApplyPresetToSelection(AmberDustMotesPreset.Configure, AmberDustMotesPreset.ConfigureRenderer, "Amber Dust Motes");
        }

        [MenuItem(MenuRoot + "Apply Bioluminescent Spores (Deep Forest)", validate = true)]
        private static bool ValidateApplyBioluminescentSpores()
        {
            return HasSelectedParticleSystem();
        }

        [MenuItem(MenuRoot + "Apply Bioluminescent Spores (Deep Forest)")]
        private static void ApplyBioluminescentSpores()
        {
            ApplyPresetToSelection(BioluminescentSporesPreset.Configure, BioluminescentSporesPreset.ConfigureRenderer, "Bioluminescent Spores (Deep Forest)");
        }

        private static bool HasSelectedParticleSystem()
        {
            var selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
                return false;

            foreach (var go in selected)
            {
                if (go != null && go.GetComponent<ParticleSystem>() != null)
                    return true;
            }

            return false;
        }
        private static void ApplyPresetToSelection(System.Action<ParticleSystem> configurePs, System.Action<ParticleSystemRenderer> configureRenderer, string presetName)
        {
            var selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                Debug.LogWarning($"[ParticlesPresets] No GameObject selected for {presetName} preset.");
                return;
            }

            int appliedCount = 0;

            foreach (var go in selected)
            {
                if (go == null) continue;

                var ps = go.GetComponent<ParticleSystem>();
                if (ps == null) continue;

                Undo.RecordObject(ps, $"Apply {presetName} Preset");
                configurePs?.Invoke(ps);

                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (renderer != null && configureRenderer != null)
                {
                    Undo.RecordObject(renderer, $"Apply {presetName} Preset Renderer");
                    configureRenderer(renderer);
                }

                appliedCount++;
            }

            if (appliedCount > 0)
            {
                Debug.Log($"[ParticlesPresets] Applied {presetName} preset to {appliedCount} ParticleSystem(s).");
            }
            else
            {
                Debug.LogWarning($"[ParticlesPresets] No ParticleSystem found on selected GameObjects for {presetName} preset.");
            }
        }
    }
}
