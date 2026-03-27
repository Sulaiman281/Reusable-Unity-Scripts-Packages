using UnityEngine;

namespace WitShells.ParticlesPresets
{
    public static class MagicLeafFallPreset
    {
        public static void Configure(ParticleSystem ps)
        {
            var main = ps.main;
            main.loop = true;
            main.duration = 5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 180;
            main.gravityModifier = 0.06f;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(18f);
            emission.rateOverDistance = 0f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(7f, 0.1f, 7f);

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.6f, -0.2f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.2f, 0.9f, 0.5f),  0f),    // bright magic green
                    new GradientColorKey(new Color(0.6f, 0.3f, 0.9f),  0.25f), // violet magic
                    new GradientColorKey(new Color(0.95f, 0.8f, 0.1f), 0.55f), // golden yellow
                    new GradientColorKey(new Color(0.9f, 0.35f, 0.1f), 0.8f),  // autumn orange
                    new GradientColorKey(new Color(0.15f, 0.6f, 0.9f), 1f)     // cool blue sparkle
                },
                new[]
                {
                    new GradientAlphaKey(0f,   0f),
                    new GradientAlphaKey(0.85f, 0.08f),
                    new GradientAlphaKey(0.8f, 0.6f),
                    new GradientAlphaKey(0f,   1f)
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var sizeCurve = new AnimationCurve(
                new Keyframe(0f,   0.1f),
                new Keyframe(0.15f, 1f),
                new Keyframe(0.75f, 0.85f),
                new Keyframe(1f,   0f)
            );
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var rotationOverLifetime = ps.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.45f;
            noise.frequency = 0.2f;
            noise.scrollSpeed = 0.08f;
            noise.octaveCount = 2;
            noise.damping = true;

            var lights = ps.lights;
            lights.enabled = false;

            var trails = ps.trails;
            trails.enabled = true;
            trails.ratio = 0.25f;
            trails.lifetime = new ParticleSystem.MinMaxCurve(0.3f);
            trails.minVertexDistance = 0.1f;
            trails.worldSpace = true;
            trails.dieWithParticles = true;
            trails.sizeAffectsWidth = true;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(0.4f);
            trails.inheritParticleColor = true;
            trails.colorOverLifetime = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 0.4f), new Color(1f, 1f, 1f, 0f));

            var collision = ps.collision;
            collision.enabled = false;

            var subEmitters = ps.subEmitters;
            subEmitters.enabled = false;
        }

        public static void ConfigureRenderer(ParticleSystemRenderer renderer)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortingFudge = 0.4f;
            renderer.minParticleSize = 0.02f;
            renderer.maxParticleSize = 0.3f;
            renderer.trailMaterial = renderer.sharedMaterial;
        }
    }
}
