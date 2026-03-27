using UnityEngine;

namespace WitShells.ParticlesPresets
{
    public static class BioluminescentSporesPreset
    {
        public static void Configure(ParticleSystem ps)
        {
            var main = ps.main;
            main.loop                = true;
            main.duration            = 8f;
            main.startLifetime       = new ParticleSystem.MinMaxCurve(5f, 11f);
            main.startSpeed          = new ParticleSystem.MinMaxCurve(0.02f, 0.18f);
            main.startSize           = new ParticleSystem.MinMaxCurve(0.02f, 0.09f);
            main.startRotation       = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.startColor          = new ParticleSystem.MinMaxGradient(
                new Color(0.0f, 0.9f, 0.6f, 1f),   // teal-green
                new Color(0.1f, 0.4f, 1.0f, 1f)    // deep blue
            );
            main.simulationSpace     = ParticleSystemSimulationSpace.World;
            main.maxParticles        = 350;
            main.gravityModifier     = -0.008f;   // spores gently defy gravity
            main.playOnAwake         = true;

            var emission = ps.emission;
            emission.enabled         = true;
            emission.rateOverTime    = new ParticleSystem.MinMaxCurve(28f);
            emission.rateOverDistance = 0f;

            var shape = ps.shape;
            shape.enabled            = true;
            shape.shapeType          = ParticleSystemShapeType.Box;
            shape.scale              = new Vector3(8f, 5f, 8f);
            shape.randomDirectionAmount = 1f;   // fully random scatter

            var velocity = ps.velocityOverLifetime;
            velocity.enabled         = true;
            velocity.space           = ParticleSystemSimulationSpace.World;
            velocity.x               = new ParticleSystem.MinMaxCurve(-0.07f, 0.07f);
            velocity.z               = new ParticleSystem.MinMaxCurve(-0.07f, 0.07f);
            velocity.y               = new ParticleSystem.MinMaxCurve(-0.03f, 0.1f);

            // Pulsing colour — teal → vivid cyan blue → violet → back to teal
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.0f,  1.0f,  0.65f), 0f),    // bright teal
                    new GradientColorKey(new Color(0.0f,  0.75f, 1.0f),  0.25f), // vivid cyan
                    new GradientColorKey(new Color(0.35f, 0.2f,  1.0f),  0.55f), // electric violet
                    new GradientColorKey(new Color(0.0f,  0.85f, 0.55f), 0.8f),  // deep forest teal
                    new GradientColorKey(new Color(0.0f,  1.0f,  0.65f), 1f)     // back to teal
                },
                new[]
                {
                    new GradientAlphaKey(0f,    0f),
                    new GradientAlphaKey(0.9f,  0.08f),
                    new GradientAlphaKey(0.85f, 0.4f),
                    new GradientAlphaKey(0.7f,  0.75f),
                    new GradientAlphaKey(0f,    1f)
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            // Pulse: grow → shrink → grow → shrink over lifetime
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var sizeCurve = new AnimationCurve(
                new Keyframe(0f,    0f),
                new Keyframe(0.1f,  1f),
                new Keyframe(0.3f,  0.55f),
                new Keyframe(0.55f, 0.95f),
                new Keyframe(0.75f, 0.45f),
                new Keyframe(0.9f,  0.7f),
                new Keyframe(1f,    0f)
            );
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var rotationOverLifetime = ps.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);

            // Rich multi-octave noise for organic floating drift
            var noise = ps.noise;
            noise.enabled          = true;
            noise.strength         = 0.5f;
            noise.frequency        = 0.18f;
            noise.scrollSpeed      = 0.06f;
            noise.octaveCount      = 3;
            noise.octaveMultiplier = 0.5f;
            noise.octaveScale      = 2f;
            noise.quality          = ParticleSystemNoiseQuality.High;
            noise.damping          = true;

            // Faint glowing trails — short lived, fade quickly
            var trails = ps.trails;
            trails.enabled              = true;
            trails.ratio                = 0.35f;
            trails.lifetime             = new ParticleSystem.MinMaxCurve(0.4f);
            trails.minVertexDistance    = 0.05f;
            trails.worldSpace           = true;
            trails.dieWithParticles     = true;
            trails.sizeAffectsWidth     = true;
            trails.widthOverTrail       = new ParticleSystem.MinMaxCurve(0.3f);
            trails.inheritParticleColor = true;
            trails.colorOverLifetime    = new ParticleSystem.MinMaxGradient(
                new Color(0f, 1f, 0.6f, 0.5f),
                new Color(0f, 0.5f, 1f,  0f)
            );

            var collision = ps.collision;
            collision.enabled = false;

            var lights = ps.lights;
            lights.enabled = false;

            var subEmitters = ps.subEmitters;
            subEmitters.enabled = false;
        }

        public static void ConfigureRenderer(ParticleSystemRenderer renderer)
        {
            renderer.renderMode    = ParticleSystemRenderMode.Billboard;
            renderer.alignment     = ParticleSystemRenderSpace.View;
            renderer.sortingFudge  = 0.5f;
            renderer.minParticleSize = 0.003f;
            renderer.maxParticleSize = 0.12f;
            renderer.trailMaterial   = renderer.sharedMaterial;
        }
    }
}
